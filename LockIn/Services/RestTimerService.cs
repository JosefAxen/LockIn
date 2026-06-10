namespace LockIn.Services;

public class RestTimerService
{
    private CancellationTokenSource? _cts;

    public event Action<int>? Tick;
    public event Action? Completed;

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;
    public int TotalSeconds { get; private set; }
    public int SecondsRemaining { get; private set; }

    public void Start(int seconds)
    {
        Cancel();
        TotalSeconds = seconds;
        SecondsRemaining = seconds;
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
        while (SecondsRemaining > 0 && !token.IsCancellationRequested)
        {
            await Task.Delay(1000, token).ContinueWith(_ => { });
            if (token.IsCancellationRequested) return;
            SecondsRemaining--;
            Tick?.Invoke(SecondsRemaining);
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
