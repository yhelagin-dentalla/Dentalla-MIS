using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.Services;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class PatientCardView : UserControl
{
    public PatientCardView()
    {
        InitializeComponent();
    }

    private void OnBookAppointmentClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PatientWorkspaceViewModel viewModel || viewModel.Patient is null)
            return;

        AppointmentWorkspaceWindow.ShowFor(
            this,
            viewModel.Patient.Id,
            viewModel.Patient.FullName,
            viewModel.Patient.CardNumber);
    }

    private void OnStartEncounterClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PatientWorkspaceViewModel viewModel || viewModel.Patient is null)
            return;

        viewModel.StatusText = "Новый приём требует open Encounter + ClinicalNote. Текущая legacy-модель Encounter ещё не позволяет безопасно создать незавершённый приём; действие будет включено после migration lifecycle, без записи фиктивного EndedLocal.";
    }

    private void OnPrintClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PatientWorkspaceViewModel viewModel || viewModel.Patient is null)
            return;

        PatientPrintService.OpenPrintPreview(viewModel.Patient);
        viewModel.StatusText = "Открыта печатная версия карточки пациента.";
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
