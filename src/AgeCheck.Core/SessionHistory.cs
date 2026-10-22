using System.Collections.ObjectModel;

namespace AgeCheck.Core;
public sealed record ScanEntry(Guid ScanId, string Name, DateTimeOffset ScannedAt, Outcome Outcome, bool Demo, bool Confirmed, int? Age = null, string? VisitLabel = null)
{
    public string TimeLabel => ScannedAt.ToLocalTime().ToString("h:mm:ss tt");
    public string StatusLabel => (Demo ? "DEMO · " : "") + (Confirmed ? "Dates confirmed · " : "") + (Outcome switch { Outcome.Passed => "21+", Outcome.Under21 => "Under 21", Outcome.Expired => "Expired", _ => "Manual review" });
    public bool CanCopy => Name != "NAME NOT AVAILABLE";
    public string AgeLabel => Age is { } age ? $"Age {age}" : "Age unavailable";
}
// Names/results only, deliberately no file I/O, dates of birth, card numbers or raw data.
public sealed class SessionHistory
{
    public const int Capacity = 100;
    private readonly ObservableCollection<ScanEntry> entries = new();
    public ReadOnlyObservableCollection<ScanEntry> Entries { get; }
    public bool Enabled { get; private set; } = true;
    public SessionHistory() => Entries = new(entries);
    public void SetEnabled(bool enabled) { Enabled=enabled; if(!enabled) Clear(); }
    public void Record(Guid id, CheckResult result, bool demo, bool confirmed, DateTimeOffset time, DateOnly? today = null, string? visitLabel = null)
    {
        if (!Enabled) return;
        int? age = result.Outcome != Outcome.Review && result.Birth is { } birth ? AgeDetails.From(birth, today ?? DateOnly.FromDateTime(time.LocalDateTime))?.Age : null;
        var entry=new ScanEntry(id,result.DisplayName ?? "NAME NOT AVAILABLE",time,result.Outcome,demo,confirmed,age,visitLabel);
        int index=-1;
        for(int i=0;i<entries.Count;i++) if(entries[i].ScanId==id) { index=i; break; }
        if(index>=0) { entries[index]=entry with { ScannedAt=entries[index].ScannedAt }; return; }
        entries.Insert(0,entry);
        if(entries.Count>Capacity) entries.RemoveAt(entries.Count-1);
    }
    public void Clear() => entries.Clear();
}
