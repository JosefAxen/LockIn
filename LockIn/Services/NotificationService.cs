#if IOS
using UserNotifications;
using Foundation;
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

    public void ScheduleReminders(int daysBitmask, int timeMinutes)
    {
        CancelReminders();
#if IOS
        var h = timeMinutes / 60;
        var m = timeMinutes % 60;
        var iOSWeekdays = new[] { 2, 3, 4, 5, 6, 7, 1 };
        for (var bit = 0; bit < 7; bit++)
        {
            if ((daysBitmask & (1 << bit)) == 0) continue;
            var content = new UNMutableNotificationContent
            {
                Title = AppResources.Notification_Reminder_Title,
                Body  = AppResources.Notification_Reminder_Body,
                Sound = UNNotificationSound.Default
            };
            var components = new NSDateComponents
            {
                Hour    = h,
                Minute  = m,
                Weekday = iOSWeekdays[bit]
            };
            var trigger = UNCalendarNotificationTrigger.CreateTrigger(components, repeats: true);
            var id      = $"reminder_wd_{iOSWeekdays[bit]}";
            var request = UNNotificationRequest.FromIdentifier(id, content, trigger);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, null);
        }
#endif
    }

    public void CancelReminders()
    {
#if IOS
        var ids = new[] { "reminder_wd_1", "reminder_wd_2", "reminder_wd_3",
                          "reminder_wd_4", "reminder_wd_5", "reminder_wd_6", "reminder_wd_7" };
        UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
#endif
    }
}
