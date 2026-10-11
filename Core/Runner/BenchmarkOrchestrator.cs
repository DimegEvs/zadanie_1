using System.Diagnostics;
using WukongBenchAuto.Core.Hardware;
using WukongBenchAuto.Core.Launch;
using WukongBenchAuto.Core.Models;
using WukongBenchAuto.Core.Parsing;
using WukongBenchAuto.Core.Settings;

namespace WukongBenchAuto.Core.Runner;

/// <summary>Координация одного прохода: настройки → запуск → ожидание → парсинг.</summary>
public sealed class BenchmarkOrchestrator(string gameDirectory)
{
    private static readonly string HistoryDirectory = Path.Combine(
        Path.GetTempPath(), "b1", "BenchMarkHistory", "Tool");

    private readonly IGameSettingsStore _store = new GameSettingsStore(gameDirectory);
    private readonly BenchmarkLauncher _launcher = new(gameDirectory);

    /// <summary>Резервная копия ini на время работы.</summary>
    private string? _backup;

    public event Action<string>? Status;

    /// <summary>Сделать резервную копию GameUserSettings.ini.</summary>
    public void BackupSettings()
    {
        var ini = _store.Read();
        _backup = ini;
        var path = Path.Combine(Path.GetTempPath(), "GameUserSettings.ini.bak");
        File.WriteAllText(path, ini);
        Status?.Invoke($"Резервная копия настроек: {path}");
    }

    /// <summary>Восстановить исходные настройки (в том числе при падении/Ctrl+C).</summary>
    public void RestoreSettings()
    {
        if (_backup is null)
            return;
        try
        {
            _store.Write(_backup);
            Status?.Invoke("Исходные настройки GameUserSettings.ini восстановлены.");
        }
        catch (Exception ex)
        {
            Status?.Invoke($"Не удалось восстановить настройки: {ex.Message}");
        }
        finally
        {
            _backup = null;
        }
    }

    /// <summary>Выполнить один проход бенчмарка с заданным профилем.</summary>
    public async Task<BenchmarkRunResult> RunAsync(
        BenchmarkProfile profile,
        CancellationToken ct,
        TimeSpan? timeout = null)
    {
        var startedAt = DateTime.Now;
        var sw = Stopwatch.StartNew();
        var limit = timeout ?? TimeSpan.FromMinutes(20);

        Status?.Invoke($"[{profile.Name}] Применяю настройки профиля...");
        var originalIni = _store.Read();
        var applied = IniProfileApplier.Apply(originalIni, profile);
        _store.Write(applied);

        var settings = new AppliedSettings
        {
            ProfileName = profile.Name,
            Values = IniProfileApplier.Snapshot(profile),
        };

        Directory.CreateDirectory(HistoryDirectory);
        var known = Directory.GetFiles(HistoryDirectory).ToHashSet();

        Status?.Invoke($"[{profile.Name}] Запускаю бенчмарк...");
        _launcher.Launch();

        Status?.Invoke($"[{profile.Name}] Жду окно игры...");
        using var process = await _launcher.WaitForGameWindowAsync(TimeSpan.FromMinutes(3), ct);

        Status?.Invoke($"[{profile.Name}] Автоматизирую меню...");
        await MenuAutomation.StartBenchmarkAsync(process.MainWindowHandle, ct);

        using var sampler = new LoadSampler();
        Status?.Invoke($"[{profile.Name}] Тест идёт...");

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (sw.Elapsed > limit)
            {
                BenchmarkLauncher.KillBenchmarkProcesses();
                throw new TimeoutException(
                    $"[{profile.Name}] Бенчмарк не завершился за {limit.TotalMinutes:0} минут.");
            }

            await Task.Delay(1500, ct);
            sampler.Sample();

            var file = FindNewResult(known);
            if (file is null)
                continue;

            // Файл появился — даём бенчмарку дописать и подождать закрытия.
            Status?.Invoke($"[{profile.Name}] Результат получен, жду завершения процесса...");
            try
            {
                await process.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromMinutes(2), ct);
            }
            catch (TimeoutException)
            {
                BenchmarkLauncher.KillBenchmarkProcesses();
            }

            var json = TryReadAll(file)
                ?? throw new BenchmarkParseException($"Не удалось прочитать файл результата: {file}");
            var summary = BenchmarkResultParser.ParseSummary(json);
            var system = BenchmarkResultParser.ParseSystemInfo(json,
                HardwareInfo.GetRamConfig(),
                HardwareInfo.GetPrimaryMonitor().RefreshHz + " Hz");

            return new BenchmarkRunResult
            {
                TestName = profile.Name,
                StartedAt = startedAt,
                Duration = sw.Elapsed,
                Settings = settings,
                Summary = summary,
                System = system,
                Load = sampler.GetResult(),
                ResultFilePath = file,
            };
        }
    }

    private static string? FindNewResult(IReadOnlySet<string> known)
    {
        foreach (var f in Directory.GetFiles(HistoryDirectory))
            if (!known.Contains(f))
                return f;
        return null;
    }

    private static string? TryReadAll(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
