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
        By.CssSelector("input[data-cy='search.form.location.button']"), // new explicit preferred locator
        By.CssSelector("input[name='location']"),
        By.CssSelector("input[placeholder*='Lokal']"),
        By.CssSelector("input[placeholder*='lokal']"),
        By.CssSelector("input[placeholder*='Miejsc']"),
        By.CssSelector("input[aria-label*='Lokal']"),
        By.XPath("//input[contains(translate(@placeholder,'LOKALM','lokalm'),'lokal') or contains(translate(@placeholder,'MIEJSC','miejsc'),'miejsc')]")
    };

    private readonly IReadOnlyCollection<By> _priceMinLocators = new List<By>
    {
        By.CssSelector("input[name='priceMin']"),
        By.CssSelector("input[id*='priceFrom']"),
        By.XPath("//input[contains(@placeholder,'od') and (contains(@placeholder,'Cena') or contains(@aria-label,'Cena'))]")
    };

    private readonly IReadOnlyCollection<By> _priceMaxLocators = new List<By>
    {
        By.CssSelector("input[name='priceMax']"),
        By.CssSelector("input[id*='priceTo']"),
        By.XPath("//input[contains(@placeholder,'do') and (contains(@placeholder,'Cena') or contains(@aria-label,'Cena'))]")
    };

    private readonly IReadOnlyCollection<By> _searchButtonLocators = new List<By>
    {
        By.Id("search-form-submit"),
        By.CssSelector("button[id='search-form-submit']"),
        By.CssSelector("button[type='submit']"),
        By.CssSelector("button[data-cy='search-button']"),
        By.CssSelector("button[data-cy='homepage.search.submit.button']"), // Additional potential locator
        By.CssSelector("form button[type='submit']"), // Generic form submit
        By.XPath("//button[contains(.,'Szukaj') or contains(.,'Search')]"),
        By.XPath("//button[contains(@class,'search') or contains(@class,'submit')]") // Class-based fallback
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
    public bool SetLocation(string location)
    {
        var driver = AqualityServices.Browser.Driver;
        var input = TryFindFirstDisplayed(driver, _locationLocators);
        if (input == null)
        {
            Logger.Warn("Location input not found with any known locator.");
            return false;
        }
        Logger.Info($"Setting location: {location}");
        input.Clear();
        input.SendKeys(location);
        return true;
    }

    // Explicit location setter using data-cy attribute and pressing Enter to confirm suggestion.
    public bool SetLocationWithEnter(string location)
    {
        var driver = AqualityServices.Browser.Driver;
        var input = driver.FindElements(By.CssSelector("input[data-cy='search.form.location.button']"))
            .FirstOrDefault(e => e.Displayed && e.Enabled);
        if (input == null)
        {
            Logger.Warn("Explicit location input with data-cy not found; falling back to generic SetLocation");
            return SetLocation(location);
        }
        Logger.Info($"Setting location (ENTER confirm): {location}");
        try
        {
            if (!string.IsNullOrEmpty(input.GetAttribute("value"))) input.Clear();
            input.SendKeys(location);
            // Wait for suggestions to appear
            Thread.Sleep(1000);
            input.SendKeys(Keys.Enter);
            // Wait until value reflects selection (best-effort)
            AqualityServices.ConditionalWait.WaitFor(() =>
                (input.GetAttribute("value") ?? string.Empty).Contains(location, StringComparison.OrdinalIgnoreCase),
                timeout: TimeSpan.FromSeconds(5));
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to set location with ENTER, fallback to SetLocation");
            return SetLocation(location);
        }
    }

    public bool SetPriceRange(int min, int max)
    {
        var driver = AqualityServices.Browser.Driver;
        var minBox = TryFindFirstDisplayed(driver, _priceMinLocators);
        var maxBox = TryFindFirstDisplayed(driver, _priceMaxLocators);
        if (minBox == null || maxBox == null)
        {
            Logger.Warn("Price range inputs not both found. Skipping explicit price filter.");
            return false;
        }
        Logger.Info($"Setting price range: {min} - {max}");
        minBox.Clear();
        minBox.SendKeys(min.ToString());
        maxBox.Clear();
        maxBox.SendKeys(max.ToString());
        return true;
    }

    public void SetLocationAndPriceFilters(string location, int minPrice, int maxPrice)
    {
        Logger.Info($"Applying filters: location='{location}', price {minPrice}-{maxPrice}");
        var locSet = SetLocationWithEnter(location); // use explicit method for reliability
        var priceSet = SetPriceRange(minPrice, maxPrice);
        Logger.Debug($"Location set: {locSet}, Price set: {priceSet}");
    }
    #endregion

    #region Actions
    public bool ClickSearch()
    {
        var driver = AqualityServices.Browser.Driver;
        
        // Wait a bit for any dynamic content to settle
        Thread.Sleep(1000);
        
        var btn = TryFindFirstDisplayed(driver, _searchButtonLocators);
        if (btn == null)
        {
            Logger.Error("Search button not found using provided locators. Logging all buttons on page:");
            try
            {
                var allButtons = driver.FindElements(By.TagName("button"));
                foreach (var button in allButtons.Take(10)) // Log first 10 buttons
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
