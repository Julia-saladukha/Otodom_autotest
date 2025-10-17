using Reqnroll;
using Aquality.Selenium.Core.Logging;
using Aquality.Selenium.Browsers;

namespace Autotests_task1.Helpers;

public static class ScenarioContextHelper
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();

    public static void Set<T>(ScenarioContext context, string key, T value)
    {
        context[key] = value!;
        Logger.Debug($"ScenarioContext: Set {key} = {value}");
    }

    public static void Save<T>(ScenarioContext context, string key, T value)
    {
        context[key] = value!;
        Logger.Debug($"ScenarioContext: Saved {key} = {value}");
    }

    public static T Get<T>(ScenarioContext context, string key)
    {
        if (context.TryGetValue(key, out var val))
        {
            Logger.Debug($"ScenarioContext: Retrieved {key} = {val}");
            return (T)val;
        }
        Logger.Error($"ScenarioContext: Key '{key}' not found");
        throw new KeyNotFoundException($"Key '{key}' not found in ScenarioContext");
    }

    public static bool TryGet<T>(ScenarioContext context, string key, out T value)
    {
        if (context.TryGetValue(key, out var val) && val is T casted)
        {
            value = casted;
            Logger.Debug($"ScenarioContext: Successfully retrieved {key} = {value}");
            return true;
        }
        value = default!;
        Logger.Debug($"ScenarioContext: Failed to retrieve {key}");
        return false;
    }
}
