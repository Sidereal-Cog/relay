using System.Windows;
using System.Windows.Input;
using NotificationSoundRouter.Models;

namespace NotificationSoundRouter.UI;

public partial class AppPickerDialog : Window
{
    public AppConfig? SelectedApp { get; private set; }

    public AppPickerDialog(IEnumerable<AppConfig> candidates)
    {
        InitializeComponent();
        AppListBox.ItemsSource = candidates.ToList();
        AppListBox.SelectedIndex = 0;
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        SelectedApp = AppListBox.SelectedItem as AppConfig;
        DialogResult = SelectedApp is not null;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnListBoxDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (AppListBox.SelectedItem is not null)
            OnOkClicked(sender, e);
    }
}
