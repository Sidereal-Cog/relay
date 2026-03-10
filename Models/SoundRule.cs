namespace Relay.Models;

public enum MatchField { Title, Body }
public enum MatchType  { Contains, StartsWith, EndsWith, Equals }

public sealed class SoundRule
{
    public MatchField MatchField { get; set; }
    public MatchType  MatchType  { get; set; }
    public string MatchValue { get; set; } = string.Empty;
    public string Sound      { get; set; } = string.Empty;
}
