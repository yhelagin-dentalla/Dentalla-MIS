using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class AppointmentWorkspaceWindow : Window
{
    private AppointmentWorkspaceViewModel? _viewModel;

    public AppointmentWorkspaceWindow()
    {
        InitializeComponent();
    }

    public AppointmentWorkspaceWindow(Guid patientId, string patientName, string? cardNumber = null)
        : this()
    {
        _viewModel = new AppointmentWorkspaceViewModel(patientId, patientName, cardNumber);
        DataContext = _viewModel;
        Title = $"Запись на приём — {patientName}";
        Opened += OnOpened;
    }

    public static void ShowFor(Control source, Guid patientId, string patientName, string? cardNumber = null)
    {
        var window = new AppointmentWorkspaceWindow(patientId, patientName, cardNumber);
        if (TopLevel.GetTopLevel(source) is Window owner)
            window.Show(owner);
        else
            window.Show();
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        if (_viewModel is not null)
            await _viewModel.LoadAsync();
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null) await _viewModel.RefreshScheduleAsync();
    }

    private void OnNewClick(object? sender, RoutedEventArgs e) => _viewModel?.StartNew();

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null) await _viewModel.SaveAsync();
    }

    private async void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null) await _viewModel.ConfirmAsync();
    }

    private async void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null) await _viewModel.CancelAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
