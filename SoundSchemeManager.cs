using Microsoft.Win32;

namespace NotificationSoundRouter;

public static class SoundSchemeManager
{
    // Registry path for the Windows notification sound.
    // Empty string value = no sound, matching what Windows sets when you pick "(None)".
    private const string NotificationKeyPath =
        @"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current";

    private static string? _originalValue;

    public static void Silence()
    {
        using var key = Registry.CurrentUser.OpenSubKey(NotificationKeyPath, writable: true);
        if (key is null) return;  // Absent on some SKUs — fail gracefully

        _originalValue = key.GetValue("") as string;
        key.SetValue("", "", RegistryValueKind.String);
    }

    public static void Restore()
    {
        if (_originalValue is null) return;

        using var key = Registry.CurrentUser.OpenSubKey(NotificationKeyPath, writable: true);
        key?.SetValue("", _originalValue, RegistryValueKind.String);
        _originalValue = null;
    }

    public static bool IsSilenced()
    {
        using var key = Registry.CurrentUser.OpenSubKey(NotificationKeyPath, writable: false);
        return (key?.GetValue("") as string) == string.Empty;
    }
}
