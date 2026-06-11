using AudioToolbox;
using LockIn.Services;

namespace LockIn.Services;

public class SoundService : ISoundService
{
    public void PlayTimerComplete()
    {
        var sound = new SystemSound(1054u);
        sound.PlaySystemSound();
    }
}
