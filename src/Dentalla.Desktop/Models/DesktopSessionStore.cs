namespace Dentalla.Desktop.Models;

public static class DesktopSessionStore
{
    public static DesktopSessionContext? Current { get; set; }

    public static string? AccessToken => Current?.AccessToken;
}
