namespace WukongBenchAuto.Core.Models;

/// <summary>Полный набор настроек графики для одного прохода бенчмарка.</summary>
public sealed record BenchmarkProfile
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";

    // Разрешение и масштаб рендера
    public int ResolutionWidth { get; init; }
    public int ResolutionHeight { get; init; }
    /// <summary>Масштаб рендера ImageQuality: 25..100 (процент от разрешения).</summary>
    public int ImageQuality { get; init; }

    // Базовый уровень качества (пресет UI)
    public int QualityLevel { get; init; }

    // Качество сцен (1..5, где 1 — минимум, 5 — максимум)
    public int ViewDistance { get; init; }
    public int AntiAliasing { get; init; }
    public int PostProcessing { get; init; }
    public int ShadowQuality { get; init; }
    public int TextureQuality { get; init; }
    public int MaterialQuality { get; init; }
    public int VegetationQuality { get; init; }
    public int FxQuality { get; init; }
    public int GlobalIllumination { get; init; }
    public int ReflectionQuality { get; init; }

    // Переключатели
    public int Rtx { get; init; }
    public int RtxLevel { get; init; } = 1;
    public int Dlss { get; init; }
    public int InsertFrame { get; init; }
    public int MotionBlur { get; init; }
    public int Dx12 { get; init; } = 1;
    public int VSync { get; init; }
    public int FrameRateLimit { get; init; }
}

/// <summary>Настройки, фактически записанные в ini для одного прохода.</summary>
public sealed record AppliedSettings
{
    public required string ProfileName { get; init; }
    public required IReadOnlyDictionary<string, string> Values { get; init; }
}

/// <summary>Агрегированный результат одного прохода бенчмарка.</summary>
public sealed record BenchmarkRunResult
{
    public required string TestName { get; init; }
    public required DateTime StartedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public required AppliedSettings Settings { get; init; }
    public required BenchmarkSummary Summary { get; init; }
    public required SystemInfo System { get; init; }
    /// <summary>Средняя загрузка CPU/GPU во время прохода (если сэмплер был активен).</summary>
    public LoadSample? Load { get; init; }
    public required string ResultFilePath { get; init; }
}

/// <summary>Агрегированные метрики из JSON результата.</summary>
public sealed record BenchmarkSummary
{
    public double FpsAvg { get; init; }
    public double FpsMax { get; init; }
    public double FpsMin { get; init; }
    public double Fps95 { get; init; }
    public double CpuUsage { get; init; }
    public double GpuUsage { get; init; }
    public double VideoMemGb { get; init; }
    public double CpuFrameTimeMs { get; init; }
    public double GpuFrameTimeMs { get; init; }
    public int FrameCount { get; init; }
    public int QualityLevel { get; init; }
    public int ImageQuality { get; init; }
    public int ViewDistance { get; init; }
    public int AntiAliasing { get; init; }
    public int PostProcessing { get; init; }
    public int ShadowQuality { get; init; }
    public int TextureQuality { get; init; }
    public int MaterialQuality { get; init; }
    public int VegetationQuality { get; init; }
    public int MotionBlur { get; init; }
    public int Rtx { get; init; }
    public int Dlss { get; init; }
    public int InsertFrame { get; init; }
    public int Dx12 { get; init; }
}

/// <summary>Системная информация из JSON результата бенчмарка.</summary>
public sealed record SystemInfo
{
    public string CpuModel { get; init; } = "";
    public string GpuModel { get; init; } = "";
    public string GpuDriverVersion { get; init; } = "";
    public string VideoMemory { get; init; } = "";
    public string SystemMemory { get; init; } = "";
    public string OsVersion { get; init; } = "";
    public string GameVersion { get; init; } = "";
    public string ScreenResolution { get; init; } = "";
    public string MonitorRefreshRate { get; init; } = "";
    public string RamConfig { get; init; } = "";
}

/// <summary>Замер средней загрузки CPU/GPU за проход.</summary>
public sealed record LoadSample
{
    public double AvgCpuPercent { get; init; }
    public double AvgGpuPercent { get; init; }
    public int Samples { get; init; }
}
