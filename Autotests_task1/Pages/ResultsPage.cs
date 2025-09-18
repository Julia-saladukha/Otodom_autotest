using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using OpenQA.Selenium;
using Aquality.Selenium.Browsers;
using NLog;
using System.Text.RegularExpressions;
using Reqnroll;

namespace Autotests_task1.Pages;

public class ResultsPage : BasePage
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Updated selectors for current Otodom structure
    private ILabel ResultsHeader => ElementFactory.GetLabel(By.CssSelector("h1"), "Results header");
    
    // More comprehensive selectors for listings and prices
    private readonly IReadOnlyCollection<By> _listingSelectors = new List<By>
    {
        By.CssSelector("li[data-cy='listing-item']"),
        By.CssSelector("article[data-cy='listing-item']"),
        By.CssSelector("[data-cy='listing-item']"),
        By.CssSelector("[data-cy*='listing-item']"), // new wildcard selector
        By.CssSelector("li[data-testid='listing-item']"),
        By.CssSelector("article[data-testid='listing-item']"),
        By.CssSelector(".listing-item"),
        By.CssSelector("[class*='listing']"),
        By.XPath("//li[contains(@class,'listing') or contains(@data-cy,'listing')]") ,
        By.XPath("//article[contains(@class,'listing') or contains(@data-cy,'listing')]") ,
        By.XPath("//div[contains(@data-cy,'listing-item')]") // fallback div container
    };

    private readonly IReadOnlyCollection<By> _priceSelectors = new List<By>
    {
        By.CssSelector("[data-cy='listing-item'] span[aria-label='price']"),
        By.CssSelector("[data-cy*='listing-item'] [data-cy='price']"),
        By.CssSelector("[data-cy*='listing-item'] .price"),
        By.CssSelector("[data-cy*='listing-item'] span[class*='price']"),
        By.XPath("//li[contains(@data-cy,'listing-item')]//span[contains(.,'z') or contains(.,'PLN')]"),
        By.XPath("//*[@data-cy='listing-item']//*[contains(text(),'z') or contains(text(),'PLN')]")
    };

    private IButton ClearPriceButton => ElementFactory.GetButton(By.CssSelector("button[data-testid='clear-price']"), "Clear price");
    private ITextBox SurfaceFrom => ElementFactory.GetTextBox(By.CssSelector("input[name='surfaceMin']"), "Surface from");
    private ITextBox SurfaceTo => ElementFactory.GetTextBox(By.CssSelector("input[name='surfaceMax']"), "Surface to");
    private IButton SearchButton => ElementFactory.GetButton(By.CssSelector("button[type='submit']"), "Search");

    public ResultsPage() : base(By.CssSelector("main"), "Results Page") { }

    public bool IsOpened() => ResultsHeader.State.IsDisplayed;

    public bool WaitForResultsToLoad(TimeSpan? timeout = null)
    {
        var to = timeout ?? TimeSpan.FromSeconds(25);
        Logger.Info("Waiting for results (listings) to load via WaitForResultsToLoad...");
        return AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                var drv = AqualityServices.Browser.Driver;
                // Use simplified selectors required by task first
                if (drv.FindElements(By.CssSelector("[data-cy*='listing-item']")).Any(e => e.Displayed)) return true;
                if (drv.FindElements(By.XPath("//div[contains(@data-cy,'listing-item')]")).Any(e => e.Displayed)) return true;
                // fallback to full list
                return _listingSelectors.Any(sel => drv.FindElements(sel).Any(el => el.Displayed));
            }
            catch { return false; }
        }, to);
    }

    public bool WaitForListings(TimeSpan? timeout = null)
    {
        var to = timeout ?? TimeSpan.FromSeconds(25);
        Logger.Info("Waiting for listings to load...");
        
        var found = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var offers = GetOffers();
            Logger.Debug($"Found {offers.Count} offers using current selectors");
            return offers.Count > 0;
        }, to);
        
        if (!found)
        {
            Logger.Warn("No listings found. Logging page info for debugging:");
            try
            {
                var url = AqualityServices.Browser.CurrentUrl;
                Logger.Info($"Current URL: {url}");
                
                var driver = AqualityServices.Browser.Driver;
                var pageTitle = driver.Title;
                Logger.Info($"Page title: {pageTitle}");
                
                foreach (var selector in _listingSelectors)
                {
                    try
                    {
                        var elements = driver.FindElements(selector);
                        Logger.Info($"Selector '{selector}' found {elements.Count} elements");
                        if (elements.Count > 0)
                        {
                            var firstElement = elements.First();
                            var elementText = firstElement.Text ?? "";
                            var text = elementText.Length > 100 ? elementText.Substring(0, 100) : elementText;
                            Logger.Info($"First element text sample: '{text}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Error with selector '{selector}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to gather debugging info");
            }
        }
        
        return found;
    }

    public bool WaitForPriceData(TimeSpan? timeout = null)
    {
        var to = timeout ?? TimeSpan.FromSeconds(15);
        return AqualityServices.ConditionalWait.WaitFor(() => GetAllPrices().Any(), to);
    }

    // Existing lightweight price extraction retained for backward compatibility
    public IEnumerable<int> GetPrices()
    {
        var prices = new List<int>();
        var driver = AqualityServices.Browser.Driver;
        
        foreach (var selector in _priceSelectors)
        {
            try
            {
                var elements = driver.FindElements(selector);
                foreach (var element in elements)
                {
                    try
                    {
                        var raw = element.Text;
                        var text = raw.Replace("\u00A0", "");
                        var digits = new string(text.Where(char.IsDigit).ToArray());
                        if (int.TryParse(digits, out var value))
                        {
                            prices.Add(value);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        return prices;
    }

    // New robust price extraction method required by task
    public List<int> GetAllPrices()
    {
        var result = new List<int>();
        var driver = AqualityServices.Browser.Driver;
        foreach (var selector in _priceSelectors)
        {
            try
            {
                foreach (var el in driver.FindElements(selector))
                {
                    var raw = el.Text ?? string.Empty;
                    var digits = Regex.Replace(raw, "[^0-9]", "");
                    if (digits.Length == 0) continue;
                    if (int.TryParse(digits, out var val)) result.Add(val);
                }
            }
            catch { }
        }
        Logger.Info($"Collected {result.Count} price values: {(result.Count>0 ? string.Join(", ", result.Take(10)) + (result.Count>10?"...":"") : "<none>")}");
        return result;
    }

    public bool AreAllPricesWithinRange(int min, int max)
    {
        var prices = GetAllPrices();
        Logger.Info($"Validating prices within range {min}-{max}. Prices: {(prices.Count>0?string.Join(", ", prices):"<none>")}");
        return prices.Count > 0 && prices.All(p => p >= min && p <= max);
    }

    public IEnumerable<double> GetSurfaces()
    {
        var surfaces = new List<double>();
        var driver = AqualityServices.Browser.Driver;
        var surfaceSelectors = new[]
        {
            By.CssSelector("[data-cy='listing-item'] span[aria-label='area']"),
            By.XPath("//li[@data-cy='listing-item']//span[contains(.,'m²')]")
        };

        foreach (var selector in surfaceSelectors)
        {
            try
            {
                var elements = driver.FindElements(selector);
                foreach (var element in elements)
                {
                    try
                    {
                        var text = element.Text.Replace("m²", "").Replace(",", ".").Trim();
                        if (double.TryParse(new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray()), out var value))
                        {
                            surfaces.Add(value);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        return surfaces;
    }

    // New surface methods required by task
    public List<int> GetAllSurfaces()
    {
        var list = new List<int>();
        var driver = AqualityServices.Browser.Driver;
        var surfaceCandidates = driver.FindElements(By.XPath("//*[contains(text(),'m²') or contains(text(),'m2')]")).Where(e => e.Displayed).Take(200);
        var regex = new Regex(@"(\d+)[\s\u00A0]*m[²2]", RegexOptions.IgnoreCase);
        foreach (var el in surfaceCandidates)
        {
            try
            {
                var text = el.Text;
                var match = regex.Match(text);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var val))
                {
                    list.Add(val);
                }
            }
            catch { }
        }
        Logger.Info($"Collected {list.Count} surface values: {(list.Count>0?string.Join(", ", list.Take(10)) + (list.Count>10?"...":"") : "<none>")}");
        return list;
    }

    public (double min, double max) GetSurfaceRangeFromFirstPage()
    {
        var ints = GetAllSurfaces();
        if (ints.Count == 0)
        {
            Logger.Warn("No surface data found when deriving range; returning (0,0)");
            return (0,0);
        }
        var min = (double)ints.Min();
        var max = (double)ints.Max();
        Logger.Info($"Derived surface range from first page (wrapper): {min}-{max}");
        return (min, max);
    }

    public bool AreAllSurfacesWithinRange(double min, double max)
    {
        var surfaces = GetSurfaces().ToList();
        Logger.Info($"Validating {surfaces.Count} surfaces within range {min}-{max}");
        return surfaces.Count > 0 && surfaces.All(s => s >= min && s <= max);
    }

    public void ClearPriceFilter()
    {
        if (ClearPriceButton.State.IsDisplayed)
        {
            Logger.Info("Clearing price filter");
            ClearPriceButton.Click();
        }
        else
        {
            Logger.Info("Clear price button not visible - price filter might already be cleared");
        }
    }

    public void SetSurfaceRange(double min, double max)
    {
        Logger.Info($"Setting surface range {min} - {max}");
        SurfaceFrom.ClearAndType(((int)min).ToString());
        SurfaceTo.ClearAndType(((int)Math.Ceiling(max)).ToString());
    }

    public void Search() => SearchButton.Click();

    // Use raw IWebElement instead of IElement to avoid dictionary issues
    public IReadOnlyCollection<IWebElement> GetOffers() 
    {
        var driver = AqualityServices.Browser.Driver;
        foreach (var selector in _listingSelectors)
        {
            try
            {
                var elements = driver.FindElements(selector);
                if (elements.Count > 0)
                {
                    Logger.Debug($"Found {elements.Count} offers using selector: {selector}");
                    return elements;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error with selector '{selector}': {ex.Message}");
            }
        }
        Logger.Debug("No offers found with any selector");
        return new List<IWebElement>();
    }

    public (int? price, double? surface, int? rooms) PickRandomOfferAndOpen()
    {
        var offers = GetOffers();
        if (offers.Count == 0)
        {
            Logger.Warn("No offers found to pick");
            return (null, null, null);
        }
        var rnd = new Random();
        var selected = offers.ElementAt(rnd.Next(offers.Count));
        var textLines = selected.Text.Split('\n');
        string? priceText = textLines.FirstOrDefault(t => t.Contains("z"));
        string? surfaceText = textLines.FirstOrDefault(t => t.Contains("m²") || t.Contains("m2"));
        string? roomsText = textLines.FirstOrDefault(t => t.Contains("pok") || t.Contains("room"));

        int? price = int.TryParse(new string((priceText ?? string.Empty).Where(char.IsDigit).ToArray()), out var p) ? p : null;
        double? surface = double.TryParse(new string((surfaceText ?? string.Empty).Replace(",",".").Where(c=>char.IsDigit(c)||c=='.').ToArray()), out var s) ? s : null;
        int? rooms = int.TryParse(new string((roomsText ?? string.Empty).Where(char.IsDigit).ToArray()), out var r) ? r : null;

        Logger.Info($"Selected random offer with parsed values -> Price: {price}, Surface: {surface}, Rooms: {rooms}");

        try { selected.Click(); }
        catch { ((IJavaScriptExecutor)AqualityServices.Browser.Driver).ExecuteScript("arguments[0].click();", selected); }

        return (price, surface, rooms);
    }

    // New method required by task; stores details in ScenarioContext.
    public void OpenRandomOfferAndReturnDetails(ScenarioContext scenarioContext, string contextKey = "RandomOfferDetails")
    {
        var offers = GetOffers();
        if (offers.Count == 0)
        {
            Logger.Warn("No offers available to open.");
            return;
        }
        var rnd = new Random();
        var selected = offers.ElementAt(rnd.Next(offers.Count));
        var text = selected.Text ?? string.Empty;

        int? ExtractInt(string pattern)
        {
            var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(Regex.Replace(m.Groups[1].Value, "[^0-9]", ""), out var v)) return v; return null;
        }

        // Price (allow PLN or currency symbol anywhere)
        var price = ExtractInt(@"([0-9][0-9 .\u00A0,]{2,})\s*(PLN|z|z?)?");
        // Surface (m²/m2)
        var surface = ExtractInt(@"(\d+)\s*m[²2]");
        // Rooms (looking for e.g., '3 pok' or '3 pokoje')
        var rooms = ExtractInt(@"(\d+)\s*(pok|pokoje|rooms?)");

        var details = new { price, surface, rooms };
        scenarioContext[contextKey] = details; // ScenarioContext usage: simple key-value store per scenario
        Logger.Info($"Stored random offer details in ScenarioContext under key '{contextKey}': Price={price}, Surface={surface}, Rooms={rooms}");

        try { selected.Click(); }
        catch { ((IJavaScriptExecutor)AqualityServices.Browser.Driver).ExecuteScript("arguments[0].click();", selected); }
    }
}
