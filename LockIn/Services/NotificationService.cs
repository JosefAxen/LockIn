using Plugin.LocalNotification;

namespace LockIn.Services;

public class NotificationService
{
    private const int TimerId = 100;

    public async Task RequestPermissionAsync()
    {
        await LocalNotificationCenter.Current.RequestNotificationPermissionAsync();
    }

    public void ScheduleTimer(int seconds, string exerciseName)
    {
        CancelTimer();
        var request = new NotificationRequest
        {
            NotificationId = TimerId,
            Title          = "Vilotimer klar!",
            Description    = $"Dags för nästa set – {exerciseName}",
            Schedule       = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Now.AddSeconds(seconds)
            }
        };
        _ = LocalNotificationCenter.Current.Show(request);
    }

    public void CancelTimer()
    {
        LocalNotificationCenter.Current.Cancel(TimerId);
    }
}
