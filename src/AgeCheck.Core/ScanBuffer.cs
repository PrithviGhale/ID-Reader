using System.Text;
namespace AgeCheck.Core;

// Event-driven: the UI arms one quiet-period timer, never a polling loop.
public sealed class ScanBuffer
{
    private readonly StringBuilder buffer = new();
    public bool Receiving => buffer.Length > 0;
    public bool Overflowed { get; private set; }
    public bool Append(string text)
    {
        if (Overflowed) return false;
        if (buffer.Length + text.Length > Barcode.InputLimit) { Clear(); Overflowed = true; return false; }
        buffer.Append(text);
        return true;
    }
    public string Take() { var text = buffer.ToString(); Clear(); return text; }
    public string Snapshot() => buffer.ToString();
    public void Clear() { buffer.Clear(); Overflowed = false; }
}
