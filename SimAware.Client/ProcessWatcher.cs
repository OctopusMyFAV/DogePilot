using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimAware.Client
{
    /// <summary>
    /// Watches for FlightSimulator.exe to start or stop.
    /// Raises events so the app can auto-launch or auto-exit.
    /// </summary>
    public class ProcessWatcher
    {
        public event EventHandler SimulatorStarted;
        public event EventHandler SimulatorExited;

        private const string ProcessName = "FlightSimulator"; // no .exe
        private readonly int PollIntervalMs = 3000;           // check every 3 seconds

        private CancellationTokenSource _cts;
        private bool _wasRunning = false;

        public bool IsSimulatorRunning =>
            Process.GetProcessesByName(ProcessName).Length > 0;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => WatchLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task WatchLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool isRunning = IsSimulatorRunning;

                if (isRunning && !_wasRunning)
                {
                    _wasRunning = true;
                    SimulatorStarted?.Invoke(this, EventArgs.Empty);
                }
                else if (!isRunning && _wasRunning)
                {
                    _wasRunning = false;
                    SimulatorExited?.Invoke(this, EventArgs.Empty);
                }

                try { await Task.Delay(PollIntervalMs, token); }
                catch (TaskCanceledException) { break; }
            }
        }
    }
}
