using Aquality.Selenium.Browsers;
using OpenQA.Selenium;
using Aquality.Selenium.Core.Logging;
using Autotests_task1.Helpers;

namespace Autotests_task1.Pages;

public class MainPage : BasePage
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    private static readonly string SiteUrl = ConfigHelper.GetSiteUrl();

    public MainPage() : base(By.CssSelector("body"), "Main Page") { }

    private readonly By _locationButton  = By.XPath("//input[contains(@data-cy,'search.form.location')]");
    private readonly By _locationInput  = By.XPath("//input[@id='location-search-input']");
    private readonly By _priceFromInput = By.CssSelector("input[data-cy='search-form--field--priceMin']");
    private readonly By _priceToInput   = By.CssSelector("input[data-cy='search-form--field--priceMax']");
    private readonly By _searchButton   = By.CssSelector("button[id='search-form-submit']");
    private readonly By _loginButton    = By.XPath("//button[@data-cy='navbar-my-account-button']");

    public void Open()
    {
        Logger.Info($"Opening main page: {SiteUrl}");
        AqualityServices.Browser.GoTo(SiteUrl);
        AqualityServices.Browser.WaitForPageToLoad();
    }

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
        
        input.Click();
        var input2 = FindFirstDisplayedElement(_locationInput);
        input2.SendKeys(location);
        
        bool suggestionAppeared = AqualityServices.ConditionalWait.WaitFor(() =>
            AqualityServices.Browser.Driver.FindElements(By.CssSelector("div[role='listitem']")).Any(e => e.Displayed),
            timeout: TimeSpan.FromSeconds(3));

        if (!suggestionAppeared)
        {
            Logger.Warn("No location suggestions appeared within timeout");
        }
        else
        {
            Logger.Info("Location suggestions detected");
        }

        input2.SendKeys(Keys.Enter);

        var driver = AqualityServices.Browser.Driver;
        var logoElement = driver.FindElements(By.XPath("//a[@data-sentry-element='Logo']"))
            .FirstOrDefault(e => e.Displayed && e.Enabled);
        
        if (logoElement != null)
        {
            Logger.Info("Clicking on Otodom logo to stabilize form");
            logoElement.Click();
            Logger.Info("Successfully clicked on logo");
            AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromSeconds(1));
        }
                  
        var formReady = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var priceMin = FindFirstDisplayedElement(_priceFromInput);
            return priceMin != null && IsElementFullyInteractable(priceMin);
        }, TimeSpan.FromSeconds(5));
        
        Logger.Debug($"Form ready after location: {formReady}");
        return true;
    }

    public bool SetPriceRange(int min, int max)
    {
        Logger.Info($"Setting price range: {min}-{max}");
        
        var inputsReady = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var from = FindFirstDisplayedElement(_priceFromInput);
            var to = FindFirstDisplayedElement(_priceToInput);
            
            if (from == null || to == null) return false;
            if (!IsElementFullyInteractable(from) || !IsElementFullyInteractable(to)) return false;
            
            var actions = new OpenQA.Selenium.Interactions.Actions(AqualityServices.Browser.Driver);
            
            actions.MoveToElement(from).Click().Perform();
            from.Clear();
            from.SendKeys(min.ToString());
            
            actions.MoveToElement(to).Click().Perform();
            to.Clear();
            to.SendKeys(max.ToString());
            
            return true;
            
        }, TimeSpan.FromSeconds(10));
        
        if (inputsReady)
        {
            Logger.Info("Price range set successfully");
            return true;
        }
        
        Logger.Error("Failed to set price range");
        return false;
    }

    public void SetLocationAndPriceFilters(string location, int minPrice, int maxPrice)
    {
        var locOk = SetLocation(location);
        var priceOk = SetPriceRange(minPrice, maxPrice);
        Logger.Debug($"Location set: {locOk}; Price set: {priceOk}");
    }

    public bool ClickSearchButton()
    {
        Logger.Info("Clicking search button");

        var clickSucceeded = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var btn = FindFirstDisplayedElement(_searchButton);
            if (btn == null)
            {
                Logger.Error("Search button not found");
                return false;
            }

            if (btn.Displayed && btn.Enabled)
            {
                btn.Click();
                return true;
            }

            return false;
        }, TimeSpan.FromSeconds(5));

        if (clickSucceeded)
        {
            Logger.Info("Search button clicked successfully");
            return true;
        }

        Logger.Error("Failed to click search button after waiting 5 seconds");
        return false;
    }
    private IWebElement? FindFirstDisplayedElement(By locator) {

        var a = AqualityServices.Browser.Driver.FindElements(locator);
        return AqualityServices.Browser.Driver.FindElements(locator)
            .FirstOrDefault(e => e.Displayed && e.Enabled);
}

    private bool IsElementFullyInteractable(IWebElement element)
    {
        return element != null && AqualityServices.ConditionalWait.WaitFor(() =>
            element.Displayed && element.Enabled &&
            element.Size.Width > 5 && element.Size.Height > 5 &&
            element.Location.X >= -50 && element.Location.Y >= -50,
            TimeSpan.FromMilliseconds(200)
        );
    }
}
