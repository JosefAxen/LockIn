using LockIn.Data.Repositories;
using LockIn.Models;

namespace LockIn.Services;

public sealed record VolumeAdvice(MuscleGroup Muscle, int SetDelta, string Reason);
public sealed record DeloadAdvice(string Reason);

public sealed record SetProgressionAnalysis(
    IReadOnlyList<VolumeAdvice> Volume,
    DeloadAdvice? Deload);

public interface ISetProgressionEngine
{
    Task<SetProgressionAnalysis> AnalyzeAsync();
}

// Analyserar feedback från senaste 2 pass per muskelgrupp (SessionMuscleFeedback/SessionExerciseFeedback)
// och returnerar per-muskel volym-advice + eventuell deload-flagga.
// Källa: plan sektion E "Set-progression baserat på post-workout-feedback".
public class SetProgressionEngine : ISetProgressionEngine
{
    private readonly ISessionRepository _sessionRepo;

    public SetProgressionEngine(ISessionRepository sessionRepo) => _sessionRepo = sessionRepo;

    public async Task<SetProgressionAnalysis> AnalyzeAsync()
    {
        // Hämta de 6 senaste avslutade sessionerna — räcker för att hitta 2 per muskelgrupp
        var recent = await _sessionRepo.GetRecentCompletedAsync(6);
        if (recent.Count == 0)
            return new SetProgressionAnalysis(Array.Empty<VolumeAdvice>(), null);

        // Ladda muskel-feedback per session
        var feedbackBySession = new Dictionary<int, List<SessionMuscleFeedback>>();
        foreach (var s in recent)
            feedbackBySession[s.Id] = await _sessionRepo.GetMuscleFeedbackAsync(s.Id);

        // ── Deload-signal ────────────────────────────────────────────────────────
        // Senaste session har NÅGON muskel med VolumeLevel == TooMuch → severity 2
        var lastSession = recent[0];
        var lastFeedback = feedbackBySession[lastSession.Id];

        DeloadAdvice? deload = null;
        if (lastFeedback.Any(f => f.Volume == VolumeLevel.TooMuch))
        {
            deload = new DeloadAdvice("Kör en lätt vecka nu");
        }
        else if (recent.Count >= 2)
        {
            var prevFeedback = feedbackBySession[recent[1].Id];
            // Båda senaste har ≥1 muskel med PushedLimits + joint pain ≥ Moderate
            bool lastPushed = lastFeedback.Any(f => f.Volume == VolumeLevel.PushedLimits);
            bool prevPushed = prevFeedback.Any(f => f.Volume == VolumeLevel.PushedLimits);

            if (lastPushed && prevPushed)
            {
                // Kolla joint pain i sessionexercisefeedback för dessa sessioner
                var lastJointPain = await HasModerateJointPainAsync(lastSession.Id);
                var prevJointPain = await HasModerateJointPainAsync(recent[1].Id);
                if (lastJointPain && prevJointPain)
                    deload = new DeloadAdvice("Hög belastning + ledvärk i 2 pass i rad");
            }
        }

        // ── Volume-advice per muskelgrupp ──────────────────────────────────────
        // Bygg en mapping: muskelgrupp → de senaste 2 sessions som hade feedback för den muskeln
        var allMuscles = feedbackBySession.Values
            .SelectMany(list => list.Select(f => f.MuscleGroup))
            .Distinct()
            .ToList();

        var advices = new List<VolumeAdvice>();
        foreach (var muscle in allMuscles)
        {
            // Plocka de senaste 2 sessioner som har feedback för denna muskel
            var sessionsWithMuscle = recent
                .Where(s => feedbackBySession[s.Id].Any(f => f.MuscleGroup == muscle))
                .Take(2)
                .ToList();

            if (sessionsWithMuscle.Count < 2) continue;

            var fb0 = feedbackBySession[sessionsWithMuscle[0].Id].First(f => f.MuscleGroup == muscle);
            var fb1 = feedbackBySession[sessionsWithMuscle[1].Id].First(f => f.MuscleGroup == muscle);

            bool bothPumpAmazing   = fb0.Pump == PumpLevel.Amazing && fb1.Pump == PumpLevel.Amazing;
            bool bothVolGoodRange  = IsGoodVolumeRange(fb0.Volume) && IsGoodVolumeRange(fb1.Volume);
            bool bothPumpModerate  = fb0.Pump >= PumpLevel.Moderate && fb1.Pump >= PumpLevel.Moderate;
            bool bothVolJustRight  = fb0.Volume == VolumeLevel.JustRight && fb1.Volume == VolumeLevel.JustRight;
            bool bothPumpLow       = fb0.Pump == PumpLevel.Low && fb1.Pump == PumpLevel.Low;
            bool eitherNotEnough   = fb0.Volume == VolumeLevel.NotEnough || fb1.Volume == VolumeLevel.NotEnough;
            bool bothNotEnough     = fb0.Volume == VolumeLevel.NotEnough && fb1.Volume == VolumeLevel.NotEnough;
            bool bothPushedLimits  = fb0.Volume == VolumeLevel.PushedLimits && fb1.Volume == VolumeLevel.PushedLimits;
            bool bothTooMuch       = fb0.Volume == VolumeLevel.TooMuch && fb1.Volume == VolumeLevel.TooMuch;

            if (bothPumpAmazing && bothVolGoodRange)
            {
                advices.Add(new VolumeAdvice(muscle, +2, "Perfekt pump + bra volym 2 pass i rad — öka"));
            }
            else if (bothPumpModerate && bothVolJustRight)
            {
                advices.Add(new VolumeAdvice(muscle, +1, "Stadig pump + rätt volym — öka ett set"));
            }
            else if (bothPumpLow || bothNotEnough)
            {
                advices.Add(new VolumeAdvice(muscle, +1, "För lite stimulus — lägg till ett set"));
            }
            else if (bothPushedLimits)
            {
                // Kolla om det finns ledvärk i något av passen
                bool jp0 = await HasModerateJointPainForSessionAsync(sessionsWithMuscle[0].Id);
                bool jp1 = await HasModerateJointPainForSessionAsync(sessionsWithMuscle[1].Id);
                if (jp0 || jp1)
                    advices.Add(new VolumeAdvice(muscle, -1, "Hög belastning + ledvärk — minska ett set"));
            }
            else if (bothTooMuch)
            {
                advices.Add(new VolumeAdvice(muscle, -2, "För hög volym 2 pass i rad — minska 2 sets"));
            }
        }

        return new SetProgressionAnalysis(advices, deload);
    }

    // Returnerar true om NÅGON SessionExercise i sessionen har JointPain >= Moderate
    private async Task<bool> HasModerateJointPainAsync(int sessionId)
        => await HasModerateJointPainForSessionAsync(sessionId);

    private async Task<bool> HasModerateJointPainForSessionAsync(int sessionId)
    {
        var exercises = await _sessionRepo.GetSessionExercisesAsync(sessionId);
        foreach (var se in exercises)
        {
            var fb = await _sessionRepo.GetExerciseFeedbackAsync(se.Id);
            if (fb?.JointPain >= JointPainLevel.Moderate) return true;
        }
        return false;
    }

    private static bool IsGoodVolumeRange(VolumeLevel v)
        => v == VolumeLevel.JustRight || v == VolumeLevel.PushedLimits;
}
