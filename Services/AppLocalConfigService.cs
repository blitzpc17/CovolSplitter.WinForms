using System.Text.Json;

namespace CovolSplitter.WinForms.Services;

public sealed class AppLocalConfigService
{
    private sealed class LocalConfig
    {
        public string? ConnectionString { get; set; }
    }

    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CovolSplitter"
    );

    private static readonly string FilePath = Path.Combine(
        FolderPath,
        "config.local.json"
    );

    public string? LoadConnectionString()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            var json = File.ReadAllText(FilePath);
            var config = JsonSerializer.Deserialize<LocalConfig>(json);

            return string.IsNullOrWhiteSpace(config?.ConnectionString)
                ? null
                : config.ConnectionString;
        }
        catch
        {
            return null;
        }
    }

    public void SaveConnectionString(string connectionString)
    {
        Directory.CreateDirectory(FolderPath);

        var config = new LocalConfig
        {
            ConnectionString = connectionString
        };

        var json = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(FilePath, json);
    }
}