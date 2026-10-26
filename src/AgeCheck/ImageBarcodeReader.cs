using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
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
                if (widthPercent != 30) continue;
                using var highContrast = Monochrome(tile);
                result = reader.Decode(highContrast);
                if (result?.BarcodeFormat == BarcodeFormat.PDF_417) return result.Text;
            }
        }
        return null;
    }

    private static Bitmap Monochrome(Bitmap input)
    {
        var output = new Bitmap(input.Width, input.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(output)) graphics.DrawImageUnscaled(input, 0, 0);
        var rect = new Rectangle(0, 0, output.Width, output.Height);
        var bits = output.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            int stride = Math.Abs(bits.Stride);
            byte[] pixels = new byte[stride * output.Height];
            Marshal.Copy(bits.Scan0, pixels, 0, pixels.Length);
            var histogram = new int[256];
            for (int y = 0; y < output.Height; y++)
            for (int x = 0; x < output.Width; x++)
            {
                int at = y * stride + x * 3;
                histogram[(pixels[at] * 29 + pixels[at + 1] * 150 + pixels[at + 2] * 77) >> 8]++;
            }
            long sum = 0, backgroundSum = 0;
            int count = output.Width * output.Height, backgroundCount = 0, threshold = 127;
            for (int i = 0; i < 256; i++) sum += (long)i * histogram[i];
            double best = -1;
            for (int i = 0; i < 256; i++)
            {
                backgroundCount += histogram[i]; backgroundSum += (long)i * histogram[i];
                int foregroundCount = count - backgroundCount;
                if (backgroundCount == 0 || foregroundCount == 0) continue;
                double between = Math.Pow((double)backgroundSum / backgroundCount - (double)(sum - backgroundSum) / foregroundCount, 2)
                    * backgroundCount * foregroundCount;
                if (between > best) { best = between; threshold = i; }
            }
            for (int y = 0; y < output.Height; y++)
            for (int x = 0; x < output.Width; x++)
            {
                int at = y * stride + x * 3;
                byte value = (byte)(((pixels[at] * 29 + pixels[at + 1] * 150 + pixels[at + 2] * 77) >> 8) > threshold ? 255 : 0);
                pixels[at] = pixels[at + 1] = pixels[at + 2] = value;
            }
            Marshal.Copy(pixels, 0, bits.Scan0, pixels.Length);
        }
        finally { output.UnlockBits(bits); }
        return output;
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
