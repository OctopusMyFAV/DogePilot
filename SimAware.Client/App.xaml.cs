using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using DiscordRPC;
using DiscordRPC.Logging;
using Microsoft.Extensions.DependencyInjection;
using SimAware.Client.Logic;
using SimAware.Client.SimConnectFSX;

namespace SimAware.Client
{
    public partial class App : Application
    {
        #region Single Instance Enforcer
        readonly SingletonApplicationEnforcer enforcer = new SingletonApplicationEnforcer(args =>
        {
            // already running, do nothing
        }, "DogePilot");
        #endregion

        public ServiceProvider? ServiceProvider { get; private set; }
        private MainWindow? mainWindow = null;
        private IntPtr Handle;

        public static LauncherConfig Config { get; private set; } = new LauncherConfig();
        private ProcessWatcher? _processWatcher;

        // Log file sits next to the exe
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dogepilot.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            // Catch everything and write to log instead of silently dying
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                Log("FATAL UnhandledException: " + ex.ExceptionObject);

            DispatcherUnhandledException += (s, ex) =>
            {
                Log("FATAL DispatcherException: " + ex.Exception);
                ex.Handled = true;
            };

            Log("====== DogePilot starting ======");

            if (!e.Args.Contains("--dev-instance") && enforcer.ShouldApplicationExit())
            {
                Log("Another instance already running, exiting.");
                try { Shutdown(); } catch { }
                return;
            }

            // Keep alive even when window is hidden - tray app
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Config = LauncherConfig.Load();
            Log($"Config loaded. DiscordAppId={Config.DiscordAppId} Callsign={Config.Callsign}");

            // Shut down when MSFS closes
            _processWatcher = new ProcessWatcher();
            _processWatcher.SimulatorExited += (s, ev) =>
            {
                Log("MSFS exited - shutting down DogePilot.");
                Dispatcher.Invoke(() => Shutdown());
            };
            _processWatcher.Start();
            Log("ProcessWatcher started.");

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            Log("DI container built.");

            mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Loaded += MainWindow_Loaded;

            // Must call Show() so Windows creates the HWND and message pump
            // The window is transparent + offscreen so user never sees it
            // Per SimConnect SDK: a valid Win32 HWND is required for WM_USER messages
            mainWindow.Show();
            Log("MainWindow.Show() called - window is transparent/offscreen.");
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IFlightConnector, MicrosoftSimConnection>();
            services.AddTransient(typeof(MainWindow));

            var discordRpcClient = new DiscordRpcClient(Config.DiscordAppId);
            discordRpcClient.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            discordRpcClient.OnReady += (sender, ev) => Log("Discord RPC ready.");
            discordRpcClient.OnPresenceUpdate += (sender, ev) => Log("Discord presence updated.");
            discordRpcClient.OnError += (sender, ev) => Log($"Discord RPC error: {ev.Message}");

            services.AddSingleton(discordRpcClient);
            services.AddSingleton<DiscordRichPresenceLogic>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log("DogePilot exiting.");
            _processWatcher?.Stop();
            base.OnExit(e);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the HWND - must be done after window is loaded per SimConnect SDK
            Handle = new WindowInteropHelper(mainWindow).Handle;
            Log($"HWND obtained: 0x{Handle:X} (should not be 0)");

            if (Handle == IntPtr.Zero)
            {
                Log("ERROR: HWND is zero! SimConnect cannot work without a valid window handle.");
                return;
            }

            var flightConnector = ServiceProvider?.GetService<IFlightConnector>();
            if (flightConnector is MicrosoftSimConnection simConnect)
            {
                simConnect.Closed += SimConnect_Closed;

                // Hook SimConnect Win32 messages into our window's message pump
                // Per SDK: HwndSource.AddHook is the correct way to do this in WPF
                var handleSource = HwndSource.FromHwnd(Handle);
                if (handleSource != null)
                {
                    handleSource.AddHook(simConnect.HandleSimConnectEvents);
                    Log("HwndSource hook registered for SimConnect messages.");
                }
                else
                {
                    Log("WARNING: HwndSource.FromHwnd returned null - SimConnect messages may not work.");
                }

                var viewModel = ServiceProvider?.GetService<MainViewModel>();

                try
                {
                    Log("Starting SimConnect connection loop...");
                    await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
                }
                catch (BadImageFormatException ex)
                {
                    Log($"BAD IMAGE FORMAT: SimConnect DLL missing or wrong architecture. {ex.Message}");
                    Log("Fix: Install SimConnect from MSFS2020 > Options > General > Developers > SDK > Install");
                    MessageBox.Show(
                        "SimConnect DLL was not found or is the wrong version.\n\n" +
                        "To fix this:\n" +
                        "1. Open MSFS2020\n" +
                        "2. Go to Options > General > Developers\n" +
                        "3. Install the SDK\n\n" +
                        "Then restart DogePilot.\n\n" +
                        "Check dogepilot.log next to the exe for full details.",
                        "DogePilot - SimConnect Missing",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    Log($"UNEXPECTED ERROR in MainWindow_Loaded: {ex}");
                }
            }
            else
            {
                Log("ERROR: Could not get MicrosoftSimConnection from DI container.");
            }
        }

        private async Task InitializeSimConnectAsync(MicrosoftSimConnection simConnect, MainViewModel? viewModel)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    Log($"SimConnect connection attempt #{attempt}...");
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Connecting;

                    // Per SimConnect SDK: constructor throws COMException if MSFS is not running
                    // This is expected - we just retry every 5 seconds
                    simConnect.Initialize(Handle, slowMode: false);

                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Connected;
                    Log($"SimConnect connected successfully on attempt #{attempt}.");
                    break;
                }
                catch (COMException ex)
                {
                    // 0x80004005 = E_FAIL = MSFS not running yet, normal - just wait and retry
                    Log($"SimConnect attempt #{attempt} failed (MSFS not ready): 0x{ex.HResult:X8} - retrying in 5s...");
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Failed;
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
        }

        private async void SimConnect_Closed(object? sender, EventArgs e)
        {
            Log("SimConnect connection closed - reconnecting...");
            var simConnect = sender as MicrosoftSimConnection;
            var viewModel = ServiceProvider?.GetService<MainViewModel>();
            if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Idle;
            if (simConnect != null)
                await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
        }

        public static void Log(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                Debug.WriteLine(line);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
