using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class AppointmentWorkspaceWindow : Window
{
    public AppointmentWorkspaceWindow()
    {
        InitializeComponent();
    }

    public AppointmentWorkspaceWindow(PatientWorkspaceViewModel patientContext)
        : this()
    {
        DataContext = patientContext;
        Title = patientContext.Patient is null
            ? "Запись на приём"
            : $"Запись на приём — {patientContext.Patient.FullName}";
    }

    public static void ShowFor(Window owner, PatientWorkspaceViewModel patientContext)
    {
        var window = new AppointmentWorkspaceWindow(patientContext);
        window.Show(owner);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
