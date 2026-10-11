using System.Management;
using System.Runtime.InteropServices;
using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Hardware;

/// <summary>Доп. информация о железе, которую Benchmark Tool не отдаёт.</summary>
public static class HardwareInfo
{
    public static (int Width, int Height, int RefreshHz) GetPrimaryMonitor()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate " +
                "FROM Win32_VideoController");
            foreach (var o in searcher.Get())
            {
                var w = Convert.ToInt32(o["CurrentHorizontalResolution"]);
                var h = Convert.ToInt32(o["CurrentVerticalResolution"]);
                var r = Convert.ToInt32(o["CurrentRefreshRate"]);
                if (w > 0 && h > 0)
                    return (w, h, r);
            }
        }
        catch (Exception ex) when (ex is ManagementException or COMException)
        {
            // Фолбэк ниже.
        }

        return (SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height, 60);
    }

    public static string GetRamConfig()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Capacity, Speed FROM Win32_PhysicalMemory");
            var sticks = new List<(long CapacityMb, int Speed)>();
            foreach (var o in searcher.Get())
            {
                var capacity = Convert.ToInt64(o["Capacity"]) / (1024 * 1024);
                var speed = o["Speed"] is null ? 0 : Convert.ToInt32(o["Speed"]);
                sticks.Add((capacity, speed));
            }

            if (sticks.Count == 0)
                return "";

            var totalGb = sticks.Sum(s => s.CapacityMb) / 1024.0;
            var ramSpeed = sticks.FirstOrDefault(s => s.Speed > 0).Speed;
            var layout = sticks.Count >= 2 ? $"{sticks.Count} модуля" : "1 модуль";
            return ramSpeed > 0
                ? $"{totalGb:0.#} GB, {layout}, {ramSpeed} MHz"
                : $"{totalGb:0.#} GB, {layout}";
        }
        catch (Exception ex) when (ex is ManagementException or COMException)
        {
            return "";
        }
    }
}
