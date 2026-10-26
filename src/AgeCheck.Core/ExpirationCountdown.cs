namespace AgeCheck.Core;

public static class ExpirationCountdown
{
    public static string Describe(DateOnly? expiration, DateOnly today, bool dateChecked)
    {
        if (expiration is null) return "Expiration unavailable";
        if (!dateChecked) return "Compare the expiration date with the card";
        int days = expiration.Value.DayNumber - today.DayNumber;
        return days switch
        {
            > 1 => $"{days:N0} days until expiration",
            1 => "1 day until expiration",
            0 => "Expires today",
            -1 => "Expired 1 day ago",
            _ => $"Expired {-days:N0} days ago"
        };
    }
}
