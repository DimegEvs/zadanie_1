using WukongBenchAuto.Core.Config;
using WukongBenchAuto.Core.Hardware;
using WukongBenchAuto.Core.Launch;
using WukongBenchAuto.Core.Models;
using WukongBenchAuto.Core.Reporting;
using WukongBenchAuto.Core.Runner;
using WukongBenchAuto.Core.Settings;

namespace WukongBenchAuto;

/// <summary>Консольный режим (--cli): полностью автоматический прогон без UI.</summary>
internal static class CliProgram
{
    public static async Task<int> Run(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string? explicitPath = null;
        bool checkOnly = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--path" or "-p" && i + 1 < args.Length)
            {
                explicitPath = args[i + 1];
                i++;
            }
            else if (args[i] is "--check")
            {
                checkOnly = true;
            }
        }

        if (checkOnly)
        {
            var dir = ResolveGameDirectory(explicitPath);
            Console.WriteLine(dir is null
                ? "Бенчмарк НЕ найден (аргумент/конфиг/Steam-библиотеки)."
                : $"Бенчмарк найден: {dir}");
            return dir is null ? 1 : 0;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            return await RunAsync(explicitPath, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nОтменено пользователем.");
            return 130;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nОшибка: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunAsync(string? explicitPath, CancellationToken ct)
    {
        var gameDir = ResolveGameDirectory(explicitPath);
        if (gameDir is null)
        {
            Console.WriteLine("Не удалось найти Black Myth Wukong Benchmark Tool автоматически.");
            gameDir = AskForPath();
            if (gameDir is null)
            {
                Console.WriteLine("Путь не указан — выход.");
                return 1;
            }
        }

        Console.WriteLine($"Папка бенчмарка: {gameDir}");
        SaveConfig(gameDir);

        var orchestrator = new BenchmarkOrchestrator(gameDir);
        orchestrator.Status += msg => Console.WriteLine(msg);
        orchestrator.BackupSettings();

        var (w, h, _) = HardwareInfo.GetPrimaryMonitor();

        try
        {
            var cpu = await orchestrator.RunAsync(Profiles.Cpu(w, h), ct);
            Console.WriteLine();
            Console.WriteLine($"[CPU] Готово: avg {cpu.Summary.FpsAvg:0.#} FPS, CPU frame {cpu.Summary.CpuFrameTimeMs:0.0} ms, GPU frame {cpu.Summary.GpuFrameTimeMs:0.0} ms");

            // Пауза между прогонами: Steam должен отпустить предыдущий запуск.
            await Task.Delay(3000, ct);

            var gpu = await orchestrator.RunAsync(Profiles.Gpu(w, h), ct);
            Console.WriteLine();
            Console.WriteLine($"[GPU] Готово: avg {gpu.Summary.FpsAvg:0.#} FPS, CPU frame {gpu.Summary.CpuFrameTimeMs:0.0} ms, GPU frame {gpu.Summary.GpuFrameTimeMs:0.0} ms");

            var report = new Report
            {
                GeneratedAt = DateTime.Now,
                System = cpu.System,
                CpuTest = cpu,
                GpuTest = gpu,
            };

            ConsoleReportWriter.Write(report);

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "report.json");
            var htmlPath = Path.Combine(AppContext.BaseDirectory, "report.html");
            ReportSerializer.Save(report, jsonPath);
            HtmlReportWriter.Save(report, htmlPath);

            Console.WriteLine();
            Console.WriteLine($"Отчёт: {jsonPath}");
            Console.WriteLine($"Отчёт: {htmlPath}");
            return 0;
        }
        finally
        {
            BenchmarkLauncher.KillBenchmarkProcesses();
            orchestrator.RestoreSettings();
        }
    }

    private static string? ResolveGameDirectory(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            if (GameInstallValidator.IsValid(explicitPath))
                return Path.GetFullPath(explicitPath);
            Console.WriteLine($"Указанный путь некорректен (нет файлов бенчмарка): {explicitPath}");
            return null;
        }

        var config = AppConfig.Load(AppConfig.DefaultPath);
        if (!string.IsNullOrWhiteSpace(config.GameDirectory) && GameInstallValidator.IsValid(config.GameDirectory))
            return config.GameDirectory;

        Console.WriteLine("Ищу Benchmark Tool в библиотеках Steam...");
        var found = SteamLocator.FindBenchmarkTool();
        if (found is not null)
            Console.WriteLine("Найдено автоматически.");
        return found;
    }

    private static string? AskForPath()
    {
        Console.Write("Введите путь к папке 'Black Myth Wukong Benchmark Tool': ");
        var path = Console.ReadLine()?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (!GameInstallValidator.IsValid(path))
        {
            Console.WriteLine("В этой папке нет файлов бенчмарка (b1_benchmark.exe, b1\\Binaries\\Win64\\b1-Win64-Shipping.exe).");
            return null;
        }

        return Path.GetFullPath(path);
    }

    private static void SaveConfig(string gameDir)
    {
        var config = AppConfig.Load(AppConfig.DefaultPath);
        if (!string.Equals(config.GameDirectory, gameDir, StringComparison.OrdinalIgnoreCase))
        {
            config.GameDirectory = gameDir;
            config.Save(AppConfig.DefaultPath);
        }
    }
}
