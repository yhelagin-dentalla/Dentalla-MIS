using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace Dentalla.Desktop.Views.Workspaces;
public partial class DirectorWorkspaceView : UserControl
{
    public DirectorWorkspaceView() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
