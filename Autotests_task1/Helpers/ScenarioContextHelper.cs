using Reqnroll;

namespace Autotests_task1.Helpers;

public static class ScenarioContextHelper
{
    public static void Set<T>(ScenarioContext context, string key, T value) => context[key] = value!;

    public static T Get<T>(ScenarioContext context, string key) => context.TryGetValue(key, out var val) ? (T)val : throw new KeyNotFoundException($"Key '{key}' not found in ScenarioContext");

    public static bool TryGet<T>(ScenarioContext context, string key, out T value)
    {
        if (context.TryGetValue(key, out var val) && val is T casted)
        {
            value = casted;
            return true;
        }
        value = default!;
        return false;
    }
}
