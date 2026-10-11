using System.Text.RegularExpressions;

namespace WukongBenchAuto.Core.Config;

/// <summary>Валидация папки установки Benchmark Tool.</summary>
public static class GameInstallValidator
{
    public const string FolderName = "Black Myth Wukong Benchmark Tool";
    public const string BenchmarkExe = "b1_benchmark.exe";
    public const string GameExe = "b1-Win64-Shipping.exe";

    private static readonly string[] RequiredRelativePaths =
    [
        BenchmarkExe,
        Path.Combine("b1", "Binaries", "Win64", GameExe),
    ];

    /// <summary>Путь к GameUserSettings.ini внутри папки игры.</summary>
    public static string IniPath(string gameDir) =>
        Path.Combine(gameDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");

    /// <summary>Проверить, что папка действительно содержит Benchmark Tool.</summary>
    public static bool IsValid(string? gameDir)
    {
        if (string.IsNullOrWhiteSpace(gameDir))
            return false;

        try
        {
            return RequiredRelativePaths.All(p => File.Exists(Path.Combine(gameDir, p)));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

/// <summary>
/// Поиск папки Benchmark Tool: реестр Steam → libraryfolders.vdf → перебор библиотек.
/// Возвращает папку steamapps/common, содержащую установленный Benchmark Tool.
/// </summary>
public static partial class SteamLocator
{
    [GeneratedRegex("\"path\"\\s*\"(?<path>[^\"]+)\"")]
    private static partial Regex LibraryPathRegex();

    private static readonly string[] RegistryKeys =
    [
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
        @"HKEY_CURRENT_USER\Software\Valve\Steam",
    ];

    public static IReadOnlyList<string> GetSteamLibraries()
    {
        var libraries = new List<string>();

        foreach (var key in RegistryKeys)
        {
            var steamPath = Microsoft.Win32.Registry.GetValue(key, "SteamPath", null) as string;
            if (string.IsNullOrEmpty(steamPath))
                continue;

            var steamRoot = NormalizePath(steamPath);
            var appsDir = Path.Combine(steamRoot, "steamapps");

            if (!libraries.Contains(appsDir, StringComparer.OrdinalIgnoreCase))
                libraries.Add(appsDir);

            var vdf = Path.Combine(appsDir, "libraryfolders.vdf");
            if (!File.Exists(vdf))
                continue;

            try
            {
                foreach (Match m in LibraryPathRegex().Matches(File.ReadAllText(vdf)))
                {
                    var lib = NormalizePath(m.Groups["path"].Value);
                    var apps = Path.Combine(lib, "steamapps");
                    if (Directory.Exists(apps) && !libraries.Contains(apps, StringComparer.OrdinalIgnoreCase))
                        libraries.Add(apps);
                }
            }
            catch (IOException)
            {
                // Файл мог быть занят Steam'ом — пропускаем.
            }
        }

        return libraries;
    }

    /// <summary>Найти папку Benchmark Tool во всех известных Steam-библиотеках.</summary>
    public static string? FindBenchmarkTool()
    {
        foreach (var appsDir in GetSteamLibraries())
        {
            var candidate = Path.Combine(appsDir, "common", GameInstallValidator.FolderName);
            if (GameInstallValidator.IsValid(candidate))
                return candidate;
        }

        return null;
    }

    private static string NormalizePath(string raw)
    {
        var trimmed = raw.Trim().TrimEnd('\\', '/').Replace('/', Path.DirectorySeparatorChar);
        // VDF хранит двойные бэкслэши: E:\\SteamLibrary → E:\SteamLibrary.
        var unescaped = trimmed.Replace("\\\\", "\\");
        return Path.GetFullPath(unescaped);
    }
}
