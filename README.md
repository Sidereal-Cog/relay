# Relay

A Windows 11 system tray utility that gives you per-app control over notification sounds.

Windows plays the same generic sound for every app. Relay intercepts toast notifications invisibly and plays whatever `.wav` file you assign to each app — with optional content-match rules so a direct message sounds different from a channel mention.

---

## Features

- **Per-app sounds** — assign a different `.wav` to each app
- **Content-match rules** — match on notification title or body text (contains, starts with, ends with, equals)
- **Default fallback** — plays a fallback sound for any unconfigured app
- **Enable/disable toggle** — pause routing from the tray without closing the app
- **Registry-safe** — silences the Windows notification sound on launch, restores it on exit
- **Tray-only** — no taskbar entry, no persistent window; right-click the tray icon to open Settings

---

## Requirements

- Windows 11 (build 19041+)
- .NET 8 runtime
- Installed as an MSIX package (required for `UserNotificationListener` API access)

---

## Building

```powershell
dotnet build
```

Targets `net8.0-windows10.0.19041.0`, `win-x64`. Requires both WPF and WinForms workloads (included in the .NET 8 Windows Desktop SDK).

### Dev Registration (MSIX without signing)

After building, register the package from the output directory to get package identity:

```powershell
Add-AppxPackage -Register ".\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Package.appxmanifest"
```

Run as the **current user** (not admin). This is required — without package identity, `UserNotificationListener.RequestAccessAsync()` returns `NotSupported`.

To unregister:

```powershell
Get-AppxPackage *SiderealCog.Relay* | Remove-AppxPackage
```

---

## Project Structure

```
NotificationSoundRouter/          ← repo/solution folder (named for C# namespace)
├── NotificationSoundRouter.csproj
├── Package.appxmanifest          # MSIX identity: SiderealCog.Relay
├── Program.cs
├── TrayApp.cs
├── NotificationListener.cs
├── SoundRouter.cs
├── SoundSchemeManager.cs
├── ConfigManager.cs              # Config stored at %AppData%\Relay\config.json
├── Models/
│   ├── RootConfig.cs
│   ├── AppConfig.cs
│   └── SoundRule.cs
├── UI/
│   ├── BrandStyles.xaml          # Sidereal Cog brand ResourceDictionary
│   ├── SettingsWindow.xaml/.cs
│   └── AppPickerDialog.xaml/.cs
├── Resources/
│   ├── tray.ico                  # Tray icon (replace placeholder with real ICO)
│   └── silence.wav
└── Assets/                       # MSIX package logos (replace placeholders)
```

---

## Config File

Location: `%AppData%\Relay\config.json`

```json
{
  "defaultSound": "C:\\Users\\You\\Sounds\\default.wav",
  "isEnabled": true,
  "restoreSoundOnExit": true,
  "apps": [
    {
      "appId": "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App",
      "displayName": "Windows Terminal",
      "sound": "C:\\Users\\You\\Sounds\\terminal.wav",
      "rules": []
    },
    {
      "appId": "Slack.Slack",
      "displayName": "Slack",
      "sound": "C:\\Users\\You\\Sounds\\slack.wav",
      "rules": [
        {
          "matchField": "title",
          "matchType": "contains",
          "matchValue": "direct message",
          "sound": "C:\\Users\\You\\Sounds\\slack-dm.wav"
        }
      ]
    }
  ]
}
```

Rules are evaluated top-to-bottom; first match wins. `matchType` values: `contains`, `startsWith`, `endsWith`, `equals` (all case-insensitive).

---

## Known Limitations

Some apps (Discord, Slack desktop, Spotify, Teams) route audio through their **own internal engine**, bypassing the Windows toast sound system entirely. Relay will still fire for these apps (the visual toast is intercepted), but the app may also play its own sound simultaneously. Disable in-app notification sounds for those apps to avoid doubling up.

---

## Architecture Notes

- WinForms `Application.Run` drives the message pump for both the tray and WPF windows (shared STA thread)
- WPF `Application` is created with `ShutdownMode.OnExplicitShutdown` — never call `Application.Run()` on it
- `NotificationChanged` fires on thread-pool threads; `SoundRouter.Route()` is safe to call there (reads a `volatile` config reference, creates a new `SoundPlayer` per call)
- Config writes are atomic via `File.Replace` (temp → final → backup) to prevent corruption on crash

---

## Roadmap

- [ ] Real tray icon + MSIX assets (replace placeholder navy squares)
- [ ] End-to-end notification routing test
- [ ] Volume control per app
- [ ] `.mp3` / `.ogg` support via NAudio
