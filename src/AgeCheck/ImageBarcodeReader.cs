using System.Drawing;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace AgeCheck;

internal static class ImageBarcodeReader
{
    internal const int MaxFileBytes = 25 * 1024 * 1024;
    internal const long MaxPixels = 24_000_000;

    internal static string? Decode(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaxFileBytes)
            throw new InvalidDataException("Choose a photo smaller than 25 MB.");

        using var stream = new MemoryStream(bytes, writable: false);
        using var image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
        if ((long)image.Width * image.Height > MaxPixels)
            throw new InvalidDataException("Choose a photo smaller than 24 megapixels.");
        using var bitmap = new Bitmap(image);
        var reader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.PDF_417],
                TryHarder = true,
                TryInverted = true
            }
        };
        var result = reader.Decode(bitmap);
        if (result?.BarcodeFormat == BarcodeFormat.PDF_417) return result.Text;

        // Phone photos often devote only a small part of the frame to the ID.
        // Try overlapping regions so the PDF417 detector sees the barcode at a useful scale.
        foreach (var (widthPercent, heightPercent) in new[] { (45, 60), (30, 50) })
        {
            int tileWidth = Math.Min(bitmap.Width, Math.Max(400, bitmap.Width * widthPercent / 100));
            int tileHeight = Math.Min(bitmap.Height, Math.Max(650, bitmap.Height * heightPercent / 100));
            foreach (int y in Positions(bitmap.Height, tileHeight))
            foreach (int x in Positions(bitmap.Width, tileWidth))
            {
                using var tile = bitmap.Clone(new Rectangle(x, y, tileWidth, tileHeight), bitmap.PixelFormat);
                result = reader.Decode(tile);
                if (result?.BarcodeFormat == BarcodeFormat.PDF_417) return result.Text;
            }
        }
        return null;
    }

    private static IEnumerable<int> Positions(int length, int tile)
    {
        if (tile >= length) { yield return 0; yield break; }
        int last = length - tile;
        int step = Math.Max(1, tile / 2);
        for (int position = 0; position < last; position += step) yield return position;
        yield return last;
    }
}
