using WukongBenchAuto.Core.Parsing;

namespace WukongBenchAuto.Tests;

public class BenchmarkResultParserTests
{
    private const string SampleJson = """
        {
          "TimeStamp": 1791721215,
          "FPSAvg": 86,
          "FPSMax": 99,
          "FPSMin": 25,
          "FPS95": 79,
          "CPUAvg": 1,
          "GPUAvg": 94,
          "VideoMem": 6.5,
          "GameVer": "1.0.3.14649",
          "SysVer": "10.0.26200.1.256.64bit",
          "CPUModel": "AMD Ryzen 7 5700X 8-Core Processor             ",
          "GPUModel": "AMD Radeon RX 6750 GRE 12GB",
          "GpuDriverVer": "AMD Software: Adrenalin Edition 26.8.1",
          "VideoMemSize": "12GB",
          "SysMem": "32GB",
          "ScreenResolution": "2560 × 1440",
          "QualityLevel": 3,
          "ImageQuality": 75,
          "ViewDistance": 3,
          "Rtx": 0,
          "Dx12": 1,
          "Records": [
            { "TimeStamp": 1, "FrameRate": 84.0, "CPUUsage": 1.0, "GPUUsage": 80.0, "CPUFrameTime": 5.0, "GPUFrameTime": 20.0, "VideoMemoryUsage": 6.4 },
            { "TimeStamp": 2, "FrameRate": 86.0, "CPUUsage": 2.0, "GPUUsage": 90.0, "CPUFrameTime": 7.0, "GPUFrameTime": 22.0, "VideoMemoryUsage": 6.5 }
          ]
        }
        """;

    [Fact]
    public void ParseSummary_ExtractsFpsMetrics()
    {
        var s = BenchmarkResultParser.ParseSummary(SampleJson);
        Assert.Equal(86, s.FpsAvg);
        Assert.Equal(99, s.FpsMax);
        Assert.Equal(25, s.FpsMin);
        Assert.Equal(79, s.Fps95);
    }

    [Fact]
    public void ParseSummary_ComputesAverageFrameTimes()
    {
        var s = BenchmarkResultParser.ParseSummary(SampleJson);
        Assert.Equal(6.0, s.CpuFrameTimeMs, precision: 6);
        Assert.Equal(21.0, s.GpuFrameTimeMs, precision: 6);
        Assert.Equal(2, s.FrameCount);
    }

    [Fact]
    public void ParseSummary_ExtractsSettings()
    {
        var s = BenchmarkResultParser.ParseSummary(SampleJson);
        Assert.Equal(75, s.ImageQuality);
        Assert.Equal(0, s.Rtx);
        Assert.Equal(1, s.Dx12);
        Assert.Equal(6.5, s.VideoMemGb);
    }

    [Fact]
    public void ParseSystemInfo_ExtractsHardware()
    {
        var sys = BenchmarkResultParser.ParseSystemInfo(SampleJson, "32 GB, 2 модуля, 3200 MHz", "144 Hz");
        Assert.Equal("AMD Ryzen 7 5700X 8-Core Processor", sys.CpuModel);
        Assert.Equal("AMD Radeon RX 6750 GRE 12GB", sys.GpuModel);
        Assert.Equal("12GB", sys.VideoMemory);
        Assert.Equal("144 Hz", sys.MonitorRefreshRate);
        Assert.Equal("32 GB, 2 модуля, 3200 MHz", sys.RamConfig);
    }

    [Fact]
    public void ParseSummary_MissingFields_AreZero()
    {
        var s = BenchmarkResultParser.ParseSummary("""{"FPSAvg": 10}""");
        Assert.Equal(10, s.FpsAvg);
        Assert.Equal(0, s.FpsMin);
        Assert.Equal(0, s.CpuFrameTimeMs);
    }

    [Fact]
    public void ParseSummary_BrokenJson_ThrowsParseError()
    {
        Assert.Throws<BenchmarkParseException>(() => BenchmarkResultParser.ParseSummary("{oops"));
    }
}
