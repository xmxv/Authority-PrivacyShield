using System.Windows;

namespace PrivacyShield;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (AppSettings.Load().SkipIntro)
            new MainWindow().Show();
        else
            new IntroWindow().Show();
    }
}
