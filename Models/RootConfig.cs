namespace Relay.Models;

public sealed class RootConfig
{
    public string DefaultSound { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool RestoreSoundOnExit { get; set; } = true;
    public List<AppConfig> Apps { get; set; } = new();
}
