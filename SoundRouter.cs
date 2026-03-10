using System.IO;
using System.Media;
using Relay.Models;
using MatchType = Relay.Models.MatchType;

namespace Relay;

public static class SoundRouter
{
    // Called from thread-pool threads (NotificationChanged event).
    // Reads volatile config reference — no locking needed for reads.
    public static void Route(string appId, string title, string body)
    {
        var config = ConfigManager.Current;
        if (!config.IsEnabled) return;

        var appConfig = config.Apps.FirstOrDefault(
            a => string.Equals(a.AppId, appId, StringComparison.OrdinalIgnoreCase));

        string soundPath;

        if (appConfig is null)
        {
            soundPath = config.DefaultSound;
        }
        else
        {
            soundPath = appConfig.Sound;

            foreach (var rule in appConfig.Rules)
            {
                string field = rule.MatchField == MatchField.Title ? title : body;
                if (Matches(field, rule.MatchType, rule.MatchValue))
                {
                    soundPath = rule.Sound;
                    break;
                }
            }
        }

        PlaySound(soundPath);
    }

    private static bool Matches(string input, MatchType type, string value)
    {
        return type switch
        {
            MatchType.Contains   => input.Contains(value,   StringComparison.OrdinalIgnoreCase),
            MatchType.StartsWith => input.StartsWith(value, StringComparison.OrdinalIgnoreCase),
            MatchType.EndsWith   => input.EndsWith(value,   StringComparison.OrdinalIgnoreCase),
            MatchType.Equals     => input.Equals(value,     StringComparison.OrdinalIgnoreCase),
            _                    => false
        };
    }

    private static void PlaySound(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        // New instance per call — SoundPlayer is not thread-safe for concurrent shared use.
        // Play() is non-blocking; audio runs on a dedicated Win32 audio thread.
        var player = new SoundPlayer(path);
        player.Play();
    }
}
