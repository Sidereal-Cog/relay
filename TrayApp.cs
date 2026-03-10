using System.Windows.Forms;
using Relay.UI;

namespace Relay;

public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly NotificationListener _listener;
    private System.Windows.Application? _wpfApp;
    private SettingsWindow? _settingsWindow;

    public TrayApp(NotificationListener listener)
    {
        _listener = listener;

        _toggleItem = new ToolStripMenuItem(string.Empty, null, OnToggleClicked);
        UpdateToggleItem();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, OnOpenSettingsClicked);
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, OnExitClicked);

        _notifyIcon = new NotifyIcon
        {
            Icon             = new System.Drawing.Icon("Resources\\tray.ico"),
            Text             = "Relay",
            Visible          = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += OnOpenSettingsClicked;

        InitializeWpfApplication();
    }

    private void InitializeWpfApplication()
    {
        if (System.Windows.Application.Current is null)
        {
            _wpfApp = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }
    }

    private void OnOpenSettingsClicked(object? sender, EventArgs e)
    {
        // Prevent duplicate windows — bring existing one to front
        if (_settingsWindow is not null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_listener);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void OnToggleClicked(object? sender, EventArgs e)
    {
        var current = ConfigManager.Current;
        ConfigManager.Update(new Models.RootConfig
        {
            DefaultSound      = current.DefaultSound,
            IsEnabled         = !current.IsEnabled,
            RestoreSoundOnExit = current.RestoreSoundOnExit,
            Apps              = current.Apps
        });
        UpdateToggleItem();
    }

    private void UpdateToggleItem()
    {
        bool enabled = ConfigManager.Current.IsEnabled;
        _toggleItem.Text    = enabled ? "Enabled" : "Disabled";
        _toggleItem.Checked = enabled;
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        if (ConfigManager.Current.RestoreSoundOnExit)
            SoundSchemeManager.Restore();

        _settingsWindow?.Close();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _listener.Dispose();
        _wpfApp?.Shutdown();

        Application.Exit();
    }
}
