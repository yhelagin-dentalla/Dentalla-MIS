using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.ViewModels;
using Dentalla.Desktop.Views;

namespace Dentalla.Desktop.Views.Workspaces;

public partial class DoctorWorkspaceView : UserControl
{
    public DoctorWorkspaceView()
    {
        InitializeComponent();
        DoubleTapped += OnDoubleTapped;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not TextBlock textBlock)
            return;

        AppointmentRowViewModel? appointment = textBlock.DataContext as AppointmentRowViewModel;

        if (appointment is null && DataContext is MainWindowViewModel mainViewModel)
            appointment = mainViewModel.SelectedAppointment;

        if (appointment is null ||
            !string.Equals(textBlock.Text, appointment.PatientName, StringComparison.Ordinal) ||
            DataContext is not MainWindowViewModel viewModel)
            return;

        PatientWorkspaceWindow.ShowFor(
            this,
            viewModel.Session,
            appointment.PatientId,
            appointment.PatientName,
            appointment.CardNumber);
        e.Handled = true;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
