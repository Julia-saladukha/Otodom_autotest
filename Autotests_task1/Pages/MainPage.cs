using Aquality.Selenium.Browsers;
using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using Aquality.Selenium.Core.Elements;
using OpenQA.Selenium;
using Aquality.Selenium.Core.Logging;
using Autotests_task1.Helpers;

namespace Autotests_task1.Pages;

public class MainPage : BasePage
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    private static readonly IElementFactory Factory = AqualityServices.Get<IElementFactory>();
    private static readonly string SiteUrl = ConfigHelper.GetSiteUrl();

    public MainPage() : base(By.CssSelector("body"), "Main Page") { }

    private readonly By _locationButton  = By.XPath("//input[contains(@data-cy,'search.form.location')]");
    private readonly By _locationInput  = By.XPath("//input[@id='location-search-input']");
    private readonly By _priceFromInput = By.CssSelector("input[data-cy='search-form--field--priceMin']");
    private readonly By _priceToInput   = By.CssSelector("input[data-cy='search-form--field--priceMax']");
    private readonly By _searchButton   = By.CssSelector("button[id='search-form-submit']");
    private readonly By _loginButton    = By.XPath("//div[@data-sentry-element='NexusNavUserMenuWrapper']//button[@data-cy='navbar-my-account-button']");

    public void Open()
    {
        Logger.Info($"Opening main page: {SiteUrl}");
        AqualityServices.Browser.GoTo(SiteUrl);
        AqualityServices.Browser.WaitForPageToLoad();
    }

    public void AcceptCookiesIfPresent()
    {
        var cookieButtons = Factory.FindElements<IButton>(By.Id("onetrust-accept-btn-handler"), "Cookie accept buttons");
        var btn = cookieButtons.FirstOrDefault(b => b.State.IsDisplayed && b.State.IsEnabled);
        
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
                btn.JsActions.Click();
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
        var login = FindFirstDisplayedButton(_loginButton);
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
        var input = FindFirstDisplayedTextBox(_locationButton);
        if (input == null)
        {
            Logger.Error("Location input not found");
            return false;
        }
        
        input.Click();
        var input2 = FindFirstDisplayedTextBox(_locationInput);
        if (input2 == null)
        {
            Logger.Error("Location search input not found");
            return false;
        }
        
        input2.SendKeys(location);

        bool suggestionAppeared = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var suggestions = Factory.FindElements<ILabel>(
                By.CssSelector("div[role='listitem']"),
                "Location suggestions");

            return suggestions.Any(e => e.State.IsDisplayed);
        }, TimeSpan.FromSeconds(3));

        if (!suggestionAppeared)
        {
            Logger.Warn("No location suggestions appeared within timeout");
        }
        else
        {
            Logger.Info("Location suggestions detected");
        }

        input2.SendKeys(Keys.Enter);

        var logoButtons = Factory.FindElements<IButton>(By.XPath("//a[@data-sentry-element='Logo']"), "Logo buttons");
        var logoElement = logoButtons.FirstOrDefault(e => e.State.IsDisplayed && e.State.IsEnabled);
        
        if (logoElement != null)
        {
            Logger.Info("Clicking on Otodom logo to stabilize form");
            logoElement.Click();
            Logger.Info("Successfully clicked on logo");
            AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromSeconds(1));
        }
                  
        var formReady = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var priceMin = FindFirstDisplayedTextBox(_priceFromInput);
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
            var from = FindFirstDisplayedTextBox(_priceFromInput);
            var to = FindFirstDisplayedTextBox(_priceToInput);
            
            if (from == null || to == null) return false;
            if (!IsElementFullyInteractable(from) || !IsElementFullyInteractable(to)) return false;
            
            from.Click();
            from.ClearAndType(min.ToString());
            
            to.Click();
            to.ClearAndType(max.ToString());
            
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
            var btn = FindFirstDisplayedButton(_searchButton);
            if (btn == null)
            {
                Logger.Error("Search button not found");
                return false;
            }

            if (btn.State.IsDisplayed && btn.State.IsEnabled)
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
    
    private ITextBox? FindFirstDisplayedTextBox(By locator)
    {
        var textBoxes = Factory.FindElements<ITextBox>(locator, $"TextBox for {locator}");
        return textBoxes.FirstOrDefault(e => e.State.IsDisplayed && e.State.IsEnabled);
    }

    private IButton? FindFirstDisplayedButton(By locator)
    {
        var buttons = Factory.FindElements<IButton>(locator, $"Button for {locator}");
        return buttons.FirstOrDefault(e => e.State.IsDisplayed && e.State.IsEnabled);
    }

    private bool IsElementFullyInteractable(IElement element)
    {
        if (element == null) return false;
        
        return AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var webElement = element.GetElement();
            return element.State.IsDisplayed && 
                   element.State.IsEnabled &&
                   webElement.Size.Width > 5 && 
                   webElement.Size.Height > 5 &&
                   webElement.Location.X >= -50 && 
                   webElement.Location.Y >= -50;
        }, TimeSpan.FromMilliseconds(200));
    }
}
