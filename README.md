<div align="center">

<img src="dogepilot.png" width="180"/>

# 🐕✈️ DogePilot

### such flight. very discord. wow.

**Auto Discord Rich Presence for Microsoft Flight Simulator 2020**
*by soupfhaii_*

[![Build Status](https://github.com/OctopusMyFAV/DogePilot/actions/workflows/build-windows.yml/badge.svg)](https://github.com/OctopusMyFAV/DogePilot/actions)

</div>

---

## 🐾 wow. what is this?

> very smart plugin. much automatic. no click needed. wow.

**DogePilot** sits in your system tray and watches for MSFS2020.
When it sees `FlightSimulator.exe` — it launches itself. When MSFS closes — it stops itself.
You do nothing. Very lazy. Much perfect. 🐕

---

## ✈️ such features. very wow.

```
🐕  Auto-launches when MSFS2020 starts
🐕  Auto-closes when MSFS2020 stops
🐕  Shows your altitude, speed & location on Discord
🐕  Doge pilot small image on Discord RPC (obviously)
🐕  Runs at Windows startup (one click)
🐕  Lives in system tray, stays out of your way
```

---

## 📦 how install. much easy.

### Step 1 — Get the build

1. Go to the [**Actions tab**](../../actions)
2. Click the latest green ✅ run
3. Scroll down → **Artifacts** → download **`DogePilot-MSFS2020-win-x64.zip`**
4. Extract to a folder on your Windows machine

Make sure these are all together:
```
📁 DogePilot/
 ├── DogePilotLauncher.exe   ← run this one 🐕
 ├── DogePilot.Client.exe
 ├── SimConnect.dll
 ├── dogepilot.png
 ├── icon.ico
 └── config.json             ← auto-created on first run
```

---

### Step 2 — Discord App ID. very important. wow.

1. Go to 👉 https://discord.com/developers/applications
2. Click **New Application** → name it `DogePilot` (or whatever)
3. Sidebar → **Rich Presence → Art Assets**
   - Upload your image → name it `icon_large`
   - Upload `dogepilot.png` → name it `dogepilot`
4. Copy your **Application ID** from **General Information**
5. Open `config.json` and paste it:

```json
{
  "DiscordAppId": "123456789012345678",
  "Callsign": "DOGE1",
  "LaunchDelayMs": 8000
}
```

---

### Step 3 — such run. very launch.

1. Run **`DogePilotLauncher.exe`**
2. Doge appears in your system tray 🐕
3. Right-click → **Run at Windows Startup**
4. Never think about it again

> much set and forget. very wow.

---

## 🎮 Discord will show this. very flex.

| When | Discord Says |
|------|-------------|
| Loading / preflight | `✈️⛅ Doing Pre-flights on MSFS2020` |
| Flying | `✈ Flying at 35000ft 480kts On MSFS2020` |
| On ground | `On the ground eating Nasgor ⌛✈️🧑‍✈️` |
| Near airport | `🧑‍✈️✈️ Flying Near WSSS, Singapore` |

*Small doge pilot image shown on all states. much style.*

---

## 🔧 tray icon menu. such options.

| Option | Does what |
|--------|-----------|
| Launch DogePilot now | Force launch without waiting for MSFS |
| Stop DogePilot | Kill it manually |
| Run at Windows Startup | Toggle auto-start (uses registry) |
| Exit Launcher | Stop everything |

---

## 🏗️ how to build. for nerds only.

### Push to GitHub → Actions builds it automatically

```bash
git add .
git commit -m "wow such commit"
git push
```

Then grab the artifact from the **Actions** tab. That's it.
No installing dotnet. No compiling. Much easy. Very lazy. 🐕

---

## ❓ SimConnect not found?

Install it from inside MSFS2020:

```
Options → General → Developers → SDK → Install
```

Then restart DogePilot.

---

## 📜 credits

- Original project by **Arvin Abdollahzadeh** (SimAware)
- Doge-ified & auto-launcher by **soupfhaii_**
- Doge image: classic shibe in pilot uniform 🐕✈️

---

<div align="center">

*such open source. very MIT. wow.*

<img src="dogepilot.png" width="80"/>

**🐕 much fly. very discord. wow. 🐕**

</div>
