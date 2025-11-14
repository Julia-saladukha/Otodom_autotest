using Aquality.Selenium.Browsers;
using Aquality.Selenium.Forms;
using Aquality.Selenium.Core.Logging;
using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using OpenQA.Selenium;

namespace Autotests_task1.Pages;

public abstract class BasePage : Form
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    protected static readonly IElementFactory Factory = AqualityServices.Get<IElementFactory>();

    protected BasePage(By locator, string name) : base(locator, name) {}

    private readonly By _cookieAcceptButton = By.Id("onetrust-accept-btn-handler");

    public void CloseCookieIfPresent()
    {
        if (!AqualityServices.IsBrowserStarted) return;

        var cookieButtons = Factory.FindElements<IButton>(_cookieAcceptButton, "Cookie accept buttons");
        var btn = cookieButtons.FirstOrDefault(b => b.State.IsDisplayed && b.State.IsEnabled);
        
        if (btn == null) return;

        Logger.Info("Cookie consent popup detected. Accepting cookies.");

        var clicked = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var buttons = Factory.FindElements<IButton>(_cookieAcceptButton, "Cookie buttons");
            var cookieBtn = buttons.FirstOrDefault(b => b.State.IsDisplayed && b.State.IsEnabled);
            if (cookieBtn == null) return true;

            cookieBtn.Click();
            return true;
        }, TimeSpan.FromSeconds(2));

        if (!clicked)
        {
            Logger.Warn("Standard click failed for cookie button, trying JS");
            btn.JsActions.Click();
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
