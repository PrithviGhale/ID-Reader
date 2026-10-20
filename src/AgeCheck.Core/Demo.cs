using System.Globalization;
namespace AgeCheck.Core;
public static class Demo
{
    public static string Create(DateOnly birth, DateOnly expiry, DateOnly issued, string country = "USA")
    {
        string f = country == "CAN" ? "yyyyMMdd" : "MMddyyyy";
        string Date(DateOnly d) => d.ToString(f, CultureInfo.InvariantCulture);
        var body = $"DLDCG{country}\nDBB{Date(birth)}\nDBA{Date(expiry)}\nDBD{Date(issued)}\r";
        return $"@\n\u001e\rANSI 636000100001DL0031{body.Length:D4}{body}";
    }
}
