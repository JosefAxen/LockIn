namespace LockIn.Services;

public class ActiveWorkoutStateService
{
    public bool IsActive { get; private set; }
    public event Action? StateChanged;

    public void Activate() { IsActive = true; StateChanged?.Invoke(); }
    public void Deactivate() { IsActive = false; StateChanged?.Invoke(); }
}
