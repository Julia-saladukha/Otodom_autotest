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

    // Explicit selectors requested in task description - Enhanced with more options
    private readonly IReadOnlyCollection<By> _priceMinLocators = new List<By>
    {
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
                if (element.Displayed) // Check if element still exists
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
        var mainLocator = By.CssSelector("input[data-cy='search.form.location.button']");
        try
        {
            Logger.Info("Locating location input by primary locator");
            var input = driver.FindElements(mainLocator).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (input == null)
            {
                Logger.Warn("Primary location input not found, aborting (per specification no fallbacks used here)");
                return false;
            }

            Logger.Info("Clearing location input");
            try { input.Clear(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value='';", input); }

            Logger.Info($"Typing location value: {location}");
            input.SendKeys(location);

            Logger.Info("Sending ENTER to select first suggestion");
            input.SendKeys(Keys.Enter);

            Logger.Info("Waiting briefly for suggestion to apply");
            Thread.Sleep(800); // brief wait to allow selection to register

            // Optional: verify the value stuck
            var currentValue = input.GetAttribute("value") ?? string.Empty;
            Logger.Info($"Location input current value after ENTER: '{currentValue}'");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to set location");
            return false;
        }
    }

    public bool SetLocationToWarszawa() => SetLocation("Warszawa");

    public bool SetPriceRange(int min, int max)
    {
        var driver = AqualityServices.Browser.Driver;
        var minBox = TryFindFirstDisplayed(driver, _priceMinLocators) ?? TryFindFirstDisplayed(driver, _priceMinLocatorsLegacy);
        var maxBox = TryFindFirstDisplayed(driver, _priceMaxLocators) ?? TryFindFirstDisplayed(driver, _priceMaxLocatorsLegacy);
        if (minBox == null || maxBox == null)
        {
            Logger.Warn($"Price range inputs not found. MinBox: {minBox != null}, MaxBox: {maxBox != null}");
            return false;
        }
        Logger.Info($"Setting price range: {min} - {max}");
        try
        {
            minBox.Clear();
            minBox.SendKeys(min.ToString());
            maxBox.Clear();
            maxBox.SendKeys(max.ToString());
            Thread.Sleep(500); // Allow form to process input
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
            Logger.Error("Search button not found using provided locators. Logging available buttons:");
            try
            {
                var allButtons = driver.FindElements(By.TagName("button"));
                Logger.Info($"Total buttons found on page: {allButtons.Count}");
                foreach (var button in allButtons.Take(10))
                {
                    try
                    {
                        var id = button.GetAttribute("id") ?? "no-id";
                        var classes = button.GetAttribute("class") ?? "no-class";
                        var text = button.Text ?? "no-text";
                        var type = button.GetAttribute("type") ?? "no-type";
                        var dataCy = button.GetAttribute("data-cy") ?? "no-data-cy";
                        Logger.Info($"Button: id='{id}', class='{classes}', text='{text}', type='{type}', data-cy='{dataCy}'");
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
        Logger.Info($"Found search button, attempting to click");
        return SafeClick(driver, btn);
    }

    public bool ClickSearchButton()
    {
        var driver = AqualityServices.Browser.Driver;
        try
        {
            // Wait a moment for any dynamic content
            Thread.Sleep(1000);
            var btn = driver.FindElements(By.Id("search-form-submit")).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (btn == null)
            {
                Logger.Warn("Search button with id 'search-form-submit' not found or not enabled.");
                var searchButtons = driver.FindElements(By.XPath("//button[contains(@id,'search') or contains(@class,'search') or contains(.,'Szukaj')]"));
                Logger.Info($"Found {searchButtons.Count} potential search buttons:");
                foreach (var searchBtn in searchButtons.Take(5))
                {
                    try
                    {
                        var id = searchBtn.GetAttribute("id") ?? "no-id";
                        var classes = searchBtn.GetAttribute("class") ?? "no-class";
                        var text = searchBtn.Text ?? "no-text";
                        Logger.Info($"Alternative search button: id='{id}', class='{classes}', text='{text}', displayed={searchBtn.Displayed}, enabled={searchBtn.Enabled}");
                    }
                    catch { }
                }
                return false;
            }
            Logger.Info("Clicking primary search button (id='search-form-submit')");
            return SafeClick(driver, btn);
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
