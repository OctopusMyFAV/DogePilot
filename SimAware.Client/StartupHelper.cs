using Microsoft.Win32;
using System.Diagnostics;

namespace DogePilot
{
    public static class StartupHelper
    {
        private const string AppName = "DogePilotLauncher";
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsRegistered()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch { return false; }
        }

        public static void Register()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath == null) return;
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            catch { }
        }

        public static void Remove()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.DeleteValue(AppName, throwOnMissingValue: false);
            }
            catch { }
        }
    }
}
