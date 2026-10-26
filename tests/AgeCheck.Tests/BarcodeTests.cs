using AgeCheck.Core;
namespace AgeCheck.Tests;
public class BarcodeTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);
    private static string Valid(string country = "USA") => Demo.Create(new(2000, 3, 12), new(2028, 10, 19), new(2024, 10, 19), country);
    private static string Frame(string body) => $"@\n\u001e\rANSI 636000100001DL0031{body.Length:D4}{body}";
    [Theory]
    [InlineData(2005, 10, 18, Outcome.Passed)]
    [InlineData(2005, 10, 19, Outcome.Passed)]
    [InlineData(2005, 10, 20, Outcome.Under21)]
    public void ExactBirthday(int y, int m, int d, Outcome outcome) => Assert.Equal(outcome, Barcode.Evaluate(new(y,m,d), Today.AddYears(1), Today).Outcome);
    [Theory]
    [InlineData(27, Outcome.Under21)]
    [InlineData(28, Outcome.Review)]
    public void LeapDayFebruaryBoundary(int day, Outcome outcome) => Assert.Equal(outcome, Barcode.Evaluate(new(2004,2,29), new(2030,1,1), new(2025,2,day)).Outcome);
    [Fact] public void LeapDayMarchBoundary() => Assert.Equal(Outcome.Passed, Barcode.Evaluate(new(2004,2,29), new(2030,1,1), new(2025,3,1)).Outcome);
    [Theory]
    [InlineData(-1, Outcome.Expired)] [InlineData(0, Outcome.Passed)] [InlineData(1, Outcome.Passed)]
    public void ExpirationInclusive(int days, Outcome outcome) => Assert.Equal(outcome, Barcode.Evaluate(new(2000,1,1), Today.AddDays(days), Today).Outcome);
    [Theory] [InlineData("USA")] [InlineData("CAN")]
    public void CountryDates(string country) { var r = Barcode.Check(Valid(country), Today); Assert.Equal(Outcome.Passed,r.Outcome); Assert.Equal(new DateOnly(2000,3,12),r.Birth); }
    [Fact] public void MissingCountryNeverGuesses() => Assert.Equal(Outcome.Review, Barcode.Check(Frame("DLDBB03122000\nDBA10192028\nDBD10192024\r"),Today).Outcome);
    [Theory]
    [InlineData("636039", "New Hampshire")]
    [InlineData("636014", "California")]
    [InlineData("636002", "Massachusetts")]
    [InlineData("999999", null)]
    public void IssuingStateComesFromIinNotAddress(string iin, string? expected)
    {
        var barcode = Frame("DLDCGUSA\nDBB03122000\nDBA10192028\nDBD10192024\nDAJNH\r");
        Assert.Contains("DAJNH", barcode);
        Assert.Equal(expected, Barcode.Check(barcode[..9] + iin + barcode[15..], Today).IssuingState);
    }
    [Theory]
    [InlineData(10, "10 days until expiration")]
    [InlineData(1, "1 day until expiration")]
    [InlineData(0, "Expires today")]
    [InlineData(-1, "Expired 1 day ago")]
    [InlineData(-3, "Expired 3 days ago")]
    public void ExpirationCountdownUsesCalendarDays(int days, string expected) =>
        Assert.Equal(expected, ExpirationCountdown.Describe(Today.AddDays(days), Today, true));
    [Fact] public void UncheckedExpirationHasNoCountdown() =>
        Assert.Equal("Compare the expiration date with the card", ExpirationCountdown.Describe(Today.AddDays(5), Today, false));
    [Theory]
    [InlineData("DBB02302000\nDBA10192028\nDBD10192024")]
    [InlineData("DBB03122000\nDBA10192028")]
    [InlineData("DBA10192028\nDBD10192024")]
    [InlineData("DBB03122000\nDBD10192024")]
    [InlineData("DBB03122000\nDBB03122000\nDBA10192028\nDBD10192024")]
    [InlineData("DBB03122000\nDBB03122010\nDBA10192028\nDBD10192024")]
    [InlineData("DBB03122000\nDBA10192023\nDBD10192024")]
    [InlineData("DBB03122000\nDBA10192028\nDBD10192027")]
    [InlineData("DBB03122030\nDBA10192038\nDBD10192034")]
    [InlineData("DBB20000312\nDBA20281019\nDBD20241019")]
    [InlineData("DBB03122000\nDBA10192028\nDBD10191999")]
    [InlineData("DBB03122000\nDBA10192028\nDBD10192024\n??Xbad")]
    public void InvalidOrConflictingFields(string fields) => Assert.Equal(Outcome.Review,Barcode.Check(Frame("DLDCGUSA\n"+fields+"\r"),Today).Outcome);
    [Fact] public void EveryTruncatedPrefixRejected()
    {
        var text = Valid();
        for (int i=0;i<text.Length;i++) Assert.Equal(Outcome.Review,Barcode.Check(text[..i],Today).Outcome);
    }
    [Theory] [InlineData("08")] [InlineData("09")] [InlineData("10")] [InlineData("11")]
    public void SupportedVersions(string version) { var t=Valid(); Assert.Equal(Outcome.Passed,Barcode.Check(t[..15]+version+t[17..],Today).Outcome); }
    [Fact] public void UnsupportedVersion() { var t=Valid(); Assert.Equal(Outcome.Review,Barcode.Check(t[..15]+"12"+t[17..],Today).Outcome); }
    [Theory] [InlineData("\r")] [InlineData("\t")] [InlineData("\r\n")] [InlineData("\u0003")]
    public void Suffixes(string suffix) => Assert.Equal(Outcome.Passed,Barcode.Check(Valid()+suffix,Today).Outcome);
    [Fact] public void ControlSubstitutions() => Assert.Equal(Outcome.Passed,Barcode.Check(Valid().Replace("\r","[CR]").Replace("\n","[LF]").Replace("\u001e","[RS]"),Today).Outcome);
    [Fact] public void MultipleScansRejected() => Assert.Equal(Outcome.Review,Barcode.Check(Valid()+Valid(),Today).Outcome);
    [Fact] public void SymbologyPrefix() => Assert.Equal(Outcome.Passed,Barcode.Check("]L0"+Valid(),Today).Outcome);
    [Fact] public void OptionalFinalSeparator() => Assert.Equal(Outcome.Passed,Barcode.Check(Frame("DLDCGUSA\nDBB03122000\nDBA10192028\nDBD10192024\n\r"),Today).Outcome);
    [Fact] public void ConflictingAgeThreshold() => Assert.Equal(Outcome.Review,Barcode.Check(Frame("DLDCGUSA\nDBB03122000\nDBA10192028\nDBD10192024\nDDJ03122030\r"),Today).Outcome);
    [Fact] public void MatchingAgeThreshold() => Assert.Equal(Outcome.Passed,Barcode.Check(Frame("DLDCGUSA\nDBB03122000\nDBA10192028\nDBD10192024\nDDJ03122021\r"),Today).Outcome);
    [Fact] public void AdditionalJurisdictionSubfile()
    {
        string body=Valid()[31..]; string extra="ZVZVA01\r";
        var text=$"@\n\u001e\rANSI 636000100002DL0041{body.Length:D4}ZV{41+body.Length:D4}{extra.Length:D4}{body}{extra}";
        Assert.Equal(Outcome.Passed,Barcode.Check(text,Today).Outcome);
        Assert.Equal(Outcome.Review,Barcode.Check(text.Replace($"ZV{41+body.Length:D4}","ZV0041"),Today).Outcome);
    }
    [Fact] public void BadOffset() => Assert.Equal(Outcome.Review,Barcode.Check(Valid().Replace("DL0031","DL0030"),Today).Outcome);
    [Fact] public void MultilineBufferNeverSubmitsOnNewline()
    {
        var b=new ScanBuffer(); foreach(char c in Valid()) Assert.True(b.Append(c.ToString()));
        Assert.True(b.Receiving); Assert.Equal(Outcome.Passed,Barcode.Check(b.Take(),Today).Outcome); Assert.False(b.Receiving);
    }
    [Fact] public void PartialScanNeedsReview() { var b=new ScanBuffer(); b.Append(Valid()[..50]); Assert.Equal(Outcome.Review,Barcode.Check(b.Take(),Today).Outcome); }
    [Fact] public void InputLimitLatchesUntilClear()
    {
        var b=new ScanBuffer(); Assert.True(b.Append(new string('a',Barcode.InputLimit))); Assert.False(b.Append("x"));
        Assert.True(b.Overflowed); Assert.False(b.Receiving); Assert.False(b.Append(Valid())); b.Clear(); Assert.True(b.Append(Valid()));
    }
    [Fact] public void ClearDropsPartialData() { var b=new ScanBuffer(); b.Append(Valid()[..40]); b.Clear(); Assert.Empty(b.Take()); }
    [Fact] public void RandomMalformedInputsNeverThrow()
    {
        var rng=new Random(42);
        for(int i=0;i<500;i++) { var chars=new char[rng.Next(0,2000)]; for(int j=0;j<chars.Length;j++) chars[j]=(char)rng.Next(0,128); Assert.Equal(Outcome.Review,Barcode.Check(new string(chars),Today).Outcome); }
    }
}
