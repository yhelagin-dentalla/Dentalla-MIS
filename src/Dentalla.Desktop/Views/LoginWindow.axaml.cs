using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.Models;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginWindowViewModel _viewModel = new();

    public LoginWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await _viewModel.LoadAsync();
    }

    private async void OnReloadClick(object? sender, RoutedEventArgs e)
        => await _viewModel.LoadAsync();

    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanContinue)
            return;

        var session = await _viewModel.CreateSessionAsync();
        if (session is null)
            return;

        DesktopSessionStore.Current = session;
        var mainWindow = new MainWindow(session);

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = mainWindow;

        mainWindow.Show();
        Close();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
