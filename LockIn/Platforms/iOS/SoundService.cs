using AudioToolbox;
using LockIn.Services;

namespace LockIn.Services;

public class SoundService : ISoundService
{
    public void PlayTimerComplete()
    {
        var sound = new SystemSound(1054u); // Tink
        sound.PlaySystemSound();
    }

    public void PlayAchievementUnlocked()
    {
        var sound = new SystemSound(1023u); // Chord — bright celebratory sound
        sound.PlaySystemSound();
    }
}
