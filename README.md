<div align="center">

<img src="dogepilot.png" width="180"/>

# DogePilot

**Discord Rich Presence for Microsoft Flight Simulator 2020**

*made by soupfhaii_*

[![Build Status](https://github.com/OctopusMyFAV/DogePilot/actions/workflows/build-windows.yml/badge.svg)](https://github.com/OctopusMyFAV/DogePilot/actions)

</div>

---

## What is this?

DogePilot is a background app that connects to MSFS2020 and updates your Discord status while you fly. It watches for `FlightSimulator.exe` to start, launches itself automatically, and shuts itself down when you close the sim. You don't have to touch anything.

---

## Features

- Launches automatically when MSFS2020 starts
- Closes automatically when MSFS2020 stops
- Shows your altitude, speed and location on Discord
- Doge pilot shows as the small image on your Discord status
- Can run at Windows startup so you never have to think about it
- Sits in the system tray and stays out of your way

---

## Installation

### Step 1 - Download the build

1. Go to the [Actions tab](../../actions)
2. Click the latest green checkmark run
3. Scroll down to **Artifacts** and download **`DogePilot-MSFS2020-win-x64.zip`**
4. Extract it to a folder on your Windows machine

Your folder should look like this:

```
DogePilot/
 ├── DogePilotLauncher.exe   <-- run this one
 ├── DogePilot.Client.exe
 ├── SimConnect.dll
 ├── dogepilot.png
 ├── icon.ico
 └── config.json             <-- gets created on first run
```

---

### Step 2 - Set up your Discord App ID

1. Go to https://discord.com/developers/applications
2. Click **New Application** and name it whatever you want
3. Go to **Rich Presence > Art Assets** in the sidebar
   - Upload an image and name it `icon_large`
   - Upload `dogepilot.png` and name it `dogepilot`
4. Copy your **Application ID** from the General Information page
5. Open `config.json` and paste it in:

```json
{
  "DiscordAppId": "123456789012345678",
  "Callsign": "DOGE1",
  "LaunchDelayMs": 8000
}
```

---

### Step 3 - Run it

1. Run **`DogePilotLauncher.exe`**
2. A tray icon appears in your taskbar
3. Right-click it and hit **Run at Windows Startup**
4. Done. It handles everything from here.

---

## What your Discord status looks like

| Situation | Status |
|-----------|--------|
| Loading / preflight | `✈️ Doing Pre-flights on MSFS2020` |
| In the air | `✈ Flying at 35000ft 480kts On MSFS2020` |
| On the ground | `On the ground eating Nasgor` |
| Near an airport | `Flying Near WSSS, Singapore` |

The doge pilot image shows up as the small icon on all of them.

---

## Tray icon options

| Option | What it does |
|--------|--------------|
| Launch DogePilot now | Start it without waiting for MSFS |
| Stop DogePilot | Kill it manually |
| Run at Windows Startup | Toggle auto-start on login |
| Exit Launcher | Stop the launcher completely |

---

## Building it yourself

Just push to GitHub and Actions will build it for you:

```bash
git add .
git commit -m "update"
git push
```

Then grab the zip from the **Actions** tab under Artifacts. No need to install anything locally.

---

## SimConnect not found?

Install it from inside MSFS2020:

```
Options > General > Developers > SDK > Install
```

Restart DogePilot after installing.

---

## Credits

- Original project by **Arvin Abdollahzadeh** (SimAware)
- Modified and launcher added by **soupfhaii_**

---

<div align="center">
<img src="dogepilot.png" width="80"/>
</div>
