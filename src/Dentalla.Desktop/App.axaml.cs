using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.Views;

namespace Dentalla.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        Program.Trace("App.Initialize entered.");
        AvaloniaXamlLoader.Load(this);
        Program.Trace("App.Initialize completed.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Program.Trace($"OnFrameworkInitializationCompleted entered. Lifetime={ApplicationLifetime?.GetType().FullName ?? "<null>"}.");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Program.Trace("Creating LoginWindow.");
            desktop.MainWindow = new LoginWindow();
            Program.Trace("LoginWindow assigned as MainWindow.");
        }
        else
        {
            Program.Trace("Classic desktop lifetime was not available; no MainWindow was created.");
        }

        base.OnFrameworkInitializationCompleted();
        Program.Trace("OnFrameworkInitializationCompleted completed.");
    }
}
