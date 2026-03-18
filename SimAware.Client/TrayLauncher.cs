using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DogePilot
{
    public class TrayLauncher : IDisposable
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ProcessWatcher _watcher;
        private readonly LauncherConfig _config;

        private Process? _simAwareProcess = null;
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
            if (_watcher.IsSimulatorRunning)
            {
                SetStatus("MSFS detected — launching DogePilot...");
                _ = LaunchClientAsync();
            }
            else
            {
                SetStatus("DogePilot — Waiting for MSFS...");
            }

            _watcher.Start();
            Application.Run();
        }

        private async void OnSimulatorStarted(object? sender, EventArgs e)
        {
            SetStatus($"MSFS started — launching in {_config.LaunchDelayMs / 1000}s...");
            await Task.Delay(_config.LaunchDelayMs);
            await LaunchClientAsync();
        }

        private void OnSimulatorExited(object? sender, EventArgs e)
        {
            SetStatus("MSFS closed — stopping DogePilot...");
            KillClient();
            SetStatus("DogePilot — Waiting for MSFS...");
        }

        private async Task LaunchClientAsync()
        {
            if (_simAwareProcess != null && !_simAwareProcess.HasExited)
                return;

            var exe = GetClientExePath();
            if (exe == null)
            {
                SetStatus("ERROR: DogePilot.Client.exe not found!");
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
                        WorkingDirectory = Path.GetDirectoryName(exe) ?? ""
                    },
                    EnableRaisingEvents = true
                };

                _simAwareProcess.Exited += (s, e) => { _simAwareProcess = null; };
                _simAwareProcess.Start();

                SetStatus("DogePilot is running ✓");
                ShowBalloon("DogePilot", "Discord Rich Presence started for MSFS2020.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                SetStatus("Failed to launch: " + ex.Message);
            }

            await Task.CompletedTask;
        }

        private void KillClient()
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
            catch { }
            finally { _simAwareProcess = null; }
        }

        private static string? GetClientExePath()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var exe = Path.Combine(dir, "DogePilot.Client.exe");
            return File.Exists(exe) ? exe : null;
        }

        private void SetStatus(string status)
        {
            var text = "DogePilot: " + status;
            _trayIcon.Text = text.Length > 63 ? text[..63] : text;
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            _trayIcon.ShowBalloonTip(4000, title, text, icon);
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add(new ToolStripMenuItem("DogePilot Launcher") { Enabled = false });
            menu.Items.Add(new ToolStripSeparator());

            var launchItem = new ToolStripMenuItem("Launch DogePilot now");
            launchItem.Click += async (s, e) => await LaunchClientAsync();
            menu.Items.Add(launchItem);

            var stopItem = new ToolStripMenuItem("Stop DogePilot");
            stopItem.Click += (s, e) => KillClient();
            menu.Items.Add(stopItem);

            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Run at Windows Startup")
            {
                Checked = StartupHelper.IsRegistered()
            };
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
                KillClient();
                _watcher.Stop();
                Application.Exit();
            };
            menu.Items.Add(exitItem);

            return menu;
        }

        private static Icon LoadIcon()
        {
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
                _trayIcon.Dispose();
            }
        }
    }
}
