using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Reporting;

/// <summary>Читаемый вывод результатов в консоль.</summary>
public static class ConsoleReportWriter
{
    public static void Write(Report report)
    {
        WriteSystem(report.System);
        if (report.CpuTest is { } cpu)
            WriteTest(cpu);
        if (report.GpuTest is { } gpu)
            WriteTest(gpu);
        WriteSettingsTable(report);
    }

    private static void WriteSystem(SystemInfo s)
    {
        Console.WriteLine();
        Console.WriteLine("=== Система ===");
        Console.WriteLine($"CPU:                {s.CpuModel}");
        Console.WriteLine($"GPU:                {s.GpuModel} ({s.VideoMemory})");
        Console.WriteLine($"Драйвер GPU:        {s.GpuDriverVersion}");
        Console.WriteLine($"RAM:                {s.SystemMemory} ({s.RamConfig})");
        Console.WriteLine($"ОС:                 {s.OsVersion}");
        Console.WriteLine($"Разрешение:         {s.ScreenResolution} @ {s.MonitorRefreshRate}");
        Console.WriteLine($"Версия бенчмарка:   {s.GameVersion}");
    }

    private static void WriteTest(BenchmarkRunResult r)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {r.TestName}-тест ({r.Duration:mm\\:ss}) ===");
        var s = r.Summary;
        Console.WriteLine($"FPS (avg):          {s.FpsAvg:0.#}");
        Console.WriteLine($"FPS (min):          {s.FpsMin:0.#}");
        Console.WriteLine($"FPS (max):          {s.FpsMax:0.#}");
        Console.WriteLine($"FPS (95-й перц.):   {s.Fps95:0.#}");
        Console.WriteLine($"CPU frame time:     {s.CpuFrameTimeMs:0.0} ms");
        Console.WriteLine($"GPU frame time:     {s.GpuFrameTimeMs:0.0} ms");
        Console.WriteLine($"Видеопамять:        {s.VideoMemGb:0.#} GB");
        Console.WriteLine($"Кадров записано:    {s.FrameCount}");
        if (r.Load is { } load)
        {
            Console.WriteLine($"Загрузка CPU:       {load.AvgCpuPercent:0}% (замеров: {load.Samples})");
            Console.WriteLine($"Загрузка GPU:       {load.AvgGpuPercent:0}%");
        }
    }

    private static void WriteSettingsTable(Report report)
    {
        if (report.CpuTest is null || report.GpuTest is null)
            return;

        Console.WriteLine();
        Console.WriteLine("=== Настройки (CPU vs GPU) ===");
        var keys = report.CpuTest.Settings.Values.Keys
            .Union(report.GpuTest.Settings.Values.Keys)
            .OrderBy(k => k)
            .ToList();

        var label = keys.Select(k => (k, cpu: report.CpuTest.Settings.Values.GetValueOrDefault(k, "—"),
            gpu: report.GpuTest.Settings.Values.GetValueOrDefault(k, "—"))).ToList();

        var widthKey = Math.Max("Параметр".Length, label.Max(x => x.k.Length));
        var widthCpu = Math.Max("CPU-тест".Length, label.Max(x => x.cpu.Length));
        var widthGpu = Math.Max("GPU-тест".Length, label.Max(x => x.gpu.Length));

        Console.WriteLine($"| {"Параметр".PadRight(widthKey)} | {"CPU-тест".PadRight(widthCpu)} | {"GPU-тест".PadRight(widthGpu)} |");
        Console.WriteLine($"| {new string('-', widthKey)} | {new string('-', widthCpu)} | {new string('-', widthGpu)} |");
        foreach (var (k, c, g) in label)
            Console.WriteLine($"| {k.PadRight(widthKey)} | {c.PadRight(widthCpu)} | {g.PadRight(widthGpu)} |");
    }
}
