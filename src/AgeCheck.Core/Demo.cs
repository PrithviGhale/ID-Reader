using System.Globalization;
namespace AgeCheck.Core;
public static class Demo
{
    public static string KeyboardHeader(DateOnly birth, DateOnly expiry, DateOnly issued, string country = "USA")
    {
        var data = "DLDAQSYNTHETIC0001D" + Flattened(birth, expiry, issued, country);
        return $"@ÂNSI 636000100001DL0031{data.Length:D4}{data}";
    }
    public static string Flattened(DateOnly birth, DateOnly expiry, DateOnly issued, string country = "USA")
    {
        string f = country == "CAN" ? "yyyyMMdd" : "MMddyyyy";
        string Date(DateOnly d) => d.ToString(f, CultureInfo.InvariantCulture);
        return $"CSSAMPLEDDENDACALEXDDFNDADTESTDDGNDCADDCBNONEDCDNONEDBD{Date(issued)}DBB{Date(birth)}DBA{Date(expiry)}DBC1DCG{country}DDANDDB{Date(new(2024,1,1))}";
    }
    public static string CreateNamed(DateOnly birth, DateOnly expiry, DateOnly issued, string first = "ALEX", string last = "SAMPLE")
    {
        string body = "DLDCS"+last+"\nDAC"+first+"\nDDEN\nDDFN\n"+Create(birth,expiry,issued)[33..];
        return $"@\n\u001e\rANSI 636000100001DL0031{body.Length:D4}{body}";
    }
    public static string Create(DateOnly birth, DateOnly expiry, DateOnly issued, string country = "USA")
    {
        string f = country == "CAN" ? "yyyyMMdd" : "MMddyyyy";
        string Date(DateOnly d) => d.ToString(f, CultureInfo.InvariantCulture);
        var body = $"DLDCG{country}\nDBB{Date(birth)}\nDBA{Date(expiry)}\nDBD{Date(issued)}\r";
        return $"@\n\u001e\rANSI 636000100001DL0031{body.Length:D4}{body}";
    }
}
