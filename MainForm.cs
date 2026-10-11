using System.Diagnostics;
using WukongBenchAuto.Core.Config;
using WukongBenchAuto.Core.Hardware;
using WukongBenchAuto.Core.Launch;
using WukongBenchAuto.Core.Models;
using WukongBenchAuto.Core.Reporting;
using WukongBenchAuto.Core.Runner;
using WukongBenchAuto.Core.Settings;

namespace WukongBenchAuto;

/// <summary>WinForms-оболочка: лог прогресса, кнопка запуска, статус.</summary>
public sealed class MainForm : Form
{
    private readonly TextBox _log;
    private readonly Button _runButton;
    private readonly Button _openReportButton;
    private readonly Label _status;
    private readonly TextBox _pathBox;
    private readonly Button _pickButton;
    private CancellationTokenSource? _cts;
    private bool _running;

    public MainForm()
    {
        Text = "WukongBenchAuto — автоматический запуск Black Myth: Wukong Benchmark";
        Font = new Font("Segoe UI", 9.5f);
        Size = new Size(860, 620);
        MinimumSize = new Size(720, 500);
        StartPosition = FormStartPosition.CenterScreen;

        _log = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = SystemColors.Window,
            Font = new Font("Consolas", 9.5f),
        };

        _runButton = new Button
        {
            Text = "Запустить бенчмарки (CPU + GPU)",
            Height = 40,
            Dock = DockStyle.Fill,
        };
        _runButton.Click += async (_, _) => await RunAllAsync();

        _openReportButton = new Button
        {
            Text = "Открыть отчёт (HTML)",
            Height = 40,
            Dock = DockStyle.Fill,
            Enabled = false,
        };
        _openReportButton.Click += (_, _) => OpenReport();

        _status = new Label
        {
            Text = "Готов к работе.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 28,
        };

        _pathBox = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
        };

        _pickButton = new Button
        {
            Text = "Указать папку...",
            Dock = DockStyle.Fill,
        };
        _pickButton.Click += (_, _) => PickFolder();

        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 2,
            RowCount = 1,
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        pathPanel.Controls.Add(_pathBox, 0, 0);
        pathPanel.Controls.Add(_pickButton, 1, 0);

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            ColumnCount = 1,
            RowCount = 1,
        };
        statusPanel.Controls.Add(_status, 0, 0);

        var buttonsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            ColumnCount = 2,
            RowCount = 1,
        };
        buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        buttonsPanel.Controls.Add(_runButton, 0, 0);
        buttonsPanel.Controls.Add(_openReportButton, 1, 0);

        Controls.Add(_log);
        Controls.Add(pathPanel);
        Controls.Add(statusPanel);
        Controls.Add(buttonsPanel);

        AcceptButton = _runButton;

        Load += (_, _) => ResolvePath();
    }

    private void ResolvePath()
    {
        var dir = TryResolveGameDirectory();
        if (dir is not null)
        {
            _pathBox.Text = dir;
            _status.Text = "Папка бенчмарка определена.";
        }
        else
        {
            _status.Text = "Бенчмарк не найден — укажите папку вручную.";
        }
    }

    private string? TryResolveGameDirectory()
    {
        var config = AppConfig.Load(AppConfig.DefaultPath);
        if (!string.IsNullOrWhiteSpace(config.GameDirectory) && GameInstallValidator.IsValid(config.GameDirectory))
            return config.GameDirectory;

        return SteamLocator.FindBenchmarkTool();
    }

    private void PickFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку 'Black Myth Wukong Benchmark Tool'",
            ShowNewFolderButton = false,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (!GameInstallValidator.IsValid(dialog.SelectedPath))
        {
            MessageBox.Show(this,
                "В выбранной папке нет файлов бенчмарка (b1_benchmark.exe, b1\\Binaries\\Win64\\b1-Win64-Shipping.exe).",
                "Неверная папка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _pathBox.Text = dialog.SelectedPath;
        SaveConfig(dialog.SelectedPath);
        _status.Text = "Папка сохранена.";
    }

    private void SaveConfig(string gameDir)
    {
        var config = AppConfig.Load(AppConfig.DefaultPath);
        config.GameDirectory = gameDir;
        config.Save(AppConfig.DefaultPath);
    }

    private void OpenReport()
    {
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "report.html");
        if (File.Exists(htmlPath))
            Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
    }

    private async Task RunAllAsync()
    {
        if (_running)
            return;

        var gameDir = _pathBox.Text;
        if (string.IsNullOrWhiteSpace(gameDir) || !GameInstallValidator.IsValid(gameDir))
        {
            MessageBox.Show(this, "Сначала укажите корректную папку бенчмарка.", "Нет пути",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _running = true;
        _runButton.Enabled = false;
        _pickButton.Enabled = false;
        _openReportButton.Enabled = false;
        _cts = new CancellationTokenSource();

        try
        {
            await RunBenchmarkAsync(gameDir, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log("Отменено пользователем.");
            _status.Text = "Отменено.";
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            _status.Text = "Ошибка: " + ex.Message;
        }
        finally
        {
            _running = false;
            _runButton.Enabled = true;
            _pickButton.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task RunBenchmarkAsync(string gameDir, CancellationToken ct)
    {
        _status.Text = "Выполняется...";
        Log($"Папка бенчмарка: {gameDir}");

        var orchestrator = new BenchmarkOrchestrator(gameDir);
        orchestrator.Status += msg => BeginInvoke(() => Log(msg));

        orchestrator.BackupSettings();
        var (w, h, _) = HardwareInfo.GetPrimaryMonitor();

        try
        {
            var cpu = await orchestrator.RunAsync(Profiles.Cpu(w, h), ct);
            BeginInvoke(() => Log($"[CPU] Готово: avg {cpu.Summary.FpsAvg:0.#} FPS, CPU frame {cpu.Summary.CpuFrameTimeMs:0.0} ms, GPU frame {cpu.Summary.GpuFrameTimeMs:0.0} ms"));

            // Пауза между прогонами: Steam должен отпустить предыдущий запуск.
            await Task.Delay(3000, ct);

            var gpu = await orchestrator.RunAsync(Profiles.Gpu(w, h), ct);
            BeginInvoke(() => Log($"[GPU] Готово: avg {gpu.Summary.FpsAvg:0.#} FPS, CPU frame {gpu.Summary.CpuFrameTimeMs:0.0} ms, GPU frame {gpu.Summary.GpuFrameTimeMs:0.0} ms"));

            var report = new Report
            {
                GeneratedAt = DateTime.Now,
                System = cpu.System,
                CpuTest = cpu,
                GpuTest = gpu,
            };

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "report.json");
            var htmlPath = Path.Combine(AppContext.BaseDirectory, "report.html");
            ReportSerializer.Save(report, jsonPath);
            HtmlReportWriter.Save(report, htmlPath);

            BeginInvoke(() =>
            {
                Log($"Отчёт: {jsonPath}");
                Log($"Отчёт: {htmlPath}");
                _openReportButton.Enabled = true;
                _status.Text = "Готово: оба теста завершены.";
            });

            BeginInvoke(() => ConsoleReportWriter.Write(report));
        }
        finally
        {
            BenchmarkLauncher.KillBenchmarkProcesses();
            orchestrator.RestoreSettings();
        }
    }

    private void Log(string message)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _status.Text = message;
    }
}
