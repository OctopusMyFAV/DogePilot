using System;
using System.Windows.Forms;

namespace SimAware.Launcher
{
    /// <summary>
    /// Entry point for SimAwareLauncher.exe
    /// 
    /// This is a tiny WinForms app (no window) that:
    ///   1. Sits in the system tray silently
    ///   2. Watches for FlightSimulator.exe every 3 seconds
    ///   3. Launches SimAware.Client.exe when MSFS starts
    ///   4. Kills SimAware.Client.exe when MSFS closes
    ///   5. Optionally registers itself to run at Windows startup
    /// 
    /// Put SimAwareLauncher.exe in the same folder as SimAware.Client.exe
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var config = LauncherConfig.Load();

            using var launcher = new TrayLauncher(config);
            launcher.Run();
        }
    }
}
