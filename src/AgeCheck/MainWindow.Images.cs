using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using AgeCheck.Core;
using Microsoft.Win32;
using ZXing;
using ZXing.Common;
using DrawingBitmap = System.Drawing.Bitmap;

namespace AgeCheck;

public partial class MainWindow
{
    private int imageGeneration;

    private async void PasteImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Clipboard.ContainsImage())
            {
                ImageStatus.Text = "No photo is on the clipboard. Copy an image of the back of the ID first.";
                return;
            }
            var image = Clipboard.GetImage();
            if (image is null) throw new InvalidDataException("The clipboard image could not be opened.");
            if ((long)image.PixelWidth * image.PixelHeight > ImageBarcodeReader.MaxPixels)
                throw new InvalidDataException("Choose a photo smaller than 24 megapixels.");
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            await ReadImage(stream.ToArray());
        }
        catch (Exception ex) when (ex is ExternalException or InvalidDataException or NotSupportedException or ArgumentException)
        {
            ImageStatus.Text = "Could not read the pasted photo. " + ex.Message;
        }
    }

    private async void OpenImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose an ID barcode photo", Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var file = new FileInfo(dialog.FileName);
            if (file.Length > ImageBarcodeReader.MaxFileBytes) throw new InvalidDataException("Choose a photo smaller than 25 MB.");
            await ReadImage(await File.ReadAllBytesAsync(file.FullName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            ImageStatus.Text = "Could not open that photo. " + ex.Message;
        }
    }

    private async Task ReadImage(byte[] bytes)
    {
        Clear();
        int generation = ++imageGeneration;
        PasteImageButton.IsEnabled = OpenImageButton.IsEnabled = false;
        ImageResultPanel.Visibility = Visibility.Collapsed;
        ImageStatus.Text = "Reading the PDF417 barcode on this PC...";
        try
        {
            string? decoded = await Task.Run(() => ImageBarcodeReader.Decode(bytes));
            if (generation != imageGeneration || activePage != ImagePage) return;
            if (decoded is null)
            {
                ImageStatus.Text = "The PDF417 barcode could not be read. In this photo it may be too small or blurry. Move closer, keep the full barcode in frame, tap the barcode to focus, and use even light. If it still fails, scan the physical card or review it manually.";
                return;
            }
            if (decoded.Length > Barcode.InputLimit)
                throw new InvalidDataException("The decoded barcode is too long for an ID check.");
            var result = Barcode.Check(decoded, Today);
            Clear();
            ShowResult(result, false, decoded);
            ImageResultHeadline.Text = Headline.Text;
            ImageResultName.Text = result.DisplayName ?? "Name unavailable";
            ImageResultDetails.Text = $"Age: {(result.Outcome == Outcome.Review ? "review needed" : AgeLabel.Text)}  |  Birth date: {BirthLabel.Text}\nExpires: {ExpiryLabel.Text}  |  {ExpiryCountdownLabel.Text}\nIssued by: {IssuingStateLabel.Text}\n{result.Detail}";
            ImageResultPanel.Visibility = Visibility.Visible;
            Animate(ImageResultPanel);
            ImageStatus.Text = result.Outcome == Outcome.Review ? "Barcode decoded; the ID data needs manual review." : "Barcode decoded and checked locally. Compare it with the physical ID.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or OutOfMemoryException or ExternalException)
        {
            if (generation == imageGeneration) ImageStatus.Text = "Could not read this photo. " + (ex is InvalidDataException ? ex.Message : "Try a clearer JPG or PNG of the full barcode.");
        }
        finally
        {
            PasteImageButton.IsEnabled = OpenImageButton.IsEnabled = true;
        }
    }

    private async Task ImageSelfTest()
    {
        var checks = new List<string>();
        try
        {
            var raw = Demo.CreateNamed(Today.AddYears(-25), Today.AddYears(3), Today.AddYears(-1));
            var writer = new ZXing.Windows.Compatibility.BarcodeWriter
            {
                Format = BarcodeFormat.PDF_417,
                Options = new EncodingOptions { Width = 1100, Height = 320, Margin = 24 }
            };
            using var bitmap = writer.Write(raw);
            using (var distant = new DrawingBitmap(2160, 2880))
            using (var sideways = new DrawingBitmap(bitmap))
            {
                sideways.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                using (var graphics = System.Drawing.Graphics.FromImage(distant))
                {
                    graphics.Clear(System.Drawing.Color.Beige);
                    graphics.DrawImageUnscaled(sideways, 1400, 800);
                }
                using var distantStream = new MemoryStream();
                distant.Save(distantStream, System.Drawing.Imaging.ImageFormat.Png);
                var distantText = ImageBarcodeReader.Decode(distantStream.ToArray());
                if (distantText is null || Barcode.Check(distantText, Today).Outcome != Outcome.Passed)
                    throw new InvalidDataException("Distant sideways synthetic PDF417 did not decode.");
                checks.Add("Distant sideways PDF417 in a full photo decoded");
            }
            foreach (var rotation in new[] { System.Drawing.RotateFlipType.RotateNoneFlipNone, System.Drawing.RotateFlipType.Rotate90FlipNone, System.Drawing.RotateFlipType.Rotate180FlipNone })
            {
                using var variant = new DrawingBitmap(bitmap);
                variant.RotateFlip(rotation);
                using var stream = new MemoryStream();
                variant.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                var bytes = stream.ToArray();
                var decoded = await Task.Run(() => ImageBarcodeReader.Decode(bytes));
                if (decoded is null || Barcode.Check(decoded, Today).Outcome != Outcome.Passed)
                    throw new InvalidDataException($"PDF417 image did not pass at rotation {rotation}.");
                checks.Add($"Synthetic PDF417 passed at {rotation}");
                if (rotation == System.Drawing.RotateFlipType.RotateNoneFlipNone)
                {
                    Page(ImagePage);
                    await ReadImage(bytes);
                    if (ImageResultPanel.Visibility != Visibility.Visible || ImageResultHeadline.Text != "21 or older")
                        throw new InvalidDataException("Image Scanner UI did not show the decoded age result.");
                    checks.Add("Image Scanner page displayed the 21+ result");
                }
            }
            using var blank = new DrawingBitmap(800, 300);
            using (var graphics = System.Drawing.Graphics.FromImage(blank)) graphics.Clear(System.Drawing.Color.White);
            using var emptyStream = new MemoryStream();
            blank.Save(emptyStream, System.Drawing.Imaging.ImageFormat.Png);
            if (ImageBarcodeReader.Decode(emptyStream.ToArray()) is not null) throw new InvalidDataException("Blank photo decoded as an ID.");
            checks.Add("Blank photo requires another image");
            foreach (var (years, expired, expected) in new[] { (19, false, Outcome.Under21), (30, true, Outcome.Expired) })
            {
                var specimen = Demo.CreateNamed(Today.AddYears(-years), expired ? Today.AddDays(-1) : Today.AddYears(3), Today.AddYears(-1));
                using var specimenBitmap = writer.Write(specimen);
                using var specimenStream = new MemoryStream();
                specimenBitmap.Save(specimenStream, System.Drawing.Imaging.ImageFormat.Png);
                var specimenText = ImageBarcodeReader.Decode(specimenStream.ToArray());
                if (specimenText is null || Barcode.Check(specimenText, Today).Outcome != expected)
                    throw new InvalidDataException($"Synthetic image did not produce {expected}.");
                checks.Add($"Synthetic image produced {expected}");
            }
            Directory.CreateDirectory("artifacts");
            File.WriteAllLines("artifacts/image-test-results.txt", checks);
            Close();
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory("artifacts");
            checks.Add("FAILED: " + ex);
            File.WriteAllLines("artifacts/image-test-results.txt", checks);
            Environment.Exit(1);
        }
    }
}
