using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SimAware.Client
{
    /// <summary>
    /// Persists user configuration to config.json next to the executable.
    /// </summary>
    public class LauncherConfig
    {
        // Known MSFS install paths — user can add more in config.json
        public List<string> MsfsPaths { get; set; } = new List<string>
        {
            // Microsoft Store / Xbox App install
            @"C:\XboxGames\Microsoft Flight Simulator\Content\FlightSimulator.exe",
            // Steam install
            @"D:\SteamLibrary\steamapps\common\MicrosoftFlightSimulator\FlightSimulator.exe",
            // Steam install on C:
            @"C:\Program Files (x86)\Steam\steamapps\common\MicrosoftFlightSimulator\FlightSimulator.exe",
        };

        // Your Discord callsign — if empty, a random one is generated at runtime
        public string Callsign { get; set; } = "";

        // Your Discord Application Client ID
        // Get one at https://discord.com/developers/applications
        public string DiscordAppId { get; set; } = "YOUR_DISCORD_APP_ID_HERE";

        // How long (ms) to wait after MSFS launches before starting SimAware
        // MSFS takes a while to be ready for SimConnect
        public int LaunchDelayMs { get; set; } = 8000;

        // ── Persistence ──────────────────────────────────────────────────────────

        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static LauncherConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaults = new LauncherConfig();
                defaults.Save(); // write defaults so user can see the file
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<LauncherConfig>(json, JsonOptions)
                       ?? new LauncherConfig();
            }
            catch
            {
                return new LauncherConfig(); // fallback to defaults on corrupt file
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch { /* non-fatal */ }
        }
    }
}
