using AgeCheck.Core;
namespace AgeCheck.Tests;
public class NamesAndHistoryTests
{
    private static DateOnly Today => new(2026,10,20);
    private static string Named(string first="ALEX",string last="SAMPLE")=>Demo.CreateNamed(new(1998,7,14),new(2029,8,31),new(2025,8,1),first,last);
    private static string Frame(string body)=>$"@\n\u001e\rANSI 636000100001DL0031{body.Length:D4}{body}";
    [Fact] public void StrictNameUppercaseFirstAndLastOnly()
    {
        var r=Barcode.Check(Named("Alex","O'Neil"),Today); Assert.Equal(Outcome.Passed,r.Outcome); Assert.Equal("ALEX O'NEIL",r.DisplayName);
    }
    [Fact] public void FlattenedNameRecoveredWithFlags()
    {
        var r=Barcode.Check(Demo.KeyboardHeader(new(1998,7,14),new(2029,8,31),new(2025,8,1)),Today,true);
        Assert.True(r.NeedsDateConfirmation); Assert.Equal("ALEX SAMPLE",r.DisplayName); Assert.DoesNotContain("TEST",r.DisplayName!);
    }
    [Fact] public void MissingNameNeverGuessed() => Assert.Null(Barcode.Check(Demo.Create(new(1998,7,14),new(2029,8,31),new(2025,8,1)),Today).DisplayName);
    [Theory] [InlineData("T")] [InlineData("U")]
    public void TruncatedNameCannotBeCopied(string flag)
    {
        var r=Barcode.Check(Named().Replace("DDFN","DDF"+flag),Today); Assert.Null(r.DisplayName); Assert.NotNull(r.NameNote);
    }
    [Fact] public void MissingFlattenedNameBoundaryDoesNotInventName()
    {
        var s=Demo.Flattened(new(1998,7,14),new(2029,8,31),new(2025,8,1)).Replace("DACALEXDDFN","DAC");
        Assert.Null(Barcode.Check(s,Today,true).DisplayName);
    }
    [Fact] public void DuplicateFlatNamesAreUnavailable()
    {
        var s=Demo.Flattened(new(1998,7,14),new(2029,8,31),new(2025,8,1))+"DCSOTHERDACOTHERDADNONE";
        Assert.Null(Barcode.Check(s,Today,true).DisplayName);
    }
    [Theory] [InlineData("NONE")] [InlineData("unavl")] [InlineData("ALEX123")]
    public void PlaceholderOrInvalidNameUnavailable(string first) => Assert.Null(Barcode.Check(Named(first),Today).DisplayName);
    [Theory]
    [InlineData("DDB01012030")]
    [InlineData("DDB02302024")]
    [InlineData("DBC7")]
    [InlineData("DDAQ")]
    [InlineData("DDGX")]
    public void AdditionalInconsistenciesRequireReview(string field)
    {
        var r=Barcode.Check(Frame("DLDCGUSA\nDBB07141998\nDBA08312029\nDBD08012025\n"+field+"\r"),Today);
        Assert.Equal(Outcome.Review,r.Outcome); Assert.False(r.NeedsDateConfirmation);
    }
    [Fact] public void NonRealIdFlagIsNotAFakeIndicator()
    {
        Assert.Equal(Outcome.Passed,Barcode.Check(Frame("DLDCGUSA\nDBB07141998\nDBA08312029\nDBD08012025\nDDAN\r"),Today).Outcome);
    }
    [Fact] public void RevisionConflictInRecoveryCannotBeConfirmed()
    {
        var s=Demo.Flattened(new(1998,7,14),new(2029,8,31),new(2025,8,1)).Replace("DDB01012024","DDB01012030");
        var r=Barcode.Check(s,Today,true); Assert.Equal(Outcome.Review,r.Outcome); Assert.False(r.NeedsDateConfirmation);
    }
    [Fact] public void SessionHistoryUpdatesConfirmationWithoutDuplicate()
    {
        var h=new SessionHistory(); var id=Guid.NewGuid(); var r=Barcode.Check(Named(),Today); var time=DateTimeOffset.UtcNow;
        h.Record(id,r with{Outcome=Outcome.Review},false,false,time); h.Record(id,r,false,true,time.AddSeconds(4));
        Assert.Single(h.Entries); Assert.Equal("ALEX SAMPLE",h.Entries[0].Name); Assert.True(h.Entries[0].Confirmed); Assert.Equal(time,h.Entries[0].ScannedAt);
    }
    [Fact] public void EveryNewScanHasOwnEntryEvenSameName()
    {
        var h=new SessionHistory(); var r=Barcode.Check(Named(),Today);
        h.Record(Guid.NewGuid(),r,false,false,DateTimeOffset.UtcNow); h.Record(Guid.NewGuid(),r,false,false,DateTimeOffset.UtcNow); Assert.Equal(2,h.Entries.Count);
    }
    [Fact] public void SessionHistoryBoundedAndDisableClears()
    {
        var h=new SessionHistory(); for(int i=0;i<120;i++)h.Record(Guid.NewGuid(),new(Outcome.Expired,""),true,false,DateTimeOffset.UtcNow);
        Assert.Equal(SessionHistory.Capacity,h.Entries.Count); h.SetEnabled(false); Assert.Empty(h.Entries);
        h.Record(Guid.NewGuid(),new(Outcome.Passed,""),false,false,DateTimeOffset.UtcNow); Assert.Empty(h.Entries);
        h.SetEnabled(true); h.Record(Guid.NewGuid(),new(Outcome.Passed,""),false,false,DateTimeOffset.UtcNow); Assert.Single(h.Entries); h.Clear(); Assert.Empty(h.Entries);
    }
    [Fact] public void FailedScannerTextLivesOnlyInSessionHistory()
    {
        var h = new SessionHistory(); var id = Guid.NewGuid(); const string raw = "SYNTHETIC-FAILED-SCAN";
        h.Record(id, new(Outcome.Review, "Manual review"), false, false, DateTimeOffset.UtcNow, rawBarcode: raw);
        Assert.Equal(raw, h.Entries[0].RawBarcode);
        Assert.DoesNotContain(raw, System.Text.Json.JsonSerializer.Serialize(h.Entries[0]));
        h.Record(id, new(Outcome.Passed, "Dates confirmed"), false, true, DateTimeOffset.UtcNow);
        Assert.Equal(raw, h.Entries[0].RawBarcode);
        h.SetEnabled(false); Assert.Empty(h.Entries);
    }
    [Fact] public void PassingAndDemoScansNeverRetainRawText()
    {
        var h = new SessionHistory();
        h.Record(Guid.NewGuid(), new(Outcome.Passed, ""), false, false, DateTimeOffset.UtcNow, rawBarcode: "SYNTHETIC-PASS");
        h.Record(Guid.NewGuid(), new(Outcome.Review, ""), true, false, DateTimeOffset.UtcNow, rawBarcode: "SYNTHETIC-DEMO");
        Assert.All(h.Entries, entry => Assert.False(entry.CanCopyBarcode));
    }
}
