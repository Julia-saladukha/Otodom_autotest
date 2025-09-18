using Aquality.Selenium.Browsers;
using Aquality.Selenium.Forms;
using NLog;
using OpenQA.Selenium;

namespace Autotests_task1.Pages;

public abstract class BasePage : Form
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected BasePage(By locator, string name) : base(locator, name) {}

    private readonly By _cookieAcceptButton = By.Id("onetrust-accept-btn-handler");

    // Extended set of possible survey/marketing/overlay close buttons (pre-universal version).
    private static readonly By[] SurveyCloseSelectors = new[]
    {
        By.CssSelector("[aria-label='Close']"),
        By.CssSelector("button[aria-label='Close']"),
        By.CssSelector("button[aria-label='Zamknij']"),
        By.CssSelector("button[data-testid='close-button']"),
        By.CssSelector("button[class*='close']"),
        By.CssSelector("[class*='close'][role='button']"),
        By.XPath("//button[contains(translate(.,'ZAMKNIJ','zamknij'),'zamknij') or contains(translate(.,'CLOSE','close'),'close')]") ,
        // Explicit selectors
        By.CssSelector(".modal-close"),
        By.CssSelector("button.modal-close"),
        By.CssSelector(".close-btn"),
        By.CssSelector("button.close-btn"),
        By.CssSelector(".n-icon--close"),
        By.CssSelector("button.n-icon--close")
    };

    public void CloseCookieIfPresent()
    {
        try
        {
            if (!AqualityServices.IsBrowserStarted) return;
            var driver = AqualityServices.Browser.Driver;
            var btn = driver.FindElements(_cookieAcceptButton).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (btn != null)
            {
                Logger.Info("Cookie consent popup detected. Accepting cookies.");
                try
                {
                    btn.Click();
                    Logger.Info("Cookie consent accepted.");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Standard click failed for cookie button, trying JS");
                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn);
                        Logger.Info("Cookie consent accepted via JS click.");
                    }
                    catch (Exception jsEx)
                    {
                        Logger.Error(jsEx, "Failed to click cookie button via JS");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error while attempting to close cookie popup (ignored)");
        }
    }

    public void CloseSurveyPopupIfPresent()
    {
        try
        {
            if (!AqualityServices.IsBrowserStarted) return;
            var driver = AqualityServices.Browser.Driver;
            foreach (var by in SurveyCloseSelectors)
            {
                try
                {
                    var closeBtn = driver.FindElements(by).FirstOrDefault(e => e.Displayed && e.Enabled);
                    if (closeBtn == null) continue;

                    Logger.Info($"Popup close control found using selector: {by}. Attempting to close popup.");
                    try
                    {
                        closeBtn.Click();
                        Logger.Info($"Closed popup via selector: {by}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Standard click on survey/overlay close failed, trying JS");
                        try
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", closeBtn);
                            Logger.Info($"Closed popup via JS using selector: {by}");
                        }
                        catch (Exception jsEx)
                        {
                            Logger.Error(jsEx, "Failed to close survey popup via JS");
                        }
                    }
                    // Continue to attempt closing additional popups if present.
                }
                catch (StaleElementReferenceException)
                {
                    Logger.Debug($"Stale element encountered for selector: {by}");
                }
                catch (Exception innerEx)
                {
                    Logger.Debug(innerEx, $"Selector iteration issue for {by}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error while attempting to close survey popup (ignored)");
        }
    }

    public void HandlePopupsIfPresent()
    {
        try
        {
            if (!AqualityServices.IsBrowserStarted) return;
            CloseCookieIfPresent();
            CloseSurveyPopupIfPresent();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Popup handling failed, continuing");
        }
    }
}
