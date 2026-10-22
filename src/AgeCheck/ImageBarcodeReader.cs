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
        return result?.BarcodeFormat == BarcodeFormat.PDF_417 ? result.Text : null;
    }
}
