using Aquality.Selenium.Browsers;
using OpenQA.Selenium;
using Aquality.Selenium.Core.Logging;

namespace Autotests_task1.Pages;

public class MainPage : BasePage
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    private const string SiteUrl = "https://www.otodom.pl/";

    public MainPage() : base(By.CssSelector("body"), "Main Page") { }

    // Single locators only
    private readonly By _locationButton  = By.XPath("//input[contains(@data-cy,'search.form.location')]");
    private readonly By _locationInput  = By.XPath("//input[@id='location-search-input']");
    private readonly By _priceFromInput = By.CssSelector("input[data-cy='search-form--field--priceMin']");
    private readonly By _priceToInput   = By.CssSelector("input[data-cy='search-form--field--priceMax']");
    private readonly By _searchButton   = By.CssSelector("button[data-cy='search.submit-form.results']");
    private readonly By _loginButton    = By.XPath("//button[@data-cy='navbar-my-account-button']");

    public void Open()
    {
        Logger.Info($"Opening main page: {SiteUrl}");
        AqualityServices.Browser.GoTo(SiteUrl);
        AqualityServices.Browser.WaitForPageToLoad();
    }

    public bool WaitUntilLoaded() => State.WaitForDisplayed(TimeSpan.FromSeconds(10));

    public void AcceptCookiesIfPresent()
    {
        var driver = AqualityServices.Browser.Driver;
        var btn = driver.FindElements(By.Id("onetrust-accept-btn-handler"))
            .FirstOrDefault(e => e.Displayed && e.Enabled);
        if (btn == null) return;
        try
        {
            btn.Click();
            Logger.Info("Cookie consent accepted.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Standard click failed for cookie button: {ex.Message}");
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn);
                Logger.Info("Cookie consent accepted via JS.");
            }
            catch (Exception jsEx)
            {
                Logger.Error($"Failed to accept cookies: {jsEx.Message}");
            }
        }
    }

    public bool OpenLogin()
    {
        var login = FindFirstDisplayedElement(_loginButton);
        if (login == null)
        {
            Logger.Error("Login button not found");
            return false;
        }
        login.Click();
        return true;
    }

    public bool SetLocation(string location)
    {
        var input = FindFirstDisplayedElement(_locationButton);
        if (input == null)
        {
            Logger.Error("Location input not found");
            return false;
        }
        try
        {
            
            input.Click();
            var input2 = FindFirstDisplayedElement(_locationInput);
            input2.SendKeys(location);

            // Wait for suggestion items
            var suggestionsShown = AqualityServices.ConditionalWait.WaitFor(() =>
                AqualityServices.Browser.Driver.FindElements(By.CssSelector("div[role='listitem']"))
                    .Any(e => e.Displayed), TimeSpan.FromSeconds(5));
            if (!suggestionsShown)
            {
                Logger.Warn("Location suggestions did not appear");
                return false;
            }
            var suggestion = AqualityServices.Browser.Driver
                .FindElements(By.CssSelector("div[role='listitem']"))
                .FirstOrDefault(e => e.Displayed);
            if (suggestion == null)
            {
                Logger.Warn("No visible suggestion to click");
                return false;
            }
            suggestion.Click();
            Logger.Info("Location suggestion selected");

            // CRITICAL: Click on logo after suggestion selection to stabilize form
            var driver = AqualityServices.Browser.Driver;
            var logoElement = driver.FindElements(By.XPath("//a[@data-sentry-element='Logo']"))
                .FirstOrDefault(e => e.Displayed && e.Enabled);
            
            if (logoElement != null)
            {
                Logger.Info("Clicking on Otodom logo to stabilize form");
                logoElement.Click();
                Logger.Info("Successfully clicked on logo");
                Thread.Sleep(1000); // Allow page to process logo click
            }
                      
            // Additional readiness check with extended timeout
            var formReady = AqualityServices.ConditionalWait.WaitFor(() =>
            {
                var priceMin = FindFirstDisplayedElement(_priceFromInput);
                return priceMin != null && IsElementFullyInteractable(priceMin);
            }, TimeSpan.FromSeconds(5));
            Logger.Debug($"Form ready after location: {formReady}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting location: {ex.Message}");
            return false;
        }
    }

    public bool SetPriceRange(int min, int max)
    {
        var ready = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var minEl = FindFirstDisplayedElement(_priceFromInput);
            var maxEl = FindFirstDisplayedElement(_priceToInput);
            return minEl != null && maxEl != null &&
                   IsElementFullyInteractable(minEl) && IsElementFullyInteractable(maxEl);
        }, TimeSpan.FromSeconds(10));
        if (!ready)
        {
            Logger.Error("Price inputs not ready");
            return false;
        }
        var from = FindFirstDisplayedElement(_priceFromInput);
        var to = FindFirstDisplayedElement(_priceToInput);
        if (from == null || to == null)
        {
            Logger.Error("Price inputs disappeared");
            return false;
        }
        var driver = AqualityServices.Browser.Driver;
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", from);
        from.Clear();
        from.SendKeys(min.ToString());
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", to);
        to.Clear();
        to.SendKeys(max.ToString());
        Logger.Info("Price range set successfully");
        return true;
    }

    public void SetLocationAndPriceFilters(string location, int minPrice, int maxPrice)
    {
        var locOk = SetLocation(location);
        var priceOk = SetPriceRange(minPrice, maxPrice);
        Logger.Debug($"Location set: {locOk}; Price set: {priceOk}");
    }

    public bool ClickSearchButton()
    {
        var btn = FindFirstDisplayedElement(_searchButton);
        if (btn == null) return false;
        btn.Click();
        return true;
    }

    public bool Search() => ClickSearchButton();

    private IWebElement? FindFirstDisplayedElement(By locator) {

        var a = AqualityServices.Browser.Driver.FindElements(locator);
        return AqualityServices.Browser.Driver.FindElements(locator)
            .FirstOrDefault(e => e.Displayed && e.Enabled);
}

    private bool IsElementFullyInteractable(IWebElement element)
    {
        if (element == null) return false;
        try
        {
            var displayed = element.Displayed;
            var enabled = element.Enabled;
            var size = element.Size;
            var loc = element.Location;
            var inViewport = loc.X >= -50 && loc.Y >= -50 && size.Width > 5 && size.Height > 5;
            _ = element.TagName; // stale check
            return displayed && enabled && size.Width > 0 && size.Height > 0 && inViewport;
        }
        catch { return false; }
    }
}
