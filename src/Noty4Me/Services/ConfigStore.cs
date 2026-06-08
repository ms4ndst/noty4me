using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Noty4Me.Models;

namespace Noty4Me.Services;

public static class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppConfig Load()
    {
        Paths.EnsureDir();
        if (!File.Exists(Paths.ConfigFile)) return new AppConfig();
        try
        {
            var json = File.ReadAllText(Paths.ConfigFile);
            return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig cfg)
    {
        Paths.EnsureDir();
        File.WriteAllText(Paths.ConfigFile, JsonSerializer.Serialize(cfg, Options));
    }
}
