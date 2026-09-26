using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace Dentalla.Desktop.Views.Workspaces;
public partial class DoctorWorkspaceView : UserControl
{
    public DoctorWorkspaceView() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
