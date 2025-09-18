using Aquality.Selenium.Browsers;
using Aquality.Selenium.Forms;
using NLog;
using OpenQA.Selenium;

namespace Autotests_task1.Pages;

public abstract class BasePage : Form
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected BasePage(By locator, string name) : base(locator, name) {}

    private By SurveyCloseButton => By.CssSelector("[aria-label='Close'], button[aria-label='Zamknij']");

    public void CloseSurveyPopupIfPresent()
    {
        try
        {
            // Check if browser is still active before trying to interact
            if (!AqualityServices.IsBrowserStarted) return;
            
            var driver = AqualityServices.Browser.Driver;
            var closeBtn = driver.FindElements(SurveyCloseButton).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (closeBtn != null)
            {
                Logger.Info("Survey popup detected. Closing it.");
                try
                {
                    closeBtn.Click();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Standard click on survey close button failed, trying JS");
                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", closeBtn);
                    }
                    catch (Exception jsEx)
                    {
                        Logger.Error(jsEx, "Failed to close survey popup via JS");
                    }
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
            // Only handle popups if browser is still running
            if (AqualityServices.IsBrowserStarted)
            {
                CloseSurveyPopupIfPresent();
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Popup handling failed, continuing");
        }
    }
}
