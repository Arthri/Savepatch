using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

internal static partial class Locator
{
    private static bool LocateSteam(
        [NotNullWhen(true)] out string? path
    )
    {
        if (OperatingSystem.IsWindows())
        {
            if (LocateSteamUsingRegistry(out path))
            {
                return true;
            }
        }

        path = null;
        return false;
    }

    [SupportedOSPlatform("windows")]
    private static bool LocateSteamUsingRegistry(
        [NotNullWhen(true)] out string? path
    )
    {
        object? pathKey = Registry.CurrentUser.OpenSubKey("""SOFTWARE\Valve\Steam""")?.GetValue("SteamPath");
        pathKey ??= (Environment.Is64BitOperatingSystem
            ? Registry.LocalMachine.OpenSubKey("""SOFTWARE\Wow6432Node\Valve\Steam""")
            : Registry.LocalMachine.OpenSubKey("""SOFTWARE\Valve\Steam"""))
            ?.GetValue("InstallPath")
            ;
        if (pathKey is not (null or string))
        {
            Console.WriteLine($"Unknown key type. ({pathKey.GetType().Name})");
            path = null;
            return false;
        }
        else if (pathKey is string pathString)
        {
            path = pathString;
            return true;
        }

        path = null;
        return false;
    }

    internal static bool LocateTerraria(
        [NotNullWhen(true)] out string? path
    )
    {
        if (LocateSteam(out string? steamPath))
        {
            var steamappsPath = Path.Combine(steamPath, "steamapps");
            var appmanifestPath = Path.Combine(steamappsPath, "appmanifest_105600.acf");
            if (File.Exists(appmanifestPath))
            {
                var appmanifest = File.ReadAllText(appmanifestPath);
                var match = InstalldirMatcher().Match(appmanifest);
                if (match.Success)
                {
                    var installDir = Path.Combine(steamappsPath, "common", match.Value);
                    var terrariaPath = Path.Combine(installDir, "Terraria.exe");
                    if (File.Exists(terrariaPath))
                    {
                        path = terrariaPath;
                        return true;
                    }
                }
            }
        }

        if (File.Exists("Terraria.exe"))
        {
            path = Path.GetFullPath("Terraria.exe");
            return true;
        }

        path = null;
        return false;
    }

    [GeneratedRegex("(?<=\t\"installdir\"\t\t\")[^\"]+(?=\")")]
    private static partial Regex InstalldirMatcher();
}
