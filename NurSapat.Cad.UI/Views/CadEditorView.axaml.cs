using Avalonia.Controls;

namespace NurSapat.Cad.UI.Views;

public partial class CadEditorView : UserControl
{
    public CadEditorView()
    {
        InitializeComponent();
    }

    public double GetLeftPanelWidth() => LeftPanel.Width;
    public double GetRightPanelWidth() => RightPanel.Width;
}