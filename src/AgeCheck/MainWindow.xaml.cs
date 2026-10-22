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
    private readonly SessionHistory history = new();
    private Guid currentScanId;
    private DateTimeOffset currentScanTime;
    private bool drawerOpen;
    private readonly SolidColorBrush edgeBrush = new(Colors.Transparent);
    private Action<string> writeClipboard = Clipboard.SetText;
    private readonly DispatcherTimer quiet = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private readonly DispatcherTimer deadline = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer privacy = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly UpdateManager? updater;
    private UpdateInfo? update;
    private bool updateBusy, downloaded, processing, inputBlocked, showingResult;
    private int scanGeneration;
    private int inputRevision;
    private long lastInputAt;
    private CheckResult? recoveredDates;
    private bool recoveryDemo;
    private bool darkTheme = true;
    private bool motionEnabled = true;
    private CheckResult? visibleResult;
    private UIElement? activePage;
    private static bool suppressAnimations;
    private readonly string settingsPath;
    private readonly CustomerStore customers;
    private bool rememberCustomers = true;
    private Guid rememberedScan;
    private CustomerVisit? currentVisit;
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    private bool CanCapture => IsActive && ScanPage.Visibility == Visibility.Visible && !inputBlocked && !drawerOpen;

    public MainWindow(string[] args)
    {
        bool developerMode = args.Any(a => a is "--self-test" or "--image-test" or "--layout-test" or "--screenshot" or "--motion-test");
        settingsPath = developerMode ? Path.Combine(Environment.CurrentDirectory, "artifacts", "test-preferences.json")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgeCheck", "settings.json");
        customers = new CustomerStore(Path.Combine(Path.GetDirectoryName(settingsPath)!, "customers.dat"), memoryOnly: developerMode);
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
                history.SetEnabled(prefs?.SessionHistory ?? true);
                rememberCustomers = prefs?.RememberCustomers ?? true;
            }
        }
        catch { /* A damaged settings file must not prevent offline use. */ }
        MotionEnabled.IsChecked = motionEnabled;
        HistoryEnabled.IsChecked = history.Enabled;
        RecognitionEnabled.IsChecked = rememberCustomers;
        HistoryList.ItemsSource = history.Entries;
        UpdateHistoryUI();
        RefreshCustomers();
        WindowFrame.BorderBrush = edgeBrush;
        activePage = ScanPage; ApplyAppearance(darkTheme, false);
        SourceInitialized += (_, _) => Theme.TitleBar(this, darkTheme);
        quiet.Tick += async (_, _) => await FinishScan();
        deadline.Tick += (_, _) => { Clear(); inputBlocked = true; ShowResult(new(Outcome.Review, "Scan exceeded 20 seconds. Clear and rescan."), false); };
        privacy.Tick += (_, _) => Clear();
        Deactivated += (_, _) => Clear();
        Closed += (_, _) => { quiet.Stop(); deadline.Stop(); privacy.Stop(); scan.Clear(); history.Clear(); };
        StateChanged += (_, _) => UpdateWindowState();
        PreviewTextInput += (_, e) => { if (CanCapture) { Receive(e.Text); e.Handled = true; } };
        PreviewKeyDown += OnKey;
        Loaded += async (_, _) =>
        {
            Focus();
            AdaptLayout(ActualWidth);
            if (args.Contains("--layout-test")) { suppressAnimations = true; await LayoutTest(); return; }
            if (args.Contains("--self-test")) { await SelfTest(); return; }
            if (args.Contains("--image-test")) { await ImageSelfTest(); return; }
            if (args.Contains("--screenshot")) { suppressAnimations = true; await ExportScreenshots(); return; }
            if (args.Contains("--motion-test")) { await MotionTest(); return; }
            Animate(PageHost, true); Animate(BrandMark, true);
            if (StartupChecks.IsChecked == true) await CheckUpdates();
        };
    }
    private void OnKey(object sender, KeyEventArgs e)
    {
        if (drawerOpen && e.Key == Key.Escape) { CloseDrawer(); e.Handled = true; return; }
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
            StartCustomer();
            privacy.Stop(); deadline.Start(); Phase.Text = "RECEIVING"; Headline.Text = "Reading your scan";
            ResultSubtitle.Text = "Waiting for the complete scanner input.";
            Detail.Text = "Keep the scanner aimed until all data has arrived."; Symbol.Text = "↓";
            PaintStatus(null);
        }
        if (!scan.Append(text))
        {
            Clear(); inputBlocked = true;
            ShowResult(new(Outcome.Review, "Input limit exceeded. Choose Clear / Next customer before rescanning."), false);
            return;
        }
        inputRevision++; lastInputAt = System.Diagnostics.Stopwatch.GetTimestamp();
        quiet.Stop(); quiet.Interval = TimeSpan.FromMilliseconds(700); quiet.Start();
    }
    private async Task FinishScan()
    {
        quiet.Stop();
        if (!scan.Receiving) return;
        if (processing) { quiet.Start(); return; }
        processing = true; int generation = scanGeneration, revision = inputRevision;
        Phase.Text = "PROCESSING"; Headline.Text = "Checking dates…";
        string raw = scan.Snapshot();
        var result = await Task.Run(() => Barcode.Check(raw, Today, allowScannerRecovery: true));
        processing = false;
        if (generation != scanGeneration || revision != inputRevision || !IsActive) return;
        // Incomplete input gets additional time to survive USB/keyboard pauses.
        // The absolute deadline still bounds a never-ending transmission.
        double idle = System.Diagnostics.Stopwatch.GetElapsedTime(lastInputAt).TotalMilliseconds;
        if (result.Outcome == Outcome.Review && idle < 2000)
        {
            quiet.Interval = TimeSpan.FromMilliseconds(Math.Max(1, 2000 - idle)); quiet.Start();
            Phase.Text = "RECEIVING"; Headline.Text = "Waiting for the rest of the scan";
            return;
        }
        deadline.Stop(); scan.Clear(); ShowResult(result, false, raw);
    }
    private void Clear()
    {
        imageGeneration++;
        scanGeneration++; scan.Clear(); quiet.Stop(); deadline.Stop(); privacy.Stop(); inputBlocked = false; showingResult = false;
        recoveredDates = null; recoveryDemo = false; RecoveryNotice.Visibility = Visibility.Collapsed;
        ResetCustomerDetails();
        visibleResult = null;
        ImageResultPanel.Visibility = Visibility.Collapsed;
        currentScanId = Guid.Empty;
        Phase.Text = "READY TO SCAN"; Headline.Text = "Ready to scan"; Symbol.Text = "⌁";
        ResultSubtitle.Text = "The next card starts a fresh check.";
        Detail.Text = "Scan the large barcode on the back of the ID. Keep this window active.";
        BirthLabel.Text = ExpiryLabel.Text = "—"; DemoBadge.Visibility = Visibility.Collapsed;
        NamePanel.Visibility = Visibility.Collapsed; NameLabel.Text = "NAME NOT AVAILABLE"; CopyNameButton.IsEnabled = false;
        NameCopyStatus.Text = "Compare the printed name before copying.";
        HistoryCopyStatus.Text = "";
        ChecksLabel.Text = "Local checks · No cloud upload";
        PaintStatus(null);
    }
    private void ShowResult(CheckResult result, bool demo, string? rawBarcode = null)
    {
        visibleResult = result;
        if (currentScanId == Guid.Empty) StartCustomer();
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
            Outcome.Passed => result.KeyboardTransport ? "Scanned dates meet the 21+ age threshold" : "Barcode age check passed",
            Outcome.Under21 => "Barcode birth date is below the age threshold",
            Outcome.Expired => "Barcode expiration date has passed",
            _ => result.NeedsDateConfirmation ? "Scanner text recovered · confirmation needed" : "Manual review required"
        };
        Detail.Text = result.Detail;
        PaintStatus(result.Outcome);
        NamePanel.Visibility = Visibility.Visible;
        NameLabel.Text = result.DisplayName ?? "NAME NOT AVAILABLE";
        CopyNameButton.IsEnabled = result.DisplayName != null;
        NameCopyStatus.Text = result.NameNote ?? "Compare the printed name before copying.";
        ChecksLabel.Text = result.Outcome == Outcome.Review ? "Review needed · This is not a fake-ID verdict" : "Date and field checks complete · Authenticity unverified";
        ShowCustomerDetails(result, demo);
        history.Record(currentScanId, result, demo, false, currentScanTime, Today, CurrentVisitLabel, rawBarcode); UpdateHistoryUI();
        BirthLabel.Text = result.Birth?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—";
        ExpiryLabel.Text = result.Expiration?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—";
        Animate(ResultCard); privacy.Stop(); privacy.Start();
    }
    private void Animate(UIElement element, bool opening = false)
    {
        element.BeginAnimation(OpacityProperty, null);
        StopTransform(element.RenderTransform);
        element.Opacity = 1;
        element.RenderTransform = Transform.Identity;
        if (suppressAnimations || !motionEnabled || !SystemParameters.ClientAreaAnimation) return;
        var duration = TimeSpan.FromMilliseconds(opening ? 520 : 340);
        var easing = new QuarticEase { EasingMode = EasingMode.EaseOut };
        var move = new TranslateTransform(); var scale = new ScaleTransform(1,1);
        var transforms = new TransformGroup(); transforms.Children.Add(scale); transforms.Children.Add(move);
        element.RenderTransformOrigin = new Point(.5,.4); element.RenderTransform=transforms;
        DoubleAnimation Tween(double from,double to)=>new(from,to,duration){EasingFunction=easing,FillBehavior=FillBehavior.Stop};
        element.BeginAnimation(OpacityProperty,Tween(opening?0:.15,1));
        move.BeginAnimation(TranslateTransform.XProperty,Tween(opening?0:20,0));
        move.BeginAnimation(TranslateTransform.YProperty,Tween(opening?20:5,0));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,Tween(opening ? .975 : .985,1));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,Tween(opening ? .975 : .985,1));
    }
    private static void StopTransform(Transform transform)
    {
        if(transform is TransformGroup group) foreach(var child in group.Children) StopTransform(child);
        if(transform is TranslateTransform move) { move.BeginAnimation(TranslateTransform.XProperty,null); move.BeginAnimation(TranslateTransform.YProperty,null); }
        if(transform is ScaleTransform scale) { scale.BeginAnimation(ScaleTransform.ScaleXProperty,null); scale.BeginAnimation(ScaleTransform.ScaleYProperty,null); }
    }
    private void Page(UIElement page)
    {
        if (page == UpdatesPage) page = SettingsPage;
        CloseDrawer();
        Clear(); ScanPage.Visibility = ImagePage.Visibility = SetupPage.Visibility = SettingsPage.Visibility = HistoryPage.Visibility = Visibility.Collapsed;
        activePage = page;
        page.Visibility = Visibility.Visible; Animate(page);
        if (page == HistoryPage) RefreshCustomers();
        PaintNavigation();
        PageScroll.ScrollToTop();
    }
    private void ScanPage_Click(object sender, RoutedEventArgs e) => Page(ScanPage);
    private void ImagePage_Click(object sender, RoutedEventArgs e) => Page(ImagePage);
    private void SetupPage_Click(object sender, RoutedEventArgs e) => Page(SetupPage);
    private void UpdatesPage_Click(object sender, RoutedEventArgs e) => Page(UpdatesPage);
    private void SettingsPage_Click(object sender, RoutedEventArgs e) => Page(SettingsPage);
    private void HistoryPage_Click(object sender, RoutedEventArgs e) => Page(HistoryPage);
    private void Clear_Click(object sender, RoutedEventArgs e) => Clear();
    private void DemoResult(int years, bool expired = false)
    {
        Clear(); ShowResult(Barcode.Check(Demo.CreateNamed(Today.AddYears(-years), expired ? Today.AddDays(-1) : Today.AddYears(3), Today.AddYears(-1)), Today), true);
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
        var original = recoveredDates;
        var result = Barcode.Evaluate(birth, expiry, Today);
        result = result with { FirstName = original.FirstName, LastName = original.LastName, NameNote = original.NameNote,
            Detail = "You confirmed both dates match the printed card. This is a date-only check; the full barcode was not verified. Compare the photograph and customer." };
        ShowResult(result, demo);
        history.Record(currentScanId,result,demo,true,currentScanTime,Today,CurrentVisitLabel); UpdateHistoryUI();
        Phase.Text = "DATES CONFIRMED";
        if (result.Outcome == Outcome.Passed) ResultSubtitle.Text = "Confirmed dates meet the 21+ age threshold";
    }
    private void AdaptLayout(double width)
    {
        bool compact = width < 800;
        BrandWordmark.Visibility = width < 600 ? Visibility.Collapsed : Visibility.Visible;
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
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(new Preferences(StartupChecks.IsChecked == true, darkTheme ? "Dark" : "Light", motionEnabled, history.Enabled, rememberCustomers)));
            SettingsStatus.Text = "Preferences saved on this PC. Customer recognition and session history have separate controls.";
        }
        catch { SettingsStatus.Text = "Could not save preferences. They still apply for this session."; UpdateStatus.Text = "Could not save preferences. They still apply for this session."; }
    }
    private void ApplyAppearance(bool dark, bool save)
    {
        darkTheme = dark; Theme.Apply(dark, RootLayout);
        Theme.TitleBar(this, dark);
        PaintNavigation(); PaintStatus(visibleResult?.Outcome);
        LightThemeButton.SetResourceReference(BackgroundProperty, dark ? "Quiet" : "Accent");
        LightThemeButton.SetResourceReference(ForegroundProperty, dark ? "Ink" : "AccentInk");
        DarkThemeButton.SetResourceReference(BackgroundProperty, dark ? "Accent" : "Quiet");
        DarkThemeButton.SetResourceReference(ForegroundProperty, dark ? "AccentInk" : "Ink");
        if (save) SavePreferences();
    }
    private void PaintNavigation()
    {
        foreach (var (button,page) in new[] { (ScanNav, ScanPage), (ImageNav, ImagePage), (HistoryNav, HistoryPage), (SetupNav, SetupPage), (SettingsDrawerNav, SettingsPage), (SettingsNav, SettingsPage) })
        {
            button.SetResourceReference(BackgroundProperty, page == activePage ? "AccentSoft" : "Surface");
            button.SetResourceReference(ForegroundProperty, page == activePage ? "AccentText" : "Muted");
        }
        LocalBadge.Text = "/  " + (activePage == ImagePage ? "IMAGE" : activePage == HistoryPage ? "HISTORY" : activePage == SetupPage ? "SETUP" : activePage == UpdatesPage ? "UPDATES" : activePage == SettingsPage ? "SETTINGS" : "SCAN");
        HomeButton.Visibility = activePage == ScanPage ? Visibility.Collapsed : Visibility.Visible;
    }
    private void PaintStatus(Outcome? outcome)
    {
        string ink = outcome is Outcome.Under21 or Outcome.Expired ? "Danger" : outcome == Outcome.Review ? "WarmInk" : "AccentText";
        string surface = outcome is Outcome.Under21 or Outcome.Expired ? "DangerSoft" : outcome == Outcome.Review ? "Warm" : "AccentSoft";
        Symbol.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, ink);
        AgeLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, ink);
        SymbolBox.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, surface);
        PaintEdge(outcome);
    }
    private void LightTheme_Click(object sender, RoutedEventArgs e) => ApplyAppearance(false, true);
    private void DarkTheme_Click(object sender, RoutedEventArgs e) => ApplyAppearance(true, true);
    private void MotionEnabled_Click(object sender, RoutedEventArgs e)
    {
        motionEnabled = MotionEnabled.IsChecked == true;
        if (!motionEnabled) { Animate(PageHost); Animate(BrandMark); Animate(ResultCard); foreach (var page in new[] { ScanPage, ImagePage, SetupPage, UpdatesPage, SettingsPage, HistoryPage }) Animate(page); PaintEdge(visibleResult?.Outcome); }
        SavePreferences();
    }
    private void UpdateButtons()
    {
        CheckButton.IsEnabled = !updateBusy && updater != null;
        DownloadButton.IsEnabled = !updateBusy && update != null && !downloaded;
        RestartButton.IsEnabled = !updateBusy && downloaded;
        DownloadProgress.Visibility = updateBusy && update != null && !downloaded ? Visibility.Visible : Visibility.Collapsed;
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
            SetUpdateAlert(found != null);
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
        foreach (string state in new[] { "ready", "passed", "under21", "expired", "review", "image", "setup", "updates" })
        {
            Page(ScanPage);
            switch (state)
            {
                case "passed": DemoResult(25); break;
                case "under21": DemoResult(19); break;
                case "expired": DemoResult(30, true); break;
                case "review": ShowResult(new(Outcome.Review, "Incomplete barcode. Check scanner settings and rescan."), true); break;
                case "image": Page(ImagePage); break;
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
    private sealed record Preferences(bool StartupChecks = true, string Theme = "Dark", bool Animations = true, bool SessionHistory = true, bool RememberCustomers = true);

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
                foreach (var state in new[] { "ready", "passed", "under21", "expired", "review", "recovery", "image", "history", "returning", "customers", "setup", "updates", "settings", "updatealert", "menu" })
                {
                    Page(ScanPage); AdaptLayout(width);
                    SetUpdateAlert(false);
                    switch (state)
                    {
                        case "passed": DemoResult(25); break;
                        case "under21": DemoResult(19); break;
                        case "expired": DemoResult(30, true); break;
                        case "review": ShowResult(new(Outcome.Review, "The scanner data is incomplete. Check the scanner settings and scan again."), true); break;
                        case "recovery": RecoveryDemo_Click(this, new RoutedEventArgs()); break;
                        case "image": Page(ImagePage); break;
                        case "setup": Page(SetupPage); break;
                        case "updates": Page(UpdatesPage); break;
                        case "settings": Page(SettingsPage); break;
                        case "history": DemoResult(25); DemoResult(19); Page(HistoryPage); break;
                        case "returning":
                            DemoResult(25); WelcomePanel.Visibility = Visibility.Visible;
                            WelcomeTitle.Text = "Welcome back, ALEX SAMPLE.";
                            WelcomeDetail.Text = "Previously scanned yesterday. Scan 3 on this PC. Today's card was checked again."; break;
                        case "customers":
                            Page(HistoryPage); CustomerCountLabel.Text = "DEMO / 1 protected customer";
                            CustomersList.ItemsSource = new[] { new CustomerRecord(new string('A',64),"ALEX SAMPLE",DateTimeOffset.Now.AddDays(-7),DateTimeOffset.Now,3) }; break;
                        case "updatealert": Page(SettingsPage); SetUpdateAlert(true); break;
                        case "menu": Menu_Click(this,new RoutedEventArgs()); break;
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
            await VerifyWorkspaceFeatures(checks);
            await VerifyCustomerFeatures(checks);
            var payload = Demo.Create(Today.AddYears(-25), Today.AddYears(2), Today.AddYears(-1));
            Receive(payload[..40]);
            Check(Phase.Text == "RECEIVING", "Receiving state");
            await Task.Delay(100);
            Check(scan.Receiving && !showingResult, "Embedded newlines do not submit");
            Receive(payload[40..]);
            await WaitForScanResult();
            Check(Headline.Text.StartsWith("21 or older"), "Complete synthetic keyboard stream passes");
            Receive("@"); Check(BirthLabel.Text == "—", "Next scan clears dates immediately");
            Clear(); DemoResult(19); Check(DemoBadge.Visibility == Visibility.Visible, "Demo marker visible");
            OnDeactivated(EventArgs.Empty); Check(BirthLabel.Text == "—" && !scan.Receiving, "Deactivation clears results and input");
            Activate(); Receive("@partial"); await WaitForScanResult();
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
        catch (Exception e) { Directory.CreateDirectory("artifacts"); checks.Add("FAILED: " + e.Message); File.WriteAllLines("artifacts/ui-test-results.txt", checks); Environment.Exit(1); }
    }
}
