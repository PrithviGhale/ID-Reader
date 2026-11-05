using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace AgeCheck;
internal static partial class Theme
{
    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    public static void TitleBar(Window window, bool dark)
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        int enabled = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
        int rounded=2;
        _ = DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
    }
    public static bool IsDark(string theme) => theme is not ("Light" or "Sand");
    public static void Apply(bool dark, FrameworkElement? surface = null) => Apply(dark ? "Dark" : "Light", surface);

    public static void Apply(string theme, FrameworkElement? surface = null)
    {
        // Four complete palettes share the same semantic colors. Keeping the
        // brushes frozen avoids a render-thread cost while results are painted.
        int choice = theme switch { "Light" => 1, "Ocean" => 2, "Sand" => 3, _ => 0 };
        var palettes = new Dictionary<string, string[]>
        {
            ["Canvas"] = ["#11100F", "#F1EFE9", "#07151D", "#F5EFE4"],
            ["Surface"] = ["#1D1C1A", "#FFFEFA", "#102530", "#FFFAF1"],
            ["Inset"] = ["#292723", "#E9E5DC", "#16313D", "#ECE2D1"],
            ["Line"] = ["#49443B", "#D1CABD", "#335665", "#D5C5AE"],
            ["Ink"] = ["#F9F4EA", "#251F19", "#F1FAFA", "#33261A"],
            ["Muted"] = ["#C4BBAE", "#5A554D", "#B3CCD2", "#665849"],
            ["Accent"] = ["#C8ED72", "#47712A", "#69E0CB", "#9A552E"],
            ["AccentInk"] = ["#202B0E", "#FFFFFF", "#0B3335", "#FFFFFF"],
            ["AccentSoft"] = ["#30401F", "#E3EED5", "#19463F", "#F4E1CF"],
            ["AccentText"] = ["#DDFE96", "#365D1E", "#89F2DE", "#794020"],
            ["Brand"] = ["#D5B8FF", "#7251A0", "#89C9F3", "#B57651"],
            ["BrandInk"] = ["#2C1C3E", "#FFFFFF", "#0D2E43", "#FFFFFF"],
            ["BrandSoft"] = ["#352841", "#EDE3F4", "#203C53", "#F2E1D4"],
            ["BrandText"] = ["#E8D3FF", "#604084", "#B2E2FF", "#804B30"],
            ["Alert"] = ["#D35E65", "#B93846", "#E46F72", "#B74342"],
            ["Quiet"] = ["#34312C", "#E5DFD4", "#26404A", "#E8DCCB"],
            ["Warm"] = ["#443323", "#FFF0D8", "#403321", "#FBE9CF"],
            ["WarmInk"] = ["#FFD49A", "#775018", "#FFD49A", "#76501D"],
            ["Danger"] = ["#FFB6AE", "#A63535", "#FFB7AD", "#A23535"],
            ["DangerSoft"] = ["#442A28", "#F9E7E3", "#442E32", "#F8E5DE"]
        };
        foreach(var (key,values) in palettes)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[choice]));
            brush.Freeze(); Application.Current.Resources[key] = brush;
            // Local resources also invalidate detached test presentation sources immediately.
            surface?.Resources[key] = brush;
        }
    }
}
