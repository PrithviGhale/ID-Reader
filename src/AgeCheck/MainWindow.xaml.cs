using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AgeCheck.Core;
using Velopack;
using Velopack.Sources;

namespace AgeCheck;
public partial class MainWindow : Window
{
    private readonly ScanBuffer scan = new();
    private readonly DispatcherTimer quiet = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private readonly DispatcherTimer deadline = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer privacy = new() { Interval = TimeSpan.FromSeconds(30) };
    private UpdateManager? updater;
    private UpdateInfo? update;
    private bool updateBusy, downloaded, processing, inputBlocked, showingResult;
    private int scanGeneration;
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgeCheck", "settings.json");
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    private bool CanCapture => IsActive && ScanPage.Visibility == Visibility.Visible && !processing && !inputBlocked;

    public MainWindow(string[] args)
    {
        InitializeComponent();
        VersionLabel.Text = "VERSION " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "development");
        try
        {
            using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "release-config.json")));
            string url = config.RootElement.GetProperty("repositoryUrl").GetString()!;
            var uri = new Uri(url);
            if (uri.Scheme != "https" || uri.Host != "github.com" || uri.Segments.Length != 3 || uri.Query != "" || uri.UserInfo != "") throw new InvalidDataException();
            updater = new UpdateManager(new GithubSource(url, null, false), new UpdateOptions { AllowVersionDowngrade = false, ExplicitChannel = config.RootElement.GetProperty("channel").GetString() });
        }
        catch { UpdateStatus.Text = "Update configuration is unavailable. Scanning still works."; CheckButton.IsEnabled = false; }
        try { if (File.Exists(settingsPath)) StartupChecks.IsChecked = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath))?.StartupChecks ?? true; }
        catch { /* A damaged settings file must not prevent offline use. */ }
        quiet.Tick += async (_, _) => await FinishScan();
        deadline.Tick += (_, _) => { Clear(); inputBlocked = true; ShowResult(new(Outcome.Review, "Scan exceeded 20 seconds. Clear and rescan."), false); };
        privacy.Tick += (_, _) => Clear();
        Deactivated += (_, _) => Clear();
        Closed += (_, _) => { quiet.Stop(); deadline.Stop(); privacy.Stop(); scan.Clear(); };
        PreviewTextInput += (_, e) => { if (CanCapture) { Receive(e.Text); e.Handled = true; } };
        PreviewKeyDown += OnKey;
        Loaded += async (_, _) =>
        {
            Focus();
            if (args.Contains("--self-test")) { await SelfTest(); return; }
            if (args.Contains("--screenshot")) { await ExportScreenshots(); return; }
            if (StartupChecks.IsChecked == true) await CheckUpdates();
        };
    }
    private void OnKey(object sender, KeyEventArgs e)
    {
        if (!CanCapture) return;
        if (e.Key == Key.Escape) { Clear(); e.Handled = true; return; }
        // Enter/Tab inside scanner input are data, never button activations.
        if (scan.Receiving && e.Key is Key.Enter or Key.Tab) { Receive(e.Key == Key.Enter ? "\r" : "\t"); e.Handled = true; }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key >= Key.A && e.Key <= Key.Z)
        {
            Receive(((char)(1 + e.Key - Key.A)).ToString()); e.Handled = true;
        }
    }
    private void Receive(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (showingResult) Clear();
        if (!scan.Receiving)
        {
            privacy.Stop(); deadline.Start(); Phase.Text = "RECEIVING"; Headline.Text = "Receiving barcode…";
            Detail.Text = "Keep the scanner aimed until all data has arrived."; Symbol.Text = "↓";
        }
        if (!scan.Append(text))
        {
            Clear(); inputBlocked = true;
            ShowResult(new(Outcome.Review, "Input limit exceeded. Choose Clear / Next customer before rescanning."), false);
            return;
        }
        quiet.Stop(); quiet.Start();
    }
    private async Task FinishScan()
    {
        quiet.Stop(); deadline.Stop();
        if (!scan.Receiving) return;
        processing = true; int generation = scanGeneration;
        Phase.Text = "PROCESSING"; Headline.Text = "Checking dates…";
        string raw = scan.Take();
        var result = await Task.Run(() => Barcode.Check(raw, Today));
        raw = string.Empty;
        processing = false;
        if (generation == scanGeneration && IsActive) ShowResult(result, false);
    }
    private void Clear()
    {
        scanGeneration++; scan.Clear(); quiet.Stop(); deadline.Stop(); privacy.Stop(); inputBlocked = false; showingResult = false;
        Phase.Text = "READY"; Headline.Text = "Ready to scan"; Symbol.Text = "↗";
        Detail.Text = "Keep this window active and scan the barcode on the back of the ID.";
        BirthLabel.Text = ExpiryLabel.Text = "—"; DemoBadge.Visibility = Visibility.Collapsed;
        SymbolBox.Background = Brush("#E6F5EC"); Symbol.Foreground = Brush("#237557");
    }
    private static SolidColorBrush Brush(string color) { var b = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!; b.Freeze(); return b; }
    private void ShowResult(CheckResult result, bool demo)
    {
        showingResult = true; Phase.Text = "CHECK COMPLETE"; DemoBadge.Visibility = demo ? Visibility.Visible : Visibility.Collapsed;
        (Headline.Text, Symbol.Text, string color, string background) = result.Outcome switch
        {
            Outcome.Passed => ("21 or older — barcode age check passed", "✓", "#176B53", "#E6F5EC"),
            Outcome.Under21 => ("Under 21", "!", "#A13935", "#FBE9E6"),
            Outcome.Expired => ("Expired", "×", "#9B5916", "#FFF0DD"),
            _ => ("Unable to check — manual review required", "?", "#795DA1", "#EEE9F6")
        };
        Detail.Text = result.Detail; Symbol.Foreground = Brush(color); SymbolBox.Background = Brush(background);
        BirthLabel.Text = result.Birth?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—";
        ExpiryLabel.Text = result.Expiration?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—";
        Animate(ResultCard); privacy.Stop(); privacy.Start();
    }
    private static void Animate(UIElement element)
    {
        if (SystemParameters.ClientAreaAnimation && (RenderCapability.Tier >> 16) > 0)
            element.BeginAnimation(OpacityProperty, new DoubleAnimation(0.65, 1, TimeSpan.FromMilliseconds(180)) { FillBehavior = FillBehavior.Stop });
    }
    private void Page(UIElement page)
    {
        Clear(); ScanPage.Visibility = SetupPage.Visibility = UpdatesPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible; Animate(page);
    }
    private void ScanPage_Click(object sender, RoutedEventArgs e) => Page(ScanPage);
    private void SetupPage_Click(object sender, RoutedEventArgs e) => Page(SetupPage);
    private void UpdatesPage_Click(object sender, RoutedEventArgs e) => Page(UpdatesPage);
    private void Clear_Click(object sender, RoutedEventArgs e) => Clear();
    private void DemoResult(int years, bool expired = false)
    {
        Clear(); ShowResult(Barcode.Check(Demo.Create(Today.AddYears(-years), expired ? Today.AddDays(-1) : Today.AddYears(3), Today.AddYears(-1)), Today), true);
    }
    private void AdultDemo_Click(object sender, RoutedEventArgs e) => DemoResult(25);
    private void YoungDemo_Click(object sender, RoutedEventArgs e) => DemoResult(19);
    private void ExpiredDemo_Click(object sender, RoutedEventArgs e) => DemoResult(30, true);
    private void StartupChecks_Click(object sender, RoutedEventArgs e)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!); File.WriteAllText(settingsPath, JsonSerializer.Serialize(new Preferences(StartupChecks.IsChecked == true))); }
        catch { UpdateStatus.Text = "Could not save the preference. It applies for this session."; }
    }
    private void UpdateButtons()
    {
        CheckButton.IsEnabled = !updateBusy && updater != null;
        DownloadButton.IsEnabled = !updateBusy && update != null && !downloaded;
        RestartButton.IsEnabled = !updateBusy && downloaded;
    }
    private async Task CheckUpdates()
    {
        if (updateBusy || updater == null) return;
        if (!updater.IsInstalled) { UpdateStatus.Text = "Development / portable build. Install the Setup release to enable in-app updates."; return; }
        updateBusy = true; UpdateButtons(); UpdateStatus.Text = "Checking stable releases…";
        try
        {
            var found = await updater.CheckForUpdatesAsync();
            update = found; downloaded = false;
            UpdateStatus.Text = found == null ? "You have the latest available stable version." : $"Version {found.TargetFullRelease.Version} is available.";
            ReleaseNotes.Text = found?.TargetFullRelease.NotesMarkdown ?? "No release notes supplied.";
        }
        catch { UpdateStatus.Text = "Could not reach the update feed. Check your connection or try later if GitHub is rate-limiting requests. Scanning remains available."; }
        finally { updateBusy = false; UpdateButtons(); }
    }
    private async void CheckUpdates_Click(object sender, RoutedEventArgs e) => await CheckUpdates();
    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (updateBusy || updater == null || update == null) return;
        updateBusy = true; UpdateButtons(); UpdateStatus.Text = "Downloading and verifying update…";
        try
        {
            await updater.DownloadUpdatesAsync(update, p => Dispatcher.BeginInvoke(() => DownloadProgress.Value = p));
            downloaded = true; UpdateStatus.Text = "Update ready. Restart when you have finished serving the customer.";
        }
        catch { UpdateStatus.Text = "Download or package verification failed. Check your connection and disk space, then retry."; }
        finally { updateBusy = false; UpdateButtons(); }
    }
    private void Restart_Click(object sender, RoutedEventArgs e)
    {
        if (scan.Receiving || processing || updateBusy || !downloaded || update == null || updater == null) { UpdateStatus.Text = "Finish the current scan before restarting."; return; }
        Clear(); inputBlocked = true; updateBusy = true; UpdateButtons();
        try { updater.ApplyUpdatesAndRestart(update); }
        catch { updateBusy = false; inputBlocked = false; UpdateStatus.Text = "Could not apply the update. Close other copies of AgeCheck and retry."; UpdateButtons(); }
    }
    private async Task ExportScreenshots()
    {
        // Explicit developer-only export: synthetic demonstrations, never real scan data.
        Directory.CreateDirectory("artifacts/screenshots");
        foreach (string state in new[] { "ready", "passed", "under21", "expired", "review", "setup", "updates" })
        {
            Page(ScanPage);
            switch (state)
            {
                case "passed": DemoResult(25); break;
                case "under21": DemoResult(19); break;
                case "expired": DemoResult(30, true); break;
                case "review": ShowResult(new(Outcome.Review, "Incomplete barcode. Check scanner settings and rescan."), true); break;
                case "setup": Page(SetupPage); break;
                case "updates": Page(UpdatesPage); break;
            }
            await Task.Delay(250); UpdateLayout();
            var content = (FrameworkElement)Content;
            var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            ((System.Windows.Controls.Panel)content).Background = Background;
            bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create($"artifacts/screenshots/{state}.png"); encoder.Save(stream);
        }
        Close();
    }
    private sealed record Preferences(bool StartupChecks);

    private async Task SelfTest()
    {
        var checks = new List<string>();
        void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); checks.Add(label); }
        try
        {
            Activate(); Page(ScanPage);
            var payload = Demo.Create(Today.AddYears(-25), Today.AddYears(2), Today.AddYears(-1));
            Receive(payload[..40]);
            Check(Phase.Text == "RECEIVING", "Receiving state");
            await Task.Delay(100);
            Check(scan.Receiving && !showingResult, "Embedded newlines do not submit");
            Receive(payload[40..]);
            await Task.Delay(1000);
            Check(Headline.Text.StartsWith("21 or older"), "Complete synthetic keyboard stream passes");
            Receive("@"); Check(BirthLabel.Text == "—", "Next scan clears dates immediately");
            Clear(); DemoResult(19); Check(DemoBadge.Visibility == Visibility.Visible, "Demo marker visible");
            OnDeactivated(EventArgs.Empty); Check(BirthLabel.Text == "—" && !scan.Receiving, "Deactivation clears results and input");
            Activate(); Receive("@partial"); await Task.Delay(1000);
            Check(Headline.Text.StartsWith("Unable to check"), "Partial stream requests review");
            Clear(); Receive(new string('x', Barcode.InputLimit + 1)); Check(inputBlocked, "Oversized stream blocked");
            Clear(); Check(!inputBlocked, "Clear allows next customer");
            DemoResult(25); await Task.Delay(31000); Check(BirthLabel.Text == "—" && !showingResult, "Real 30-second privacy timer clears results");
            await CheckUpdates(); Check(UpdateStatus.Text.Contains("Development / portable"), "Uninstalled update path is safe");
            Check(!quiet.IsEnabled && !deadline.IsEnabled && !privacy.IsEnabled, "All scan timers stopped at idle");
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            var before = process.TotalProcessorTime; await Task.Delay(5000); process.Refresh();
            checks.Add($"Idle sample: {(process.TotalProcessorTime - before).TotalMilliseconds:F1} ms CPU over 5 seconds; working set {process.WorkingSet64 / 1048576.0:F1} MiB");
            Directory.CreateDirectory("artifacts"); File.WriteAllLines("artifacts/ui-test-results.txt", checks);
            Close();
        }
        catch (Exception e) { Directory.CreateDirectory("artifacts"); File.WriteAllText("artifacts/ui-test-results.txt", "FAILED: " + e.Message); Environment.Exit(1); }
    }
}
