using Aquality.Selenium.Browsers;
using Aquality.Selenium.Forms;
using Aquality.Selenium.Core.Logging;
using OpenQA.Selenium;

namespace Autotests_task1.Pages;

public abstract class BasePage : Form
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();

    protected BasePage(By locator, string name) : base(locator, name) {}

    private readonly By _cookieAcceptButton = By.Id("onetrust-accept-btn-handler");

    public void CloseCookieIfPresent()
    {
        if (!AqualityServices.IsBrowserStarted) return;

        var driver = AqualityServices.Browser.Driver;
        var btn = driver.FindElements(_cookieAcceptButton).FirstOrDefault(e => e.Displayed && e.Enabled);
        if (btn == null) return;

        Logger.Info("Cookie consent popup detected. Accepting cookies.");

        var clicked = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var cookieBtn = driver.FindElements(_cookieAcceptButton).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (cookieBtn == null) return true;

            cookieBtn.Click();
            return true;
        }, TimeSpan.FromSeconds(2));

        if (!clicked)
        {
            Logger.Warn("Standard click failed for cookie button, trying JS");
            AqualityServices.Browser.ExecuteScript("arguments[0].click();", btn);
            Logger.Info("Cookie consent accepted via JS click.");
        }
        else
        {
            Logger.Info("Cookie consent accepted.");
        }
    }

    public void HandlePopupsIfPresent()
    {
        if (!AqualityServices.IsBrowserStarted) return;
        CloseCookieIfPresent();
    }
}
