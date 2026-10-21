using AgeCheck.Core;

namespace AgeCheck.Tests;

public class PublishedSpecimenTests
{
    // Public fictional specimen from the 2020 AAMVA design standard, Annex D.13.
    // https://www.aamva.org/getmedia/99ac7057-0f4d-4461-b0a2-3a5532e1b35c/AAMVA-2020-DLID-Card-Design-Standard.pdf
    private static string Specimen => "@\n\u001e\rANSI 636000100002DL00410278ZV03190008" +
        "DLDAQT64235789\nDCSSAMPLE\nDDEN\nDACMICHAEL\nDDFN\nDADJOHN\nDDGN\nDCUJR\n" +
        "DCAD\nDCBK\nDCDPH\nDBD06062019\nDBB06061986\nDBA12102024\nDBC1\nDAU068 in\nDAYBRO\n" +
        "DAG2300 WEST BROAD STREET\nDAIRICHMOND\nDAJVA\nDAK232690000 \n" +
        "DCF2424244747474786102204\nDCGUSA\nDCK123456789\nDDAF\nDDB06062018\nDDC06062020\nDDD1\rZVZVA01\r";

    [Fact]
    public void PrintedSpecimenDirectoryMismatchIsNotSilentlyAccepted()
    {
        // The text transcribed from the published example has 277 ID bytes,
        // while its directory declares 278. Preserve the original rejection.
        var result = Barcode.Check(Specimen, new(2026, 10, 21), true);
        Assert.Equal(Outcome.Review, result.Outcome);
        Assert.False(result.NeedsDateConfirmation);
        Assert.Contains("framing", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(2023, 10, 21, Outcome.Passed)]
    [InlineData(2026, 10, 21, Outcome.Expired)]
    public void PublishedFieldsWithConsistentDirectoryUseTheirActualDates(int year, int month, int day, Outcome expected)
    {
        string corrected = Specimen.Replace("DL00410278ZV03190008", "DL00410277ZV03180008");
        var result = Barcode.Check(corrected, new(year, month, day));
        Assert.True(result.Outcome == expected, result.Detail);
        Assert.Equal(new DateOnly(1986, 6, 6), result.Birth);
        Assert.Equal(new DateOnly(2024, 12, 10), result.Expiration);
        Assert.Equal("MICHAEL SAMPLE", result.DisplayName);
    }

    [Fact]
    public void FlattenedSpecimenWithAmbiguousFieldMarkersRequiresManualReview()
    {
        string flat = Specimen.Replace("@\n\u001e\rANSI ", "@\rÂNSI ").Replace("\n", "").Replace("\r", "");
        var result = Barcode.Check(flat, new(2026, 10, 21), true);
        // The final D of RICHMOND plus DAJ creates an extra apparent DDA marker.
        // With field separators gone, the parser must not guess that ambiguity away.
        Assert.False(result.NeedsDateConfirmation);
        Assert.Equal(Outcome.Review, result.Outcome);
    }
}
