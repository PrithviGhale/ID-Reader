using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace AgeCheck;

public partial class MainWindow
{
    private static bool hoverInstalled;

    private static void InstallButtonHover()
    {
        if (hoverInstalled) return;
        hoverInstalled = true;
        EventManager.RegisterClassHandler(typeof(Button), MouseEnterEvent, new MouseEventHandler(OnButtonEnter));
        EventManager.RegisterClassHandler(typeof(Button), MouseLeaveEvent, new MouseEventHandler(OnButtonLeave));
    }

    private static void OnButtonEnter(object sender, MouseEventArgs e)
    {
        if (sender is Button button && Window.GetWindow(button) is MainWindow window) window.HoverButton(button, true);
    }

    private static void OnButtonLeave(object sender, MouseEventArgs e)
    {
        if (sender is Button button && Window.GetWindow(button) is MainWindow window) window.HoverButton(button, false);
    }

    private void HoverButton(Button button, bool entered)
    {
        if (!button.IsEnabled) return;
        button.Effect = entered ? new DropShadowEffect
        {
            Color = darkTheme ? Color.FromRgb(83, 211, 222) : Color.FromRgb(16, 123, 170),
            BlurRadius = 17, ShadowDepth = 0, Opacity = .28
        } : null;
        if (button.RenderTransform is not ScaleTransform scale)
        {
            scale = new ScaleTransform(1, 1);
            button.RenderTransformOrigin = new Point(.5, .5);
            button.RenderTransform = scale;
        }
        double target = entered ? 1.025 : 1;
        if (suppressAnimations || !motionEnabled || !SystemParameters.ClientAreaAnimation)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleX = scale.ScaleY = target;
            return;
        }
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(170)) { EasingFunction = easing };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private void StartScanBeam()
    {
        ScanBeam.Visibility = Visibility.Visible;
        if (suppressAnimations || !motionEnabled || !SystemParameters.ClientAreaAnimation) return;
        var move = new TranslateTransform();
        ScanBeam.RenderTransform = move;
        move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, Math.Max(80, ResultCard.ActualWidth - 190),
            TimeSpan.FromMilliseconds(1250)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
    }

    private void StopScanBeam()
    {
        if (ScanBeam.RenderTransform is TranslateTransform move) move.BeginAnimation(TranslateTransform.XProperty, null);
        ScanBeam.RenderTransform = Transform.Identity;
        ScanBeam.Visibility = Visibility.Collapsed;
    }

    private void UpdateStateBadge(string? state)
    {
        StateMonogram.Text = StateAbbreviation(state);
        IssuingStateLabel.Text = state ?? "Unavailable in checked barcode";
        IssuingStateLabel.ToolTip = state is null ? "Issuer code was not recognized" : "Issuer identified from the barcode header: " + state;
        IssuingStateLabel.FontFamily = new FontFamily(state is "New Hampshire" or "Florida" ? "Georgia" : "Segoe UI");
        IssuingStateLabel.FontStyle = state == "Florida" ? FontStyles.Italic : FontStyles.Normal;
        IssuingStateLabel.FontWeight = state == "California" ? FontWeights.ExtraBold : FontWeights.SemiBold;
        IssuingStateLabel.FontSize = state is "New Hampshire" or "Florida" ? 17 : 15;
        IssuingStateLabel.SetResourceReference(TextBlock.ForegroundProperty, state == "Florida" ? "AccentText" : "BrandText");
    }

    // Use the issuer code in the barcode header, never the holder's address state.
    private static string StateAbbreviation(string? state) => state switch
    {
        "Alabama" => "AL", "Alaska" => "AK", "Arizona" => "AZ", "Arkansas" => "AR", "California" => "CA",
        "Colorado" => "CO", "Connecticut" => "CT", "Delaware" => "DE", "District of Columbia" => "DC", "Florida" => "FL",
        "Georgia" => "GA", "Hawaii" => "HI", "Idaho" => "ID", "Illinois" => "IL", "Indiana" => "IN",
        "Iowa" => "IA", "Kansas" => "KS", "Kentucky" => "KY", "Louisiana" => "LA", "Maine" => "ME",
        "Maryland" => "MD", "Massachusetts" => "MA", "Michigan" => "MI", "Minnesota" => "MN", "Mississippi" => "MS",
        "Missouri" => "MO", "Montana" => "MT", "Nebraska" => "NE", "Nevada" => "NV", "New Hampshire" => "NH",
        "New Jersey" => "NJ", "New Mexico" => "NM", "New York" => "NY", "North Carolina" => "NC", "North Dakota" => "ND",
        "Ohio" => "OH", "Oklahoma" => "OK", "Oregon" => "OR", "Pennsylvania" => "PA", "Rhode Island" => "RI",
        "South Carolina" => "SC", "South Dakota" => "SD", "Tennessee" => "TN", "Texas" => "TX", "Utah" => "UT",
        "Vermont" => "VT", "Virginia" => "VA", "Washington" => "WA", "West Virginia" => "WV", "Wisconsin" => "WI", "Wyoming" => "WY",
        "Puerto Rico" => "PR", "Guam" => "GU", "American Samoa" => "AS", "U.S. Virgin Islands" => "VI",
        "Northern Mariana Islands" => "MP", "U.S. State Department" => "US",
        "Alberta" => "AB", "British Columbia" => "BC", "Manitoba" => "MB", "New Brunswick" => "NB",
        "Newfoundland and Labrador" => "NL", "Northwest Territories" => "NT", "Nova Scotia" => "NS", "Nunavut" => "NU",
        "Ontario" => "ON", "Prince Edward Island" => "PE", "Quebec" => "QC", "Saskatchewan" => "SK", "Yukon" => "YT",
        _ => "—"
    };
}
