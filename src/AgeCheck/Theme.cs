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
    public static void Apply(bool dark, FrameworkElement? surface = null)
    {
        var colors = new Dictionary<string, string>
        {
            ["Canvas"] = dark ? "#080F1C" : "#E9F1F7",
            ["Surface"] = dark ? "#142133" : "#FFFFFF",
            ["Inset"] = dark ? "#0D1929" : "#F0F6FA",
            ["Line"] = dark ? "#2C4258" : "#CEDDE8",
            ["Ink"] = dark ? "#F3F8FC" : "#122C40",
            ["Muted"] = dark ? "#AAC0D1" : "#516E81",
            ["Accent"] = dark ? "#77E8CF" : "#087B77",
            ["AccentInk"] = dark ? "#073E42" : "#FFFFFF",
            ["AccentSoft"] = dark ? "#153E44" : "#DBF3EE",
            ["AccentText"] = dark ? "#85F2DE" : "#086E6C",
            ["Brand"] = dark ? "#67BAF8" : "#146DA4",
            ["BrandInk"] = dark ? "#0C2A42" : "#FFFFFF",
            ["BrandSoft"] = dark ? "#173652" : "#DBEDFA",
            ["BrandText"] = dark ? "#9DD6FF" : "#145F8C",
            ["Alert"] = "#B93846",
            ["Quiet"] = dark ? "#25384D" : "#DFEBF2",
            ["Warm"] = dark ? "#3B3021" : "#FFF4E2",
            ["WarmInk"] = dark ? "#F4CE90" : "#80551C",
            ["Danger"] = dark ? "#FFC4C0" : "#A13C38",
            ["DangerSoft"] = dark ? "#40282D" : "#FCECEA"
        };
        foreach(var (key,value) in colors)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
            brush.Freeze(); Application.Current.Resources[key] = brush;
            // Local resources also invalidate detached test presentation sources immediately.
            surface?.Resources[key] = brush;
        }
    }
}
