using QuestPDF.Infrastructure;

namespace pharmacyPOS.API.Services;

/// <summary>
/// QuestPDF/SkiaSharp must be initialized before any PDF is generated.
/// Linux VPS images often lack system fonts and native Skia libraries.
/// </summary>
public static class QuestPdfBootstrap
{
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseEnvironmentFonts = false;

        _initialized = true;
    }
}
