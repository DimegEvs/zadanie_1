using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Reporting;

/// <summary>Корневой объект report.json.</summary>
public sealed record Report
{
    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; init; }

    [JsonPropertyName("system")]
    public SystemInfo System { get; init; } = new();

    [JsonPropertyName("cpuTest")]
    public BenchmarkRunResult? CpuTest { get; init; }

    [JsonPropertyName("gpuTest")]
    public BenchmarkRunResult? GpuTest { get; init; }
}

public static class ReportSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToJson(Report report) =>
        JsonSerializer.Serialize(report, Options);

    public static void Save(Report report, string path) =>
        File.WriteAllText(path, ToJson(report), new UTF8Encoding(false));
}
