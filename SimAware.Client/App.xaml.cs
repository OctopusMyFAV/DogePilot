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
            // already running
        }, "DogePilot");
        #endregion

        public ServiceProvider? ServiceProvider { get; private set; }
        private MainWindow? mainWindow = null;
        private IntPtr Handle;
        private HwndSource? handleSource = null;

        public static LauncherConfig Config { get; private set; } = new LauncherConfig();
        private ProcessWatcher? _processWatcher;

        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dogepilot.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                Log("FATAL UnhandledException: " + ex.ExceptionObject);

            DispatcherUnhandledException += (s, ex) =>
            {
                Log("FATAL DispatcherException: " + ex.Exception?.Message);
                ex.Handled = true;
            };

            Log("====== DogePilot starting ======");

            if (!e.Args.Contains("--dev-instance") && enforcer.ShouldApplicationExit())
            {
                Log("Another instance already running, exiting.");
                try { Shutdown(); } catch { }
                return;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Config = LauncherConfig.Load();
            Log($"Config loaded. DiscordAppId={Config.DiscordAppId} Callsign={Config.Callsign}");

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
            mainWindow.Show();
            Log("MainWindow shown.");
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
            Handle = new WindowInteropHelper(mainWindow).Handle;
            Log($"HWND obtained: 0x{Handle:X}");

            if (Handle == IntPtr.Zero)
            {
                Log("ERROR: HWND is zero!");
                return;
            }

            var flightConnector = ServiceProvider?.GetService<IFlightConnector>();
            if (flightConnector is MicrosoftSimConnection simConnect)
            {
                simConnect.Closed += SimConnect_Closed;

                var viewModel = ServiceProvider?.GetService<MainViewModel>();

                try
                {
                    Log("Starting SimConnect connection loop...");
                    await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);

                    // Only register the hook AFTER SimConnect is successfully initialized
                    // This prevents BadImageFormatException from premature Win32 message processing
                    Log("Registering HwndSource hook for SimConnect messages...");
                    handleSource = HwndSource.FromHwnd(Handle);
                    if (handleSource != null)
                    {
                        handleSource.AddHook(simConnect.HandleSimConnectEvents);
                        Log("HwndSource hook registered.");
                    }
                    else
                    {
                        Log("WARNING: HwndSource.FromHwnd returned null.");
                    }
                }
                catch (BadImageFormatException ex)
                {
                    Log($"BAD IMAGE FORMAT: {ex.Message}");
                    MessageBox.Show(
                        "SimConnect DLL version mismatch.\n\n" +
                        "Copy this file to your DogePilot folder:\n" +
                        "C:\\MSFS SDK\\SimConnect SDK\\lib\\managed\\Microsoft.FlightSimulator.SimConnect.dll\n\n" +
                        "And also:\n" +
                        "C:\\MSFS SDK\\SimConnect SDK\\lib\\SimConnect.dll\n\n" +
                        "Check dogepilot.log for details.",
                        "DogePilot - DLL Mismatch",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    Log($"UNEXPECTED ERROR: {ex}");
                }
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
                    Log($"SimConnect attempt #{attempt}...");
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Connecting;
                    simConnect.Initialize(Handle, slowMode: false);
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Connected;
                    Log($"SimConnect connected on attempt #{attempt}.");
                    break;
                }
                catch (COMException ex)
                {
                    Log($"SimConnect attempt #{attempt} failed (MSFS not ready): 0x{ex.HResult:X8} - retrying in 5s...");
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Failed;
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
        }

        private async void SimConnect_Closed(object? sender, EventArgs e)
        {
            Log("SimConnect closed - reconnecting...");

            // Remove old hook before reconnecting
            handleSource?.RemoveHook((sender as MicrosoftSimConnection)!.HandleSimConnectEvents);

            var simConnect = sender as MicrosoftSimConnection;
            var viewModel = ServiceProvider?.GetService<MainViewModel>();
            if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Idle;

            if (simConnect != null)
            {
                await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);

                // Re-register hook after reconnect
                if (handleSource != null)
                {
                    handleSource.AddHook(simConnect.HandleSimConnectEvents);
                    Log("HwndSource hook re-registered after reconnect.");
                }
            }
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
