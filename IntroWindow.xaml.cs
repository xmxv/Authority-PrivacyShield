using System.Windows;
using System.Windows.Input;

namespace PrivacyShield;

public partial class IntroWindow : Window
{
    public IntroWindow()
    {
        InitializeComponent();
    }

    private void Drag_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); } catch { }
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (chkDontShow.IsChecked == true)
        {
            var settings = AppSettings.Load();
            settings.SkipIntro = true;
            settings.Save();
        }

        new MainWindow().Show();
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
