using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Settings;

/// <summary>Формат одной пары UISettingData: ("Key", "Value").</summary>
public sealed record UiSetting(string Key, string Value);

public interface IGameSettingsStore
{
    /// <summary>Прочитать текущее содержимое GameUserSettings.ini.</summary>
    string Read();

    /// <summary>Записать содержимое GameUserSettings.ini.</summary>
    void Write(string content);
}

/// <summary>Реальный ini в папке игры.</summary>
public sealed class GameSettingsStore(string gameDirectory) : IGameSettingsStore
{
    private static readonly Encoding IniEncoding = new UTF8Encoding(false);

    public string Read()
    {
        var path = GameInstallPath();
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"GameUserSettings.ini не найден: {path}. Запустите бенчмарк вручную один раз, чтобы файл создался.");
        return File.ReadAllText(path);
    }

    public void Write(string content)
    {
        File.WriteAllText(GameInstallPath(), content, IniEncoding);
    }

    private string GameInstallPath() => GameInstallValidatorPath(gameDirectory);

    private static string GameInstallValidatorPath(string dir) =>
        Path.Combine(dir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
}

/// <summary>
/// Применение профиля настроек к GameUserSettings.ini:
/// пары UISettingData=("Key","Value") + ключи [ScalabilityGroups] + разрешение/VSync/лимит FPS.
/// </summary>
public static partial class IniProfileApplier
{
    [GeneratedRegex("\\(\"(?<key>[^\"]+)\",\\s*\"(?<value>[^\"]*)\"\\)")]
    private static partial Regex UiPairRegex();

    [GeneratedRegex(@"UISettingData=\((?<data>.*)\)")]
    private static partial Regex UiDataRegex();

    [GeneratedRegex(@"(?m)^(?<key>sg\.\w+)=.*$")]
    private static partial Regex SgLineRegex();

    [GeneratedRegex(@"(?m)^(?<key>\w+)=.*$")]
    private static partial Regex TopLineRegex();

    /// <summary>Наборы настроек, управляющие парами UISettingData.</summary>
    private static readonly Dictionary<string, Func<BenchmarkProfile, string>> UiValues = new()
    {
        ["QualityLevel"] = p => p.QualityLevel.ToString(),
        ["ImageQuality"] = p => p.ImageQuality.ToString(),
        ["Rtx"] = p => p.Rtx.ToString(),
        ["RtxLevel"] = p => p.RtxLevel.ToString(),
        ["Dlss"] = p => p.Dlss.ToString(),
        ["InsertFrame"] = p => p.InsertFrame.ToString(),
        ["MotionBlur"] = p => p.MotionBlur.ToString(),
        ["Dx12"] = p => p.Dx12.ToString(),
        ["Vsync"] = p => p.VSync.ToString(),
        ["ViewDistance"] = p => p.ViewDistance.ToString(),
        ["AntiAliasing"] = p => p.AntiAliasing.ToString(),
        ["PostProcessing"] = p => p.PostProcessing.ToString(),
        ["ShadowQuality"] = p => p.ShadowQuality.ToString(),
        ["TextureQuality"] = p => p.TextureQuality.ToString(),
        ["MaterialQuality"] = p => p.MaterialQuality.ToString(),
        ["VegetationQuality"] = p => p.VegetationQuality.ToString(),
        ["FxQuality"] = p => p.FxQuality.ToString(),
        ["GlobalIllumination"] = p => p.GlobalIllumination.ToString(),
        ["ReflectionQuality"] = p => p.ReflectionQuality.ToString(),
    };

    /// <summary>Ключи ScalabilityGroups, производные от качества сцен.</summary>
    private static readonly (string Key, Func<BenchmarkProfile, int> Value)[] Scalability =
    [
        ("sg.ViewDistanceQuality", p => p.ViewDistance),
        ("sg.AntiAliasingQuality", p => p.AntiAliasing),
        ("sg.PostProcessQuality", p => p.PostProcessing),
        ("sg.ShadowQuality", p => p.ShadowQuality),
        ("sg.TextureQuality", p => p.TextureQuality),
        ("sg.ShadingQuality", p => p.MaterialQuality),
        ("sg.EffectsQuality", p => p.FxQuality),
        ("sg.FoliageQuality", p => p.VegetationQuality),
        ("sg.GlobalIlluminationQuality", p => p.GlobalIllumination),
        ("sg.ReflectionQuality", p => p.ReflectionQuality),
        ("sg.RayTracingQuality", p => p.Rtx > 0 ? 3 : 0),
        ("sg.ResolutionQuality", p => p.ImageQuality),
    ];

    /// <summary>Применить профиль к ini-тексту. Возвращает новый текст.</summary>
    public static string Apply(string ini, BenchmarkProfile profile)
    {
        ini = ReplaceUiData(ini, profile);
        ini = ReplaceTopKey(ini, "ResolutionSizeX", profile.ResolutionWidth.ToString());
        ini = ReplaceTopKey(ini, "ResolutionSizeY", profile.ResolutionHeight.ToString());
        ini = ReplaceTopKey(ini, "LastUserConfirmedResolutionSizeX", profile.ResolutionWidth.ToString());
        ini = ReplaceTopKey(ini, "LastUserConfirmedResolutionSizeY", profile.ResolutionHeight.ToString());
        ini = ReplaceTopKey(ini, "bUseVSync", profile.VSync == 1 ? "True" : "False");
        ini = ReplaceTopKey(ini, "FrameRateLimit", profile.FrameRateLimit.ToString("0.000000", CultureInfo.InvariantCulture));
        ini = ReplaceTopKey(ini, "bNeverShowStartupUI", "True");
        ini = ReplaceSgSection(ini, profile);
        return ini;
    }

    /// <summary>Разобрать пары UISettingData в словарь.</summary>
    public static IReadOnlyDictionary<string, string> ParseUiData(string ini)
    {
        var m = UiDataRegex().Match(ini);
        if (!m.Success)
            return new Dictionary<string, string>();

        return UiPairRegex().Matches(m.Groups["data"].Value)
            .ToDictionary(p => p.Groups["key"].Value, p => p.Groups["value"].Value);
    }

    private static Dictionary<string, string> ParseUiDataMutable(string ini)
    {
        var m = UiDataRegex().Match(ini);
        return m.Success
            ? UiPairRegex().Matches(m.Groups["data"].Value)
                .ToDictionary(p => p.Groups["key"].Value, p => p.Groups["value"].Value)
            : [];
    }

    /// <summary>Снимок всех значений профиля (для отчёта и сравнения).</summary>
    public static IReadOnlyDictionary<string, string> Snapshot(BenchmarkProfile profile)
    {
        var dict = new Dictionary<string, string>
        {
            ["ResolutionSizeX"] = profile.ResolutionWidth.ToString(),
            ["ResolutionSizeY"] = profile.ResolutionHeight.ToString(),
            ["ImageQuality"] = profile.ImageQuality.ToString(),
            ["bUseVSync"] = profile.VSync == 1 ? "True" : "False",
            ["FrameRateLimit"] = profile.FrameRateLimit.ToString("0.000000", CultureInfo.InvariantCulture),
            ["sg.ResolutionQuality"] = profile.ImageQuality.ToString(),
        };

        foreach (var (key, get) in Scalability)
            if (key != "sg.ResolutionQuality")
                dict[key] = get(profile).ToString();

        foreach (var (key, get) in UiValues)
            dict[$"UI:{key}"] = get(profile);

        return dict;
    }

    private static string ReplaceUiData(string ini, BenchmarkProfile profile)
    {
        var pairs = new List<string>();
        var existing = ParseUiDataMutable(ini);

        foreach (var (key, get) in UiValues)
        {
            var value = get(profile);
            existing[key] = value;
            pairs.Add($"(\"{key}\", \"{value}\")");
        }

        // Сохраняем прочие существующие пары (PrivacyAgreement и т.п.).
        foreach (var (key, value) in existing)
            if (!UiValues.ContainsKey(key))
                pairs.Add($"(\"{key}\", \"{value}\")");

        return UiDataRegex().Replace(ini, m => $"UISettingData=({string.Join(",", pairs)})");
    }

    private static string ReplaceTopKey(string ini, string key, string value)
    {
        var replaced = false;
        var result = TopLineRegex().Replace(ini, m =>
        {
            if (!string.Equals(m.Groups["key"].Value, key, StringComparison.Ordinal))
                return m.Value;
            replaced = true;
            return $"{key}={value}";
        });

        return replaced ? result : result + $"\r\n{key}={value}";
    }

    private static string ReplaceSgSection(string ini, BenchmarkProfile profile)
    {
        var values = Scalability.ToDictionary(s => s.Key, s => s.Value(profile).ToString());
        return SgLineRegex().Replace(ini, m =>
        {
            var key = m.Groups["key"].Value;
            return values.TryGetValue(key, out var v) ? $"{key}={v}" : m.Value;
        });
    }
}
