using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Settings;

/// <summary>
/// Профили настроек для CPU- и GPU-проходов.
///
/// Обоснование:
/// - CPU-проход: минимальное разрешение (1280×720), масштаб рендера 25% и ВСЕ
///   качества графики на минимуме отсекают GPU-нагрузку, чтобы FPS почти полностью
///   зависел от процессора. CPU при этом всё равно остаётся главным ограничителем:
///   частота кадров определяется скоростью CPU (движок, сцена, симуляция), а не GPU.
/// - GPU-проход: нативное разрешение монитора, масштаб рендера 100, максимальные
///   качества и трассировка лучей. Апскейлинг и генерация кадров отключены,
///   чтобы нагрузка не занижалась.
/// В обоих проходах VSync и лимит FPS выключены (bUseVSync=False, FrameRateLimit=0).
/// </summary>
public static class Profiles
{
    public static BenchmarkProfile Cpu(int monitorWidth, int monitorHeight) => new()
    {
        Name = "CPU",
        Description = "Минимальная нагрузка на GPU, максимальная на CPU",
        ResolutionWidth = 1280,
        ResolutionHeight = 720,
        ImageQuality = 25,
        QualityLevel = 1,
        ViewDistance = 1,
        AntiAliasing = 1,
        PostProcessing = 1,
        ShadowQuality = 1,
        TextureQuality = 1,
        MaterialQuality = 1,
        VegetationQuality = 1,
        FxQuality = 1,
        GlobalIllumination = 1,
        ReflectionQuality = 1,
        Rtx = 0,
        RtxLevel = 1,
        Dlss = 0,
        InsertFrame = 0,
        MotionBlur = 0,
        Dx12 = 1,
        VSync = 0,
        FrameRateLimit = 0,
    };

    public static BenchmarkProfile Gpu(int monitorWidth, int monitorHeight) => new()
    {
        Name = "GPU",
        Description = "Максимальная нагрузка на GPU",
        ResolutionWidth = monitorWidth,
        ResolutionHeight = monitorHeight,
        ImageQuality = 100,
        QualityLevel = 5,
        ViewDistance = 5,
        AntiAliasing = 5,
        PostProcessing = 5,
        ShadowQuality = 5,
        TextureQuality = 5,
        MaterialQuality = 5,
        VegetationQuality = 5,
        FxQuality = 5,
        GlobalIllumination = 5,
        ReflectionQuality = 5,
        Rtx = 1,
        RtxLevel = 3,
        Dlss = 0,
        InsertFrame = 0,
        MotionBlur = 0,
        Dx12 = 1,
        VSync = 0,
        FrameRateLimit = 0,
    };
}
