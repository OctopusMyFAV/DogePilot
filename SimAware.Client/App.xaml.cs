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
            // already running, do nothing - tray app
        }, "DogePilot");
        #endregion

        public ServiceProvider? ServiceProvider { get; private set; }

        private MainWindow? mainWindow = null;
        private IntPtr Handle;

        public static LauncherConfig Config { get; private set; } = new LauncherConfig();
        private ProcessWatcher? _processWatcher;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!e.Args.Contains("--dev-instance") && enforcer.ShouldApplicationExit())
            {
                try { Shutdown(); } catch { }
                return;
            }

            // Keep app alive even if all windows are closed (tray app)
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Config = LauncherConfig.Load();

            // Shut down when MSFS closes
            _processWatcher = new ProcessWatcher();
            _processWatcher.SimulatorExited += (s, ev) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine("MSFS exited - shutting down DogePilot.");
                    Shutdown();
                });
            };
            _processWatcher.Start();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Loaded += MainWindow_Loaded;

            // Don't call Show() - window stays hidden, we just need its HWND for SimConnect
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IFlightConnector, MicrosoftSimConnection>();
            services.AddTransient(typeof(MainWindow));

            var discordRpcClient = new DiscordRpcClient(Config.DiscordAppId);
            discordRpcClient.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            discordRpcClient.OnReady += (sender, e) =>
                Debug.WriteLine("Connected to Discord RPC");
            discordRpcClient.OnPresenceUpdate += (sender, e) =>
                Debug.WriteLine($"Presence Updated {e.Presence}");

            services.AddSingleton(discordRpcClient);
            services.AddSingleton<DiscordRichPresenceLogic>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _processWatcher?.Stop();
            base.OnExit(e);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var flightConnector = ServiceProvider?.GetService<IFlightConnector>();
            if (flightConnector is MicrosoftSimConnection simConnect)
            {
                simConnect.Closed += SimConnect_Closed;

                Handle = new WindowInteropHelper(mainWindow).Handle;
                var handleSource = HwndSource.FromHwnd(Handle);
                handleSource?.AddHook(simConnect.HandleSimConnectEvents);

                var viewModel = ServiceProvider?.GetService<MainViewModel>();

                try
                {
                    await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
                }
                catch (BadImageFormatException)
                {
                    // SimConnect DLL missing - notify but DON'T shutdown, stay in tray
                    MessageBox.Show(
                        "SimConnect was not found.\n\nPlease install it from MSFS2020:\nOptions > General > Developers > SDK > Install\n\nThen restart DogePilot.",
                        "DogePilot - SimConnect Missing",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    // Don't shutdown - user can fix it and restart manually from tray
                }
            }
        }

        private async Task InitializeSimConnectAsync(MicrosoftSimConnection simConnect, MainViewModel? viewModel)
        {
            while (true)
            {
                try
                {
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Connecting;
                    simConnect.Initialize(Handle, slowMode: false);
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Connected;
                    Debug.WriteLine("SimConnect connected.");
                    break;
                }
                catch (COMException)
                {
                    // MSFS not ready yet - keep retrying silently every 5 seconds
                    if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Failed;
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
        }

        private async void SimConnect_Closed(object? sender, EventArgs e)
        {
            var simConnect = sender as MicrosoftSimConnection;
            var viewModel = ServiceProvider?.GetService<MainViewModel>();
            if (viewModel != null) viewModel.SimConnectionState = ConnectionState.Idle;
            if (simConnect != null)
                await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
        }
    }
}
