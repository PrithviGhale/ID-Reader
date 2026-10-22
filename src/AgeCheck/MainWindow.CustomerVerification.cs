using System.IO;
using System.Text;
using System.Windows;
using AgeCheck.Core;

namespace AgeCheck;
public partial class MainWindow
{
    private async Task VerifyCustomerFeatures(List<string> checks)
    {
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); checks.Add(label); }
        Page(SettingsPage);
        Check(HomeButton.Visibility == Visibility.Visible && UpdatesPage.IsVisible, "Updates live inside Settings with a visible return-home button");
        SetUpdateAlert(true);
        Check(UpdateBadge.Visibility == Visibility.Visible && SettingsUpdateBadge.Visibility == Visibility.Visible, "Available update marks both Settings and its update card");
        SetUpdateAlert(false); ScanPage_Click(this,new RoutedEventArgs());
        Check(ScanPage.Visibility == Visibility.Visible && HomeButton.Visibility == Visibility.Collapsed, "Home returns directly to the Scan page");
        customers.DeleteAll(); rememberCustomers=true; RecognitionEnabled.IsChecked=true; Activate();
        string sample=Demo.CompleteKeyboard(Today.AddYears(-21),Today.AddYears(2),Today.AddYears(-1));
        Receive(sample); await WaitForScanResult();
        Check(AgeLabel.Text == "21" && BirthdayLabel.Text.Contains("birthday today"), "Current age and birthday-today message appear beside the result");
        Check(customers.Records.Count == 1 && customers.Records[0].Visits == 1 && WelcomePanel.Visibility == Visibility.Collapsed, "First complete named scan creates one customer record");
        ShowResult(visibleResult!,false);
        Check(customers.Records[0].Visits == 1, "Repainting the same scan never adds a visit");
        Receive(sample); await WaitForScanResult();
        Check(WelcomePanel.Visibility == Visibility.Visible && WelcomeTitle.Text.Contains("ALEX SAMPLE") && customers.Records[0].Visits == 2, "A returning card shows welcome back and increments once");
        Check(history.Entries[0].Age == 21 && history.Entries[0].VisitLabel!.Contains("Seen before"), "History shows age and recognition status");
        Receive(Demo.CompleteKeyboard(Today.AddYears(-19).AddDays(1),Today.AddYears(2),Today.AddYears(-1))); await WaitForScanResult();
        Check(Headline.Text == "Under 21" && Under21Panel.Visibility == Visibility.Visible && Under21Label.Text.Contains("until 21"), "Underage result includes an exact days-until-21 countdown");
        Check(((System.Windows.Media.SolidColorBrush)AgeLabel.Foreground).Color == ((System.Windows.Media.SolidColorBrush)RootLayout.Resources["Danger"]).Color, "Underage age readout uses the warning color rather than the passing color");
        OnDeactivated(EventArgs.Empty);
        Check(AgeLabel.Text == "\u2014" && BirthdayPanel.Visibility == Visibility.Collapsed && WelcomePanel.Visibility == Visibility.Collapsed && currentVisit == null, "Focus loss clears age, birthday and welcome details");
        Activate(); Page(SettingsPage); RecognitionEnabled.IsChecked=false; RecognitionEnabled_Click(this,new RoutedEventArgs());
        int count=customers.Records.Count; Page(ScanPage); Receive(sample); await WaitForScanResult();
        Check(WelcomePanel.Visibility == Visibility.Collapsed && customers.Records.Count == count, "Recognition off pauses matching and storage while scanning works");
        Page(SettingsPage); RecognitionEnabled.IsChecked=true; RecognitionEnabled_Click(this,new RoutedEventArgs());
        DeleteCustomers_Click(this,new RoutedEventArgs()); CancelDeleteCustomers_Click(this,new RoutedEventArgs());
        Check(customers.Records.Count == count, "Canceling deletion preserves saved customers");
        Page(HistoryPage); HistoryList.SelectedIndex = 0;
        Check(HistoryDetail.Visibility == Visibility.Visible && HistoryDetailName.Text == "ALEX SAMPLE" && HistoryDetailAge.Text.Contains("Age 21"), "Selecting a session scan opens its name and age details");
        CustomersList.SelectedIndex = 0;
        Check(HistoryDetailName.Text == "ALEX SAMPLE" && HistoryDetailAge.Text.Contains("scan the card again"), "Selecting a saved customer opens a privacy-safe detail card");
        Page(SettingsPage);
        DeleteCustomers_Click(this,new RoutedEventArgs()); ConfirmDeleteCustomers_Click(this,new RoutedEventArgs());
        Check(customers.Records.Count == 0, "Confirmed delete removes every remembered customer");
        Page(ScanPage); DemoResult(25);
        Check(customers.Records.Count == 0, "Demo cards are never saved as customers");
        string storePath=Path.GetFullPath(Path.Combine("artifacts","customer-store-test-"+Guid.NewGuid().ToString("N")+".dat"));
        try
        {
            var result=Barcode.Check(sample,Today,true); var first=new CustomerStore(storePath);
            Check(first.Remember(result,DateTimeOffset.UtcNow) is { Returning:false }, "Windows protected customer store saves the first visit");
            byte[] disk=File.ReadAllBytes(storePath);
            Check(!Encoding.UTF8.GetString(disk).Contains("ALEX SAMPLE") && !Encoding.UTF8.GetString(disk).Contains("SYNTHETIC"), "Saved customer file exposes neither a plaintext name nor card number");
            var reopened=new CustomerStore(storePath);
            Check(reopened.Remember(result,DateTimeOffset.UtcNow) is { Returning:true, Visits:2 }, "Customer recognition survives reopening the encrypted store");
            Check(first.Remember(result,DateTimeOffset.UtcNow) is { Returning:true, Visits:3 }, "Concurrent stores merge visits under the file lock");
            Check(reopened.DeleteAll() && first.Remember(result,DateTimeOffset.UtcNow) is { Returning:false, Visits:1 }, "Deletion in another copy cannot resurrect old customer records");
            File.WriteAllBytes(storePath,[1,2,3]); var damaged=new CustomerStore(storePath);
            Check(!damaged.Available && damaged.Remember(result,DateTimeOffset.UtcNow)==null && File.ReadAllBytes(storePath).SequenceEqual(new byte[]{1,2,3}), "Unreadable storage stays untouched and does not block an age result");
            Check(damaged.DeleteAll() && damaged.Remember(result,DateTimeOffset.UtcNow) is { Returning:false }, "Deleting damaged storage safely starts a fresh customer book");
        }
        finally { File.Delete(storePath); File.Delete(storePath+".lock"); }
        customers.DeleteAll(); history.Clear(); RefreshCustomers(); Page(ScanPage); Activate();
    }
}
