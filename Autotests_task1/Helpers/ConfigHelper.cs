using System.Text.Json;

namespace Autotests_task1.Helpers;

public static class ConfigHelper
{
    private static readonly Lazy<JsonDocument> Secrets = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.secrets.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Secrets file not found: {path}. Provide appsettings.secrets.json with credentials.");
        }
        return JsonDocument.Parse(File.ReadAllText(path));
    });

    public static string GetUsername() => GetString("credentials:username");
    public static string GetPassword() => GetString("credentials:password");

    private static string GetString(string path)
    {
        var parts = path.Split(':');
        JsonElement current = Secrets.Value.RootElement;
        foreach (var part in parts)
        {
            if (current.TryGetProperty(part, out var next))
            {
                current = next;
            }
            else
            {
                throw new KeyNotFoundException($"Key '{path}' not found in secrets file");
            }
        }
        return current.GetString() ?? string.Empty;
    }
}
