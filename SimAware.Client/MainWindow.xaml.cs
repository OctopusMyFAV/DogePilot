using SimAware.Client.Logic;
using SimAware.Client.SimConnectFSX;
using System;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SimAware.Client
{
    public partial class MainWindow : Window
    {
        private readonly Random random = new Random();
        private readonly MainViewModel viewModel;
        private readonly DiscordRichPresenceLogic discordRichPresenceLogic;
        private readonly IFlightConnector flightConnector;

        public MainWindow(IFlightConnector flightConnector, MainViewModel viewModel, DiscordRichPresenceLogic discordRichPresenceLogic)
        {
            InitializeComponent();
            this.flightConnector = flightConnector;
            this.viewModel = viewModel;
            this.discordRichPresenceLogic = discordRichPresenceLogic;
        }

        public void RestoreWindow()
        {
            // tray-only app, nothing to restore
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.Log("MainWindow_Loaded - initializing Discord RPC...");

            var callsign = !string.IsNullOrWhiteSpace(App.Config?.Callsign)
                ? App.Config.Callsign
                : GenerateCallSign();

            viewModel.Callsign = callsign;
            App.Log($"Using callsign: {callsign}");

            discordRichPresenceLogic.Initialize();
            discordRichPresenceLogic.Start(callsign);
            App.Log("Discord RPC initialized and started.");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { }

        private string GenerateCallSign()
        {
            var b = new StringBuilder();
            b.Append((char)('A' + random.Next(26)));
            b.Append((char)('A' + random.Next(26)));
            b.Append('-');
            b.Append((char)('A' + random.Next(26)));
            b.Append((char)('A' + random.Next(26)));
            b.Append((char)('A' + random.Next(26)));
            return b.ToString();
        }
    }
}
