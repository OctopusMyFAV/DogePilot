using Microsoft.Win32;
using System;
using System.Reflection;

namespace SimAware.Client
{
    /// <summary>
    /// Registers or removes the launcher from the Windows startup registry key
    /// so it runs automatically when the user logs in.
    /// </summary>
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
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath == null) return;

                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            catch { /* non-fatal */ }
        }

        public static void Remove()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.DeleteValue(AppName, throwOnMissingValue: false);
            }
            catch { /* non-fatal */ }
        }
    }
}
