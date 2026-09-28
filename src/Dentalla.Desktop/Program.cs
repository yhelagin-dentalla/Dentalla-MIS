using System.Text;
using Avalonia;
using Avalonia.Fonts.Inter;

namespace Dentalla.Desktop;

internal static class Program
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DentallaMIS",
        "Logs");

    private static readonly string StartupLogPath = Path.Combine(LogDirectory, "startup.log");

    [STAThread]
    public static int Main(string[] args)
    {
        Directory.CreateDirectory(LogDirectory);
        Trace("Process entered Main.");
        Trace($"BaseDirectory: {AppContext.BaseDirectory}");
        Trace($"CurrentDirectory: {Environment.CurrentDirectory}");
        Trace($"OS: {Environment.OSVersion}");
        Trace($"Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            Trace("AppDomain.UnhandledException: " + eventArgs.ExceptionObject);

        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            Trace("TaskScheduler.UnobservedTaskException: " + eventArgs.Exception);
            eventArgs.SetObserved();
        };

        try
        {
            Trace("Building Avalonia app.");
            var app = BuildAvaloniaApp();
            Trace("Starting classic desktop lifetime.");
            var exitCode = app.StartWithClassicDesktopLifetime(args);
            Trace($"Desktop lifetime returned. ExitCode={exitCode}.");
            return exitCode;
        }
        catch (Exception ex)
        {
            Trace("FATAL startup exception: " + ex);
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    internal static void Trace(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(
                StartupLogPath,
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            // Startup diagnostics must never become another startup failure.
        }
    }
}
