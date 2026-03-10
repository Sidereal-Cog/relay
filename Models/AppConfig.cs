using System.Collections.ObjectModel;

namespace Relay.Models;

public sealed class AppConfig
{
    public string AppId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Sound { get; set; } = string.Empty;
    public ObservableCollection<SoundRule> Rules { get; set; } = new();
}
