using CommunityToolkit.Mvvm.ComponentModel;
using Dentalla.Desktop.Models;

namespace Dentalla.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    public PatientWorkspaceViewModel PatientWorkspace { get; }

    [ObservableProperty]
    private bool isPatientWorkspace;

    public bool IsRoleWorkspace => !IsPatientWorkspace;

    private void InitializePatientWorkspace(DesktopSessionContext session)
        => PatientWorkspace = new PatientWorkspaceViewModel(session);

    public void ShowPatients() => IsPatientWorkspace = true;

    public void ShowRoleWorkspace() => IsPatientWorkspace = false;

    partial void OnIsPatientWorkspaceChanged(bool value)
        => OnPropertyChanged(nameof(IsRoleWorkspace));
}
