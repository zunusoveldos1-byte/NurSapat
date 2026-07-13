using Avalonia.Media.Imaging;

namespace NurSapat.App.Services.Help;

public interface IUserGuideService
{
    string GuideFilePath { get; }

    bool GuideExists { get; }

    IReadOnlyList<Bitmap> LoadPreviewPages(int maxPages = 50);

    void OpenInDefaultViewer();
}
