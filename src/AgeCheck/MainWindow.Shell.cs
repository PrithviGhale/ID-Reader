using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using AgeCheck.Core;

namespace AgeCheck;
public partial class MainWindow
{
    private void StartCustomer() { currentScanId=Guid.NewGuid(); currentScanTime=DateTimeOffset.Now; }
    private void PaintEdge(Outcome? outcome)
    {
        string key = outcome switch { Outcome.Passed=>"Accent", Outcome.Under21 or Outcome.Expired=>"Danger", Outcome.Review=>"WarmInk", _=>"Line" };
        var target=((SolidColorBrush)RootLayout.Resources[key]).Color;
        var from=edgeBrush.Color; edgeBrush.BeginAnimation(SolidColorBrush.ColorProperty,null); edgeBrush.Color=target;
        if(!suppressAnimations && motionEnabled && SystemParameters.ClientAreaAnimation)
            edgeBrush.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(from,target,TimeSpan.FromMilliseconds(300)){FillBehavior=FillBehavior.Stop});
    }
    private void Menu_Click(object sender,RoutedEventArgs e)
    {
        if(drawerOpen) { CloseDrawer(); return; }
        if(scan.Receiving || processing) Clear();
        drawerOpen=true; DrawerLayer.Visibility=Visibility.Visible;
        if(!suppressAnimations && motionEnabled && SystemParameters.ClientAreaAnimation)
        {
            var move=new TranslateTransform(); NavDrawer.RenderTransform=move;
            move.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-32,0,TimeSpan.FromMilliseconds(260)){EasingFunction=new QuarticEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});
            DrawerLayer.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)){FillBehavior=FillBehavior.Stop});
        }
        MenuButton.ToolTip="Hide navigation";
    }
    private void CloseDrawer()
    {
        drawerOpen=false; DrawerLayer.Visibility=Visibility.Collapsed; DrawerLayer.BeginAnimation(OpacityProperty,null);
        if(NavDrawer.RenderTransform is TranslateTransform move) move.BeginAnimation(TranslateTransform.XProperty,null);
        MenuButton.ToolTip="Show navigation";
    }
    private void DrawerScrim_Click(object sender,MouseButtonEventArgs e) => CloseDrawer();
    private void Minimize_Click(object sender,RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void Maximize_Click(object sender,RoutedEventArgs e)
    {
        if(WindowState==WindowState.Maximized) SystemCommands.RestoreWindow(this); else SystemCommands.MaximizeWindow(this);
    }
    private void Close_Click(object sender,RoutedEventArgs e) => SystemCommands.CloseWindow(this);
    private void UpdateWindowState()
    {
        WindowFrame.CornerRadius = new CornerRadius(WindowState==WindowState.Maximized?0:17);
        StatusGlow.CornerRadius = new CornerRadius(WindowState==WindowState.Maximized?0:14);
        MaximizeGlyph.Text = WindowState==WindowState.Maximized?"\uE923":"\uE922";
    }
    private void UpdateHistoryUI()
    {
        HistoryCountLabel.Text=history.Enabled?$"{history.Entries.Count} scans · up to {SessionHistory.Capacity}":"Session history is off";
        HistoryPrivacyLabel.Text=(history.Enabled ? "Session history is on. " : "Session history is off. ") + (rememberCustomers ? "Customer recognition is on: names and visit records are protected on this PC." : "Customer recognition is off. Current details still clear automatically.");
        HistoryEmptyLabel.Visibility=history.Entries.Count==0?Visibility.Visible:Visibility.Collapsed;
        HistoryEmptyLabel.Text=history.Enabled?"No scans yet. Scan a card or try a clearly marked demo.":"Turn on session history in Settings to keep names and results until the app closes.";
    }
    private void ClearHistory_Click(object sender,RoutedEventArgs e) { history.Clear(); HistoryDetail.Visibility=Visibility.Collapsed; UpdateHistoryUI(); }
    private void HistoryEnabled_Click(object sender,RoutedEventArgs e)
    {
        history.SetEnabled(HistoryEnabled.IsChecked==true); HistoryDetail.Visibility=Visibility.Collapsed; UpdateHistoryUI(); SavePreferences();
    }
    private void CopyName_Click(object sender,RoutedEventArgs e)
    {
        if(visibleResult?.DisplayName is { } name) CopyName(name);
    }
    private void HistoryCopy_Click(object sender,RoutedEventArgs e)
    {
        if(sender is FrameworkElement{DataContext:ScanEntry entry} && entry.CanCopy) CopyName(entry.Name);
    }
    private void HistoryBarcodeCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ScanEntry entry }) CopyScannerText(entry);
    }
    private void HistoryDetailCopyBarcode_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is ScanEntry entry) CopyScannerText(entry);
    }
    private void CopyBarcode_Click(object sender, RoutedEventArgs e)
    {
        if (currentRawBarcode is not { Length: > 0 } raw) return;
        try { writeClipboard(ShareableBarcode(raw)); CopyBarcodeStatus.Text = "Barcode text copied. It contains personal information; paste only where intended."; }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        { CopyBarcodeStatus.Text = "Clipboard is busy. Try copying again."; }
    }
    private static string ShareableBarcode(string raw) => raw.Replace("\u001e", "[RS]").Replace("\u001d", "[GS]").Replace("\u0003", "[ETX]")
        .Replace("\r", "[CR]").Replace("\n", "[LF]");
    private void CopyScannerText(ScanEntry entry)
    {
        if (entry.RawBarcode is not { Length: > 0 } raw || !history.Entries.Contains(entry)) return;
        string shareable = ShareableBarcode(raw);
        try { writeClipboard(shareable); BarcodeCopyStatus.Text = HistoryCopyStatus.Text = "Scanner text copied. It contains personal information; paste only where intended."; }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        { BarcodeCopyStatus.Text = HistoryCopyStatus.Text = "Clipboard is busy. Try copying again."; }
    }
    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryDetail == null || HistoryList.SelectedItem is not ScanEntry entry) return;
        CustomersList.SelectedItem = null;
        HistoryDetail.Visibility = Visibility.Visible;
        HistoryDetailHeading.Text = "SCAN DETAILS";
        HistoryDetailName.Text = entry.Name;
        HistoryDetailAge.Text = entry.AgeLabel + " at this scan";
        HistoryDetailStatus.Text = entry.StatusLabel + (entry.VisitLabel is { } visit ? " · " + visit : "");
        HistoryDetailTime.Text = "Scanned " + entry.ScannedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt");
        HistoryDetailBirth.Text = entry.Birth?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "Unavailable";
        HistoryDetailExpiry.Text = entry.Expiration?.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "Unavailable";
        HistoryDetailExpiryCountdown.Text = ExpirationCountdown.Describe(entry.Expiration, DateOnly.FromDateTime(entry.ScannedAt.LocalDateTime), entry.Outcome != Outcome.Review);
        HistoryDetailIssuer.Text = entry.IssuingState is { } state ? "Issued by " + state : "Issuing state unavailable";
        HistoryDetailReason.Text = entry.Detail ?? "";
        HistoryDetailDates.Visibility = Visibility.Visible;
        HistoryDetailCopyBarcode.Visibility = BarcodeCopyStatus.Visibility = entry.CanCopyBarcode ? Visibility.Visible : Visibility.Collapsed;
        BarcodeCopyStatus.Text = "Scanner text contains personal information. Paste it only where intended.";
        HistoryDetail.BringIntoView();
    }
    private void CustomersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryDetail == null || CustomersList.SelectedItem is not CustomerRecord entry) return;
        HistoryList.SelectedItem = null;
        HistoryDetail.Visibility = Visibility.Visible;
        HistoryDetailHeading.Text = "SAVED CUSTOMER";
        HistoryDetailName.Text = entry.Name;
        HistoryDetailAge.Text = "Current age: scan the card again";
        HistoryDetailStatus.Text = entry.VisitLabel;
        HistoryDetailTime.Text = entry.LastSeenLabel;
        HistoryDetailDates.Visibility = Visibility.Collapsed;
        HistoryDetailExpiryCountdown.Text = HistoryDetailIssuer.Text = "";
        HistoryDetailReason.Text = "Saved visits do not contain birth or expiration dates. Scan the card again to check today's age and expiration.";
        HistoryDetailCopyBarcode.Visibility = BarcodeCopyStatus.Visibility = Visibility.Collapsed;
        HistoryDetail.BringIntoView();
    }
    private void CopyName(string name)
    {
        try { writeClipboard(name); NameCopyStatus.Text=HistoryCopyStatus.Text="Copied. Paste into your rewards form."; }
        catch(Exception e) when(e is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        { NameCopyStatus.Text=HistoryCopyStatus.Text="Clipboard is busy. Try copying again."; }
    }
}
