using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AgeCheck.Core;

namespace AgeCheck;
public partial class MainWindow
{
    private async Task WaitForScanResult()
    {
        // Allow dispatcher and thread-pool scheduling to vary on busy build machines.
        // The scanner still uses its real quiet-period timer and background parser.
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (!showingResult && DateTime.UtcNow < timeout) await Task.Delay(50);
        if (!showingResult)
            throw new InvalidOperationException($"Scan did not finish: active={IsActive}, receiving={scan.Receiving}, processing={processing}, phase={Phase.Text}");
    }
    private async Task VerifyPreferencesAndKeyboard(List<string> checks)
    {
        void Check(bool ok, string label) { if(!ok) throw new InvalidOperationException(label); checks.Add(label); }
        // All persistence in developer modes goes to artifacts, never the user's real settings.
        ApplyAppearance(false,true);
        Check(((SolidColorBrush)Application.Current.Resources["Ink"]).Color.R < 100,"Light theme applied");
        Check(((SolidColorBrush)RootLayout.Background).Color.R > 200,"Light canvas brush updates on the live visual tree");
        var saved = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath));
        Check(saved?.Theme == "Light", "Light theme preference saved");
        ApplyAppearance(true,true);
        Check(((SolidColorBrush)Application.Current.Resources["Ink"]).Color.R > 200,"Dark theme applied");
        Check(((SolidColorBrush)RootLayout.Background).Color.R < 50,"Dark canvas brush updates on the live visual tree");
        saved = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath));
        Check(saved?.Theme == "Dark", "Dark theme preference saved");
        foreach(bool dark in new[]{false,true})
        {
            ApplyAppearance(dark,false);
            foreach(var surface in new[]{"Surface","Canvas","Inset"})
                foreach(var ink in new[]{"Ink","Muted"}) Check(Contrast(ink,surface)>=4.5,$"{(dark?"Dark":"Light")} {ink}/{surface} contrast >= 4.5");
        }
        ApplyAppearance(true,false);
        var legacy = JsonSerializer.Deserialize<Preferences>("{\"StartupChecks\":false}");
        Check(legacy is { StartupChecks:false, Theme:"Dark", Animations:true },"Old preferences migrate with safe defaults");
        MotionEnabled.IsChecked=false; MotionEnabled_Click(this,new RoutedEventArgs());
        Check(JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath))?.Animations==false,"Animation preference saved");
        MotionEnabled.IsChecked=true; MotionEnabled_Click(this,new RoutedEventArgs());
        Page(ScanPage); Activate();
        string sample=Demo.KeyboardHeader(Today.AddYears(-26),Today.AddYears(3),Today.AddYears(-1));
        for(int offset=0;offset<sample.Length;offset+=13)
        {
            var composition = new TextComposition(InputManager.Current,this,sample.Substring(offset,Math.Min(13,sample.Length-offset)));
            var input = new TextCompositionEventArgs(Keyboard.PrimaryDevice,composition) { RoutedEvent=TextCompositionManager.PreviewTextInputEvent };
            RaiseEvent(input); Check(input.Handled,"Synthetic scanner chunk captured through WPF text route");
        }
        await WaitForScanResult();
        Check(recoveredDates?.NeedsDateConfirmation==true && BirthLabel.Text!="—","Accented keyboard header recovers through real input handler");
        ConfirmDates_Click(this,new RoutedEventArgs());
        Check(Phase.Text=="DATES CONFIRMED" && Headline.Text=="21 or older","Routed scanner dates confirmed successfully");
        Page(SettingsPage);
        var blocked = new TextCompositionEventArgs(Keyboard.PrimaryDevice,new TextComposition(InputManager.Current,this,sample)) { RoutedEvent=TextCompositionManager.PreviewTextInputEvent };
        RaiseEvent(blocked); Check(!scan.Receiving,"Scanner capture disabled outside Scan page");
        Page(ScanPage);
    }
    private static double Contrast(string ink,string surface)
    {
        double L(string name)
        {
            var c=((SolidColorBrush)Application.Current.Resources[name]).Color;
            double F(byte b) { double v=b/255.0; return v<=0.04045?v/12.92:Math.Pow((v+0.055)/1.055,2.4); }
            return .2126*F(c.R)+.7152*F(c.G)+.0722*F(c.B);
        }
        double a=L(ink),b=L(surface); return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);
    }
    private async Task VerifyWorkspaceFeatures(List<string> checks)
    {
        void Check(bool ok,string label) { if(!ok) throw new InvalidOperationException(label); checks.Add(label); }
        var previousClipboard=writeClipboard; string? copied=null; writeClipboard=value=>copied=value;
        bool previousSuppression=suppressAnimations; suppressAnimations=true;
        try
        {
            history.SetEnabled(true); HistoryEnabled.IsChecked=true; history.Clear(); Page(ScanPage); Activate();
            Check(WindowStyle==WindowStyle.None && System.Windows.Shell.WindowChrome.GetWindowChrome(this)!=null,"Native title bar replaced by integrated window chrome");
            DemoResult(21);
            Check(edgeBrush.Color==((SolidColorBrush)RootLayout.Resources["Accent"]).Color,"Exactly 21 uses green window edges");
            Check(NameLabel.Text=="ALEX SAMPLE" && CopyNameButton.IsEnabled,"First and last name displayed in uppercase");
            CopyName_Click(this,new RoutedEventArgs()); Check(copied=="ALEX SAMPLE","Name copy contains first and last only");
            Check(history.Entries.Count==1 && history.Entries[0].Demo,"Demo history entry is explicitly marked");
            string next=Demo.CreateNamed(Today.AddYears(-19),Today.AddYears(2),Today.AddYears(-1),"JAMIE","EXAMPLE");
            Receive(next[..30]); Check(!CopyNameButton.IsEnabled && NameLabel.Text=="NAME NOT AVAILABLE","New card immediately clears the previous name");
            Receive(next[30..]); await WaitForScanResult();
            Check(NameLabel.Text=="JAMIE EXAMPLE" && Headline.Text=="Under 21","Second scan replaces the customer without Next button");
            Check(edgeBrush.Color==((SolidColorBrush)RootLayout.Resources["Danger"]).Color,"Under 21 uses red window edges");
            Check(history.Entries.Count==2 && history.Entries[0].Name=="JAMIE EXAMPLE","Both scans appear separately in session history");
            DemoResult(30,true); Check(edgeBrush.Color==((SolidColorBrush)RootLayout.Resources["Danger"]).Color,"Expired ID uses red window edges");
            RecoveryDemo_Click(this,new RoutedEventArgs()); int before=history.Entries.Count;
            Check(edgeBrush.Color==((SolidColorBrush)RootLayout.Resources["WarmInk"]).Color,"Review uses orange window edges");
            ConfirmDates_Click(this,new RoutedEventArgs());
            Check(history.Entries.Count==before && history.Entries[0].Confirmed,"Confirmation updates history without duplicates");
            Check(NameLabel.Text=="ALEX SAMPLE","Recovered name survives date confirmation");
            Page(HistoryPage); OnDeactivated(EventArgs.Empty);
            Check(history.Entries.Count==before,"Session history remains available after current result clears");
            var button=new System.Windows.Controls.Button{DataContext=history.Entries[0]};
            HistoryCopy_Click(button,new RoutedEventArgs()); Check(copied=="ALEX SAMPLE","History copy uses the same safe name-only action");
            Page(ScanPage); Activate(); Menu_Click(this,new RoutedEventArgs());
            Check(drawerOpen && DrawerLayer.Visibility==Visibility.Visible && !CanCapture,"Menu opens and pauses scan capture");
            Menu_Click(this,new RoutedEventArgs()); Check(!drawerOpen && DrawerLayer.Visibility==Visibility.Collapsed,"Hamburger hides the menu again");
            Maximize_Click(this,new RoutedEventArgs()); await Task.Delay(250);
            Check(WindowState==WindowState.Maximized,"Integrated maximize control works");
            Maximize_Click(this,new RoutedEventArgs()); await Task.Delay(250);
            Check(WindowState==WindowState.Normal && WindowFrame.CornerRadius.TopLeft>0,"Integrated restore control restores rounded frame");
            HistoryEnabled.IsChecked=false; HistoryEnabled_Click(this,new RoutedEventArgs());
            Check(!history.Enabled && history.Entries.Count==0,"Disabling history immediately clears all entries");
            Check(JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath))?.SessionHistory==false,"History preference saves without card data");
            HistoryEnabled.IsChecked=true; HistoryEnabled_Click(this,new RoutedEventArgs()); ClearHistory_Click(this,new RoutedEventArgs());
            Page(ScanPage); Activate();
        }
        finally { writeClipboard=previousClipboard; suppressAnimations=previousSuppression; }
    }
    private async Task MotionTest()
    {
        var report=new List<string>();
        void Check(bool ok,string label) { if(!ok) throw new InvalidOperationException(label); report.Add(label); }
        try
        {
            motionEnabled=true; suppressAnimations=false;
            Animate(PageHost,true);
            if(SystemParameters.ClientAreaAnimation)
            {
                Check(PageHost.HasAnimatedProperties,"Opening reveal begins");
                await Task.Delay(700);
                Check(AtRest(PageHost),"Opening reveal ends at full opacity and zero offset");
                Page(SetupPage); Check(SetupPage.HasAnimatedProperties,"Page transition begins");
                await Task.Delay(500);
                Check(AtRest(SetupPage),"Page transition ends at rest");
                for(int i=0;i<12;i++) Page(i%2==0?ScanPage:SettingsPage);
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);
                await Task.Delay(650);
                Check(Math.Abs(SettingsPage.Opacity-1)<.001,$"Rapid tab changes settle correctly (opacity {SettingsPage.Opacity:F3})");
                Menu_Click(this,new RoutedEventArgs()); await Task.Delay(380);
                Check(NavDrawer.RenderTransform is TranslateTransform { X:0 },"Drawer reveal settles at its normal position");
                Menu_Click(this,new RoutedEventArgs()); Check(!drawerOpen,"Drawer closes cleanly");
            }
            else report.Add("Windows animations disabled: motion correctly skipped");
            motionEnabled=false; Animate(PageHost,true);
            Check(!PageHost.HasAnimatedProperties && PageHost.Opacity==1,"Reduced motion cancels active animations");
            Directory.CreateDirectory("artifacts"); File.WriteAllLines("artifacts/motion-results.txt",report); Close();
        }
        catch(Exception e) { Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/motion-results.txt","FAILED: "+e.Message);Environment.Exit(1); }
    }
    private static bool AtRest(UIElement element)
    {
        var matrix=element.RenderTransform.Value;
        return Math.Abs(element.Opacity-1)<.001 && Math.Abs(matrix.OffsetX)<.001 && Math.Abs(matrix.OffsetY)<.001 && Math.Abs(matrix.M11-1)<.001 && Math.Abs(matrix.M22-1)<.001;
    }
}
