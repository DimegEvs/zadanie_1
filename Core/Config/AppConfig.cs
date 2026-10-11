using System.Text.Json;

namespace WukongBenchAuto.Core.Config;

/// <summary>Локальный конфиг: сохранённый путь к игре. Лежит рядом с exe.</summary>
public sealed class AppConfig
{
    public string GameDirectory { get; set; } = "";

    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            return new AppConfig();

        try
        {
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), JsonOptions)
                   ?? new AppConfig();
        }
        catch (JsonException)
        {
            return new AppConfig();
        }
    }

    public void Save(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
