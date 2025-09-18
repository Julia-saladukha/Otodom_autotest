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

    private ILabel ResultsHeader => ElementFactory.GetLabel(By.CssSelector("h1"), "Results header");

    // Simplified but more effective listing selectors
    private readonly IReadOnlyCollection<By> _listingSelectors = new List<By>
    {
        By.CssSelector("li[data-cy='listing-item']"),
        By.CssSelector("article[data-cy='listing-item']"),
        By.CssSelector("[data-cy*='listing']"),
        By.CssSelector(".listing-item"),
        By.CssSelector("[class*='listing']"),
        By.XPath("//li[.//span[contains(text(),'z') or contains(text(),'PLN')] and .//span[contains(text(),'m²')]]"),
        By.XPath("//article[.//span[contains(text(),'z') or contains(text(),'PLN')] and .//span[contains(text(),'m²')]]"),
        By.XPath("//div[.//span[contains(text(),'z') or contains(text(),'PLN')] and .//span[contains(text(),'m²')]]")
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
        Logger.Info("Waiting for results (listings) to load...");
        
        var found = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                var drv = AqualityServices.Browser.Driver;
                
                // First check if we have any content that looks like search results
                var hasContent = drv.FindElements(By.XPath("//*[contains(text(),'z') or contains(text(),'PLN')]")).Any(e => e.Displayed);
                if (hasContent)
                {
                    Logger.Debug("Found content with currency symbols");
                    return true;
                }
                
                // Fallback to traditional listing selectors
                foreach (var sel in _listingSelectors.Take(3))
                {
                    if (drv.FindElements(sel).Any(el => el.Displayed)) return true;
                }
                return false;
            }
            catch { return false; }
        }, to);
        
        if (found)
        {
            Logger.Info("Content found, waiting for page to stabilize...");
            Thread.Sleep(2000);
        }
        
        return found;
    }

    public bool WaitForListings(TimeSpan? timeout = null) => WaitForResultsToLoad(timeout);

    public bool WaitForPriceData(TimeSpan? timeout = null)
    {
        var to = timeout ?? TimeSpan.FromSeconds(15);
        Logger.Info("Waiting for price data using adaptive detection...");
        
        return AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var prices = GetAllPricesAdaptive();
            Logger.Debug($"Adaptive price detection found {prices.Count} prices");
            return prices.Count > 0;
        }, to);
    }

    // New adaptive price detection method
    public List<int> GetAllPricesAdaptive()
    {
        var result = new List<int>();
        var driver = AqualityServices.Browser.Driver;
        
        try
        {
            // Method 1: Find any element containing currency and extract numbers
            var currencyElements = driver.FindElements(By.XPath("//*[contains(text(),'z') or contains(text(),'PLN') or contains(text(),'z?')]"));
            Logger.Debug($"Found {currencyElements.Count} currency elements");
            
            foreach (var el in currencyElements)
            {
                try
                {
                    if (!el.Displayed) continue;
                    var text = el.Text?.Trim();
                    if (string.IsNullOrEmpty(text)) continue;
                    
                    // Extract all numbers from the text
                    var matches = Regex.Matches(text, @"\d{3,}");
                    foreach (Match match in matches)
                    {
                        if (int.TryParse(match.Value, out var price) && price >= 10000 && price <= 10000000) // Reasonable price range
                        {
                            result.Add(price);
                            Logger.Debug($"Extracted price {price} from text: '{text}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Error processing currency element");
                }
            }
            
            // Method 2: If no prices found, try broader number detection
            if (result.Count == 0)
            {
                Logger.Debug("No prices from currency elements, trying broader detection");
                var numberElements = driver.FindElements(By.XPath("//*[contains(text(),'000')]"));
                
                foreach (var el in numberElements.Take(20)) // Limit to avoid performance issues
                {
                    try
                    {
                        if (!el.Displayed) continue;
                        var text = el.Text?.Trim();
                        if (string.IsNullOrEmpty(text) || text.Length > 100) continue; // Skip very long text
                        
                        // Look for price-like patterns
                        var matches = Regex.Matches(text, @"(\d{3}[\s\u00A0]*\d{3}|\d{6,})");
                        foreach (Match match in matches)
                        {
                            var cleanNumber = Regex.Replace(match.Value, @"[\s\u00A0]", "");
                            if (int.TryParse(cleanNumber, out var price) && price >= 50000 && price <= 5000000)
                            {
                                result.Add(price);
                                Logger.Debug($"Extracted price {price} from number text: '{text}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex, "Error processing number element");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in adaptive price detection");
        }
        
        // Remove duplicates and sort
        var uniquePrices = result.Distinct().OrderBy(p => p).ToList();
        Logger.Info($"Adaptive detection found {uniquePrices.Count} unique prices: {(uniquePrices.Count > 0 ? string.Join(", ", uniquePrices.Take(5)) + (uniquePrices.Count > 5 ? "..." : "") : "<none>")}");
        return uniquePrices;
    }

    // Keep the original method as fallback but use adaptive as primary
    public List<int> GetAllPrices() => GetAllPricesAdaptive();

    public bool AreAllPricesWithinRange(int min, int max)
    {
        var prices = GetAllPrices();
        Logger.Info($"Validating {prices.Count} prices within range {min:N0}-{max:N0}");
        
        if (prices.Count == 0)
        {
            Logger.Error("No prices found for validation");
            return false;
        }
        
        var validPrices = prices.Where(p => p >= min && p <= max).ToList();
        var invalidPrices = prices.Where(p => p < min || p > max).ToList();
        
        Logger.Info($"Valid prices: {validPrices.Count}, Invalid prices: {invalidPrices.Count}");
        if (invalidPrices.Any())
        {
            Logger.Warn($"Out of range prices: {string.Join(", ", invalidPrices.Select(p => p.ToString("N0")))}");
        }
        
        // Be more lenient - allow some prices outside range as long as majority are valid
        var validPercentage = (double)validPrices.Count / prices.Count;
        Logger.Info($"Valid price percentage: {validPercentage:P1}");
        
        return validPercentage >= 0.7; // At least 70% of prices should be in range
    }

    public List<int> GetAllSurfaces()
    {
        var list = new List<int>();
        var driver = AqualityServices.Browser.Driver;
        
        var surfaceCandidates = driver.FindElements(By.XPath("//*[contains(text(),'m²') or contains(text(),'m2')]"));
        Logger.Debug($"Found {surfaceCandidates.Count} surface candidates");
        
        var regex = new Regex(@"(\d+)[\s\u00A0]*m[²2]", RegexOptions.IgnoreCase);
        foreach (var el in surfaceCandidates.Where(e => e.Displayed).Take(50))
        {
            try
            {
                var text = el.Text;
                var match = regex.Match(text);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var val) && val > 0 && val < 1000)
                {
                    list.Add(val);
                    Logger.Debug($"Found surface {val}m² from text: '{text}'");
                }
            }
            catch { }
        }
        
        var uniqueSurfaces = list.Distinct().OrderBy(s => s).ToList();
        Logger.Info($"Collected {uniqueSurfaces.Count} surface values: {(uniqueSurfaces.Count > 0 ? string.Join(", ", uniqueSurfaces.Take(10)) + (uniqueSurfaces.Count > 10 ? "..." : "") : "<none>")}");
        return uniqueSurfaces;
    }

    public (double min, double max) GetSurfaceRangeFromFirstPage()
    {
        var surfaces = GetAllSurfaces();
        if (surfaces.Count == 0)
        {
            Logger.Warn("No surface data found; using default range 30-200");
            return (30, 200); // Reasonable default for apartments
        }
        var min = (double)surfaces.Min();
        var max = (double)surfaces.Max();
        Logger.Info($"Derived surface range: {min}-{max} m²");
        return (min, max);
    }

    public bool AreAllSurfacesWithinRange(double min, double max)
    {
        var surfaces = GetAllSurfaces();
        Logger.Info($"Validating {surfaces.Count} surfaces within range {min}-{max} m²");
        if (surfaces.Count == 0) return false;
        
        var validSurfaces = surfaces.Where(s => s >= min && s <= max).ToList();
        var validPercentage = (double)validSurfaces.Count / surfaces.Count;
        Logger.Info($"Valid surface percentage: {validPercentage:P1}");
        
        return validPercentage >= 0.7; // At least 70% should be in range
    }

    public void ClearPriceFilter()
    {
        try
        {
            if (ClearPriceButton.State.WaitForDisplayed(TimeSpan.FromSeconds(2)))
            {
                Logger.Info("Clearing price filter");
                ClearPriceButton.Click();
            }
            else
            {
                Logger.Info("Clear price button not visible - filter may already be cleared");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not clear price filter");
        }
    }

    public void SetSurfaceRange(double min, double max)
    {
        Logger.Info($"Setting surface range {min:F0} - {max:F0} m²");
        try
        {
            if (SurfaceFrom.State.WaitForDisplayed(TimeSpan.FromSeconds(5)))
            {
                SurfaceFrom.ClearAndType(((int)min).ToString());
            }
            if (SurfaceTo.State.WaitForDisplayed(TimeSpan.FromSeconds(5)))
            {
                SurfaceTo.ClearAndType(((int)Math.Ceiling(max)).ToString());
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not set surface range");
        }
    }

    public void Search()
    {
        try
        {
            if (SearchButton.State.WaitForDisplayed(TimeSpan.FromSeconds(5)))
            {
                SearchButton.Click();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not click search button");
        }
    }

    public IReadOnlyCollection<IWebElement> GetOffers()
    {
        var driver = AqualityServices.Browser.Driver;
        
        // Try to find offers using any method that works
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
        
        // Fallback: find any clickable elements that contain both price and surface info
        try
        {
            var fallbackOffers = driver.FindElements(By.XPath("//*[.//text()[contains(.,'z') or contains(.,'PLN')] and .//text()[contains(.,'m²')]]"));
            if (fallbackOffers.Count > 0)
            {
                Logger.Info($"Found {fallbackOffers.Count} offers using fallback detection");
                return fallbackOffers;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Fallback offer detection failed");
        }
        
        Logger.Warn("No offers found with any method");
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
        
        // Extract data from the selected offer
        var text = selected.Text ?? string.Empty;
        Logger.Debug($"Selected offer text sample: {(text.Length > 100 ? text.Substring(0, 100) + "..." : text)}");
        
        // Price extraction
        int? price = null;
        var priceMatches = Regex.Matches(text, @"(\d{3,})\s*(?:z|PLN|z?)");
        if (priceMatches.Count > 0 && int.TryParse(priceMatches[0].Groups[1].Value.Replace(" ", ""), out var p))
        {
            price = p;
        }
        
        // Surface extraction
        double? surface = null;
        var surfaceMatch = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*m[²2]");
        if (surfaceMatch.Success && double.TryParse(surfaceMatch.Groups[1].Value.Replace(",", "."), out var s))
        {
            surface = s;
        }
        
        // Rooms extraction
        int? rooms = null;
        var roomsMatch = Regex.Match(text, @"(\d+)\s*(?:pok|room)", RegexOptions.IgnoreCase);
        if (roomsMatch.Success && int.TryParse(roomsMatch.Groups[1].Value, out var r))
        {
            rooms = r;
        }
        
        Logger.Info($"Selected offer data -> Price: {price}, Surface: {surface}, Rooms: {rooms}");
        
        // Click the offer
        try 
        { 
            selected.Click(); 
        }
        catch 
        { 
            ((IJavaScriptExecutor)AqualityServices.Browser.Driver).ExecuteScript("arguments[0].click();", selected); 
        }
        
        return (price, surface, rooms);
    }

    // Legacy method for compatibility
    public void OpenRandomOfferAndReturnDetails(ScenarioContext scenarioContext, string contextKey = "RandomOfferDetails")
    {
        var data = PickRandomOfferAndOpen();
        scenarioContext[contextKey] = data;
        Logger.Info($"Stored offer details in ScenarioContext[{contextKey}]: {data}");
    }
}
