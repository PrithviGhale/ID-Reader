namespace AgeCheck.Core;

public sealed record CustomerRecord(string Fingerprint, string Name, DateTimeOffset FirstSeen, DateTimeOffset LastSeen, int Visits)
{
    public string VisitLabel => $"{Visits:N0} {(Visits == 1 ? "scan" : "scans")} on this PC";
    public string LastSeenLabel => "Last scanned " + LastSeen.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt");
}
public sealed record CustomerVisit(bool Returning, int Visits, DateTimeOffset? PreviousVisit);

// No barcode, card number, address, photo or date of birth is retained here.
public sealed class CustomerBook
{
    public const int Capacity = 5000;
    private readonly Dictionary<string, CustomerRecord> records = new(StringComparer.Ordinal);
    public IReadOnlyList<CustomerRecord> Records => records.Values.OrderByDescending(r => r.LastSeen).ToArray();
    public CustomerBook(IEnumerable<CustomerRecord>? saved = null)
    {
        var restored = (saved ?? []).ToArray();
        if (restored.Length > Capacity) throw new ArgumentException("Too many customer records.");
        foreach (var record in restored)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.Fingerprint) || record.Fingerprint.Length != 64 || record.Fingerprint.Any(c => !Uri.IsHexDigit(c))
                || string.IsNullOrWhiteSpace(record.Name) || record.Name.Length > 161 || record.Name.Any(char.IsControl) || record.Visits < 1 || record.LastSeen < record.FirstSeen)
                throw new ArgumentException("Invalid customer record.");
            if (!records.TryAdd(record.Fingerprint, record)) throw new ArgumentException("Duplicate customer record.");
        }
    }
    public CustomerVisit Visit(string fingerprint, string name, DateTimeOffset now)
    {
        bool returning = records.TryGetValue(fingerprint, out var previous);
        int visits = previous == null ? 1 : previous.Visits == int.MaxValue ? int.MaxValue : previous.Visits + 1;
        records[fingerprint] = new(fingerprint, name, previous?.FirstSeen ?? now, now < previous?.LastSeen ? previous.LastSeen : now, visits);
        if (records.Count > Capacity) records.Remove(records.Values.MinBy(r => r.LastSeen)!.Fingerprint);
        return new(returning, visits, previous?.LastSeen);
    }
}
