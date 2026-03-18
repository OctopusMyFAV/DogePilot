using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimAware.Client
{
    /// <summary>
    /// System tray application that:
    ///   - Runs silently in the background at Windows startup
    ///   - Watches for FlightSimulator.exe
    ///   - Auto-launches SimAware.Client when MSFS starts
    ///   - Auto-closes SimAware.Client when MSFS exits
    /// </summary>
    public class TrayLauncher : IDisposable
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ProcessWatcher _watcher;
        private readonly LauncherConfig _config;

        private Process _simAwareProcess = null;
        private bool _disposed = false;

        public TrayLauncher(LauncherConfig config)
        {
            _config = config;
            _watcher = new ProcessWatcher();

            _trayIcon = new NotifyIcon
            {
                Icon = LoadIcon(),
                Text = "DogePilot — Waiting for MSFS...",
                Visible = true,
                ContextMenuStrip = BuildContextMenu()
            };

            _watcher.SimulatorStarted += OnSimulatorStarted;
            _watcher.SimulatorExited  += OnSimulatorExited;
        }

        public void Run()
        {
            // If MSFS is already running when we start, launch immediately
            if (_watcher.IsSimulatorRunning)
            {
                SetStatus("MSFS detected — launching SimAware...");
                _ = LaunchSimAwareAsync();
            }
            else
            {
                SetStatus("DogePilot — Waiting for MSFS...");
            }

            _watcher.Start();
            Application.Run(); // WinForms message loop for the tray icon
        }

        // ── Watcher callbacks ─────────────────────────────────────────────────

        private async void OnSimulatorStarted(object sender, EventArgs e)
        {
            SetStatus($"MSFS started — launching SimAware in {_config.LaunchDelayMs / 1000}s...");
            await Task.Delay(_config.LaunchDelayMs); // wait for MSFS to be ready
            await LaunchSimAwareAsync();
        }

        private void OnSimulatorExited(object sender, EventArgs e)
        {
            SetStatus("MSFS closed — stopping SimAware...");
            KillSimAware();
            SetStatus("DogePilot — Waiting for MSFS...");
        }

        // ── SimAware process management ───────────────────────────────────────

        private async Task LaunchSimAwareAsync()
        {
            // Don't double-launch
            if (_simAwareProcess != null && !_simAwareProcess.HasExited)
                return;

            var exe = GetSimAwareExePath();
            if (exe == null)
            {
                SetStatus("ERROR: DogePilot.Client.exe not found next to launcher!");
                ShowBalloon("DogePilot Launcher", "Could not find DogePilot.Client.exe — check your install folder.", ToolTipIcon.Error);
                return;
            }

            try
            {
                _simAwareProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(exe)
                    },
                    EnableRaisingEvents = true
                };

                _simAwareProcess.Exited += (s, e) =>
                {
                    // SimAware closed on its own — that's fine, just clear the handle
                    _simAwareProcess = null;
                };

                _simAwareProcess.Start();
                SetStatus("SimAware is running ✓");
                ShowBalloon("DogePilot", "Discord Rich Presence started for MSFS2020.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                SetStatus("Failed to launch SimAware: " + ex.Message);
            }
        }

        private void KillSimAware()
        {
            try
            {
                if (_simAwareProcess != null && !_simAwareProcess.HasExited)
                {
                    _simAwareProcess.CloseMainWindow();
                    if (!_simAwareProcess.WaitForExit(3000))
                        _simAwareProcess.Kill();
                }
            }
            catch { /* process may have already exited */ }
            finally
            {
                _simAwareProcess = null;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GetSimAwareExePath()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var exe = Path.Combine(dir, "DogePilot.Client.exe");
            return File.Exists(exe) ? exe : null;
        }

        private void SetStatus(string status)
        {
            if (_trayIcon != null)
                _trayIcon.Text = "SimAware: " + (status.Length > 60 ? status[..60] : status);
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            _trayIcon?.ShowBalloonTip(4000, title, text, icon);
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var statusItem = new ToolStripMenuItem("DogePilot Launcher") { Enabled = false };
            menu.Items.Add(statusItem);
            menu.Items.Add(new ToolStripSeparator());

            var launchItem = new ToolStripMenuItem("Launch SimAware now");
            launchItem.Click += async (s, e) => await LaunchSimAwareAsync();
            menu.Items.Add(launchItem);

            var stopItem = new ToolStripMenuItem("Stop SimAware");
            stopItem.Click += (s, e) => KillSimAware();
            menu.Items.Add(stopItem);

            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Run at Windows Startup");
            startupItem.Checked = StartupHelper.IsRegistered();
            startupItem.Click += (s, e) =>
            {
                if (StartupHelper.IsRegistered())
                {
                    StartupHelper.Remove();
                    startupItem.Checked = false;
                    ShowBalloon("DogePilot Launcher", "Removed from Windows startup.", ToolTipIcon.Info);
                }
                else
                {
                    StartupHelper.Register();
                    startupItem.Checked = true;
                    ShowBalloon("DogePilot Launcher", "Will now auto-start with Windows.", ToolTipIcon.Info);
                }
            };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit Launcher");
            exitItem.Click += (s, e) =>
            {
                KillSimAware();
                _watcher.Stop();
                Application.Exit();
            };
            menu.Items.Add(exitItem);

            return menu;
        }

        private static Icon LoadIcon()
        {
            // Try to load the app icon, fall back to a system icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath)) return new Icon(iconPath);
            }
            catch { }
            return SystemIcons.Application;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _watcher.Stop();
                _trayIcon?.Dispose();
            }
        }
    }
}
