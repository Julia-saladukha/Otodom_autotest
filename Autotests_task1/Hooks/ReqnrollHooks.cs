using Reqnroll;
using Aquality.Selenium.Browsers;
using Aquality.Selenium.Core.Logging;
using Autotests_task1.Pages;

namespace Autotests_task1.Hooks;

[Binding]
public class ReqnrollHooks
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    private readonly ScenarioContext _scenarioContext;

    public ReqnrollHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Logger.Info("Test run started");
    }

    [BeforeScenario(Order = 0)]
    public void PrepareBrowserAndHighlighting()
    {
        Logger.Info("Preparing browser for scenario (clear cookies, maximize)");
        if (AqualityServices.IsBrowserStarted)
        {
            AqualityServices.Browser.Driver.Manage().Cookies.DeleteAllCookies();
        }
        else
        {
            _ = AqualityServices.Browser;
        }
        AqualityServices.Browser.Maximize();
        Logger.Info($"Starting scenario: {_scenarioContext.ScenarioInfo.Title}");
    }

    [BeforeStep(Order = -10)]
    public void HandlePopups()
    {
        if (!AqualityServices.IsBrowserStarted) return;
        
        var page = new MainPage();
        var handledSuccessfully = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            page.HandlePopupsIfPresent();
            return true;
        }, TimeSpan.FromMilliseconds(100));
        
        if (handledSuccessfully)
        {
            Logger.Info("Checked for popups before step");
        }
        else
        {
            Logger.Debug("Popup handling skipped (no browser or popups present)");
        }
    }

    [AfterScenario(Order = 100)]
    public void Cleanup()
    {
        if (_scenarioContext.TestError != null)
        {
            Logger.Error($"Scenario failed: {_scenarioContext.TestError.Message}");
        }
        Logger.Info($"Finishing scenario: {_scenarioContext.ScenarioInfo.Title}");
        if (AqualityServices.IsBrowserStarted)
        {
            AqualityServices.Browser.Quit();
        }
    }
}
