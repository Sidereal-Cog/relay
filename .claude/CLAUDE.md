# Relay — Project Context for Claude

## What This Is

**Relay** is a Windows 11 system tray app (C#, .NET 8) that intercepts toast notifications via WinRT `UserNotificationListener` and plays per-app custom `.wav` files. The global Windows notification sound is silenced via registry on startup and restored on exit.

Product name: **Relay** | MSIX identity: `SiderealCog.Relay` | Executable: `Relay.exe` | Namespace: `Relay`
Solution: `Relay.sln` | Project: `Relay.csproj` | Folder: `NotificationSoundRouter/` (legacy folder name)

## Stack

- **Target framework:** `net8.0-windows10.0.19041.0` (WinRT projections built-in — do NOT add `Microsoft.Windows.SDK.Contracts`)
- **UI:** WinForms `NotifyIcon` for tray + WPF windows for Settings (shared STA thread)
- **Packaging:** MSIX required — `UserNotificationListener` needs `userNotificationListener` restricted capability
- **Config:** `%AppData%\Relay\config.json`
- **Brand:** Sidereal Cog — styles in `UI/BrandStyles.xaml`

## Build

```powershell
dotnet build
```

Dev MSIX registration (no signing required):
```powershell
Add-AppxPackage -Register ".\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Package.appxmanifest"
```

Run as current user, not admin. Unregister with:
```powershell
Get-AppxPackage *SiderealCog.Relay* | Remove-AppxPackage
```

## Critical Build Rules

1. **No `Microsoft.Windows.SDK.Contracts`** — causes NETSDK1130; the TFM already provides WinRT types
2. **Add `using System.IO;` explicitly** — not included in implicit usings for this dual WPF+WinForms target
3. **`System.IO.MatchType` conflicts with `Models.MatchType`** — use alias in SoundRouter.cs: `using MatchType = NotificationSoundRouter.Models.MatchType;`
4. **Ambiguous WPF/WinForms types** — use `System.Windows.MessageBox` and `Microsoft.Win32.OpenFileDialog` (not bare names)
5. **No `ApplicationIcon` without a real ICO** — `Bitmap.Save(..., ImageFormat.Icon)` is not a valid ICO
6. **No `PlaceholderText` on WPF `TextBox`** — that's a WinUI property

## Threading Model

- `[STAThread]` on `Main` is mandatory — WinRT COM, WPF, and WinForms all require STA
- Do NOT use `async Task Main` — breaks STA, causes silent WinRT failures
- `listener.InitializeAsync().GetAwaiter().GetResult()` is correct before `Application.Run`
- `NotificationChanged` fires on thread-pool threads — never touch UI from there
- WPF `Application` must have `ShutdownMode.OnExplicitShutdown` — never call `Application.Run()` on it

## Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point, init order, STA setup |
| `TrayApp.cs` | NotifyIcon, WPF Application singleton, exit cleanup |
| `NotificationListener.cs` | WinRT wrapper; `GetNotification()` (check method name per SDK version) |
| `SoundRouter.cs` | Rule matching logic, `SoundPlayer.Play()` per-instance |
| `SoundSchemeManager.cs` | Registry silence/restore |
| `ConfigManager.cs` | Atomic JSON read/write, volatile config reference; dir = `%AppData%\Relay\` |
| `UI/BrandStyles.xaml` | Sidereal Cog brand ResourceDictionary |
| `UI/SettingsWindow.xaml/.cs` | Main settings UI (title: "Relay — Settings") |
| `UI/AppPickerDialog.xaml/.cs` | "Add app" picker |
| `Models/` | `RootConfig`, `AppConfig`, `SoundRule` — `AppConfig.Rules` is `ObservableCollection` |
| `Package.appxmanifest` | `rescap:userNotificationListener`; Identity `SiderealCog.Relay`; Executable `Relay.exe` |

## Config Schema

```json
{
  "defaultSound": "path/to/default.wav",
  "isEnabled": true,
  "restoreSoundOnExit": true,
  "apps": [{
    "appId": "AUMID",
    "displayName": "App Name",
    "sound": "path/to/sound.wav",
    "rules": [{
      "matchField": "title|body",
      "matchType": "contains|startsWith|endsWith|equals",
      "matchValue": "text to match",
      "sound": "path/to/sound.wav"
    }]
  }]
}
```

## Sidereal Cog Brand

See skill `sc-brand-guidelines` for full spec. Quick reference:
- Navy `#1a1f3a`, Blue `#4a9eff`, Silver `#c0c8d8`
- Fonts: Inter → Space Grotesk → Segoe UI
- Spacing: 4px multiples
- Buttons: primary = blue bg + white text; secondary = transparent + blue border
- Voice: direct and understated — "Settings saved." not "Settings saved successfully!"
- All WPF styles live in `UI/BrandStyles.xaml`

## Placeholder Assets (need replacement)

- `Resources/tray.ico` — 16×16 navy square (not a valid ICO from `Bitmap.Save`)
- `Assets/*.png` — 1×1 navy squares; MSIX requires real images for Store/sideload

## Known Limitation

Self-managing apps (Discord, Slack desktop, Teams, Spotify) play sounds through their own audio engine. The `NotificationChanged` event still fires, so Relay can play a sound — but the app's own sound may play simultaneously. Users should disable in-app sounds for these apps.
