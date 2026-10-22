namespace AgeCheck.Core;

public sealed record AgeDetails(int Age, int DaysUntilBirthday, int? DaysUntil21)
{
    public bool BirthdayToday => DaysUntilBirthday == 0;
    public string BirthdayLabel => BirthdayToday ? "It's their birthday today!" : $"Birthday in {DaysUntilBirthday} {(DaysUntilBirthday == 1 ? "day" : "days")}";
    public string ThresholdLabel => DaysUntil21 is { } days ? $"{days:N0} {(days == 1 ? "day" : "days")} until 21" : "21st birthday reached";

    public static AgeDetails? From(DateOnly birth, DateOnly today)
    {
        if (birth > today || birth.Year < today.Year - 125 || today.Year == 9999) return null;
        // Match Barcode.Evaluate: leap-day anniversaries use March 1 in non-leap years.
        DateOnly Anniversary(int year) => birth.Month == 2 && birth.Day == 29 && !DateTime.IsLeapYear(year)
            ? new(year, 3, 1) : new(year, birth.Month, birth.Day);
        var birthday = Anniversary(today.Year);
        int age = today.Year - birth.Year - (today < birthday ? 1 : 0);
        var next = today <= birthday ? birthday : Anniversary(today.Year + 1);
        var threshold = Anniversary(birth.Year + 21);
        return new(age, next.DayNumber - today.DayNumber, today < threshold ? threshold.DayNumber - today.DayNumber : null);
    }
}
