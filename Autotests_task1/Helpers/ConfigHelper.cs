using System.Text.Json;
using Aquality.Selenium.Core.Logging;
using Aquality.Selenium.Browsers;

namespace Autotests_task1.Helpers;

public static class ConfigHelper
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    
    private static readonly Lazy<JsonDocument> SecretsDocument = new(() =>
    {
        var secretsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.secrets.json");
        
        if (!File.Exists(secretsPath))
        {
            Logger.Error($"Secrets file not found: {secretsPath}");
            throw new FileNotFoundException(
                $"Secrets file not found: {secretsPath}. " +
                "Please ensure appsettings.secrets.json exists with credential configuration.");
        }
        
        try
        {
            var jsonContent = File.ReadAllText(secretsPath);
            Logger.Debug("Successfully loaded secrets configuration file");
            return JsonDocument.Parse(jsonContent);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to parse secrets configuration file: {ex.Message}");
            throw new InvalidOperationException("Failed to parse appsettings.secrets.json", ex);
        }
    });

    public static string GetUsername()
    {
        try
        {
            var username = GetConfigValue("credentials", "username");
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Username is empty in configuration file");
            }
            
            Logger.Debug($"Retrieved username from configuration: {MaskEmail(username)}");
            return username;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to retrieve username from configuration: {ex.Message}");
            throw new InvalidOperationException("Failed to get username from appsettings.secrets.json", ex);
        }
    }

    public static string GetPassword()
    {
        try
        {
            var password = GetConfigValue("credentials", "password");
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Password is empty in configuration file");
            }
            
            Logger.Debug($"Retrieved password from configuration (length: {password.Length} characters)");
            return password;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to retrieve password from configuration: {ex.Message}");
            throw new InvalidOperationException("Failed to get password from appsettings.secrets.json", ex);
        }
    }

    public static string GetBaseUrl()
    {
        try
        {
            return GetConfigValue("otodom", "baseUrl") ?? "https://www.otodom.pl/";
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to get base URL from configuration, using default: {ex.Message}");
            return "https://www.otodom.pl/";
        }
    }

    public static string GetLoginUrl()
    {
        try
        {
            return GetConfigValue("otodom", "loginUrl") ?? "https://login.otodom.pl/";
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to get login URL from configuration, using default: {ex.Message}");
            return "https://login.otodom.pl/";
        }
    }

    public static string GetSiteUrl()
    {
        try
        {
            return GetConfigValue("otodom", "siteUrl") ?? "https://www.otodom.pl/";
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to get site URL from configuration, using default: {ex.Message}");
            return "https://www.otodom.pl/";
        }
    }

    private static string GetConfigValue(string section, string key)
    {
        var document = SecretsDocument.Value;
        var root = document.RootElement;

        if (!root.TryGetProperty(section, out var sectionElement))
        {
            throw new KeyNotFoundException($"Section '{section}' not found in configuration file");
        }

        if (!sectionElement.TryGetProperty(key, out var valueElement))
        {
            throw new KeyNotFoundException($"Key '{key}' not found in section '{section}' of configuration file");
        }

        return valueElement.GetString() ?? string.Empty;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            return "***";
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return "***";
        }

        var localPart = parts[0];
        var domainPart = parts[1];

        var maskedLocal = localPart.Length <= 2 
            ? new string('*', localPart.Length)
            : $"{localPart[0]}{new string('*', localPart.Length - 2)}{localPart[^1]}";

        return $"{maskedLocal}@{domainPart}";
    }
}
