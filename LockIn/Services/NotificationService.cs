namespace LockIn.Services;

public class NotificationService
{
    private const string TimerId = "rest_timer";

    public async Task RequestPermissionAsync()
    {
#if IOS
        var center = UserNotifications.UNUserNotificationCenter.Current;
        await center.RequestAuthorizationAsync(
            UserNotifications.UNAuthorizationOptions.Alert |
            UserNotifications.UNAuthorizationOptions.Sound);
#else
        await Task.CompletedTask;
#endif
    }

    public void ScheduleTimer(int seconds, string exerciseName)
    {
#if IOS
        CancelTimer();
        var content = new UserNotifications.UNMutableNotificationContent
        {
            Title = "Vilotimer klar!",
            Body = $"Dags för nästa set – {exerciseName}",
            Sound = UserNotifications.UNNotificationSound.Default
        };
        var trigger = UserNotifications.UNTimeIntervalNotificationTrigger
            .CreateTrigger(seconds, false);
        var request = UserNotifications.UNNotificationRequest
            .FromIdentifier(TimerId, content, trigger);
        _ = UserNotifications.UNUserNotificationCenter.Current
            .AddNotificationRequestAsync(request);
#endif
    }

    public void CancelTimer()
    {
#if IOS
        var center = UserNotifications.UNUserNotificationCenter.Current;
        center.RemovePendingNotificationRequests(new[] { TimerId });
        center.RemoveDeliveredNotifications(new[] { TimerId });
#endif
    }
}
