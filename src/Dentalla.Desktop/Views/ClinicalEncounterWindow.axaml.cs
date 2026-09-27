using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Dentalla.Desktop.ViewModels;

namespace Dentalla.Desktop.Views;

public partial class ClinicalEncounterWindow : Window
{
    public ClinicalEncounterWindow() => InitializeComponent();
    private async void OnSaveClick(object? sender, RoutedEventArgs e) { if (DataContext is PatientWorkspaceViewModel vm) await vm.SaveClinicalNoteAsync(); }
    private async void OnSignClick(object? sender, RoutedEventArgs e) { if (DataContext is PatientWorkspaceViewModel vm) await vm.SignClinicalNoteAsync(); }
    private async void OnCompleteClick(object? sender, RoutedEventArgs e) { if (DataContext is not PatientWorkspaceViewModel vm) return; await vm.CompleteEncounterAsync(); if (vm.ActiveEncounter?.StatusCode == "Completed") Close(); }
    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
