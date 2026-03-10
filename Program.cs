using System.Windows.Forms;
using NotificationSoundRouter;

// WinForms, WPF, and WinRT COM all require a single-threaded apartment.
// Do not convert Main to async Task — it breaks STA and causes silent WinRT failures.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows10.0.19041.0")]

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // Config must be loaded before any routing or UI runs
        ConfigManager.Load();

        // Silence the Windows notification sound immediately
        SoundSchemeManager.Silence();

        var listener = new NotificationListener();

        // Block synchronously before the message pump starts.
        // RequestAccessAsync shows a system consent dialog that does not require
        // the WinForms pump, so blocking here with GetAwaiter().GetResult() is correct.
        bool accessGranted = listener.InitializeAsync().GetAwaiter().GetResult();

        if (!accessGranted)
        {
            MessageBox.Show(
                "Notification access was denied.\n\n" +
                "Grant access in Settings › Privacy & security › Notifications.",
                "Access denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            // Continue — config management still works; routing is a no-op until access is granted
        }

        // Runs the WinForms message pump until Application.Exit() is called from TrayApp
        Application.Run(new TrayApp(listener));
    }
}
