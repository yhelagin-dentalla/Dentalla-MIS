using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace Dentalla.Desktop.Views.Workspaces;
public partial class MarketerWorkspaceView : UserControl
{
    public MarketerWorkspaceView() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
