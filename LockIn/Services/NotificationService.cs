#if IOS
using UserNotifications;
#endif

using LockIn.Resources.Strings;

namespace LockIn.Services;

public class NotificationService
{
    private const string TimerId = "rest_timer";

    public async Task RequestPermissionAsync()
    {
#if IOS
        await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
#else
        await Task.CompletedTask;
#endif
    }

    public void ScheduleTimer(int seconds, string exerciseName)
    {
        CancelTimer();
#if IOS
        var content = new UNMutableNotificationContent
        {
            Title = AppResources.Notification_RestTimer_Title,
            Body  = string.Format(AppResources.Notification_RestTimer_Body_Format, exerciseName),
            Sound = UNNotificationSound.Default
        };
        var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(seconds, repeats: false);
        var request = UNNotificationRequest.FromIdentifier(TimerId, content, trigger);
        UNUserNotificationCenter.Current.AddNotificationRequest(request, null);
#endif
    }

    public void CancelTimer()
    {
#if IOS
        UNUserNotificationCenter.Current.RemovePendingNotificationRequests(new[] { TimerId });
#endif
    }
}
