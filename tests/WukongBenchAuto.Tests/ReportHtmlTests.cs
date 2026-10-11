using WukongBenchAuto.Core.Reporting;

namespace WukongBenchAuto.Tests;
public class ReportHtmlTests
{
    [Fact]
    public void Render_ContainsSections()
    {
        var html = HtmlReportWriter.Render(SampleReport());
        Assert.Contains("Система", html);
        Assert.Contains("CPU-тест", html);
        Assert.Contains("GPU-тест", html);
        Assert.Contains("Настройки", html);
    }

    [Fact]
    public void Render_EscapesHtml()
    {
        var report = SampleReport();
        var html = HtmlReportWriter.Render(report);
        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("&lt;script&gt;", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_HasThemeSupport()
    {
        var html = HtmlReportWriter.Render(SampleReport());
        Assert.Contains("prefers-color-scheme: dark", html);
    }

    [Fact]
    public void Render_NoExternalDependencies()
    {
        var html = HtmlReportWriter.Render(SampleReport());
        Assert.DoesNotContain("http://", html);
        Assert.DoesNotContain("https://", html);
    }

    private static Report SampleReport() => new()
    {
        GeneratedAt = new DateTime(2026, 10, 11, 12, 0, 0),
        System = new Core.Models.SystemInfo
        {
            CpuModel = "Test <script>alert(1)</script> CPU",
            GpuModel = "Test GPU",
            VideoMemory = "12GB",
        },
        CpuTest = new Core.Models.BenchmarkRunResult
        {
            TestName = "CPU",
            StartedAt = DateTime.Now,
            Duration = TimeSpan.FromMinutes(2),
            Settings = new Core.Models.AppliedSettings
            {
                ProfileName = "CPU",
                Values = new Dictionary<string, string> { ["ImageQuality"] = "25" },
            },
            Summary = new Core.Models.BenchmarkSummary
            {
                FpsAvg = 200, CpuFrameTimeMs = 5, GpuFrameTimeMs = 2, FrameCount = 100,
            },
            System = new Core.Models.SystemInfo(),
            ResultFilePath = "x",
        },
        GpuTest = new Core.Models.BenchmarkRunResult
        {
            TestName = "GPU",
            StartedAt = DateTime.Now,
            Duration = TimeSpan.FromMinutes(3),
            Settings = new Core.Models.AppliedSettings
            {
                ProfileName = "GPU",
                Values = new Dictionary<string, string> { ["ImageQuality"] = "100" },
            },
            Summary = new Core.Models.BenchmarkSummary
            {
                FpsAvg = 60, CpuFrameTimeMs = 4, GpuFrameTimeMs = 16, FrameCount = 100,
            },
            System = new Core.Models.SystemInfo(),
            ResultFilePath = "y",
        },
    };
}
