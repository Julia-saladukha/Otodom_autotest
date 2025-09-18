using Aquality.Selenium.Browsers;
using Aquality.Selenium.Elements;
using Aquality.Selenium.Elements.Interfaces;
using NLog;
using OpenQA.Selenium;

namespace Autotests_task1.Pages;

public class MainPage : BasePage
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string SiteUrl = "https://www.otodom.pl/";
    private static readonly TimeSpan DefaultUiWait = TimeSpan.FromSeconds(10);

    public MainPage() : base(By.CssSelector("body"), "Main Page") { }

    #region Static/Direct Elements
    private IButton AcceptCookiesBtn => ElementFactory.GetButton(By.Id("onetrust-accept-btn-handler"), "Cookies Accept");
    #endregion

    #region Dynamic Locator Pools with Extended Search Button Options
    private readonly IReadOnlyCollection<By> _locationLocators = new List<By>
    {
        By.CssSelector("input[data-cy='search.form.location.button']"),
        By.CssSelector("input[name='location']"),
        By.CssSelector("input[placeholder*='Lokal']"),
        By.CssSelector("input[placeholder*='lokal']"),
        By.CssSelector("input[placeholder*='Miejsc']"),
        By.CssSelector("input[aria-label*='Lokal']"),
        By.XPath("//input[contains(translate(@placeholder,'LOKALM','lokalm'),'lokal') or contains(translate(@placeholder,'MIEJSC','miejsc'),'miejsc')]")
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

    // Explicit selectors requested in task description
    private readonly IReadOnlyCollection<By> _priceMinLocators = new List<By>
    {
        By.Id("priceFrom"),
        By.CssSelector("input[name*='priceFrom']"),
        By.CssSelector("input[name*='price_from']"),
        By.CssSelector("input[id*='priceFrom']"),
        By.CssSelector("input[id*='price_from']"),
        By.CssSelector("input[name='priceMin']"),
        By.CssSelector("input[name='price_min']")
    };

    private readonly IReadOnlyCollection<By> _priceMaxLocators = new List<By>
    {
        By.Id("priceTo"),
        By.CssSelector("input[name*='priceTo']"),
        By.CssSelector("input[name*='price_to']"),
        By.CssSelector("input[id*='priceTo']"),
        By.CssSelector("input[id*='price_to']"),
        By.CssSelector("input[name='priceMax']"),
        By.CssSelector("input[name='price_max']")
    };

    private readonly IReadOnlyCollection<By> _searchButtonLocators = new List<By>
    {
        By.Id("search-form-submit"),
        By.CssSelector("button[id='search-form-submit']"),
        By.CssSelector("button[type='submit']"),
        By.CssSelector("button[data-cy='search-button']"),
        By.CssSelector("button[data-cy='homepage.search.submit.button']"),
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
            catch (StaleElementReferenceException)
            {
                Logger.Debug($"Stale element with locator: {by}");
            }
            catch (NoSuchElementException)
            {
                Logger.Debug($"No element found with locator: {by}");
            }
        }
        return null;
    }

    private static bool SafeClick(IWebDriver driver, IWebElement element)
    {
        try { element.Click(); return true; }
        catch (Exception ex)
        {
            try
            {
                Logger.Debug(ex, "Standard click failed, trying JS click");
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
                return true;
            }
            catch (Exception jsEx)
            {
                Logger.Warn(jsEx, "JS click also failed");
                return false;
            }
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
    // New required implementation: focuses only on explicit data-cy selector, waits for suggestions, presses Enter.
    public bool SetLocation(string location)
    {
        var driver = AqualityServices.Browser.Driver;
        Logger.Info($"Setting location: {location}");
        IWebElement? input = null;
        try
        {
            input = driver.FindElements(By.CssSelector("input[data-cy='search.form.location.button']"))
                .FirstOrDefault(e => e.Displayed && e.Enabled);
            if (input == null)
            {
                Logger.Warn("Location input with required data-cy attribute not found.");
                return false;
            }
            if (!string.IsNullOrEmpty(input.GetAttribute("value")))
            {
                input.Clear();
            }
            input.SendKeys(location);
            // Wait for suggestion dropdown/listbox
            AqualityServices.ConditionalWait.WaitFor(() =>
            {
                try
                {
                    return driver.FindElements(By.CssSelector("[role='listbox']")).Any(lb => lb.Displayed) ||
                           driver.FindElements(By.CssSelector("[role='option']")).Any(opt => opt.Displayed) ||
                           driver.FindElements(By.XPath("//li[contains(@id,'react-select') or contains(@class,'option')]")).Any(el => el.Displayed);
                }
                catch { return false; }
            }, timeout: TimeSpan.FromSeconds(5));
            input.SendKeys(Keys.Enter);
            // Best-effort confirmation that chosen value applied
            AqualityServices.ConditionalWait.WaitFor(() =>
            {
                try
                {
                    var val = input.GetAttribute("value") ?? string.Empty;
                    return val.Contains(location, StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            }, timeout: TimeSpan.FromSeconds(5));
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to set location using explicit method");
            return false;
        }
    }

    // Retain previous explicit method but delegate to new SetLocation for consistency.
    public bool SetLocationWithEnter(string location) => SetLocation(location);

    public bool SetLocationToWarszawa() => SetLocation("Warszawa");

    public bool SetPriceRange(int min, int max)
    {
        var driver = AqualityServices.Browser.Driver;
        var minBox = TryFindFirstDisplayed(driver, _priceMinLocators) ?? TryFindFirstDisplayed(driver, _priceMinLocatorsLegacy);
        var maxBox = TryFindFirstDisplayed(driver, _priceMaxLocators) ?? TryFindFirstDisplayed(driver, _priceMaxLocatorsLegacy);
        if (minBox == null || maxBox == null)
        {
            Logger.Warn("Price range inputs not both found. Skipping explicit price filter.");
            return false;
        }
        Logger.Info($"Setting price range: {min} - {max}");
        try
        {
            minBox.Clear();
            minBox.SendKeys(min.ToString());
            maxBox.Clear();
            maxBox.SendKeys(max.ToString());
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
        Thread.Sleep(1000); // allow UI settle
        var btn = TryFindFirstDisplayed(driver, _searchButtonLocators);
        if (btn == null)
        {
            Logger.Error("Search button not found using provided locators. Logging sample buttons.");
            try
            {
                var allButtons = driver.FindElements(By.TagName("button"));
                foreach (var button in allButtons.Take(10))
                {
                    try
                    {
                        var id = button.GetAttribute("id") ?? "no-id";
                        var classes = button.GetAttribute("class") ?? "no-class";
                        var text = button.Text ?? "no-text";
                        var type = button.GetAttribute("type") ?? "no-type";
                        Logger.Info($"Button found: id='{id}', class='{classes}', text='{text}', type='{type}'");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to enumerate buttons");
            }
            return false;
        }
        Logger.Info("Found and clicking Search button");
        return SafeClick(driver, btn);
    }

    public bool ClickSearchButton()
    {
        var driver = AqualityServices.Browser.Driver;
        try
        {
            var btn = driver.FindElements(By.Id("search-form-submit")).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (btn == null)
            {
                Logger.Warn("Search button with id 'search-form-submit' not found.");
                return false;
            }
            Logger.Info("Clicking primary search button (id='search-form-submit')");
            if (!SafeClick(driver, btn)) return false;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to click search button by id");
            return false;
        }
    }

    public bool Search() => ClickSearch();

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
