using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace Dentalla.Desktop.Views.Workspaces;
public partial class AdministratorWorkspaceView : UserControl
{
    public AdministratorWorkspaceView() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
