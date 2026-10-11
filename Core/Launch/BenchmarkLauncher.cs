using System.Diagnostics;
using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Launch;

/// <summary>Запуск бенчмарка и определение его завершения.</summary>
public sealed class BenchmarkLauncher
{
    public const string AppId = "3132990";
    public const string GameProcessName = "b1-Win64-Shipping";
    public const string LauncherProcessName = "b1_benchmark";

    private readonly string _steamExe;

    public BenchmarkLauncher(string gameDirectory)
    {
        _steamExe = FindSteamExe(gameDirectory)
            ?? throw new InvalidOperationException(
                "steam.exe не найден. Проверьте установку Steam.");
    }

    /// <summary>Запустить Benchmark Tool через Steam (steam://rungameid).</summary>
    public void Launch()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _steamExe,
            Arguments = $"-applaunch {AppId}",
            UseShellExecute = true,
        });
    }

    /// <summary>Дождаться появления процесса игры с окном.</summary>
    public async Task<Process> WaitForGameWindowAsync(
        TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            ct.ThrowIfCancellationRequested();
            var p = FindMainWindowProcess();
            if (p is not null)
                return p;
            await Task.Delay(500, ct);
        }

        throw new TimeoutException(
            $"Окно бенчмарка не появилось за {timeout.TotalSeconds:0} секунд.");
    }

    public static Process? FindMainWindowProcess()
    {
        foreach (var p in Process.GetProcessesByName(GameProcessName))
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                    return p;
                p.Dispose();
            }
            catch (Exception)
            {
                p.Dispose();
            }
        }

        return null;
    }

    /// <summary>Завершить процессы бенчмарка (после появления файла результата).</summary>
    public static void KillBenchmarkProcesses()
    {
        foreach (var name in new[] { GameProcessName, LauncherProcessName })
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill(entireProcessTree: true);
                        p.WaitForExit(5000);
                    }
                }
                catch (Exception)
                {
                    // Процесс мог завершиться сам — не критично.
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
    }

    private static string? FindSteamExe(string gameDirectory)
    {
        // 1. Реестр.
        foreach (var key in new[]
                 {
                     @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
                     @"HKEY_CURRENT_USER\Software\Valve\Steam",
                 })
        {
            if (Microsoft.Win32.Registry.GetValue(key, "SteamExe", null) is string exe && File.Exists(exe))
                return exe;
            var steamPath = Microsoft.Win32.Registry.GetValue(key, "SteamPath", null) as string;
            if (!string.IsNullOrEmpty(steamPath))
            {
                var candidate = Path.Combine(steamPath.Replace('/', Path.DirectorySeparatorChar), "steam.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        // 2. Рядом с библиотекой игры: <gameDir>/../../steam.exe — редкий случай.
        var guess = Path.GetFullPath(Path.Combine(gameDirectory, "..", "..", "..", "steam.exe"));
        return File.Exists(guess) ? guess : null;
    }
}
