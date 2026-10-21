using AgeCheck.Core;

namespace AgeCheck.Tests;

public class ScannerTransportTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);
    private static string Sample() => Demo.KeyboardHeader(new(1997, 6, 12), new(2030, 7, 18), new(2025, 7, 18));

    [Theory]
    [InlineData("@\rÂNSI ")]
    [InlineData("@\n\u001e\rÂNSI ")]
    [InlineData("@\r\nANSI ")]
    [InlineData("@\nANSI ")]
    [InlineData("@\tANSI ")]
    [InlineData("\r\n@ÂNSI ")]
    [InlineData("]L0@\rÂNSI ")]
    [InlineData("\r\n]L0@A\u0302NSI ")]
    [InlineData("ÂNSI ")]
    [InlineData("A\u0302NSI ")]
    public void MixedKeyboardHeaderSeparatorsRequireConfirmation(string header)
    {
        var raw = header + Sample()[6..];
        var result = Barcode.Check(raw, Today, true);
        Assert.Equal(Outcome.Review, result.Outcome);
        Assert.True(result.NeedsDateConfirmation);
        Assert.Equal(new DateOnly(1997, 6, 12), result.Birth);
        Assert.Equal(new DateOnly(2030, 7, 18), result.Expiration);
        Assert.Equal("ALEX SAMPLE", result.DisplayName);
        Assert.Equal(Outcome.Passed, Barcode.Evaluate(result.Birth!.Value, result.Expiration!.Value, Today).Outcome);
        Assert.False(Barcode.Check(raw, Today).NeedsDateConfirmation);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\t")]
    [InlineData("\u001e")]
    [InlineData("[LF]")]
    [InlineData("[CR][LF]")]
    public void PartlyPreservedFieldSeparatorsRecover(string separator)
    {
        var raw = Sample().Replace("DBB", separator + "DBB").Replace("DCG", separator + "DCG");
        Assert.True(Barcode.Check(raw, Today, true).NeedsDateConfirmation);
    }

    [Theory]
    [InlineData("@\nÀNSI ")]
    [InlineData("@\nÃNSI ")]
    [InlineData("@\nÂNSI")]
    [InlineData("UNTRUSTED@ÂNSI ")]
    [InlineData("]L1@ÂNSI ")]
    [InlineData("@\u0000ÂNSI ")]
    public void UnknownHeaderMutationsStayBlocked(string header)
    {
        var result = Barcode.Check(header + Sample()[6..], Today, true);
        Assert.Equal(Outcome.Review, result.Outcome);
        Assert.False(result.NeedsDateConfirmation);
    }

    [Fact]
    public void HeaderRepairDoesNotRepairValues()
    {
        foreach (string field in new[] { "SÂMPLE", "DBB0612199Â", "DCGUSÂ" })
        {
            string raw = Sample().Replace("@ÂNSI ", "@\rÂNSI ");
            raw = field.StartsWith("DBB", StringComparison.Ordinal) ? raw.Replace("DBB06121997", field)
                : field.StartsWith("DCG", StringComparison.Ordinal) ? raw.Replace("DCGUSA", field) : raw.Replace("SAMPLE", field);
            Assert.False(Barcode.Check(raw, Today, true).NeedsDateConfirmation);
        }
    }

    [Fact]
    public void TransportRepairsNeverAutoPassPartialOrJoinedCards()
    {
        string raw = Sample().Replace("@ÂNSI ", "@\r\nÂNSI ");
        for (int i = 0; i <= raw.Length; i++) Assert.Equal(Outcome.Review, Barcode.Check(raw[..i], Today, true).Outcome);
        Assert.False(Barcode.Check(raw + raw, Today, true).NeedsDateConfirmation);
        Assert.False(Barcode.Check(raw.Replace("DBB06121997", "DBB06121997DBB06121997"), Today, true).NeedsDateConfirmation);
    }
}
