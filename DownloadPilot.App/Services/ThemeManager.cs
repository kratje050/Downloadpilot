using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using WpfApplication = System.Windows.Application;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace DownloadPilot.App.Services;

internal static class ThemeManager
{
    private static readonly IReadOnlyDictionary<string, string> LightPalette = new Dictionary<string, string>
    {
        ["AppBackgroundBrush"] = "#F4F6F8",
        ["SurfaceBrush"] = "#FFFFFF",
        ["SurfaceMutedBrush"] = "#F0F2F4",
        ["SurfaceHoverBrush"] = "#F7F8FA",
        ["TextPrimaryBrush"] = "#181B20",
        ["TextSecondaryBrush"] = "#69717D",
        ["BorderBrush"] = "#E1E5E9",
        ["AccentBrush"] = "#167C75",
        ["AccentHoverBrush"] = "#116A64",
        ["AccentSoftBrush"] = "#E2F3F0",
        ["OnAccentBrush"] = "#FFFFFF",
        ["SuccessBrush"] = "#23845D",
        ["SuccessSoftBrush"] = "#E6F4EC",
        ["InfoBrush"] = "#3675D3",
        ["InfoSoftBrush"] = "#EAF1FC",
        ["WarningBrush"] = "#C17B16",
        ["WarningSoftBrush"] = "#FBF0DC",
        ["CoralBrush"] = "#C65A56",
        ["CoralSoftBrush"] = "#FAEAE9",
        ["DangerBrush"] = "#BD3D4A"
    };

    private static readonly IReadOnlyDictionary<string, string> DarkPalette = new Dictionary<string, string>
    {
        ["AppBackgroundBrush"] = "#101215",
        ["SurfaceBrush"] = "#191C20",
        ["SurfaceMutedBrush"] = "#22262B",
        ["SurfaceHoverBrush"] = "#1E2227",
        ["TextPrimaryBrush"] = "#F1F3F5",
        ["TextSecondaryBrush"] = "#A9B0BA",
        ["BorderBrush"] = "#30353C",
        ["AccentBrush"] = "#2AA99E",
        ["AccentHoverBrush"] = "#35BDB1",
        ["AccentSoftBrush"] = "#193936",
        ["OnAccentBrush"] = "#071A18",
        ["SuccessBrush"] = "#55B987",
        ["SuccessSoftBrush"] = "#1B3428",
        ["InfoBrush"] = "#70A2EB",
        ["InfoSoftBrush"] = "#1D2E48",
        ["WarningBrush"] = "#E2A546",
        ["WarningSoftBrush"] = "#3C2D18",
        ["CoralBrush"] = "#E17B75",
        ["CoralSoftBrush"] = "#402522",
        ["DangerBrush"] = "#E66D78"
    };

    public static void Apply(string? theme)
    {
        var useDark = string.Equals(theme, "Donker", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(theme, "Windows", StringComparison.OrdinalIgnoreCase) && IsWindowsDarkMode());
        var palette = useDark ? DarkPalette : LightPalette;

        if (WpfApplication.Current is null)
        {
            return;
        }

        foreach (var (key, value) in palette)
        {
            WpfApplication.Current.Resources[key] = new SolidColorBrush(
                (WpfColor)WpfColorConverter.ConvertFromString(value));
        }
    }

    private static bool IsWindowsDarkMode()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }
}
