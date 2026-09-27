using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.Services;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class PatientCardView : UserControl
{
    public PatientCardView() => InitializeComponent();

    private void OnBookAppointmentClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PatientWorkspaceViewModel viewModel || viewModel.Patient is null) return;
        AppointmentWorkspaceWindow.ShowFor(this, viewModel.Patient.Id, viewModel.Patient.FullName, viewModel.Patient.CardNumber);
    }

    private async void OnStartEncounterClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PatientWorkspaceViewModel viewModel || viewModel.Patient is null) return;
        if (!await viewModel.StartEncounterAsync()) return;
        var window = new ClinicalEncounterWindow { DataContext = viewModel };
        if (TopLevel.GetTopLevel(this) is Window owner) await window.ShowDialog(owner); else window.Show();
    }

    private void OnPrintClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PatientWorkspaceViewModel viewModel || viewModel.Patient is null) return;
        PatientPrintService.OpenPrintPreview(viewModel.Patient); viewModel.StatusText = "Открыта печатная версия карточки пациента.";
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
