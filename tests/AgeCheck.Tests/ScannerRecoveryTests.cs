using AgeCheck.Core;
namespace AgeCheck.Tests;
public class ScannerRecoveryTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);
    private static string Sample(string country = "USA") => Demo.Flattened(new(1998, 7, 14), new(2029, 8, 31), new(2024, 8, 1), country);
    [Theory] [InlineData("USA")] [InlineData("CAN")]
    public void FlatScanRecoversDatesButRequiresConfirmation(string country)
    {
        var result = Barcode.Check(Sample(country), Today, true);
        Assert.Equal(Outcome.Review, result.Outcome);
        Assert.True(result.NeedsDateConfirmation);
        Assert.Equal(new DateOnly(1998,7,14), result.Birth);
        Assert.Equal(new DateOnly(2029,8,31), result.Expiration);
    }
    [Theory] [InlineData("CS")] [InlineData("DCS")] [InlineData("DLDCS")] [InlineData("IDDCS")]
    public void SupportedNamePrefixes(string prefix) => Assert.True(Barcode.Check(prefix + Sample()[2..], Today, true).NeedsDateConfirmation);
    [Fact] public void LegacyStrictCallNeverRecovers() => Assert.False(Barcode.Check(Sample(), Today).NeedsDateConfirmation);
    [Theory]
    [InlineData("DBB07141998", "DBB07141998DBB07141998")]
    [InlineData("DBB07141998", "DBB07141998DBB07142010")]
    [InlineData("DBB07141998", "DBB02301998")]
    [InlineData("DBB07141998", "DBB0714199")]
    [InlineData("DBB07141998", "DBB071419989")]
    [InlineData("DBB07141998", "")]
    [InlineData("DBA08312029", "")]
    [InlineData("DBD08012024", "")]
    [InlineData("DCGUSA", "DCGUS")]
    [InlineData("DCGUSA", "DCGUSADCGCAN")]
    [InlineData("DBD08012024", "DBD08012030")]
    [InlineData("DBA08312029", "DBA08312023")]
    public void AmbiguousIncompleteOrInconsistentDatesCannotBeConfirmed(string from, string to)
    {
        var r = Barcode.Check(Sample().Replace(from,to),Today,true);
        Assert.Equal(Outcome.Review,r.Outcome); Assert.False(r.NeedsDateConfirmation);
    }
    [Fact] public void OptionalAgeConflictStillRejected()
    {
        var r=Barcode.Check(Sample()+"DDJ07142050",Today,true); Assert.False(r.NeedsDateConfirmation);
    }
    [Fact] public void ConcatenatedCardsNeverRecover() => Assert.False(Barcode.Check(Sample()+Sample(),Today,true).NeedsDateConfirmation);
    [Fact] public void RecoveryNeverAutomaticallyPassesTruncatedInput()
    {
        string s=Sample(); for(int i=0;i<=s.Length;i++) Assert.Equal(Outcome.Review,Barcode.Check(s[..i],Today,true).Outcome);
    }
    [Fact] public void FlatHeaderWithSupportedVersionRecovers()
    {
        Assert.True(Barcode.Check("@ANSI 636000100001DL00310999D"+Sample(),Today,true).NeedsDateConfirmation);
        Assert.False(Barcode.Check("@ANSI 636000990001DL00310999D"+Sample(),Today,true).NeedsDateConfirmation);
    }
    [Fact] public void DamagedFramedScanCannotBypassValidation()
    {
        var s=Demo.Create(new(1998,7,14),new(2029,8,31),new(2024,8,1));
        var r=Barcode.Check(s[..^1],Today,true); Assert.False(r.NeedsDateConfirmation); Assert.Equal(Outcome.Review,r.Outcome);
    }
    [Fact] public void LineSeparatorsAndScannerSuffixAreAcceptedForRecovery() => Assert.True(Barcode.Check(Sample().Replace("DBB","\r\nDBB")+"\r\n",Today,true).NeedsDateConfirmation);
    [Fact] public void ExpiredOrUnderageDatesRemainReviewUntilConfirmed()
    {
        Assert.True(Barcode.Check(Demo.Flattened(new(2010,1,1),new(2029,1,1),new(2025,1,1)),Today,true).NeedsDateConfirmation);
        Assert.True(Barcode.Check(Demo.Flattened(new(1990,1,1),new(2026,1,1),new(2025,1,1)),Today,true).NeedsDateConfirmation);
    }
}
