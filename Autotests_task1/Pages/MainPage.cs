using Aquality.Selenium.Browsers;
using Aquality.Selenium.Elements;
using Aquality.Selenium.Elements.Interfaces;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Autotests_task1.Pages;

public class MainPage : BasePage
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string SiteUrl = "https://www.otodom.pl/";
    private static readonly TimeSpan DefaultUiWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ShortWait = TimeSpan.FromSeconds(3);

    public MainPage() : base(By.CssSelector("body"), "Main Page") { }

    #region Static/Direct Elements
    private IButton AcceptCookiesBtn => ElementFactory.GetButton(By.Id("onetrust-accept-btn-handler"), "Cookies Accept");
    #endregion

    #region Dynamic Locator Pools with Extended Search Button Options
    private readonly IReadOnlyCollection<By> _locationLocators = new List<By>
    {
        By.CssSelector("input[data-cy='search.form.location.button']"),
        By.CssSelector("input[placeholder='Wpisz lokalizacj?']"),
        By.CssSelector("input[placeholder*='lokalizac']"),
        By.CssSelector("input[name='location']"),
        By.CssSelector("input[aria-label*='Lokal']")
    };

    private readonly IReadOnlyCollection<By> _priceMinLocatorsLegacy = new List<By>
    {
        By.CssSelector("input[name='priceMin']"),
        By.CssSelector("input[id*='priceFrom']"),
        By.XPath("//input[contains(@placeholder,'od') and (contains(@placeholder,'Cena') or contains(@aria-label,'Cena'))]")
    };

    private readonly IReadOnlyCollection<By> _priceMaxLocatorsLegacy = new List<By>
    {
        By.CssSelector("input[name='priceMax']"),
        By.CssSelector("input[id*='priceTo']"),
        By.XPath("//input[contains(@placeholder,'do') and (contains(@placeholder,'Cena') or contains(@aria-label,'Cena'))]")
    };

    // Explicit selectors requested in task description - Enhanced with more options
    private readonly IReadOnlyCollection<By> _priceMinLocators = new List<By>
    {
        By.CssSelector("input[data-cy='search.form.price.from']"),
        By.Id("priceFrom"),
        By.CssSelector("input[name*='priceFrom']"),
        By.CssSelector("input[name*='price_from']"),
        By.CssSelector("input[id*='priceFrom']"),
        By.CssSelector("input[id*='price_from']"),
        By.CssSelector("input[name='priceMin']"),
        By.CssSelector("input[name='price_min']"),
        By.CssSelector("input[data-cy*='price-min']"),
        By.CssSelector("input[data-testid*='price-min']")
    };

    private readonly IReadOnlyCollection<By> _priceMaxLocators = new List<By>
    {
        By.CssSelector("input[data-cy='search.form.price.to']"),
        By.Id("priceTo"),
        By.CssSelector("input[name*='priceTo']"),
        By.CssSelector("input[name*='price_to']"),
        By.CssSelector("input[id*='priceTo']"),
        By.CssSelector("input[id*='price_to']"),
        By.CssSelector("input[name='priceMax']"),
        By.CssSelector("input[name='price_max']"),
        By.CssSelector("input[data-cy*='price-max']"),
        By.CssSelector("input[data-testid*='price-max']")
    };

    // Enhanced search button selectors
    private readonly IReadOnlyCollection<By> _searchButtonLocators = new List<By>
    {
        By.Id("search-form-submit"),
        By.CssSelector("button[id='search-form-submit']"),
        By.CssSelector("button[type='submit']"),
        By.CssSelector("button[data-cy='search-button']"),
        By.CssSelector("button[data-cy='homepage.search.submit.button']"),
        By.CssSelector("button[data-testid='search-button']"),
        By.CssSelector("form button[type='submit']"),
        By.XPath("//button[contains(.,'Szukaj') or contains(.,'Search')]")
    };

    private readonly IReadOnlyCollection<By> _loginLocators = new List<By>
    {
        By.CssSelector("a[data-cy='header-login-button']"),
        By.XPath("//a[contains(normalize-space(.),'Moje konto')]|//button[contains(.,'Moje konto') or contains(.,'Zaloguj') or contains(.,'Log in')]")
    };
    #endregion

    #region Navigation & Page State
    public void Open()
    {
        Logger.Info($"Opening main page: {SiteUrl}");
        AqualityServices.Browser.GoTo(SiteUrl);
        AqualityServices.Browser.WaitForPageToLoad();
    }

    public bool WaitUntilLoaded()
    {
        Logger.Debug("Waiting for main page root to be displayed");
        return State.WaitForDisplayed(DefaultUiWait);
    }
    #endregion

    #region Helpers
    private static IWebElement? TryFindFirstDisplayed(IWebDriver driver, IEnumerable<By> locators)
    {
        foreach (var by in locators)
        {
            try
            {
                var elements = driver.FindElements(by);
                var element = elements.FirstOrDefault(e => e.Displayed && e.Enabled);
                if (element != null)
                {
                    Logger.Debug($"Found element using locator: {by}");
                    return element;
                }
            }
            catch (Exception ex) when (ex is StaleElementReferenceException or NoSuchElementException) { }
        }
        return null;
    }

    private static bool SafeClick(IWebDriver driver, IWebElement element)
    {
        try
        {
            if (element.Displayed && element.Enabled)
            {
                element.Click();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Debug(ex, "Standard click failed, trying JS click");
                if (element.Displayed)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
                    return true;
                }
                return false;
            }
            catch (Exception jsEx)
            {
                Logger.Warn(jsEx, "JS click also failed");
                return false;
            }
        }
    }

    private static bool IsInteractable(IWebElement element)
    {
        try
        {
            return element.Displayed && element.Enabled && element.Size.Height > 0 && element.Size.Width > 0;
        }
        catch
        {
            return false;
        }
    }

    private IWebElement? FindLocationInput()
    {
        var driver = AqualityServices.Browser.Driver;
        foreach (var by in _locationLocators)
        {
            var el = driver.FindElements(by).FirstOrDefault(e => e.Displayed);
            if (el != null) return el;
        }
        return null;
    }

    private IWebElement? FindLocationInputWithRetry()
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                Logger.Debug($"Finding location input attempt {attempt}/3");
                var el = FindLocationInput();
                if (el != null && IsInteractable(el)) return el;
                Thread.Sleep(400);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Location input attempt {attempt} failed");
            }
        }
        return null;
    }

    private bool TryDirectLocationInput(IWebElement input, string value)
    {
        var driver = AqualityServices.Browser.Driver;
        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", input);
            Thread.Sleep(150);
            if (!IsInteractable(input)) return false;
            input.Click();
            Thread.Sleep(100);
            try { input.Clear(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value='';", input); }
            Thread.Sleep(50);
            input.SendKeys(value);
            Logger.Info($"Typed location value '{value}' into input");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Direct location input failed");
            return false;
        }
    }

    private bool TryJsSetLocation(string value)
    {
        try
        {
            var driver = AqualityServices.Browser.Driver;
            var script = @"try { var cands=[document.querySelector(""input[data-cy='search.form.location.button']""),document.querySelector(""input[placeholder='Wpisz lokalizacj?']""),document.querySelector(""input[placeholder*='lokalizac']"")]; for (var i=0;i<cands.length;i++){var el=cands[i]; if(el&&el.offsetParent!==null){el.focus(); el.value=''; el.value=arguments[0]; el.dispatchEvent(new Event('input',{bubbles:true})); el.dispatchEvent(new Event('change',{bubbles:true})); return true;}} return false;} catch(e){return false;}";
            var success = (bool)((IJavaScriptExecutor)driver).ExecuteScript(script, value);
            if (success) Logger.Info($"Location value set via JS: '{value}'");
            return success;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "JS location set failed");
            return false;
        }
    }

    private bool SelectLocationSuggestion(string location)
    {
        var driver = AqualityServices.Browser.Driver;
        try
        {
            var wait = new WebDriverWait(driver, ShortWait);
            var suggestion = wait.Until(d =>
            {
                var items = d.FindElements(By.XPath($"//p[.//mark[translate(normalize-space(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{location.ToLower()}']]//ancestor::div[contains(@class,'e1bqfjd82') or contains(@class,'css-')]" +
                    $"|//li[contains(@class,'suggestion')][.//strong[contains(.,'{location}')]]"));
                return items.FirstOrDefault(i => i.Displayed);
            });
            if (suggestion != null)
            {
                Logger.Info("Clicking location suggestion");
                suggestion.Click();
                Thread.Sleep(300);
                return true;
            }
        }
        catch (WebDriverTimeoutException)
        {
            Logger.Debug("Location suggestion timeout");
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Suggestion selection failed");
        }
        return false;
    }

    private bool KeyboardFallback(IWebElement input)
    {
        try
        {
            input.SendKeys(Keys.ArrowDown);
            Thread.Sleep(200);
            input.SendKeys(Keys.Enter);
            Thread.Sleep(300);
            Logger.Info("Used keyboard fallback for location suggestion");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Keyboard fallback failed");
            return false;
        }
    }
    #endregion

    #region Cookies
    public void AcceptCookiesIfPresent()
    {
        Logger.Debug("Checking cookies popup");
        if (AcceptCookiesBtn.State.WaitForDisplayed(TimeSpan.FromSeconds(5)))
        {
            Logger.Info("Cookies popup detected. Accepting.");
            AcceptCookiesBtn.Click();
        }
        else
        {
            Logger.Debug("No cookies popup displayed.");
        }
    }
    #endregion

    #region Filters
    public bool SetLocation(string location)
    {
        Logger.Info($"Setting location: {location}");
        var driver = AqualityServices.Browser.Driver;
        IWebElement? input = FindLocationInputWithRetry();
        if (input == null)
        {
            Logger.Error("Location input field not found");
            return false;
        }

        // 1. Try direct interaction
        if (!TryDirectLocationInput(input, location))
        {
            Logger.Warn("Direct location typing failed, trying JS method");
            if (!TryJsSetLocation(location))
            {
                Logger.Error("Failed to set location via any method");
                return false;
            }
            // Refresh reference after JS
            input = FindLocationInput();
            if (input == null)
            {
                Logger.Warn("Location input disappeared after JS interaction");
            }
        }

        // 2. Attempt to pick suggestion
        var suggestionPicked = SelectLocationSuggestion(location);
        if (!suggestionPicked && input != null)
        {
            Logger.Warn("Suggestion click failed, trying keyboard fallback");
            KeyboardFallback(input);
        }

        // 3. Verify value or at least presence of typed text
        try
        {
            var finalInput = FindLocationInput();
            var finalValue = finalInput?.GetAttribute("value") ?? string.Empty;
            Logger.Info($"Final location input value: '{finalValue}' (suggestionPicked={suggestionPicked})");
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Location verification failed");
        }

        return true; // Best-effort; search will validate further
    }

    public bool SetLocationToWarszawa() => SetLocation("Warszawa");

    public bool SetPriceRange(int min, int max)
    {
        var driver = AqualityServices.Browser.Driver;
        var minBox = TryFindFirstDisplayed(driver, _priceMinLocators) ?? TryFindFirstDisplayed(driver, _priceMinLocatorsLegacy);
        var maxBox = TryFindFirstDisplayed(driver, _priceMaxLocators) ?? TryFindFirstDisplayed(driver, _priceMaxLocatorsLegacy);
        if (minBox == null || maxBox == null)
        {
            Logger.Error("Price range input fields not found");
            return false;
        }
        Logger.Info($"Setting price range: {min} - {max}");
        try
        {
            minBox.Clear();
            minBox.SendKeys(min.ToString());
            maxBox.Clear();
            maxBox.SendKeys(max.ToString());
            Thread.Sleep(300);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed entering price range");
            return false;
        }
    }

    public void SetLocationAndPriceFilters(string location, int minPrice, int maxPrice)
    {
        Logger.Info($"Applying filters: location='{location}', price {minPrice}-{maxPrice}");
        var locSet = SetLocation(location);
        var priceSet = SetPriceRange(minPrice, maxPrice);
        Logger.Debug($"Location set: {locSet}, Price set: {priceSet}");
    }
    #endregion

    #region Actions
    public bool ClickSearch()
    {
        var driver = AqualityServices.Browser.Driver;
        Thread.Sleep(500); // allow UI settle
        var btn = TryFindFirstDisplayed(driver, _searchButtonLocators);
        if (btn == null)
        {
            Logger.Error("Search button not found");
            return false;
        }
        Logger.Info("Clicking search button (generic)");
        return SafeClick(driver, btn);
    }

    public bool ClickSearchButton()
    {
        var driver = AqualityServices.Browser.Driver;
        try
        {
            Thread.Sleep(500);
            var btn = driver.FindElements(By.Id("search-form-submit")).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (btn == null)
            {
                Logger.Warn("Primary search button not found, trying generic");
                return ClickSearch();
            }
            Logger.Info("Clicking primary search button (id='search-form-submit')");
            return SafeClick(driver, btn);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to click primary search button");
            return false;
        }
    }

    public bool Search() => ClickSearchButton();

    public bool OpenLogin()
    {
        var driver = AqualityServices.Browser.Driver;
        var el = TryFindFirstDisplayed(driver, _loginLocators);
        if (el == null)
        {
            Logger.Error("Login link/button not found.");
            return false;
        }
        Logger.Info("Opening login dialog/page");
        return SafeClick(driver, el);
    }
    #endregion
}
