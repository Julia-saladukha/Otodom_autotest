using NLog;
using Reqnroll;
using Aquality.Selenium.Browsers;
using Autotests_task1.Pages;

namespace Autotests_task1.Hooks;

[Binding]
public class ReqnrollHooks
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ScenarioContext _scenarioContext;

    public ReqnrollHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        LogManager.Setup().LoadConfigurationFromFile("nlog.config", optional: true);
    }

    [BeforeScenario(Order = 0)]
    public void PrepareBrowserAndHighlighting()
    {
        Logger.Info("Preparing browser for scenario");
        if (AqualityServices.IsBrowserStarted)
        {
            AqualityServices.Browser.Driver.Manage().Cookies.DeleteAllCookies();
            // Removed refresh that can cause DOM invalidation
        }
        else
        {
            // Accessing Browser property will start browser lazily
            _ = AqualityServices.Browser;
        }
        AqualityServices.Browser.Maximize();
        Logger.Info($"Starting scenario: {_scenarioContext.ScenarioInfo.Title}");
    }

    [BeforeStep(Order = -10)]
    public void HandlePopups()
    {
        try
        {
            // Only handle popups if browser is still active
            if (AqualityServices.IsBrowserStarted)
            {
                // Use any known page root (body) to attempt popup handling
                var basePage = new MainPage(); // lightweight instantiation just for popup checks
                basePage.HandlePopupsIfPresent();
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Popup handling before step ignored due to exception");
        }
    }

    [AfterScenario(Order = 100)]
    public void Cleanup()
    {
        if (_scenarioContext.TestError != null)
        {
            Logger.Error(_scenarioContext.TestError, "Scenario failed");
        }
        Logger.Info($"Finishing scenario: {_scenarioContext.ScenarioInfo.Title}");
        if (AqualityServices.IsBrowserStarted)
        {
            AqualityServices.Browser.Quit();
        }
    }
}
