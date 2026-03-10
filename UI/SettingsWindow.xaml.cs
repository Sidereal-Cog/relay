using System.Collections.ObjectModel;
using System.Windows;
using Relay.Models;

namespace Relay.UI;

public partial class SettingsWindow : Window
{
    private readonly NotificationListener _listener;
    private ObservableCollection<AppConfig> _apps = new();
    private RootConfig _editingConfig = new();

    public SettingsWindow(NotificationListener listener)
    {
        _listener = listener;
        InitializeComponent();
        LoadFromCurrentConfig();
    }

    // ── Load ────────────────────────────────────────────────────────────────

    private void LoadFromCurrentConfig()
    {
        var src = ConfigManager.Current;

        _editingConfig = new RootConfig
        {
            DefaultSound       = src.DefaultSound,
            IsEnabled          = src.IsEnabled,
            RestoreSoundOnExit = src.RestoreSoundOnExit,
            Apps               = src.Apps
                .Select(CloneAppConfig)
                .ToList()
        };

        DefaultSoundTextBox.Text        = _editingConfig.DefaultSound;
        EnabledCheckBox.IsChecked       = _editingConfig.IsEnabled;
        RestoreOnExitCheckBox.IsChecked = _editingConfig.RestoreSoundOnExit;

        _apps = new ObservableCollection<AppConfig>(_editingConfig.Apps);
        AppsItemsControl.ItemsSource = _apps;
    }

    // ── Save / Cancel ───────────────────────────────────────────────────────

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        _editingConfig.DefaultSound       = DefaultSoundTextBox.Text;
        _editingConfig.IsEnabled          = EnabledCheckBox.IsChecked ?? true;
        _editingConfig.RestoreSoundOnExit = RestoreOnExitCheckBox.IsChecked ?? true;
        _editingConfig.Apps               = _apps.ToList();

        ConfigManager.Update(_editingConfig);
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e) => Close();

    // ── Default sound ────────────────────────────────────────────────────────

    private void OnBrowseDefaultSoundClicked(object sender, RoutedEventArgs e)
    {
        var path = BrowseForWav();
        if (path is not null) DefaultSoundTextBox.Text = path;
    }

    // ── App management ───────────────────────────────────────────────────────

    private async void OnAddAppClicked(object sender, RoutedEventArgs e)
    {
        var notifications = await _listener.GetCurrentNotificationsAsync();

        var existingIds = _apps
            .Select(a => a.AppId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = notifications
            .Select(n => new AppConfig
            {
                AppId       = n.AppInfo.AppUserModelId,
                DisplayName = n.AppInfo.DisplayInfo.DisplayName,
                Sound       = _editingConfig.DefaultSound
            })
            .Where(a => !existingIds.Contains(a.AppId))
            .DistinctBy(a => a.AppId)
            .ToList();

        if (candidates.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "No new apps found in the notification history.\n\n" +
                "Trigger a notification from the app you want to add, then try again.",
                "No apps found",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var picker = new AppPickerDialog(candidates) { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedApp is not null)
            _apps.Add(picker.SelectedApp);
    }

    private void OnRemoveAppClicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: AppConfig app })
            _apps.Remove(app);
    }

    private void OnBrowseAppSoundClicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: AppConfig app })
        {
            var path = BrowseForWav();
            if (path is not null) app.Sound = path;
        }
    }

    // ── Rule management ──────────────────────────────────────────────────────

    private void OnAddRuleClicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: AppConfig app })
            app.Rules.Add(new SoundRule());
    }

    private void OnRemoveRuleClicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: SoundRule rule })
        {
            // Find the parent AppConfig by searching all apps
            var owner = _apps.FirstOrDefault(a => a.Rules.Contains(rule));
            owner?.Rules.Remove(rule);
        }
    }

    private void OnBrowseRuleSoundClicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: SoundRule rule })
        {
            var path = BrowseForWav();
            if (path is not null) rule.Sound = path;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? BrowseForWav()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "WAV files (*.wav)|*.wav",
            Title  = "Select sound file"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private static AppConfig CloneAppConfig(AppConfig src) => new()
    {
        AppId       = src.AppId,
        DisplayName = src.DisplayName,
        Sound       = src.Sound,
        Rules       = new ObservableCollection<SoundRule>(
            src.Rules.Select(r => new SoundRule
            {
                MatchField = r.MatchField,
                MatchType  = r.MatchType,
                MatchValue = r.MatchValue,
                Sound      = r.Sound
            }))
    };
}
