using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AgeCheck.Core;

namespace AgeCheck;
public partial class MainWindow
{
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
        await Task.Delay(1100);
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
                await Task.Delay(600);
                Check(Math.Abs(PageHost.Opacity-1)<.001 && PageHost.RenderTransform is TranslateTransform { Y:0 },"Opening reveal ends at full opacity and zero offset");
                Page(SetupPage); Check(SetupPage.HasAnimatedProperties,"Page transition begins");
                await Task.Delay(400);
                Check(Math.Abs(SetupPage.Opacity-1)<.001 && SetupPage.RenderTransform is TranslateTransform { Y:0 },"Page transition ends at rest");
                for(int i=0;i<12;i++) Page(i%2==0?ScanPage:SettingsPage);
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);
                await Task.Delay(650);
                Check(Math.Abs(SettingsPage.Opacity-1)<.001,$"Rapid tab changes settle correctly (opacity {SettingsPage.Opacity:F3})");
            }
            else report.Add("Windows animations disabled: motion correctly skipped");
            motionEnabled=false; Animate(PageHost,true);
            Check(!PageHost.HasAnimatedProperties && PageHost.Opacity==1,"Reduced motion cancels active animations");
            Directory.CreateDirectory("artifacts"); File.WriteAllLines("artifacts/motion-results.txt",report); Close();
        }
        catch(Exception e) { Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/motion-results.txt","FAILED: "+e.Message);Environment.Exit(1); }
    }
}
