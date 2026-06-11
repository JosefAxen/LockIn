using AudioToolbox;
using LockIn.Services;

namespace LockIn.Services;

public class SoundService : ISoundService
{
    public void PlayTimerComplete() =>
        SystemSound.PlaySystemSound(1054);
}
