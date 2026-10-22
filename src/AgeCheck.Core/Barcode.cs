using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace AgeCheck.Core;

public enum Outcome { Passed, Under21, Expired, Review }
public sealed record CheckResult(Outcome Outcome, string Detail, DateOnly? Birth = null, DateOnly? Expiration = null, bool NeedsDateConfirmation = false,
    string? FirstName = null, string? LastName = null, string? NameNote = null, bool KeyboardTransport = false,
    [property: JsonIgnore] string? CardIdentity = null)
{
    public string? DisplayName => FirstName != null && LastName != null ? FirstName + " " + LastName : null;
}

public static partial class Barcode
{
    public const int InputLimit = 16384;
    public static string Normalize(string raw) => raw.Replace("[LF]", "\n").Replace("[CR]", "\r")
        .Replace("[RS]", "\u001e").Replace("[GS]", "\u001d").Replace("[ETX]", "\u0003");

    public static CheckResult Check(string raw, DateOnly today, bool allowScannerRecovery = false)
    {
        CheckResult Bad(string reason) => new(Outcome.Review, reason);
        if (raw.Length > InputLimit) return Bad("Input limit exceeded. Clear and rescan.");
        var text = Normalize(raw);
        if (text.StartsWith("]L0", StringComparison.Ordinal)) text = text[3..];
        if (!text.StartsWith("@\n\u001e\rANSI ", StringComparison.Ordinal))
            return allowScannerRecovery ? RecoverScannerDates(text, today) : Bad("Unsupported barcode header or altered control characters. Check scanner setup.");
        if (text.Any(c => c > 127)) return Bad("Unsupported character encoding. Match the scanner keyboard layout to Windows and rescan.");
        if (text.Length < 21 || !Digits().IsMatch(text.Substring(9, 12))) return Bad("Incomplete or malformed AAMVA header.");
        var version = text.Substring(15, 2);
        if (version is not ("08" or "09" or "10")) return Bad("Unsupported AAMVA version. Supported versions: 08, 09 and 10.");
        int count = int.Parse(text.AsSpan(19, 2), CultureInfo.InvariantCulture);
        int directoryEnd = 21 + count * 10;
        if (count == 0 || text.Length < directoryEnd) return Bad("Incomplete subfile directory.");
        var entries = new List<(string Type, int Start, int End)>();
        for (int i = 0; i < count; i++)
        {
            int p = 21 + i * 10;
            string type = text.Substring(p, 2);
            if (type is not ("DL" or "ID") && !(type[0] == 'Z' && type[1] is >= 'A' and <= 'Z')) return Bad("Unsupported subfile type.");
            if (!Digits().IsMatch(text.Substring(p + 2, 8))) return Bad("Invalid subfile length or offset.");
            int start = int.Parse(text.AsSpan(p + 2, 4), CultureInfo.InvariantCulture);
            int length = int.Parse(text.AsSpan(p + 6, 4), CultureInfo.InvariantCulture);
            int end = start + length;
            if (start < directoryEnd || length < 3 || end > text.Length) return Bad("Truncated barcode or inconsistent subfile length. Rescan the entire barcode.");
            if (entries.Any(e => start < e.End && end > e.Start)) return Bad("Overlapping subfiles require manual review.");
            if (text.Substring(start, 2) != type || text[end - 1] != '\r') return Bad("Invalid subfile framing. Check scanner control-character settings.");
            entries.Add((type, start, end));
        }
        var ordered = entries.OrderBy(e => e.Start).ToArray();
        int expected = directoryEnd;
        foreach (var e in ordered) { if (e.Start != expected) return Bad("Unexpected gap in barcode data."); expected = e.End; }
        if (text[expected..].Any(c => c is not ('\r' or '\n' or '\t' or '\u0003'))) return Bad("Unexpected trailing data or multiple scans.");
        var ids = entries.Where(e => e.Type is "DL" or "ID").ToArray();
        if (ids.Length != 1) return Bad("Expected exactly one driver license or ID subfile.");
        var id = ids[0];
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        string body = text[(id.Start + 2)..(id.End - 1)];
        if (body.EndsWith('\n')) body = body[..^1]; // An LF may precede the segment terminator.
        foreach (string line in body.Split('\n'))
        {
            if (line.Length < 3 || !FieldCode().IsMatch(line[..3]) || line.Any(char.IsControl)) return Bad("Malformed barcode field.");
            if (!fields.TryAdd(line[..3], line[3..])) return Bad("Duplicate barcode fields require manual review.");
        }
        if (!fields.TryGetValue("DCG", out var country) || country is not ("USA" or "CAN")) return Bad("Missing or unsupported issuing country. Date format cannot be assumed.");
        string format = country == "USA" ? "MMddyyyy" : "yyyyMMdd";
        bool Date(string key, out DateOnly value)
        {
            value = default;
            return fields.TryGetValue(key, out var s) && s.Length == 8 && Digits().IsMatch(s)
                && DateOnly.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
        }
        if (!Date("DBB", out var birth) || !Date("DBA", out var expiry) || !Date("DBD", out var issued))
            return Bad("Missing or invalid birth, expiration or issue date. Manual review required.");
        if (birth > today || issued > today || issued < birth || expiry < issued || birth < today.AddYears(-125))
            return new(Outcome.Review, "Dates are inconsistent. Compare the printed card and review manually.", birth, expiry);
        var name = ReadName(fields);
        string? identity = fields.TryGetValue("DAQ", out var cardNumber) && Regex.IsMatch(cardNumber, @"^[A-Z0-9-]{1,80}$", RegexOptions.CultureInvariant)
            ? text.Substring(9, 6) + "|" + country + "|" + cardNumber + "|" + birth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
        CheckResult Finish(CheckResult r) => r with { FirstName = name.First, LastName = name.Last, NameNote = name.Note, CardIdentity = identity };
        if (fields.ContainsKey("DDB") && (!Date("DDB", out var revised) || revised > issued))
            return Finish(new(Outcome.Review, "Card revision date is invalid or later than the issue date. Compare the printed card and review manually.", birth, expiry));
        if (fields.TryGetValue("DBC", out var sex) && sex is not ("1" or "2" or "9"))
            return Finish(new(Outcome.Review, "Unsupported barcode field code. Review the printed card manually.", birth, expiry));
        if (fields.TryGetValue("DDA", out var compliance) && compliance is not ("F" or "N"))
            return Finish(new(Outcome.Review, "Unrecognized compliance field. Review the card manually; this does not prove it is fake.", birth, expiry));
        if (new[] { "DDE", "DDF", "DDG" }.Any(k => fields.TryGetValue(k,out var value) && value is not ("N" or "T" or "U")))
            return Finish(new(Outcome.Review, "Invalid name truncation flags. Review the printed card manually.", birth, expiry));
        foreach (var (key, years) in new[] { ("DDH", 18), ("DDI", 19), ("DDJ", 21) })
        {
            if (!fields.ContainsKey(key)) continue;
            if (!Date(key, out var encoded) || encoded != birth.AddYears(years))
                return Finish(new(Outcome.Review, "Encoded age-threshold date conflicts with the birth date or needs local policy review.", birth, expiry));
        }
        return Finish(Evaluate(birth, expiry, today));
    }

    private static CheckResult RecoverScannerDates(string text, DateOnly today)
    {
        CheckResult Bad(string detail) => new(Outcome.Review, detail);
        // Normalize only the fixed header token, never identity/date values. Keyboard
        // transport may preserve some separators and drop others, or compose a dead key.
        // A repaired header always stays in assisted recovery, never strict validation.
        var header = RecoveryHeader().Match(text);
        if (header.Success) text = "@ANSI " + text[header.Length..];
        if (text.Any(c => c > 127)) return Bad("Unsupported character encoding. Match the scanner keyboard layout to Windows and rescan.");
        // A keyboard scanner can strip separators, the header, and even the first D.
        // Recovery is an operator-assisted date check, NEVER full-frame validation.
        bool nameStart = text.StartsWith("DCS", StringComparison.Ordinal) || text.StartsWith("CS", StringComparison.Ordinal)
            || text.StartsWith("DLDCS", StringComparison.Ordinal) || text.StartsWith("IDDCS", StringComparison.Ordinal);
        bool flatHeader = Regex.IsMatch(text, @"^@?ANSI [0-9]{6}(08|09|10)[0-9]{4}", RegexOptions.CultureInvariant);
        if (!nameStart && !flatHeader) return Bad("The scanner did not send a supported header. Check Scanner setup and scan again.");
        text = text.TrimEnd('\r', '\n', '\t', '\u0003');
        if (text.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t' or '\u001e')))
            return Bad("Unexpected scanner control characters. Rescan or check manually.");
        text = text.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\u001e", "");
        // A complete keyboard frame can be restored only when its directory and
        // every inferred separator account for the original encoded length.
        // Do not auto-evaluate headerless recovery or invent missing field values.
        if (flatHeader && RestoreKeyboardFrame(text) is { } restored)
        {
            var checkedFrame = Check(restored, today);
            return checkedFrame with { KeyboardTransport = true,
                Detail = checkedFrame.Outcome == Outcome.Review ? checkedFrame.Detail
                    : "Age and expiration calculated from the scanned dates. Compare the photograph and printed card with the customer. ID authenticity is not verified." };
        }
        var recovered = new Dictionary<string, string>();
        foreach (var key in new[] { "DCG", "DBB", "DBA", "DBD", "DDH", "DDI", "DDJ", "DDB", "DBC", "DDA", "DDE", "DDF", "DDG" })
        {
            int occurrences = Occurrences(text,key);
            bool required = key is "DCG" or "DBB" or "DBA" or "DBD";
            if (!required && occurrences == 0) continue;
            if (occurrences != 1) return Bad("Missing or repeated date/country fields. Scan again or review the card manually.");
            string valuePattern = key switch { "DCG" => "USA|CAN", "DBC" or "DDA" or "DDE" or "DDF" or "DDG" => "[A-Z0-9]", _ => "[0-9]{8}" };
            var match = Regex.Match(text, key + "(" + valuePattern + @")(?=D[A-Z]{2}|Z[A-Z]{2}|$)", RegexOptions.CultureInvariant);
            if (!match.Success) return Bad("A date or country field is incomplete or ambiguous. Scan again or review manually.");
            recovered[key] = match.Groups[1].Value;
        }
        var recoveredNames = RecoverNameFields(text);
        foreach (var (key,value) in recoveredNames) recovered[key] = value;
        // Reuse all strict calendar, consistency and optional threshold checks.
        string body = "DL" + string.Join('\n', recovered.Select(p => p.Key + p.Value)) + "\r";
        var result = Check($"@\n\u001e\rANSI 636000100001DL0031{body.Length:D4}{body}", today);
        if (result.Outcome == Outcome.Review) return result;
        return result with { Outcome = Outcome.Review, NeedsDateConfirmation = true,
            NameNote = result.DisplayName == null ? result.NameNote : "Recovered name: compare it with the printed card before copying.",
            Detail = "Dates recovered from scanner text. Formatting is incomplete, so full barcode checks are unavailable. Compare both dates with the printed card, then confirm below." };
    }

    private static string? RestoreKeyboardFrame(string text)
    {
        var header = Regex.Match(text, @"^@?ANSI ([0-9]{6}(?:08|09|10)[0-9]{2}01)(DL|ID)0031([0-9]{4})", RegexOptions.CultureInvariant);
        if (!header.Success) return null;
        string type = header.Groups[2].Value;
        string body = text[header.Length..];
        if (!body.StartsWith(type, StringComparison.Ordinal)) return null;
        var fields = Regex.Matches(body[2..], @"D[A-Z]{2}|Z[A-Z]{2}", RegexOptions.CultureInvariant);
        if (fields.Count == 0 || fields[0].Index != 0) return null;
        // This compatibility profile has an account number first and a fixed-width
        // revision date last. A plausible date found midway through a transmission
        // is insufficient evidence that the whole card arrived.
        if (fields[0].Value != "DAQ" || fields[^1].Value != "DDB" || body.Length - fields[^1].Index - 2 != 11) return null;
        int length = int.Parse(header.Groups[3].Value, CultureInfo.InvariantCulture);
        int restoredLength = body.Length + fields.Count; // inter-field LF plus final CR
        if (length != restoredLength) return null;
        var lines = new List<string>();
        for (int i = 0; i < fields.Count; i++)
        {
            int start = fields[i].Index + 2;
            int end = i + 1 < fields.Count ? fields[i + 1].Index + 2 : body.Length;
            lines.Add(body[start..end]);
        }
        string restoredBody = type + string.Join('\n', lines) + "\r";
        return "@\n\u001e\rANSI " + header.Groups[1].Value + type + "0031" + length.ToString("D4", CultureInfo.InvariantCulture) + restoredBody;
    }

    private static (string? First,string? Last,string? Note) ReadName(Dictionary<string,string> fields)
    {
        if (!fields.TryGetValue("DAC",out var first) || !fields.TryGetValue("DCS",out var last)) return (null,null,"Name not available in this scan.");
        if (new[]{"DDE","DDF"}.Any(k => fields.TryGetValue(k,out var flag) && flag != "N")) return (null,null,"Name may be shortened. Compare the printed card.");
        string? Clean(string value)
        {
            value = Regex.Replace(value.Trim(), " +", " ").ToUpperInvariant();
            return value is not ("NONE" or "UNAVL") && Regex.IsMatch(value,@"^[A-Z][A-Z '\-]{0,79}$",RegexOptions.CultureInvariant) ? value : null;
        }
        var f=Clean(first); var l=Clean(last);
        return f != null && l != null ? (f,l,null) : (null,null,"Name could not be read safely. Compare the printed card.");
    }
    private static Dictionary<string,string> RecoverNameFields(string text)
    {
        var names = new Dictionary<string,string>();
        if (text.StartsWith("CS",StringComparison.Ordinal)) text="D"+text;
        // With lost separators, require unambiguous standard name/flag boundaries.
        foreach(var (key,next) in new[]{("DCS",@"DDE[NTU]DAC|DAC"),("DAC",@"DDF[NTU](?:DAD|DBD|DBB|DBA|DCA)|DAD|DBD|DBB|DBA|DCA")})
        {
            if (Occurrences(text,key) != 1) return new();
            var m=Regex.Match(text,key+@"((?:(?!D[A-Z]{2}|Z[A-Z]{2})[A-Za-z '\-]){1,80})(?="+next+")",RegexOptions.CultureInvariant);
            if (!m.Success) return new();
            names[key]=m.Groups[1].Value;
        }
        return names;
    }
    private static int Occurrences(string text,string key)
    {
        int count=0,offset=0;
        while((offset=text.IndexOf(key,offset,StringComparison.Ordinal))>=0) { count++; offset++; }
        return count;
    }

    public static CheckResult Evaluate(DateOnly birth, DateOnly expiry, DateOnly today)
    {
        if (birth > today || expiry < birth) return new(Outcome.Review, "Inconsistent dates.", birth, expiry);
        if (expiry < today) return new(Outcome.Expired, "The barcode expiration date is in the past.", birth, expiry);
        // February 29 reaches the threshold on March 1 in a non-leap year.
        // February 28 is deliberately referred for jurisdiction-specific review.
        int year = birth.Year + 21;
        if (year > 9999) return new(Outcome.Review, "Invalid age boundary.", birth, expiry);
        bool leap = birth.Month == 2 && birth.Day == 29 && !DateTime.IsLeapYear(year);
        if (leap && today == new DateOnly(year, 2, 28))
            return new(Outcome.Review, "Leap-day birthday: local policy may differ on February 28. Manual review required.", birth, expiry);
        var threshold = leap ? new DateOnly(year, 3, 1) : birth.AddYears(21);
        return new(today >= threshold ? Outcome.Passed : Outcome.Under21,
            today >= threshold ? "Compare the printed dates, photograph and customer before proceeding." : "The barcode birth date indicates an age below 21.", birth, expiry);
    }

    [GeneratedRegex("^[0-9]+$", RegexOptions.CultureInvariant)] private static partial Regex Digits();
    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)] private static partial Regex FieldCode();
    [GeneratedRegex(@"\A[\r\n\t\x1e]*(?:\]L0[\r\n\t\x1e]*)?@?[\r\n\t\x1e]*(?:ANSI|ÂNSI|A\u0302NSI) ", RegexOptions.CultureInvariant)]
    private static partial Regex RecoveryHeader();
}
