using Avalonia.Controls;

namespace NurSapat.Cad.UI.Views;

public partial class CadEditorView : UserControl
{
    public CadEditorView()
    {
        InitializeComponent();
    }

    public double GetLeftPanelWidth() => 150;

    public double GetRightPanelWidth() => 0;
}
