using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Contracts.Patients;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class PatientWorkspaceView : UserControl
{
    public PatientWorkspaceView()
    {
        InitializeComponent();
        DoubleTapped += OnDoubleTapped;
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

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not TextBlock textBlock ||
            textBlock.DataContext is not PatientSearchItemDto patient ||
            !string.Equals(textBlock.Text, patient.FullName, StringComparison.Ordinal) ||
            DataContext is not PatientWorkspaceViewModel viewModel)
            return;

        PatientWorkspaceWindow.ShowFor(this, viewModel.Session, patient.Id, patient.FullName, patient.CardNumber);
        e.Handled = true;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
