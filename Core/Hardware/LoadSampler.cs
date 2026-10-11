using System.Diagnostics;
using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Hardware;

/// <summary>
/// Сэмплирование загрузки CPU и GPU во время прохода.
/// GPU — через PerformanceCounter "GPU Engine" (utilization), CPU — суммарно по ядрам.
/// </summary>
public sealed class LoadSampler : IDisposable
{
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _gpuCounter;
    private readonly List<double> _cpu = [];
    private readonly List<double> _gpu = [];

    public LoadSampler()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
            _cpuCounter.NextValue(); // Первый вызов всегда 0 — сбрасываем.
        }
        catch (Exception)
        {
            _cpuCounter = null;
        }

        try
        {
            var cat = new PerformanceCounterCategory("GPU Engine");
            var instances = cat.GetInstanceNames()
                .Where(n => n.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (instances.Count > 0)
            {
                _gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage",
                    instances[0], readOnly: true);
                _gpuCounter.NextValue();
            }
        }
        catch (Exception)
        {
            _gpuCounter = null;
        }
    }

    /// <summary>Сделать замер. Вызывать раз в 1–2 секунды во время прохода.</summary>
    public void Sample()
    {
        if (_cpuCounter is not null)
            _cpu.Add(Math.Min(100, _cpuCounter.NextValue()));
        if (_gpuCounter is not null)
            _gpu.Add(Math.Min(100, _gpuCounter.NextValue()));
    }

    public LoadSample? GetResult()
    {
        if (_cpu.Count == 0 && _gpu.Count == 0)
            return null;

        return new LoadSample
        {
            AvgCpuPercent = _cpu.Count == 0 ? 0 : _cpu.Average(),
            AvgGpuPercent = _gpu.Count == 0 ? 0 : _gpu.Average(),
            Samples = Math.Max(_cpu.Count, _gpu.Count),
        };
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _gpuCounter?.Dispose();
    }
}
