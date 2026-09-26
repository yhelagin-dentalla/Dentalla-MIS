using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.Models;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(DesktopSessionContext session)
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(session);
        DataContext = _viewModel;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await _viewModel.LoadAsync();
    }

    private async void OnRoleContextSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedItem is not RoleContextOption target)
            return;

        await _viewModel.SwitchRoleContextAsync(target);
    }

    private async void OnReturnToDirectorClick(object? sender, RoutedEventArgs e)
        => await _viewModel.ReturnToDirectorAsync();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
