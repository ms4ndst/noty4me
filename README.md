# Noty4Me

A tiny Catppuccin-themed note-taking app that lives in the Windows system tray.

## Features

- **System tray app** — single left-click on the icon opens notes; right-click shows the menu (Open / Settings / Exit). Single-instance via named mutex.
- **Notes** — title + body, autosave on edit, JSON-backed.
- **Theme selector** — Catppuccin in all four flavors (Latte / Frappé / Macchiato / Mocha) with a separate accent picker (14 accents). Window chrome (title bar) is tinted to match via DWM, not just the WPF body.
- **Start with Windows** — packaged build uses MSIX `windows.startupTask` (manageable from Task Manager → Startup apps); unpackaged build falls back to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Start minimized to tray** — optional; on by default.

## Storage

The data location depends on how you run the app:

| Build | Path |
|---|---|
| Unpackaged (`dotnet run` / `.exe` directly) | `%APPDATA%\Noty4Me\` |
| MSIX-installed (1.0.2+) | `%LOCALAPPDATA%\Packages\<PackageFamilyName>\LocalState\` |

Why two paths: for MSIX-packaged apps, Windows' Package State Redirection silently redirects writes to `%APPDATA%` into the per-package hive at `…\LocalCache\Roaming\…`. That path is hard to find and not stable across reinstalls. From 1.0.2 onwards the packaged build writes to `ApplicationData.Current.LocalFolder` directly so the path is predictable. Existing 1.0.x packaged users get a one-time migration on first launch (files copied from the PSR location).

Both contain:

| File | Contents |
|---|---|
| `notes.json` | All notes (id, title, body, updated timestamp) |
| `config.json` | `Flavor`, `Accent`, `StartMinimized` |

The notes window status bar always shows the actual on-disk path for the current run.

Autostart state is *not* stored here — the OS owns it (StartupTask for packaged, `HKCU\…\Run` for unpackaged).

## Run from source

```powershell
dotnet run --project src\Noty4Me\Noty4Me.csproj -c Release
```

Requires the .NET 10 SDK. Target framework is `net10.0-windows10.0.19041.0` (Windows SDK projection needed for the `StartupTask` WinRT API).

## Build a signed MSIX

Requires the Windows 10/11 SDK on PATH (`MakeAppx.exe`, `SignTool.exe`). The build script auto-locates them under `C:\Program Files (x86)\Windows Kits\10\bin` if they aren't on PATH.

```powershell
# 1. Generate a dev signing cert (CN=Noty4Me Dev). You will be prompted for a
#    password to protect the .pfx. Pass -Password (SecureString) to skip the prompt.
.\packaging\make-cert.ps1

# 2. Publish, pack, sign. You will be prompted for the .pfx password
#    (must match what you used in step 1).
.\packaging\build-msix.ps1
```

Both scripts require an interactive password by design — no defaults — so the `.pfx` is never protected by a secret that lives in source control. `Noty4Me.pfx` and `Noty4Me.cer` themselves are gitignored.

Output: `packaging\out\Noty4Me_<version>_x64.msix` (the filename is read from `AppxManifest.xml`'s `Version`).

## Install the MSIX

A self-signed package will not install until the certificate is trusted on the target machine. **Once, as Administrator:**

```powershell
Import-Certificate -FilePath .\packaging\Noty4Me.cer `
    -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

Then any user can install:

```powershell
Add-AppxPackage .\packaging\out\Noty4Me_1.0.1.0_x64.msix
```

Uninstall:

```powershell
Get-AppxPackage Noty4Me | Remove-AppxPackage
```

## Versioning

Three files must stay in sync when bumping:

- `src\Noty4Me\Noty4Me.csproj` — `<Version>` / `<AssemblyVersion>` / `<FileVersion>`
- `packaging\AppxManifest.xml` — `<Identity Version="…">`
- The MSIX filename is derived from the manifest, so no separate update needed there.

## Versioning the signing cert

`make-cert.ps1` writes `packaging\Noty4Me.pfx` (private, gitignored) and `packaging\Noty4Me.cer` (public). The cert subject must match the manifest's `Publisher` (default `CN=Noty4Me Dev`). Change one → change the other.

## Project layout

```
src\Noty4Me\
  App.xaml(.cs)                # entry, single-instance mutex, tray bootstrap
  UI\NotesWindow.xaml(.cs)     # list + editor, autosave
  UI\SettingsWindow.xaml(.cs)  # flavor + accent pickers, autostart toggle
  UI\TrayIconHost.cs           # NotifyIcon + context menu, single-click → open
  Services\ThemeManager.cs     # flavor swap + accent rebuild (live)
  Services\WindowChrome.cs     # DWM dark mode + caption color per flavor
  Services\AutostartService.cs # StartupTask (packaged) / HKCU Run (unpackaged)
  Services\PackageContext.cs   # IsPackaged detector (MSIX vs unpackaged)
  Services\ConfigStore.cs      # config.json
  Services\NotesStore.cs       # notes.json
  Services\Paths.cs            # AppData path; PSR migration on first packaged launch
  Models\Note.cs, AppConfig.cs
  Themes\Catppuccin.*.xaml     # 4 flavor palettes (colors only)
  Themes\Theme.Common.xaml     # control styles (DynamicResource-bound)
  Assets\tray.ico
packaging\
  AppxManifest.xml             # incl. windows.startupTask extension
  Images\                      # MSIX visual assets (generated)
  make-icon.ps1                # generates tray.ico + MSIX images
  make-cert.ps1                # self-signed code-signing cert
  build-msix.ps1               # publish + MakeAppx + SignTool
```

## Theming architecture

Each flavor file (`Themes\Catppuccin.*.xaml`) contains *only* the 26 Catppuccin colors as `Color` resources (`Cat.Base`, `Cat.Text`, `Cat.Mauve`, …). Control styles in `Theme.Common.xaml` reference `Brush.*` resources via `DynamicResource`. The brushes themselves are built in code by `ThemeManager.RebuildBrushes` and pushed into `Application.Resources` on every theme change — that's what makes the live swap work without restarting the app.

`WindowChrome` hooks `ThemeManager.ThemeChanged` so the native title bar follows along.

## Theme: [Catppuccin](https://github.com/catppuccin/catppuccin)
