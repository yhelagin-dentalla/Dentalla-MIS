using CommunityToolkit.Mvvm.ComponentModel;

namespace Dentalla.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    public PatientWorkspaceViewModel PatientWorkspace { get; } = new();

    [ObservableProperty]
    private bool isPatientWorkspace;

    public bool IsRoleWorkspace => !IsPatientWorkspace;

    public void ShowPatients() => IsPatientWorkspace = true;

    public void ShowRoleWorkspace() => IsPatientWorkspace = false;

    partial void OnIsPatientWorkspaceChanged(bool value)
        => OnPropertyChanged(nameof(IsRoleWorkspace));
}
