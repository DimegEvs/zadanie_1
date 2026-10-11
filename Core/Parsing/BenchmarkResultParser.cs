using System.Text.Json;
using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Parsing;

/// <summary>Ошибка разбора результата бенчмарка.</summary>
public sealed class BenchmarkParseException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>
/// Разбор JSON-файла результата из %TEMP%\b1\BenchMarkHistory\Tool\<timestamp>.
/// </summary>
public static class BenchmarkResultParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public static BenchmarkSummary ParseSummary(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var cpuFrameTime = AverageFrameTime(root, "CPUFrameTime");
            var gpuFrameTime = AverageFrameTime(root, "GPUFrameTime");

            return new BenchmarkSummary
            {
                FpsAvg = GetDouble(root, "FPSAvg"),
                FpsMax = GetDouble(root, "FPSMax"),
                FpsMin = GetDouble(root, "FPSMin"),
                Fps95 = GetDouble(root, "FPS95"),
                CpuUsage = GetDouble(root, "CPUAvg"),
                GpuUsage = GetDouble(root, "GPUAvg"),
                VideoMemGb = GetDouble(root, "VideoMem"),
                CpuFrameTimeMs = cpuFrameTime,
                GpuFrameTimeMs = gpuFrameTime,
                FrameCount = GetFrameCount(root),
                QualityLevel = GetInt(root, "QualityLevel"),
                ImageQuality = GetInt(root, "ImageQuality"),
                ViewDistance = GetInt(root, "ViewDistance"),
                AntiAliasing = GetInt(root, "AntiAliasing"),
                PostProcessing = GetInt(root, "PostProcessing"),
                ShadowQuality = GetInt(root, "ShadowQuality"),
                TextureQuality = GetInt(root, "TextureQuality"),
                MaterialQuality = GetInt(root, "MaterialQuality"),
                VegetationQuality = GetInt(root, "VegetationQuality"),
                MotionBlur = GetInt(root, "MotionBlur"),
                Rtx = GetInt(root, "Rtx"),
                Dlss = GetInt(root, "Dlss"),
                InsertFrame = GetInt(root, "InsertFrame"),
                Dx12 = GetInt(root, "Dx12"),
            };
        }
        catch (JsonException ex)
        {
            throw new BenchmarkParseException($"Не удалось разобрать JSON результата: {ex.Message}", ex);
        }
        catch (KeyNotFoundException ex)
        {
            throw new BenchmarkParseException($"В JSON результата нет ожидаемого поля: {ex.Message}", ex);
        }
    }

    public static SystemInfo ParseSystemInfo(string json, string ramConfig, string refreshHz)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new SystemInfo
            {
                CpuModel = Clean(GetString(root, "CPUModel")),
                GpuModel = GetString(root, "GPUModel"),
                GpuDriverVersion = GetString(root, "GpuDriverVer"),
                VideoMemory = GetString(root, "VideoMemSize"),
                SystemMemory = GetString(root, "SysMem"),
                OsVersion = Clean(GetString(root, "SysVer")),
                GameVersion = GetString(root, "GameVer"),
                ScreenResolution = GetString(root, "ScreenResolution"),
                RamConfig = ramConfig,
                MonitorRefreshRate = refreshHz,
            };
        }
        catch (JsonException ex)
        {
            throw new BenchmarkParseException($"Не удалось разобрать системную информацию: {ex.Message}", ex);
        }
    }

    private static double AverageFrameTime(JsonElement root, string field)
    {
        if (!root.TryGetProperty("Records", out var records) || records.ValueKind != JsonValueKind.Array)
            return 0;

        double total = 0;
        int count = 0;
        foreach (var r in records.EnumerateArray())
        {
            if (r.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.Number)
            {
                total += v.GetDouble();
                count++;
            }
        }

        return count == 0 ? 0 : total / count;
    }

    private static int GetFrameCount(JsonElement root) =>
        root.TryGetProperty("Records", out var records) && records.ValueKind == JsonValueKind.Array
            ? records.GetArrayLength()
            : 0;

    private static double GetDouble(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble()
            : 0;

    private static int GetInt(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var v))
        {
            return v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetInt32(out var i) => i,
                JsonValueKind.Number => (int)v.GetDouble(),
                _ => 0,
            };
        }

        return 0;
    }

    private static string GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) ? v.ToString() : "";

    private static string Clean(string s) => s.Trim();
}
