using AgeCheck.Core;

namespace AgeCheck.Tests;

public class CompleteKeyboardTests
{
    private static readonly DateOnly Today = new(2026, 10, 22);
    // Fictional version-03 New York layout: DCT given name, padded values,
    // and a second jurisdiction subfile. No customer scan is stored in tests.
    private static string OldNewYork()
    {
        string[] fields = ["DCAD   ", "DCBNONE      ", "DCDNONE ", "DBA03092034",
            "DCSEXAMPLE                  ", "DCTALEX                     ", "DBD03192026",
            "DBB03092005", "DBC2", "DAYBRO", "DAU505   ", "DAG25 TEST RD              ",
            "DAITEST TOWN           ", "DAJNY", "DAK136170000  ", "DAQTEST0001",
            "DCFTESTDISCRIMINATOR", "DCGUSA", "DDAN", "DDB09282017", "DDD0"];
        string first = "DL" + string.Join('\n', fields) + "\r";
        string second = "ZNZNAEXAMPLE@ALEX      \nZNB6P;+1>PsdKY++X_\"test'0E6/Gj;5XgAs#UP\r";
        return $"@\n\u001e\rANSI 636001030302DL0041{first.Length:D4}ZN{41 + first.Length:D4}{second.Length:D4}{first}{second}";
    }

    [Fact]
    public void Version03NewYorkTwoSubfilesAndLabeledKeyboardInput()
    {
        var strict = OldNewYork();
        var labeled = strict.Replace("@\n\u001e\rANSI ", "@[LF][CR]ANSI ").Replace("\n", "[LF]").Replace("\r", "[CR]") + "[CR]";
        foreach (var scan in new[] { strict, labeled })
        {
            var result = Barcode.Check(scan, new(2026, 10, 29), allowScannerRecovery: true);
            Assert.Equal(Outcome.Passed, result.Outcome);
            Assert.Equal("ALEX EXAMPLE", result.DisplayName);
            Assert.Equal("New York", result.IssuingState);
            Assert.Equal(new DateOnly(2005, 3, 9), result.Birth);
            Assert.Equal(new DateOnly(2034, 3, 9), result.Expiration);
        }
        Assert.Equal(Outcome.Review, Barcode.Check(labeled[..^10], new(2026, 10, 29), true).Outcome);
        Assert.Equal(Outcome.Review, Barcode.Check(strict.Replace("DBB03092005", "DBB02302005"), new(2026, 10, 29), true).Outcome);
    }
    private static string Sample(int years = 25, bool expired = false) => Demo.CompleteKeyboard(
        Today.AddYears(-years), expired ? Today.AddDays(-1) : Today.AddYears(2), Today.AddYears(-1));

    [Fact]
    public void CityEndingInDDoesNotHideJurisdictionField()
    {
        string original = Sample();
        string changed = original.Replace("MANCHESTERDAJNH", "GREENFIELDDAJCA", StringComparison.Ordinal);
        // The directory length must reflect the actual transmitted barcode.
        int originalLength = int.Parse(changed.Substring(24, 4));
        changed = changed[..24] + (originalLength + changed.Length - original.Length).ToString("D4") + changed[28..];
        var result = Barcode.Check(changed, Today, true);
        Assert.Equal(Outcome.Passed, result.Outcome);
    }
    [Fact]
    public void Version09FlattenedCityEndingDShowsAge()
    {
        string body = "DLDAQSYNTHETIC42DCSSAMPLEDDENDACALEXDDFNDADTESTDDGNDCADDCBNONEDCDNONEDBD10302024DBB09201985DBA09202028DBC2DAU066 inDAYBLUDAG19 TEST LNDAHNONEDAILGREENFIELDDAJNHDAK030520000  DCFSYNTHETIC01DCGUSADCKSYNTHETIC01DDANDDB06092016";
        int fieldCount = System.Text.RegularExpressions.Regex.Matches(body[2..], @"D[A-Z]{2}|Z[A-Z]{2}").Count;
        // The city marker overlaps with DAJ, so count the actual fields instead.
        int encodedLength = body.Length + fieldCount;
        var frame = $"@\u00c2NSI 636039090001DL0031{encodedLength:D4}{body}";
        var result = Barcode.Check(frame, Today, true);
        Assert.Equal(Outcome.Passed, result.Outcome);
        Assert.Equal("ALEX SAMPLE", result.DisplayName);
    }
    [Fact]
    public void Version08TwoSubfilesWithDifferentFieldOrderShowsAge()
    {
        string[] fields = ["DCAD", "DCBNONE", "DCDNONE", "DBA03012031", "DCSSAMPLE", "DACALEX", "DADTEST", "DBD03192026", "DBB03012003", "DBC2", "DAYBRO", "DAU066 in", "DAG57 TEST DR", "DAHNONE", "DAILITTLETOWN", "DAJMA", "DAK014601613  ", "DAQTESTCARD42", "DCF03192026 REV 02222016", "DCGUSA", "DDEN", "DDFN", "DDGN", "DCKSYNTHETIC01", "DDAN", "DDB02222016"];
        string[] zone = ["ZMA506", "ZMB03192026", "ZMC", "ZMD", "ZME"];
        string first = "DL" + string.Join('\n', fields) + "\r", second = "ZM" + string.Join('\n', zone) + "\r";
        string strict = $"@\n\u001e\rANSI 636002080002DL0041{first.Length:D4}ZM{41 + first.Length:D4}{second.Length:D4}{first}{second}";
        string flat = strict.Replace("@\n\u001e\rANSI ", "@\u00c2NSI ").Replace("\n", "").Replace("\r", "");
        var result = Barcode.Check(flat, Today, true);
        Assert.Equal(Outcome.Passed, result.Outcome);
        Assert.Equal("ALEX SAMPLE", result.DisplayName);
        Assert.True(result.KeyboardTransport);
        Assert.Equal(Outcome.Review, Barcode.Check(flat[..^1], Today, true).Outcome);
    }
    [Fact]
    public void TwoSubfilesWithNameEndingDAndExtraFieldsRecoverBothTransports()
    {
        string[] fields = ["DCAD", "DCBNONE", "DCDNONE", "DBA09282030", "DCSEXAMPLE", "DACARNOLD", "DADTEST", "DBD08292026", "DBB09282004", "DBC1", "DAYBRO", "DAU076 in", "DAG42 TEST ST", "DAHNONE", "DAISAMPLETON", "DAJMA", "DAK021431550  ", "DAQSYNTHETIC42", "DCF08312026 REV 02222016", "DCGUSA", "DDEN", "DDFN", "DDGN", "DCKSYNTHETIC01", "DCUJR", "DDAF", "DDB02222016", "DDK1"];
        string[] zone = ["ZMA604", "ZMB08312026", "ZMC", "ZMD", "ZME"];
        string first = "DL" + string.Join('\n', fields) + "\r", second = "ZM" + string.Join('\n', zone) + "\r";
        string strict = $"@\n\u001e\rANSI 636002080002DL0041{first.Length:D4}ZM{41 + first.Length:D4}{second.Length:D4}{first}{second}";
        string flat = strict.Replace("@\n\u001e\rANSI ", "@\u00c2NSI ").Replace("\n", "").Replace("\r", "");
        string labeled = strict.Replace("@\n\u001e\rANSI ", "@[LF][CR]ANSI ").Replace("\n", "[LF]").Replace("\r", "[CR]") + "[CR]";
        foreach (var scan in new[] { flat, labeled })
        {
            var result = Barcode.Check(scan, Today, true);
            Assert.True(result.Outcome == Outcome.Passed, result.Detail + " / " + (scan == flat ? "flat" : "labeled"));
            Assert.Equal("ARNOLD EXAMPLE", result.DisplayName);
            Assert.True(result.KeyboardTransport);
        }
        Assert.Equal(Outcome.Review, Barcode.Check(flat[..^1], Today, true).Outcome);
    }

    [Theory]
    [InlineData(25, false, Outcome.Passed)]
    [InlineData(21, false, Outcome.Passed)]
    [InlineData(20, false, Outcome.Under21)]
    [InlineData(25, true, Outcome.Expired)]
    [InlineData(19, true, Outcome.Expired)]
    public void CompleteFrameShowsOutcomeWithoutConfirmation(int years, bool expired, Outcome expected)
    {
        var result = Barcode.Check(Sample(years, expired), Today, true);
        Assert.True(result.Outcome == expected, result.Detail);
        Assert.False(result.NeedsDateConfirmation);
        Assert.True(result.KeyboardTransport);
        Assert.Equal("ALEX SAMPLE", result.DisplayName);
    }

    [Theory]
    [InlineData("@ANSI ")]
    [InlineData("@\r\nANSI ")]
    [InlineData("@A\u0302NSI ")]
    [InlineData("]L0@\u00c2NSI ")]
    public void CompleteFrameSurvivesSupportedKeyboardHeaders(string prefix)
        => Assert.Equal(Outcome.Passed, Barcode.Check(prefix + Sample()[6..], Today, true).Outcome);

    [Fact]
    public void EveryIncompletePrefixStillRequiresReview()
    {
        string sample = Sample();
        for (int i = 0; i < sample.Length; i++) Assert.Equal(Outcome.Review, Barcode.Check(sample[..i], Today, true).Outcome);
    }

    [Theory]
    [InlineData("DBB10222001", "DBB02302001")]
    [InlineData("DBB10222001", "DBB10222001DBB10222001")]
    [InlineData("DBA10222028", "DBA10222020")]
    [InlineData("DCGUSA", "DCGXXX")]
    [InlineData("DAC", "")]
    public void CorruptionCannotProduceAutomaticPass(string from, string to)
        => Assert.Equal(Outcome.Review, Barcode.Check(Sample().Replace(from, to), Today, true).Outcome);

    [Fact]
    public void JoinedFramesCannotPass() => Assert.Equal(Outcome.Review, Barcode.Check(Sample() + Sample(), Today, true).Outcome);

    [Fact]
    public void EverySingleCharacterLossInBodyPreventsAutomaticResult()
    {
        string sample = Sample(); int body = sample.IndexOf("DLDAQ", StringComparison.Ordinal);
        for (int i = body; i < sample.Length; i++)
            Assert.Equal(Outcome.Review, Barcode.Check(sample.Remove(i, 1), Today, true).Outcome);
    }

    [Fact]
    public void AmbiguousFieldMarkersStayReview()
        => Assert.Equal(Outcome.Review, Barcode.Check(Sample().Replace("MANCHESTER", "CONCORD"), Today, true).Outcome);

    [Fact]
    public void SnapshotRetainsInputThatArrivesDuringParsing()
    {
        var buffer = new ScanBuffer();
        buffer.Append("first"); var snapshot = buffer.Snapshot(); buffer.Append("second");
        Assert.Equal("first", snapshot); Assert.Equal("firstsecond", buffer.Take());
    }
}
