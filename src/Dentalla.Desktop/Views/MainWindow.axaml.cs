using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.Models;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(DesktopSessionContext session)
        : this()
    {
        _viewModel = new MainWindowViewModel(session);
        DataContext = _viewModel;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        if (_viewModel is not null)
            await _viewModel.LoadAsync();
    }

    private async void OnRoleContextSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || sender is not ComboBox combo || combo.SelectedItem is not RoleContextOption target)
            return;
        _viewModel.ShowRoleWorkspace();
        await _viewModel.SwitchRoleContextAsync(target);
    }

    private async void OnReturnToDirectorClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;
        _viewModel.ShowRoleWorkspace();
        await _viewModel.ReturnToDirectorAsync();
    }

    private async void OnPatientsClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;
        _viewModel.ShowPatients();
        if (_viewModel.PatientWorkspace.SearchResults.Count == 0)
            await _viewModel.PatientWorkspace.SearchAsync();
    }

    private void OnRoleHomeClick(object? sender, RoutedEventArgs e) => _viewModel?.ShowRoleWorkspace();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
