using System.Diagnostics;

namespace NurSapat.Cad.UI.Diagnostics;

internal static class CadDiagnostics
{
    public static void LogWarning(string message, Exception? ex = null)
    {
        var text = ex == null ? message : $"{message}: {ex.Message}";
        Debug.WriteLine($"[Cad] {text}");
    }
}
