using WukongBenchAuto.Core.Models;

namespace WukongBenchAuto;

/// <summary>
/// Панель-редактор одного профиля бенчмарка: разрешение, масштаб рендера,
/// качества 1–5 и переключатели. Читается/пишется в BenchmarkProfile.
/// </summary>
public sealed class ProfileEditorPanel : TableLayoutPanel
{
    private readonly NumericUpDown _width;
    private readonly NumericUpDown _height;
    private readonly NumericUpDown _imageQuality;
    private readonly Dictionary<string, ComboBox> _qualities = [];
    private readonly Dictionary<string, CheckBox> _toggles = [];

    private static readonly (string Key, string Label)[] QualityFields =
    [
        ("ViewDistance", "Дальность прорисовки"),
        ("AntiAliasing", "Сглаживание"),
        ("PostProcessing", "Постобработка"),
        ("ShadowQuality", "Тени"),
        ("TextureQuality", "Текстуры"),
        ("MaterialQuality", "Материалы"),
        ("VegetationQuality", "Растительность"),
        ("FxQuality", "Эффекты"),
        ("GlobalIllumination", "Глобальное освещение"),
        ("ReflectionQuality", "Отражения"),
    ];

    private static readonly (string Key, string Label)[] ToggleFields =
    [
        ("Rtx", "Трассировка лучей (RTX)"),
        ("Dlss", "DLSS (апскейлинг)"),
        ("InsertFrame", "Генерация кадров"),
        ("MotionBlur", "Motion Blur"),
        ("VSync", "VSync"),
    ];

    public ProfileEditorPanel(string title, BenchmarkProfile profile)
    {
        Dock = DockStyle.Fill;
        ColumnCount = 2;
        AutoScroll = true;
        Padding = new Padding(8);

        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        var row = 0;

        void AddLabel(string text)
        {
            RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            Controls.Add(label, 0, row);
            SetColumnSpan(label, 2);
            row++;
        }

        NumericUpDown AddNumeric(string label, decimal value, decimal min, decimal max)
        {
            RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            }, 0, row);

            var numeric = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Dock = DockStyle.Fill,
            };
            Controls.Add(numeric, 1, row);
            row++;
            return numeric;
        }

        // Заголовок обрабатываем вне generic-механики: он не возвращает контрол.
        RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var header = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(header, 0, row);
        SetColumnSpan(header, 2);
        row++;

        _width = AddNumeric("Ширина разрешения", profile.ResolutionWidth, 640, 7680);
        _height = AddNumeric("Высота разрешения", profile.ResolutionHeight, 360, 4320);
        _imageQuality = AddNumeric("Масштаб рендера, % (ImageQuality)", profile.ImageQuality, 25, 200);

        AddLabel("Качество сцен (1 — минимум, 5 — максимум)");
        foreach (var (key, label) in QualityFields)
        {
            RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
            };
            for (int i = 1; i <= 5; i++)
                combo.Items.Add(i.ToString());
            combo.SelectedIndex = Math.Clamp(GetQuality(profile, key) - 1, 0, 4);
            _qualities[key] = combo;
            Controls.Add(combo, 1, row);
            row++;
        }

        AddLabel("Переключатели");
        foreach (var (key, label) in ToggleFields)
        {
            RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);

            var check = new CheckBox
            {
                Checked = GetToggle(profile, key) != 0,
                Dock = DockStyle.Fill,
                Text = "вкл",
            };
            _toggles[key] = check;
            Controls.Add(check, 1, row);
            row++;
        }
    }

    /// <summary>Собрать профиль из текущих значений панели.</summary>
    public BenchmarkProfile ToProfile(string name, string description, int rtxLevel, int qualityLevel)
    {
        return new BenchmarkProfile
        {
            Name = name,
            Description = description,
            ResolutionWidth = (int)_width.Value,
            ResolutionHeight = (int)_height.Value,
            ImageQuality = (int)_imageQuality.Value,
            QualityLevel = qualityLevel,
            ViewDistance = Quality("ViewDistance"),
            AntiAliasing = Quality("AntiAliasing"),
            PostProcessing = Quality("PostProcessing"),
            ShadowQuality = Quality("ShadowQuality"),
            TextureQuality = Quality("TextureQuality"),
            MaterialQuality = Quality("MaterialQuality"),
            VegetationQuality = Quality("VegetationQuality"),
            FxQuality = Quality("FxQuality"),
            GlobalIllumination = Quality("GlobalIllumination"),
            ReflectionQuality = Quality("ReflectionQuality"),
            Rtx = Toggle("Rtx"),
            RtxLevel = rtxLevel,
            Dlss = Toggle("Dlss"),
            InsertFrame = Toggle("InsertFrame"),
            MotionBlur = Toggle("MotionBlur"),
            VSync = Toggle("VSync"),
            Dx12 = 1,
            FrameRateLimit = 0,
        };
    }

    private int Quality(string key) => int.Parse(_qualities[key].Text);

    private int Toggle(string key) => _toggles[key].Checked ? 1 : 0;

    private static int GetQuality(BenchmarkProfile p, string key) => key switch
    {
        "ViewDistance" => p.ViewDistance,
        "AntiAliasing" => p.AntiAliasing,
        "PostProcessing" => p.PostProcessing,
        "ShadowQuality" => p.ShadowQuality,
        "TextureQuality" => p.TextureQuality,
        "MaterialQuality" => p.MaterialQuality,
        "VegetationQuality" => p.VegetationQuality,
        "FxQuality" => p.FxQuality,
        "GlobalIllumination" => p.GlobalIllumination,
        "ReflectionQuality" => p.ReflectionQuality,
        _ => 1,
    };

    private static int GetToggle(BenchmarkProfile p, string key) => key switch
    {
        "Rtx" => p.Rtx,
        "Dlss" => p.Dlss,
        "InsertFrame" => p.InsertFrame,
        "MotionBlur" => p.MotionBlur,
        "VSync" => p.VSync,
        _ => 0,
    };
}
