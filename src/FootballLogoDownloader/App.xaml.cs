using System.Windows;

namespace FootballLogoDownloader;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Defense-in-depth: any regex created without an explicit timeout elsewhere in the app
        // inherits a finite timeout instead of running indefinitely on hostile input.
        AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));
        base.OnStartup(e);
    }
}
