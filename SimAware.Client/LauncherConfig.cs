using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SimAware.Client
{
    public class LauncherConfig
    {
        public List<string> MsfsPaths { get; set; } = new List<string>
        {
            @"C:\XboxGames\Microsoft Flight Simulator\Content\FlightSimulator.exe",
            @"D:\SteamLibrary\steamapps\common\MicrosoftFlightSimulator\FlightSimulator.exe",
            @"C:\Program Files (x86)\Steam\steamapps\common\MicrosoftFlightSimulator\FlightSimulator.exe",
        };

        public string Callsign { get; set; } = "";
        public string DiscordAppId { get; set; } = "YOUR_DISCORD_APP_ID_HERE";
        public int LaunchDelayMs { get; set; } = 8000;

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
                defaults.Save();
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
                return new LauncherConfig();
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}
