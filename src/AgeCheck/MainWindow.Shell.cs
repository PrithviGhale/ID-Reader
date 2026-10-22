using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private void ClearHistory_Click(object sender,RoutedEventArgs e) { history.Clear(); UpdateHistoryUI(); }
    private void HistoryEnabled_Click(object sender,RoutedEventArgs e)
    {
        history.SetEnabled(HistoryEnabled.IsChecked==true); UpdateHistoryUI(); SavePreferences();
    }
    private void CopyName_Click(object sender,RoutedEventArgs e)
    {
        if(visibleResult?.DisplayName is { } name) CopyName(name);
    }
    private void HistoryCopy_Click(object sender,RoutedEventArgs e)
    {
        if(sender is FrameworkElement{DataContext:ScanEntry entry} && entry.CanCopy) CopyName(entry.Name);
    }
    private void CopyName(string name)
    {
        try { writeClipboard(name); NameCopyStatus.Text=HistoryCopyStatus.Text="Copied. Paste into your rewards form."; }
        catch(Exception e) when(e is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        { NameCopyStatus.Text=HistoryCopyStatus.Text="Clipboard is busy. Try copying again."; }
    }
}
