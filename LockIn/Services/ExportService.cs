using System.IO.Compression;
using System.Text;

namespace LockIn.Services;

public class ExportService(DatabaseService db)
{
    public async Task<string> ExportAsync()
    {
        var (sessions, sets) = await db.GetExportDataAsync();

        var exportsDir = Path.Combine(FileSystem.AppDataDirectory, "exports");
        Directory.CreateDirectory(exportsDir);

        var zipPath = Path.Combine(exportsDir,
            $"lockin_export_{DateTime.Now:yyyy-MM-dd}.zip");

        if (File.Exists(zipPath)) File.Delete(zipPath);
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        // sessions.csv
        var sessionsEntry = zip.CreateEntry("sessions.csv");
        using (var writer = new StreamWriter(sessionsEntry.Open(),
                   new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            writer.NewLine = "\r\n";
            writer.WriteLine(
                "SessionId,Date,StartTime,DurationMinutes,TemplateName," +
                "TotalSets,TotalVolumeKg,PRCount,Notes");

            foreach (var s in sessions)
            {
                var dur = s.CompletedAt.HasValue
                    ? (int)(s.CompletedAt.Value - s.StartedAt).TotalMinutes
                    : 0;
                writer.WriteLine(string.Join(",",
                    s.SessionId,
                    s.StartedAt.ToString("yyyy-MM-dd"),
                    s.StartedAt.ToString("HH:mm:ss"),
                    dur,
                    Csv(s.TemplateName),
                    s.TotalSets,
                    s.TotalVolumeKg.ToString("F1"),
                    s.PRCount,
                    Csv(s.Notes)));
            }
        }

        // sets.csv
        var setsEntry = zip.CreateEntry("sets.csv");
        using (var writer = new StreamWriter(setsEntry.Open(),
                   new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            writer.NewLine = "\r\n";
            writer.WriteLine(
                "SessionId,SessionDate,ExerciseName,MuscleGroup,Equipment," +
                "SetNumber,SetType,WeightKg,Reps,RIR,IsPR,DurationSeconds");

            foreach (var r in sets)
            {
                writer.WriteLine(string.Join(",",
                    r.SessionId,
                    r.SessionDate.ToString("yyyy-MM-dd"),
                    Csv(r.ExerciseName),
                    MuscleGroupName(r.MuscleGroup),
                    EquipmentName(r.Equipment),
                    r.SetNumber,
                    SetTypeName(r.SetType),
                    r.WeightKg.ToString("F2"),
                    r.Reps,
                    r.RIR,
                    r.IsPR ? "true" : "false",
                    r.DurationSeconds));
            }
        }

        return zipPath;
    }

    private static string Csv(string? value)
    {
        if (value is null) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string MuscleGroupName(int v) => v switch
    {
        0 => "Chest", 1 => "Back", 2 => "Shoulders", 3 => "Biceps",
        4 => "Triceps", 5 => "Legs", 6 => "Core", 7 => "FullBody",
        8 => "Other", 9 => "Forearms", _ => "Unknown"
    };

    private static string EquipmentName(int v) => v switch
    {
        0 => "Other", 1 => "Barbell", 2 => "Dumbbell", 3 => "Cable",
        4 => "Machine", 5 => "BodyOnly", 6 => "EZBar", 7 => "Kettlebell",
        8 => "Bands", 9 => "FoamRoll", 10 => "MedicineBall", _ => "Unknown"
    };

    private static string SetTypeName(int v) => v switch
    {
        0 => "Normal", 1 => "Warmup", 2 => "Time", 3 => "Dropset", _ => "Normal"
    };
}
