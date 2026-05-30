using System.Collections.ObjectModel;

namespace NurSapat.Cad.UI.Training;

public sealed class CadTrainingTracker
{
    public ObservableCollection<TrainingStep> Steps { get; } = new();

    public void Track(string tool, string action, string status)
    {
        Steps.Insert(0, new TrainingStep(DateTimeOffset.Now, tool, action, status));
    }
}
