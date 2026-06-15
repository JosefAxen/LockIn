namespace LockIn.Services;

public class RestTimerService
{
    private CancellationTokenSource? _cts;
    private DateTime _deadline;

    public event Action<int>? Tick;
    public event Action? Completed;

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;
    public int TotalSeconds { get; private set; }

    // Derived from actual wall-clock time so it stays correct after app suspend/resume
    public int SecondsRemaining => IsRunning
        ? Math.Max(0, (int)Math.Ceiling((_deadline - DateTime.Now).TotalSeconds))
        : 0;

    public void Start(int seconds)
    {
        Cancel();
        TotalSeconds = seconds;
        _deadline = DateTime.Now.AddSeconds(seconds);
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(500, token).ContinueWith(_ => { });
            if (token.IsCancellationRequested) return;
            var remaining = SecondsRemaining;
            Tick?.Invoke(remaining);
            if (remaining <= 0) break;
        }

        if (!token.IsCancellationRequested)
            Completed?.Invoke();
    }

    public static string Format(int totalSeconds)
    {
        var m = totalSeconds / 60;
        var s = totalSeconds % 60;
        return $"{m}:{s:D2}";
    }
}
