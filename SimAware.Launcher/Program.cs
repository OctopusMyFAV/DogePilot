using System.Windows.Forms;
using DogePilot;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var config = LauncherConfig.Load();
using var launcher = new TrayLauncher(config);
launcher.Run();
