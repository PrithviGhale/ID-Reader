using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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
    private CheckResult? recoveredDates;
    private bool recoveryDemo;
    private bool darkTheme = true;
    private bool motionEnabled = true;
    private CheckResult? visibleResult;
    private UIElement? activePage;
    private static bool suppressAnimations;
    private readonly string settingsPath;
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    private bool CanCapture => IsActive && ScanPage.Visibility == Visibility.Visible && !processing && !inputBlocked;

    public MainWindow(string[] args)
    {
        bool developerMode = args.Any(a => a is "--self-test" or "--layout-test" or "--screenshot" or "--motion-test");
        settingsPath = developerMode ? Path.Combine(Environment.CurrentDirectory, "artifacts", "test-preferences.json")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgeCheck", "settings.json");
        Theme.Apply(true);
        InitializeComponent();
        var workArea = SystemParameters.WorkArea;
        MinWidth = Math.Min(MinWidth, Math.Max(360, workArea.Width - 24));
        MinHeight = Math.Min(MinHeight, Math.Max(320, workArea.Height - 24));
        Width = Math.Min(Width, Math.Max(MinWidth, workArea.Width - 32));
        Height = Math.Min(Height, Math.Max(MinHeight, workArea.Height - 32));
        SizeChanged += (_, _) => AdaptLayout(ActualWidth);
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
        try
        {
            if (!developerMode && File.Exists(settingsPath))
            {
                var prefs = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath));
                StartupChecks.IsChecked = prefs?.StartupChecks ?? true;
                darkTheme = prefs?.Theme != "Light";
                motionEnabled = prefs?.Animations ?? true;
            }
        }
        catch { /* A damaged settings file must not prevent offline use. */ }
        MotionEnabled.IsChecked = motionEnabled;
        activePage = ScanPage; ApplyAppearance(darkTheme, false);
        SourceInitialized += (_, _) => Theme.TitleBar(this, darkTheme);
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
            AdaptLayout(ActualWidth);
            if (args.Contains("--layout-test")) { suppressAnimations = true; await LayoutTest(); return; }
            if (args.Contains("--self-test")) { await SelfTest(); return; }
            if (args.Contains("--screenshot")) { suppressAnimations = true; await ExportScreenshots(); return; }
            if (args.Contains("--motion-test")) { await MotionTest(); return; }
            Animate(PageHost, true); Animate(BrandMark, true);
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
            privacy.Stop(); deadline.Start(); Phase.Text = "RECEIVING"; Headline.Text = "Reading your scan";
            ResultSubtitle.Text = "Waiting for the complete scanner input.";
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
        var result = await Task.Run(() => Barcode.Check(raw, Today, allowScannerRecovery: true));
        raw = string.Empty;
        processing = false;
        if (generation == scanGeneration && IsActive) ShowResult(result, false);
    }
    private void Clear()
    {
        scanGeneration++; scan.Clear(); quiet.Stop(); deadline.Stop(); privacy.Stop(); inputBlocked = false; showingResult = false;
        recoveredDates = null; recoveryDemo = false; RecoveryNotice.Visibility = Visibility.Collapsed;
        visibleResult = null;
        Phase.Text = "READY TO SCAN"; Headline.Text = "Ready to scan"; Symbol.Text = "⌁";
        ResultSubtitle.Text = "Aim at the barcode on the back of the card.";
        Detail.Text = "Keep this window active. Your result appears here.";
        BirthLabel.Text = ExpiryLabel.Text = "—"; DemoBadge.Visibility = Visibility.Collapsed;
        PaintStatus(null);
    }
    private void ShowResult(CheckResult result, bool demo)
    {
        visibleResult = result;
        showingResult = true; Phase.Text = "CHECK COMPLETE"; DemoBadge.Visibility = demo ? Visibility.Visible : Visibility.Collapsed;
        recoveredDates = result.NeedsDateConfirmation ? result : null;
        recoveryDemo = demo;
        RecoveryNotice.Visibility = result.NeedsDateConfirmation ? Visibility.Visible : Visibility.Collapsed;
        (Headline.Text, Symbol.Text) = result.Outcome switch
        {
            Outcome.Passed => ("21 or older", "✓"),
            Outcome.Under21 => ("Under 21", "!"),
            Outcome.Expired => ("Expired", "×"),
            _ => (result.NeedsDateConfirmation ? "Check the printed dates" : "Unable to check", "?")
        };
        ResultSubtitle.Text = result.Outcome switch
        {
            Outcome.Passed => "Barcode age check passed",
            Outcome.Under21 => "Barcode birth date is below the age threshold",
            Outcome.Expired => "Barcode expiration date has passed",
            _ => result.NeedsDateConfirmation ? "Scanner text recovered · confirmation needed" : "Manual review required"
        };
        Detail.Text = result.Detail;
        PaintStatus(result.Outcome);
        BirthLabel.Text = result.Birth?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—";
        ExpiryLabel.Text = result.Expiration?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—";
        Animate(ResultCard); privacy.Stop(); privacy.Start();
    }
    private void Animate(UIElement element, bool opening = false)
    {
        element.BeginAnimation(OpacityProperty, null);
        if (element.RenderTransform is TranslateTransform old) old.BeginAnimation(TranslateTransform.YProperty, null);
        element.Opacity = 1;
        if (suppressAnimations || !motionEnabled || !SystemParameters.ClientAreaAnimation) return;
        var duration = TimeSpan.FromMilliseconds(opening ? 460 : 260);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var move = new TranslateTransform(); element.RenderTransform = move;
        element.BeginAnimation(OpacityProperty, new DoubleAnimation(opening ? 0 : 0.3, 1, duration) { EasingFunction = easing, FillBehavior = FillBehavior.Stop });
        move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(opening ? 22 : 10, 0, duration) { EasingFunction = easing, FillBehavior = FillBehavior.Stop });
    }
    private void Page(UIElement page)
    {
        Clear(); ScanPage.Visibility = SetupPage.Visibility = UpdatesPage.Visibility = SettingsPage.Visibility = Visibility.Collapsed;
        activePage = page;
        page.Visibility = Visibility.Visible; Animate(page);
        PaintNavigation();
        PageScroll.ScrollToTop();
    }
    private void ScanPage_Click(object sender, RoutedEventArgs e) => Page(ScanPage);
    private void SetupPage_Click(object sender, RoutedEventArgs e) => Page(SetupPage);
    private void UpdatesPage_Click(object sender, RoutedEventArgs e) => Page(UpdatesPage);
    private void SettingsPage_Click(object sender, RoutedEventArgs e) => Page(SettingsPage);
    private void Clear_Click(object sender, RoutedEventArgs e) => Clear();
    private void DemoResult(int years, bool expired = false)
    {
        Clear(); ShowResult(Barcode.Check(Demo.Create(Today.AddYears(-years), expired ? Today.AddDays(-1) : Today.AddYears(3), Today.AddYears(-1)), Today), true);
    }
    private void AdultDemo_Click(object sender, RoutedEventArgs e) => DemoResult(25);
    private void YoungDemo_Click(object sender, RoutedEventArgs e) => DemoResult(19);
    private void ExpiredDemo_Click(object sender, RoutedEventArgs e) => DemoResult(30, true);
    private void RecoveryDemo_Click(object sender, RoutedEventArgs e)
    {
        Clear(); ShowResult(Barcode.Check(Demo.KeyboardHeader(Today.AddYears(-25), Today.AddYears(2), Today.AddYears(-1)), Today, true), true);
    }
    private void ConfirmDates_Click(object sender, RoutedEventArgs e)
    {
        if (recoveredDates is not { Birth: { } birth, Expiration: { } expiry } || scan.Receiving || processing || !IsActive) return;
        bool demo = recoveryDemo;
        var result = Barcode.Evaluate(birth, expiry, Today);
        ShowResult(result with { Detail = "You confirmed both dates match the printed card. This is a date-only check; the full barcode was not verified. Compare the photograph and customer." }, demo);
        Phase.Text = "DATES CONFIRMED";
        if (result.Outcome == Outcome.Passed) ResultSubtitle.Text = "Confirmed dates meet the 21+ age threshold";
    }
    private void AdaptLayout(double width)
    {
        bool compact = width < 800;
        PageHost.Margin = new Thickness(width < 600 ? 16 : 32, 22, width < 600 ? 16 : 32, 22);
        ResultCard.Padding = new Thickness(width < 600 ? 18 : 26);
        ResultDatesColumn.Width = compact ? new GridLength(0) : new GridLength(280);
        System.Windows.Controls.Grid.SetColumn(DatesPanel, compact ? 0 : 1);
        System.Windows.Controls.Grid.SetRow(DatesPanel, compact ? 1 : 0);
        DatesPanel.Margin = compact ? new Thickness(0, 20, 0, 0) : new Thickness(0);
        ResultSummary.Margin = compact ? new Thickness(0) : new Thickness(0, 0, 24, 0);
        Headline.FontSize = width < 600 ? 28 : 34;
        LocalBadge.Visibility = width < 650 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void StartupChecks_Click(object sender, RoutedEventArgs e)
    {
        SavePreferences();
    }
    private void SavePreferences()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(new Preferences(StartupChecks.IsChecked == true, darkTheme ? "Dark" : "Light", motionEnabled)));
            SettingsStatus.Text = "Preferences saved on this PC. ID data is never saved.";
        }
        catch { SettingsStatus.Text = "Could not save preferences. They still apply for this session."; UpdateStatus.Text = "Could not save preferences. They still apply for this session."; }
    }
    private void ApplyAppearance(bool dark, bool save)
    {
        darkTheme = dark; Theme.Apply(dark, RootLayout);
        Theme.TitleBar(this, dark);
        PaintNavigation(); PaintStatus(visibleResult?.Outcome);
        LightThemeButton.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, dark ? "Quiet" : "Accent");
        LightThemeButton.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, dark ? "Ink" : "AccentInk");
        DarkThemeButton.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, dark ? "Accent" : "Quiet");
        DarkThemeButton.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, dark ? "AccentInk" : "Ink");
        if (save) SavePreferences();
    }
    private void PaintNavigation()
    {
        foreach (var (button,page) in new[] { (ScanNav, ScanPage), (SetupNav, SetupPage), (UpdatesNav, UpdatesPage), (SettingsNav, SettingsPage) })
        {
            button.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, page == activePage ? "AccentSoft" : "Surface");
            button.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, page == activePage ? "AccentText" : "Muted");
        }
    }
    private void PaintStatus(Outcome? outcome)
    {
        string ink = outcome is Outcome.Under21 or Outcome.Expired ? "Danger" : outcome == Outcome.Review ? "WarmInk" : "AccentText";
        string surface = outcome is Outcome.Under21 or Outcome.Expired ? "DangerSoft" : outcome == Outcome.Review ? "Warm" : "AccentSoft";
        Symbol.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, ink);
        SymbolBox.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, surface);
    }
    private void LightTheme_Click(object sender, RoutedEventArgs e) => ApplyAppearance(false, true);
    private void DarkTheme_Click(object sender, RoutedEventArgs e) => ApplyAppearance(true, true);
    private void MotionEnabled_Click(object sender, RoutedEventArgs e)
    {
        motionEnabled = MotionEnabled.IsChecked == true;
        if (!motionEnabled) { Animate(PageHost); Animate(BrandMark); Animate(ResultCard); foreach (var page in new[] { ScanPage, SetupPage, UpdatesPage, SettingsPage }) Animate(page); }
        SavePreferences();
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
        ((System.Windows.Controls.Panel)Content).Background = Background;
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
            bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create($"artifacts/screenshots/{state}.png"); encoder.Save(stream);
        }
        Close();
    }
    private sealed record Preferences(bool StartupChecks = true, string Theme = "Dark", bool Animations = true);

    private async Task LayoutTest()
    {
        var report = new List<string>();
        try
        {
            Directory.CreateDirectory("artifacts/layout");
            // Render a detached visual tree so the host monitor cannot clip larger test viewports.
            Content = null;
            RootLayout.SetValue(System.Windows.Documents.TextElement.FontFamilyProperty, FontFamily);
            RootLayout.SetValue(System.Windows.Documents.TextElement.FontSizeProperty, FontSize);
            RootLayout.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Ink");
            RootLayout.SetResourceReference(System.Windows.Controls.Panel.BackgroundProperty, "Canvas");
            foreach (bool dark in new[] { true, false })
            {
            ApplyAppearance(dark, false);
            string folder = Path.Combine("artifacts/layout", dark ? "dark" : "light");
            Directory.CreateDirectory(folder);
            foreach (var (width, height) in new[] { (480,640), (640,480), (800,600), (1024,768), (1366,768), (1920,1080), (2560,1440), (960,540), (1280,720), (1536,864) })
            {
                using var source = new HwndSource(new HwndSourceParameters("AgeCheck synthetic layout test")
                {
                    Width = width, Height = height, PositionX = -10000, PositionY = -10000,
                    WindowStyle = unchecked((int)0x80000000)
                });
                source.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
                RootLayout.Width = width; RootLayout.Height = height;
                source.RootVisual = RootLayout;
                foreach (var state in new[] { "ready", "passed", "under21", "expired", "review", "recovery", "setup", "updates", "settings" })
                {
                    Page(ScanPage); AdaptLayout(width);
                    switch (state)
                    {
                        case "passed": DemoResult(25); break;
                        case "under21": DemoResult(19); break;
                        case "expired": DemoResult(30, true); break;
                        case "review": ShowResult(new(Outcome.Review, "The scanner data is incomplete. Check the scanner settings and scan again."), true); break;
                        case "recovery": RecoveryDemo_Click(this, new RoutedEventArgs()); break;
                        case "setup": Page(SetupPage); break;
                        case "updates": Page(UpdatesPage); break;
                        case "settings": Page(SettingsPage); break;
                    }
                    RootLayout.Measure(new Size(width,height)); RootLayout.Arrange(new Rect(0,0,width,height)); RootLayout.UpdateLayout();
                    await Task.Delay(220);
                    if (PageScroll.ExtentWidth > PageScroll.ViewportWidth + 1) throw new InvalidOperationException($"Horizontal overflow: {width}x{height} {state}");
                    CheckTextBounds(PageHost, PageHost, $"{width}x{height} {state}");
                    var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(RootLayout); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create($"{folder}/{width}x{height}-{state}.png"); encoder.Save(stream);
                    report.Add($"PASS {(dark ? "dark" : "light")} {width}x{height} {state}");
                    if (state == "recovery")
                    {
                        ConfirmDatesButton.BringIntoView(); RootLayout.UpdateLayout(); await Task.Delay(60);
                        var point = ConfirmDatesButton.TransformToAncestor(PageScroll).Transform(new Point(0,0));
                        if (point.Y < -1 || point.Y + ConfirmDatesButton.ActualHeight > PageScroll.ActualHeight + 1)
                            throw new InvalidOperationException($"Confirmation unreachable: {width}x{height}");
                        var bottom = new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32); bottom.Render(RootLayout);
                        var bottomEncoder = new PngBitmapEncoder(); bottomEncoder.Frames.Add(BitmapFrame.Create(bottom));
                        using var bottomStream = File.Create($"{folder}/{width}x{height}-recovery-bottom.png"); bottomEncoder.Save(bottomStream);
                    }
                }
                source.RootVisual = null;
            }
            }
            File.WriteAllLines("artifacts/layout-results.txt",report); Close();
        }
        catch(Exception e) { File.WriteAllText("artifacts/layout-results.txt","FAILED: "+e.Message); Environment.Exit(1); }
    }
    private static void CheckTextBounds(DependencyObject node, FrameworkElement container, string context)
    {
        if (node is UIElement element && element.Visibility != Visibility.Visible) return;
        if (node is System.Windows.Controls.TextBlock block && block.ActualWidth > 0)
        {
            var bounds = block.TransformToAncestor(container).TransformBounds(new Rect(0,0,block.ActualWidth,block.ActualHeight));
            if (bounds.Left < -1 || bounds.Right > container.ActualWidth + 1) throw new InvalidOperationException($"Text outside content: {context}; label '{block.Text[..Math.Min(40,block.Text.Length)]}'; {bounds.Left:F1}..{bounds.Right:F1} of {container.ActualWidth:F1}");
        }
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++) CheckTextBounds(VisualTreeHelper.GetChild(node,i),container,context);
    }

    private async Task SelfTest()
    {
        var checks = new List<string>();
        void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); checks.Add(label); }
        try
        {
            Activate(); Page(ScanPage);
            await VerifyPreferencesAndKeyboard(checks);
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
            RecoveryDemo_Click(this,new RoutedEventArgs());
            Check(recoveredDates != null && RecoveryNotice.Visibility == Visibility.Visible, "Flattened scanner dates displayed for confirmation");
            ConfirmDates_Click(this,new RoutedEventArgs());
            Check(Phase.Text == "DATES CONFIRMED" && Headline.Text == "21 or older", "Operator confirmation performs date-only age check");
            Check(DemoBadge.Visibility == Visibility.Visible, "Recovery demo stays marked after confirmation");
            RecoveryDemo_Click(this,new RoutedEventArgs()); OnDeactivated(EventArgs.Empty);
            Check(recoveredDates == null && RecoveryNotice.Visibility == Visibility.Collapsed, "Focus loss clears recovered dates and confirmation");
            Activate();
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
