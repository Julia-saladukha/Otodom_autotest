using Aquality.Selenium.Browsers;
using OpenQA.Selenium;
using NLog;

namespace Autotests_task1.Pages;

/// <summary>
/// MainPage without try-catch blocks and minimal JavaScript - handles dynamic content properly
/// </summary>
public class MainPage : BasePage
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string SiteUrl = "https://www.otodom.pl/";

    public MainPage() : base(By.CssSelector("body"), "Main Page") { }

    private readonly By[] _locationInputs = {
        By.CssSelector("input[data-cy='search.form.location.button']"),
        By.CssSelector("input[placeholder*='lokalizac']"),
        By.CssSelector("input[name='location']")
    };

    private readonly By[] _priceFromInputs = {
        By.CssSelector("input[data-cy='search.form.price.from']"),
        By.CssSelector("input[name='priceMin']"),
        By.Id("priceFrom")
    };

    private readonly By[] _priceToInputs = {
        By.CssSelector("input[data-cy='search.form.price.to']"),
        By.CssSelector("input[name='priceMax']"),
        By.Id("priceTo")
    };

    private readonly By[] _searchButtons = {
        By.Id("search-form-submit"),
        By.CssSelector("button[type='submit']"),
        By.XPath("//button[contains(.,'Szukaj')]")
    };

    private readonly By[] _loginLocators = {
        By.CssSelector("a[data-cy='header-login-button']"),
        By.XPath("//a[contains(normalize-space(.),'Moje konto')]"),
        By.XPath("//button[contains(.,'Moje konto') or contains(.,'Zaloguj') or contains(.,'Log in')]")
    };

    public void Open()
    {
        Logger.Info($"Opening main page: {SiteUrl}");
        AqualityServices.Browser.GoTo(SiteUrl);
        AqualityServices.Browser.WaitForPageToLoad();
    }

    public bool WaitUntilLoaded()
    {
        Logger.Debug("Waiting for main page root to be displayed");
        return State.WaitForDisplayed(TimeSpan.FromSeconds(10));
    }

    public void AcceptCookiesIfPresent()
    {
        Logger.Debug("Checking cookies popup");
        var driver = AqualityServices.Browser.Driver;
        var cookieBtn = driver.FindElements(By.Id("onetrust-accept-btn-handler"))
            .FirstOrDefault(e => e.Displayed && e.Enabled);
        
        if (cookieBtn != null)
        {
            Logger.Info("Cookie consent popup detected. Accepting cookies.");
            cookieBtn.Click();
            Logger.Info("Cookie consent accepted.");
            Thread.Sleep(500);
        }
    }

    public bool OpenLogin()
    {
        Logger.Info("Opening login dialog/page");
        var loginElement = FindFirstDisplayedElement(_loginLocators);
        
        if (loginElement == null)
        {
            Logger.Error("Login link/button not found.");
            return false;
        }
        
        loginElement.Click();
        return true;
    }

    public bool SetLocation(string location)
    {
        Logger.Info($"Setting location: {location}");
        
        var input = FindFirstDisplayedElement(_locationInputs);
        if (input == null)
        {
            Logger.Error("Location input not found");
            return false;
        }

        input.Click();
        Thread.Sleep(500);
        input.Clear();
        input.SendKeys(location);
        
        // Wait for suggestions
        Thread.Sleep(2000);
        var suggestion = AqualityServices.Browser.Driver.FindElements(By.CssSelector("div[role='listitem']"))
            .FirstOrDefault(e => e.Displayed);
        
        if (suggestion != null)
        {
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
            else
            {
                // Try alternative logo selectors
                var altLogoSelectors = new By[] {
                    By.CssSelector("img[alt*='Otodom']"),
                    By.CssSelector("img[src*='otodom_logo']"),
                    By.CssSelector("a[href='/']"),
                    By.XPath("//img[@alt='Id? do strony g?ównej']")
                };
                
                foreach (var selector in altLogoSelectors)
                {
                    var altLogo = driver.FindElements(selector).FirstOrDefault(e => e.Displayed);
                    if (altLogo != null)
                    {
                        Logger.Info($"Clicking alternative logo using selector: {selector}");
                        altLogo.Click();
                        Logger.Info("Successfully clicked on alternative logo");
                        Thread.Sleep(1000);
                        break;
                    }
                }
            }
            
            // CRITICAL FIX: Extended wait for form stabilization
            Logger.Info("Waiting for form to stabilize after location selection...");
            Thread.Sleep(8000); // INCREASED from 5 to 8 seconds
            
            // Additional readiness check with extended timeout
            var formReady = AqualityServices.ConditionalWait.WaitFor(() => 
            {
                var minInput = FindFirstDisplayedElement(_priceFromInputs);
                if (minInput == null) return false;
                return IsElementFullyInteractable(minInput);
            }, TimeSpan.FromSeconds(20)); // INCREASED timeout
            
            Logger.Info($"Form readiness after location selection: {formReady}");
        }

        return true;
    }

    public bool SetPriceRange(int min, int max)
    {
        Logger.Info($"Setting price range: {min} - {max}");
        
        // MULTI-STAGE readiness check with increased attempts
        var formReady = false;
        var maxAttempts = 15; // INCREASED attempts
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            Logger.Info($"Price form readiness check - Attempt {attempt}/{maxAttempts}");
            
            var minInput = FindFirstDisplayedElement(_priceFromInputs);
            var maxInput = FindFirstDisplayedElement(_priceToInputs);
            
            if (minInput != null && maxInput != null)
            {
                var minReady = IsElementFullyInteractable(minInput);
                var maxReady = IsElementFullyInteractable(maxInput);
                
                Logger.Debug($"Elements status - min found: {minInput != null}, max found: {maxInput != null}");
                Logger.Debug($"Interactability - min ready: {minReady}, max ready: {maxReady}");
                
                if (minReady && maxReady)
                {
                    formReady = true;
                    Logger.Info("Form is fully ready for price input");
                    break;
                }
            }
            else
            {
                Logger.Warn($"Elements not found - min: {minInput != null}, max: {maxInput != null}");
            }
            
            Thread.Sleep(3000); // INCREASED wait between attempts
        }
        
        if (!formReady)
        {
            Logger.Error("Price form never became ready for interaction after all attempts");
            return false;
        }
        
        // Get fresh element references
        var minInputFinal = FindFirstDisplayedElement(_priceFromInputs);
        var maxInputFinal = FindFirstDisplayedElement(_priceToInputs);
        
        if (minInputFinal == null || maxInputFinal == null) 
        {
            Logger.Error("Price input fields disappeared during interaction");
            return false;
        }

        // REMOVED try-catch - let errors be visible!
        Logger.Info("Setting minimum price...");
        
        // ADDITIONAL FIX: Scroll element into view and ensure it's clickable
        var driver = AqualityServices.Browser.Driver;
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", minInputFinal);
        Thread.Sleep(500);
        
        // Wait for element to be truly interactable
        var minClickable = AqualityServices.ConditionalWait.WaitFor(() => {
            return minInputFinal.Displayed && minInputFinal.Enabled && minInputFinal.Size.Width > 0;
        }, TimeSpan.FromSeconds(10));
        
        if (!minClickable)
        {
            Logger.Error("Min price input never became clickable");
            return false;
        }
        
        minInputFinal.Clear();
        Thread.Sleep(1000);
        minInputFinal.SendKeys(min.ToString());
        Thread.Sleep(1000);
        
        Logger.Info("Setting maximum price...");
        
        // Same for max input
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", maxInputFinal);
        Thread.Sleep(500);
        
        var maxClickable = AqualityServices.ConditionalWait.WaitFor(() => {
            return maxInputFinal.Displayed && maxInputFinal.Enabled && maxInputFinal.Size.Width > 0;
        }, TimeSpan.FromSeconds(10));
        
        if (!maxClickable)
        {
            Logger.Error("Max price input never became clickable");
            return false;
        }
        
        maxInputFinal.Clear();
        Thread.Sleep(1000);
        maxInputFinal.SendKeys(max.ToString());
        
        Thread.Sleep(1500);
        Logger.Info("Price range set successfully");
        return true;
    }

    public void SetLocationAndPriceFilters(string location, int minPrice, int maxPrice)
    {
        Logger.Info($"Applying filters: location='{location}', price {minPrice}-{maxPrice}");
        var locSet = SetLocation(location);
        var priceSet = SetPriceRange(minPrice, maxPrice);
        Logger.Debug($"Location set: {locSet}, Price set: {priceSet}");
    }

    public bool ClickSearchButton()
    {
        Logger.Info("Clicking search button");
        var button = FindFirstDisplayedElement(_searchButtons);
        if (button == null) return false;
        button.Click();
        return true;
    }

    public bool Search() => ClickSearchButton();

    private IWebElement? FindFirstDisplayedElement(By[] locators)
    {
        var driver = AqualityServices.Browser.Driver;
        foreach (var locator in locators)
        {
            var elements = driver.FindElements(locator);
            var displayed = elements.FirstOrDefault(e => e.Displayed && e.Enabled);
            if (displayed != null) return displayed;
        }
        return null;
    }

    private bool IsElementFullyInteractable(IWebElement element)
    {
        if (element == null) return false;
        
        var displayed = element.Displayed;
        var enabled = element.Enabled;
        var hasSize = element.Size.Height > 0 && element.Size.Width > 0;
        
        // Enhanced checks for true interactability
        var location = element.Location;
        var size = element.Size;
        var inViewport = location.X >= -50 && location.Y >= -50 && size.Width > 5 && size.Height > 5;
        
        // Check element is not stale by accessing a property
        var tagName = element.TagName; // This will throw if element is stale
        
        var result = displayed && enabled && hasSize && inViewport;
        Logger.Debug($"Element full interactability: displayed={displayed}, enabled={enabled}, hasSize={hasSize}, inViewport={inViewport} => {result}");
        return result;
    }
}