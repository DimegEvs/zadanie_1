using System.Net;
using System.Text;
using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto.Core.Reporting;

/// <summary>Генерация самодостаточного HTML-отчёта (тёмная/светлая тема, без внешних зависимостей).</summary>
public static class HtmlReportWriter
{
    public static void Save(Report report, string path)
    {
        File.WriteAllText(path, Render(report), new UTF8Encoding(false));
    }

    public static string Render(Report report) => $@"<!DOCTYPE html>
<html lang=""ru"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>Black Myth: Wukong Benchmark — отчёт</title>
<style>
:root {{
  color-scheme: light dark;
  --bg: #f6f4ef;
  --bg-card: #ffffff;
  --bg-soft: #efece4;
  --fg: #1f1c17;
  --fg-muted: #6b655c;
  --accent: #b5541c;
  --accent-soft: #f3e2d3;
  --border: #d8d2c6;
  --good: #2e7d4f;
  --radius: 14px;
}}
@media (prefers-color-scheme: dark) {{
  :root {{
    --bg: #17151a;
    --bg-card: #211e26;
    --bg-soft: #2a2630;
    --fg: #ece7de;
    --fg-muted: #a49c90;
    --accent: #e8955c;
    --accent-soft: #3d2c1f;
    --border: #3a3542;
    --good: #7bc99a;
  }}
}}
* {{ box-sizing: border-box; margin: 0; }}
body {{
  font-family: 'Segoe UI', 'SF Pro Text', Roboto, 'Helvetica Neue', Arial, sans-serif;
  background: var(--bg);
  color: var(--fg);
  line-height: 1.55;
  padding: 2.5rem 1rem;
  -webkit-font-smoothing: antialiased;
}}
.page {{ max-width: 62rem; margin: 0 auto; }}
header {{ margin-bottom: 2rem; }}
h1 {{
  font-size: clamp(1.6rem, 4vw, 2.2rem);
  font-weight: 650;
  letter-spacing: -0.02em;
}}
.subtitle {{ color: var(--fg-muted); margin-top: 0.3rem; font-size: 0.95rem; }}
.grid {{
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(19rem, 1fr));
  gap: 1rem;
}}
.card {{
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 1.4rem 1.5rem;
}}
.card h2 {{
  font-size: 0.8rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.09em;
  color: var(--accent);
  margin-bottom: 1rem;
}}
.kv {{ display: grid; grid-template-columns: minmax(7.5rem, auto) 1fr; row-gap: 0.35rem; column-gap: 1rem; font-size: 0.92rem; }}
.kv dt {{ color: var(--fg-muted); }}
.kv dd {{ margin: 0; font-variant-numeric: tabular-nums; overflow-wrap: anywhere; }}
.big {{ font-size: clamp(2.2rem, 6vw, 3.2rem); font-weight: 700; letter-spacing: -0.03em; line-height: 1.1; font-variant-numeric: tabular-nums; }}
.big small {{ font-size: 0.45em; font-weight: 500; color: var(--fg-muted); letter-spacing: 0; }}
.metrics {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(6.5rem, 1fr)); gap: 0.8rem 1rem; margin-top: 1.1rem; }}
.metric .value {{ font-size: 1.25rem; font-weight: 650; font-variant-numeric: tabular-nums; }}
.metric .label {{ font-size: 0.75rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; }}
.verdict {{ margin-top: 1rem; font-size: 0.88rem; color: var(--fg-muted); }}
.verdict b {{ color: var(--good); }}
table {{
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
  font-variant-numeric: tabular-nums;
}}
th, td {{
  text-align: left;
  padding: 0.45rem 0.7rem;
  border-bottom: 1px solid var(--border);
}}
th {{ font-weight: 600; color: var(--fg-muted); font-size: 0.78rem; text-transform: uppercase; letter-spacing: 0.05em; }}
td.num {{ text-align: right; }}
footer {{ margin-top: 2rem; color: var(--fg-muted); font-size: 0.8rem; }}
</style>
</head>
<body>
<div class=""page"">
<header>
  <h1>Black Myth: Wukong Benchmark</h1>
  <p class=""subtitle"">Автоматический отчёт · сформирован {report.GeneratedAt:dd.MM.yyyy HH:mm}</p>
</header>
{RenderSystem(report.System)}
{RenderTest(report.CpuTest, "CPU-тест", "минимальная нагрузка на GPU — упор в процессор")}
{RenderTest(report.GpuTest, "GPU-тест", "максимальная нагрузка на видеокарту")}
{RenderSettings(report)}
<footer>Отчёт сгенерирован WukongBenchAuto · данные из %TEMP%\b1\BenchMarkHistory\Tool</footer>
</div>
</body>
</html>";

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "—") ?? "—";

    private static string RenderSystem(SystemInfo s) => $@"
<section class=""card"" style=""margin-bottom:1rem"">
  <h2>Система</h2>
  <dl class=""kv"">
    <dt>CPU</dt><dd>{Esc(s.CpuModel)}</dd>
    <dt>GPU</dt><dd>{Esc(s.GpuModel)}{Vram(s)}</dd>
    <dt>Драйвер GPU</dt><dd>{Esc(s.GpuDriverVersion)}</dd>
    <dt>RAM</dt><dd>{Esc(s.SystemMemory)}{RamExtra(s)}</dd>
    <dt>ОС</dt><dd>{Esc(s.OsVersion)}</dd>
    <dt>Экран</dt><dd>{Esc(s.ScreenResolution)} @ {Esc(s.MonitorRefreshRate)}</dd>
    <dt>Версия бенчмарка</dt><dd>{Esc(s.GameVersion)}</dd>
  </dl>
</section>";

    private static string Vram(SystemInfo s) =>
        string.IsNullOrWhiteSpace(s.VideoMemory) ? "" : $" · {Esc(s.VideoMemory)}";

    private static string RamExtra(SystemInfo s) =>
        string.IsNullOrWhiteSpace(s.RamConfig) ? "" : $" · {Esc(s.RamConfig)}";

    private static string RenderTest(BenchmarkRunResult? test, string title, string note)
    {
        if (test is null)
            return "";

        var s = test.Summary;
        var bottleneck = s.CpuFrameTimeMs > 0 && s.GpuFrameTimeMs > 0
            ? (s.CpuFrameTimeMs > s.GpuFrameTimeMs
                ? $@"<p class=""verdict"">Ограничитель: <b>CPU</b> (CPU frame time {s.CpuFrameTimeMs:0.0} ms &gt; GPU {s.GpuFrameTimeMs:0.0} ms)</p>"
                : $@"<p class=""verdict"">Ограничитель: <b>GPU</b> (GPU frame time {s.GpuFrameTimeMs:0.0} ms &gt;= CPU {s.CpuFrameTimeMs:0.0} ms)</p>")
            : "";

        var load = test.Load is null ? "" : $@"
    <div class=""metric""><div class=""value"">{test.Load.AvgCpuPercent:0}%</div><div class=""label"">CPU (сред.)</div></div>
    <div class=""metric""><div class=""value"">{test.Load.AvgGpuPercent:0}%</div><div class=""label"">GPU (сред.)</div></div>";

        return $@"
<section class=""card"" style=""margin-bottom:1rem"">
  <h2>{title} · <span style=""text-transform:none;letter-spacing:0;color:var(--fg-muted)"">{Esc(note)}</span></h2>
  <div class=""big"">{s.FpsAvg:0.#} <small>FPS avg</small></div>
  <div class=""metrics"">
    <div class=""metric""><div class=""value"">{s.FpsMin:0.#}</div><div class=""label"">FPS min</div></div>
    <div class=""metric""><div class=""value"">{s.FpsMax:0.#}</div><div class=""label"">FPS max</div></div>
    <div class=""metric""><div class=""value"">{s.Fps95:0.#}</div><div class=""label"">FPS 95%</div></div>
    <div class=""metric""><div class=""value"">{s.CpuFrameTimeMs:0.0} ms</div><div class=""label"">CPU frame</div></div>
    <div class=""metric""><div class=""value"">{s.GpuFrameTimeMs:0.0} ms</div><div class=""label"">GPU frame</div></div>
    <div class=""metric""><div class=""value"">{s.VideoMemGb:0.#} GB</div><div class=""label"">VRAM</div></div>{load}
  </div>
  {bottleneck}
</section>";
    }

    private static string RenderSettings(Report report)
    {
        if (report.CpuTest is null || report.GpuTest is null)
            return "";

        var keys = report.CpuTest.Settings.Values.Keys
            .Union(report.GpuTest.Settings.Values.Keys)
            .OrderBy(k => k);

        var rows = new StringBuilder();
        foreach (var k in keys)
        {
            var c = report.CpuTest.Settings.Values.GetValueOrDefault(k, "—");
            var g = report.GpuTest.Settings.Values.GetValueOrDefault(k, "—");
            rows.Append($@"<tr><td>{Esc(k)}</td><td class=""num"">{Esc(c)}</td><td class=""num"">{Esc(g)}</td></tr>");
        }

        return $@"
<section class=""card"">
  <h2>Настройки</h2>
  <table>
    <thead><tr><th>Параметр</th><th style=""text-align:right"">CPU-тест</th><th style=""text-align:right"">GPU-тест</th></tr></thead>
    <tbody>{rows}</tbody>
  </table>
</section>";
    }
}
