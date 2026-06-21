using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace pharmacyPOS.API.Services;

public class BarcodeLabelPdfService
{
    // Label layout (millimetres)
    private const float PageMarginMm = 10f;
    private const float LabelWidthMm = 35f;
    private const float BarcodeHeightMm = 15f;
    private const float NumberLineMm = 3.5f;
    private const float NameLineMm = 5f;
    private const float LabelGapMm = 2f;

    private static float LabelHeightMm => BarcodeHeightMm + NumberLineMm + NameLineMm;

    static BarcodeLabelPdfService()
    {
        QuestPdfBootstrap.EnsureInitialized();
    }

    public byte[] BuildA4LabelSheet(string barcode, string productName)
    {
        QuestPdfBootstrap.EnsureInitialized();
        var png = RenderBarcodePng(barcode, (int)(LabelWidthMm * 8), (int)(BarcodeHeightMm * 8));
        var displayName = TruncateName(productName, 48);

        const float pageWidthMm = 210f;
        const float pageHeightMm = 297f;
        var usableW = pageWidthMm - PageMarginMm * 2;
        var usableH = pageHeightMm - PageMarginMm * 2;
        var cols = Math.Max(1, (int)Math.Floor((usableW + LabelGapMm) / (LabelWidthMm + LabelGapMm)));
        var rows = Math.Max(1, (int)Math.Floor((usableH + LabelGapMm) / (LabelHeightMm + LabelGapMm)));

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(PageMarginMm, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(6));

                page.Content().Column(column =>
                {
                    for (var r = 0; r < rows; r++)
                    {
                        column.Item().Row(row =>
                        {
                            for (var c = 0; c < cols; c++)
                            {
                                if (c > 0)
                                    row.ConstantItem(LabelGapMm, Unit.Millimetre);

                                row.ConstantItem(LabelWidthMm, Unit.Millimetre)
                                    .Height(LabelHeightMm, Unit.Millimetre)
                                    .Element(cell => DrawLabel(cell, png, barcode, displayName));
                            }
                        });

                        if (r < rows - 1)
                            column.Item().Height(LabelGapMm, Unit.Millimetre);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void DrawLabel(
        IContainer container,
        byte[] barcodePng,
        string barcode,
        string productName)
    {
        container.Column(col =>
        {
            col.Item()
                .Height(BarcodeHeightMm, Unit.Millimetre)
                .AlignCenter()
                .AlignMiddle()
                .Image(barcodePng)
                .FitArea();

            col.Item()
                .Height(NumberLineMm, Unit.Millimetre)
                .AlignCenter()
                .AlignMiddle()
                .Text(barcode)
                .FontSize(6.5f)
                .SemiBold();

            col.Item()
                .Height(NameLineMm, Unit.Millimetre)
                .AlignCenter()
                .AlignTop()
                .Text(productName)
                .FontSize(5f)
                .LineHeight(1.1f)
                .ClampLines(2);
        });
    }

    private static byte[] RenderBarcodePng(string barcode, int widthPx, int heightPx)
    {
        var format = barcode.Length == 13 && barcode.All(char.IsDigit)
            ? BarcodeFormat.EAN_13
            : BarcodeFormat.CODE_128;

        var writer = new BarcodeWriter
        {
            Format = format,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = widthPx,
                Height = heightPx,
                Margin = 1,
                PureBarcode = true
            }
        };

        using var bitmap = writer.Write(barcode);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static string TruncateName(string name, int maxLen) =>
        name.Length <= maxLen ? name : name[..(maxLen - 1)] + "…";
}
