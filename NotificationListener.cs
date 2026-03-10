using System.Runtime.InteropServices;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotificationSoundRouter;

public sealed class NotificationListener : IDisposable
{
    private UserNotificationListener? _listener;
    private bool _subscribed;

    /// <summary>
    /// Requests notification access and subscribes to the NotificationChanged event.
    /// Must be called before the WinForms message pump starts (blocks the STA thread).
    /// Returns false if access was denied or not supported (app not packaged as MSIX).
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        _listener = UserNotificationListener.Current;

        var status = await _listener.RequestAccessAsync();
        if (status != UserNotificationListenerAccessStatus.Allowed)
            return false;

        _listener.NotificationChanged += OnNotificationChanged;
        _subscribed = true;
        return true;
    }

    private void OnNotificationChanged(
        UserNotificationListener sender,
        UserNotificationChangedEventArgs args)
    {
        // Only act on new notifications, not dismissals
        if (args.ChangeKind != UserNotificationChangedKind.Added)
            return;

        // The notification may have been dismissed between the event firing and this call
        UserNotification? notification;
        try
        {
            notification = sender.GetNotification(args.UserNotificationId);
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            return;
        }

        if (notification is null) return;

        ExtractAndRoute(notification);
    }

    private static void ExtractAndRoute(UserNotification notification)
    {
        string appId   = notification.AppInfo.AppUserModelId;
        string title   = string.Empty;
        string body    = string.Empty;

        try
        {
            var binding = notification.Notification.Visual?.GetBinding(
                KnownNotificationBindings.ToastGeneric);

            if (binding is not null)
            {
                var textElements = binding.GetTextElements();
                title = textElements.ElementAtOrDefault(0)?.Text ?? string.Empty;
                body  = textElements.ElementAtOrDefault(1)?.Text ?? string.Empty;
            }
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            // Content may be unavailable (e.g., locked screen policy).
            // Fall through with empty strings — appId match still works.
        }

        SoundRouter.Route(appId, title, body);
    }

    /// <summary>
    /// Returns all currently-visible toast notifications.
    /// Used by the Settings window "Add App" picker.
    /// </summary>
    public async Task<IReadOnlyList<UserNotification>> GetCurrentNotificationsAsync()
    {
        if (_listener is null) return Array.Empty<UserNotification>();
        return await _listener.GetNotificationsAsync(NotificationKinds.Toast);
    }

    public void Dispose()
    {
        if (_subscribed && _listener is not null)
        {
            _listener.NotificationChanged -= OnNotificationChanged;
            _subscribed = false;
        }
    }
}
