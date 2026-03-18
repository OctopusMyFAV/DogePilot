using DogePilot;
using System;
using SimAware.Client.Logic;
using SimAware.Client.SimConnectFSX;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DiscordRPC;
using System.Diagnostics;
using System.Windows.Interop;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.IO;
using DiscordRPC.Logging;

namespace SimAware.Client
{
    public partial class App : Application
    {
        #region Single Instance Enforcer

        readonly SingletonApplicationEnforcer enforcer = new SingletonApplicationEnforcer(args =>
        {
            Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Current.MainWindow as MainWindow;
                if (mainWindow != null && args != null)
                    mainWindow.RestoreWindow();
            });
        }, "DogePilot");

        #endregion

        public ServiceProvider ServiceProvider { get; private set; }

        private MainWindow mainWindow = null;
        private IntPtr Handle;

        // Loaded from config.json next to the exe
        public static LauncherConfig Config { get; private set; }

        // Watches FlightSimulator.exe — auto-exits SimAware when MSFS closes
        private ProcessWatcher _processWatcher;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!e.Args.Contains("--dev-instance") && enforcer.ShouldApplicationExit())
            {
                try { Shutdown(); } catch { }
                return;
            }

            // Load config.json (creates it with defaults on first run)
            Config = LauncherConfig.Load();

            // Watch for MSFS exit — close SimAware automatically
            _processWatcher = new ProcessWatcher();
            _processWatcher.SimulatorExited += (s, ev) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine("MSFS exited — shutting down SimAware.");
                    Shutdown();
                });
            };
            _processWatcher.Start();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Loaded += MainWindow_Loaded;
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IFlightConnector, MicrosoftSimConnection>();
            services.AddTransient(typeof(MainWindow));

            // Discord App ID is read from config.json
            // TODO: Set your Discord Application Client ID in config.json
            // Create your app at: https://discord.com/developers/applications
            // Then add a Rich Presence asset named "icon_large" under Art Assets
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
            var flightConnector = ServiceProvider.GetService<IFlightConnector>();
            if (flightConnector is MicrosoftSimConnection simConnect)
            {
                simConnect.Closed += SimConnect_Closed;

                Handle = new WindowInteropHelper(sender as Window).Handle;
                var HandleSource = HwndSource.FromHwnd(Handle);
                HandleSource.AddHook(simConnect.HandleSimConnectEvents);

                var viewModel = ServiceProvider.GetService<MainViewModel>();

                try
                {
                    await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
                }
                catch (BadImageFormatException)
                {
                    var result = MessageBox.Show(mainWindow,
                        @"SimConnect has not been detected. This is essential to connect to Microsoft Flight Simulator.

Do you want to install it now?
When installation is complete, please restart.",
                        "Missing Core Framework",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Path.Combine(Path.GetDirectoryName(
                                    System.Reflection.Assembly.GetExecutingAssembly().Location), "SimConnect.msi"),
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }

                    Shutdown(-1);
                }
            }
        }

        private async Task InitializeSimConnectAsync(MicrosoftSimConnection simConnect, MainViewModel viewModel)
        {
            while (true)
            {
                try
                {
                    viewModel.SimConnectionState = ConnectionState.Connecting;
                    simConnect.Initialize(Handle, slowMode: false);
                    viewModel.SimConnectionState = ConnectionState.Connected;
                    break;
                }
                catch (COMException)
                {
                    viewModel.SimConnectionState = ConnectionState.Failed;
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
        }

        private async void SimConnect_Closed(object sender, EventArgs e)
        {
            var simConnect = sender as MicrosoftSimConnection;
            var viewModel = ServiceProvider.GetService<MainViewModel>();
            viewModel.SimConnectionState = ConnectionState.Idle;
            await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
        }
    }
}
