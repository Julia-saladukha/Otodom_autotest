using System.Text.Json;
using NLog;

namespace Autotests_task1.Helpers;

// <summary>
// Simple configuration helper to read credentials from appsettings.secrets.json
// </summary>
public static class ConfigHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
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
            Logger.Error(ex, "Failed to parse secrets configuration file");
            throw new InvalidOperationException("Failed to parse appsettings.secrets.json", ex);
        }
    });

    // <summary>
    // Gets username from credentials section
    // </summary>
    // <returns>Username from configuration</returns>
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
            Logger.Error(ex, "Failed to retrieve username from configuration");
            throw new InvalidOperationException("Failed to get username from appsettings.secrets.json", ex);
        }
    }

    // <summary>
    // Gets password from credentials section
    // </summary>
    // <returns>Password from configuration</returns>
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
            Logger.Error(ex, "Failed to retrieve password from configuration");
            throw new InvalidOperationException("Failed to get password from appsettings.secrets.json", ex);
        }
    }

    // <summary>
    // Gets base URL from otodom section
    // </summary>
    // <returns>Base URL from configuration</returns>
    public static string GetBaseUrl()
    {
        try
        {
            return GetConfigValue("otodom", "baseUrl") ?? "https://www.otodom.pl/";
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to get base URL from configuration, using default");
            return "https://www.otodom.pl/";
        }
    }

    // <summary>
    // Gets login URL from otodom section
    // </summary>
    // <returns>Login URL from configuration</returns>
    public static string GetLoginUrl()
    {
        try
        {
            return GetConfigValue("otodom", "loginUrl") ?? "https://login.otodom.pl/";
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to get login URL from configuration, using default");
            return "https://login.otodom.pl/";
        }
    }

    // <summary>
    // Gets a configuration value from nested JSON structure
    // </summary>
    // <param name="section">Top-level section name</param>
    // <param name="key">Key within the section</param>
    // <returns>Configuration value as string</returns>
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

    // <summary>
    // Masks email address for secure logging
    // </summary>
    // <param name="email">Email to mask</param>
    // <returns>Masked email for logging</returns>
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
