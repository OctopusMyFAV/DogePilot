# DogePilot — MSFS2020 Discord Rich Presence

Auto-launching Discord Rich Presence for **Microsoft Flight Simulator 2020**.  
Start `DogePilotLauncher.exe` once — it watches for MSFS in the background and handles everything else automatically.

---

## How It Works

```
Windows starts
    └─> DogePilotLauncher.exe (runs silently in system tray)
            └─> Detects FlightSimulator.exe started
                    └─> Waits 8 seconds (MSFS loading)
                            └─> Launches DogePilot.Client.exe automatically
                                    └─> Discord Rich Presence active ✓
            └─> Detects FlightSimulator.exe closed
                    └─> Kills DogePilot.Client.exe automatically
```

---

## Quick Start (After Building)

1. Put all files from the build output in one folder:
   ```
   DogePilotLauncher.exe      ← run THIS
   DogePilot.Client.exe
   SimConnect.dll
   config.json               ← auto-created on first run
   icon.ico
   ```

2. Edit `config.json` and set your Discord App ID:
   ```json
   {
     "DiscordAppId": "123456789012345678",
     "Callsign": "MY-CALL",
     "LaunchDelayMs": 8000,
     "MsfsPaths": [
       "D:\\SteamLibrary\\steamapps\\common\\MicrosoftFlightSimulator\\FlightSimulator.exe",
       "C:\\XboxGames\\Microsoft Flight Simulator\\Content\\FlightSimulator.exe"
     ]
   }
   ```

3. Run `DogePilotLauncher.exe` — a plane icon appears in your system tray.

4. Right-click the tray icon → **Run at Windows Startup** to never think about it again.

5. Launch MSFS — SimAware starts automatically. Close MSFS — it stops automatically.

---

## Step 0 — Set Up Your Discord App

1. Go to https://discord.com/developers/applications
2. Click **New Application**, name it (e.g. `SimAware MSFS2020`)
3. Left sidebar → **Rich Presence → Art Assets** → upload image, name key `icon_large`
4. Copy your **Application ID** from **General Information**
5. Paste it as `DiscordAppId` in `config.json`

---

## config.json Reference

| Key | Default | Description |
|-----|---------|-------------|
| `DiscordAppId` | `"YOUR_DISCORD_APP_ID_HERE"` | Your Discord Application Client ID |
| `Callsign` | `""` | Your callsign (empty = random per session) |
| `LaunchDelayMs` | `8000` | Ms to wait after MSFS starts before launching SimAware |
| `MsfsPaths` | *(list)* | Paths to check for `FlightSimulator.exe` (not used directly — detection is by process name) |

> Note: MSFS detection works by process name (`FlightSimulator.exe`), so it works regardless of install location — the `MsfsPaths` list is just for your reference.

---

## Tray Icon Menu

| Option | What it does |
|--------|-------------|
| Launch SimAware now | Manually start SimAware without waiting for MSFS |
| Stop SimAware | Manually kill SimAware |
| Run at Windows Startup | Toggle auto-start via registry (`HKCU\...\Run`) |
| Exit Launcher | Stop watching and exit the launcher |

---

## How to Build

WPF does not compile on Linux. Use the included GitHub Actions workflow:

1. Push this repo to your GitHub account (after editing `config.json` with your App ID)
2. Go to **Actions** tab → `Build for Windows (MSFS2020)` → runs automatically on push
3. Click the run → **Artifacts** → download `DogePilot-MSFS2020-win-x64.zip`
4. Extract, put all files in one folder, run `DogePilotLauncher.exe`

**Manual trigger:** Actions → `Build for Windows (MSFS2020)` → **Run workflow**

---

> Requires .NET 8 SDK installed on the **Windows side** (not inside WSL).

---

## SimConnect Not Found?

Install it from the MSFS2020 SDK:
- In-sim: Options → General → Developers → SDK → Install
- Or run `SimConnect.msi` from the SDK folder

---

## Files Added in This Fork

| File | Description |
|------|-------------|
| `SimAware.Client/ProcessWatcher.cs` | Polls for `FlightSimulator.exe` every 3s |
| `SimAware.Client/TrayLauncher.cs` | System tray icon + auto-launch logic |
| `SimAware.Client/StartupHelper.cs` | Windows registry startup toggle |
| `SimAware.Client/LauncherConfig.cs` | Reads/writes `config.json` |
| `SimAware.Launcher/Program.cs` | Entry point for `DogePilotLauncher.exe` |
| `SimAware.Launcher/SimAware.Launcher.csproj` | Launcher project |
| `.github/workflows/build-windows.yml` | CI/CD: builds both exes on push |

---

## Discord Status Preview

| State | Discord Shows |
|-------|--------------|
| Preflight / connecting | `Preflight... \| MSFS2020` |
| In the air | `✈ Alt 35000ft \| 480kt \| MSFS2020` |
| On ground with location | `Near WSSS, Singapore` |

---

## License

Original project by Arvin Abdollahzadeh. See `LICENSE`.
