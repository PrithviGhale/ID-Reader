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
            ["Canvas"] = dark ? "#0D121B" : "#F2F5F7",
            ["Surface"] = dark ? "#171F2C" : "#FFFFFF",
            ["Inset"] = dark ? "#101722" : "#F3F6F8",
            ["Line"] = dark ? "#2C3748" : "#DFE5E9",
            ["Ink"] = dark ? "#F0F5FB" : "#192C3A",
            ["Muted"] = dark ? "#A4B3C6" : "#526879",
            ["Accent"] = dark ? "#A5E8CB" : "#156D54",
            ["AccentInk"] = dark ? "#123B2C" : "#FFFFFF",
            ["AccentSoft"] = dark ? "#203D36" : "#E6F4ED",
            ["AccentText"] = dark ? "#B3F1D5" : "#156D54",
            ["Brand"] = dark ? "#B9ACFF" : "#5846AF",
            ["BrandInk"] = dark ? "#241C49" : "#FFFFFF",
            ["BrandSoft"] = dark ? "#29253F" : "#EEEAFB",
            ["BrandText"] = dark ? "#D1C7FF" : "#5846AF",
            ["Alert"] = "#B93846",
            ["Quiet"] = dark ? "#253143" : "#E8EEF2",
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
