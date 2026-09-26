using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class PatientWorkspaceView : UserControl
{
    public PatientWorkspaceView()
    {
        InitializeComponent();
    }

    private async void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PatientWorkspaceViewModel viewModel)
            await viewModel.SearchAsync();
    }

    private async void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not PatientWorkspaceViewModel viewModel)
            return;

        await viewModel.SearchAsync();
        e.Handled = true;
    }

    private async void OnPatientSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is PatientWorkspaceViewModel viewModel && viewModel.SelectedPatient is not null)
            await viewModel.LoadSelectedPatientAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
