using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class PatientWorkspaceWindow : Window
{
    public PatientWorkspaceWindow()
    {
        InitializeComponent();
    }

    public PatientWorkspaceWindow(Guid patientId, string? patientName = null)
        : this()
    {
        if (!string.IsNullOrWhiteSpace(patientName))
            Title = $"Карточка пациента — {patientName}";

        var viewModel = new PatientWorkspaceViewModel();
        DataContext = viewModel;
        Opened += async (_, _) => await viewModel.LoadPatientAsync(patientId, patientName);
    }

    public static void ShowFor(Control source, Guid patientId, string? patientName = null)
    {
        var window = new PatientWorkspaceWindow(patientId, patientName);
        if (TopLevel.GetTopLevel(source) is Window owner)
            window.Show(owner);
        else
            window.Show();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
