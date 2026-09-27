using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Contracts.Patients;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class PatientWorkspaceWindow : Window
{
    public PatientWorkspaceWindow()
    {
        InitializeComponent();
    }

    public PatientWorkspaceWindow(Guid patientId, string? patientName = null, string? cardNumber = null)
        : this()
    {
        if (!string.IsNullOrWhiteSpace(patientName))
            Title = $"Карточка пациента — {patientName}";

        var viewModel = new PatientWorkspaceViewModel
        {
            SelectedPatient = new PatientSearchItemDto(
                patientId,
                cardNumber ?? string.Empty,
                patientName ?? "Пациент",
                null,
                null)
        };

        DataContext = viewModel;
        Opened += async (_, _) => await viewModel.LoadSelectedPatientAsync();
    }

    public static void ShowFor(
        Control source,
        Guid patientId,
        string? patientName = null,
        string? cardNumber = null)
    {
        var window = new PatientWorkspaceWindow(patientId, patientName, cardNumber);
        if (TopLevel.GetTopLevel(source) is Window owner)
            window.Show(owner);
        else
            window.Show();
    }

    private void OnNewAppointmentClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PatientWorkspaceViewModel viewModel)
            AppointmentWorkspaceWindow.ShowFor(this, viewModel);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
