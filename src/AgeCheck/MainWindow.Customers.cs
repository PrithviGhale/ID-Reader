using System.Windows;
using AgeCheck.Core;

namespace AgeCheck;
public partial class MainWindow
{
    private string? CurrentVisitLabel => currentVisit is { Returning: true } visit ? $"Seen before / scan {visit.Visits:N0}" : currentVisit != null ? "First scan on this PC" : null;
    private void ResetCustomerDetails()
    {
        AgeLabel.Text = "\u2014"; AgeCaption.Text = "CURRENT AGE";
        WelcomePanel.Visibility = BirthdayPanel.Visibility = Under21Panel.Visibility = Visibility.Collapsed;
        WelcomeTitle.Text = WelcomeDetail.Text = BirthdayLabel.Text = Under21Label.Text = "";
        currentVisit = null; rememberedScan = Guid.Empty;
    }
    private void ShowCustomerDetails(CheckResult result, bool demo)
    {
        AgeLabel.Text = "\u2014"; BirthdayPanel.Visibility = Under21Panel.Visibility = WelcomePanel.Visibility = Visibility.Collapsed;
        AgeCaption.Text = result.NeedsDateConfirmation ? "AWAITING DATE CONFIRMATION" : "CURRENT AGE";
        if (result.Outcome == Outcome.Review || result.Birth is not { } birth) return;
        if (AgeDetails.From(birth, Today) is { } details)
        {
            AgeLabel.Text = details.Age.ToString(System.Globalization.CultureInfo.InvariantCulture);
            BirthdayPanel.Visibility = Visibility.Visible; BirthdayLabel.Text = details.BirthdayLabel;
            Under21Panel.Visibility = details.DaysUntil21 != null ? Visibility.Visible : Visibility.Collapsed;
            Under21Label.Text = details.ThresholdLabel;
        }
        if (!demo && rememberCustomers && rememberedScan != currentScanId)
        {
            currentVisit = customers.Remember(result, currentScanTime); rememberedScan = currentScanId;
            RefreshCustomers();
        }
        if (!demo && rememberCustomers && currentVisit is { Returning: true, PreviousVisit: { } previous } visit)
        {
            WelcomePanel.Visibility = Visibility.Visible;
            WelcomeTitle.Text = "Welcome back, " + result.DisplayName + ".";
            WelcomeDetail.Text = $"Previously scanned {previous.ToLocalTime():MMM d, yyyy 'at' h:mm tt}. Scan {visit.Visits:N0} on this PC. Today's card was checked again.";
        }
    }
    private void RefreshCustomers()
    {
        CustomersList.ItemsSource = rememberCustomers ? customers.Records : null;
        int count = customers.Records.Count;
        CustomerCountLabel.Text = rememberCustomers ? (count == 0 ? "No saved customers yet. A complete named card starts a local record." : $"{count:N0} saved customers / up to {CustomerBook.Capacity:N0}")
            : "Recognition is off. Existing records stay protected until you delete them in Settings.";
        RecognitionStatus.Text = customers.Error ?? $"{count:N0} protected records on this PC. " + (rememberCustomers ? "Returning cards can be recognized after restarting the app." : "Matching and saving are paused.");
        UpdateHistoryUI();
    }
    private void RecognitionEnabled_Click(object sender, RoutedEventArgs e)
    {
        rememberCustomers = RecognitionEnabled.IsChecked == true;
        currentVisit = null; rememberedScan = Guid.Empty; WelcomePanel.Visibility = Visibility.Collapsed;
        RefreshCustomers(); SavePreferences();
    }
    private void DeleteCustomers_Click(object sender, RoutedEventArgs e) => DeleteCustomerNotice.Visibility = Visibility.Visible;
    private void CancelDeleteCustomers_Click(object sender, RoutedEventArgs e) => DeleteCustomerNotice.Visibility = Visibility.Collapsed;
    private void ConfirmDeleteCustomers_Click(object sender, RoutedEventArgs e)
    {
        bool deleted = customers.DeleteAll();
        DeleteCustomerNotice.Visibility = Visibility.Collapsed;
        currentVisit = null; rememberedScan = Guid.Empty; WelcomePanel.Visibility = Visibility.Collapsed;
        RefreshCustomers();
        if (deleted) RecognitionStatus.Text = "All saved customers were deleted. " + (rememberCustomers ? "New scans will start new records." : "Recognition is off.");
    }
    private void SetUpdateAlert(bool available)
    {
        UpdateBadge.Visibility = SettingsUpdateBadge.Visibility = available ? Visibility.Visible : Visibility.Collapsed;
        SettingsNav.ToolTip = available ? "An update is available / open Settings" : "Settings and app updates";
        System.Windows.Automation.AutomationProperties.SetName(SettingsNav, available ? "Settings, update available" : "Settings and app updates");
    }
}
