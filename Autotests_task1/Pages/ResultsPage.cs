using Aquality.Selenium.Browsers;
using Aquality.Selenium.Elements;
using Aquality.Selenium.Elements.Interfaces;
using NLog;
using OpenQA.Selenium;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Autotests_task1.Pages;

public class ResultsPage : BasePage
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ShortWait = TimeSpan.FromSeconds(5);

    public ResultsPage() : base(By.CssSelector("body"), "Results Page") { }

    #region Locators
    // Listing containers - multiple selectors for robustness
    private readonly IReadOnlyCollection<By> _listingLocators = new List<By>
    {
        By.CssSelector("article[data-cy*='listing']"),
        By.CssSelector("div[data-cy*='listing']"),
        By.CssSelector(".offer-item"),
        By.CssSelector(".listing-item"),
        By.CssSelector("article"),
        By.CssSelector("div[class*='offer-card']")
    };

    // Surface area selectors within listings
    private readonly IReadOnlyCollection<By> _surfaceLocators = new List<By>
    {
        By.XPath(".//span[contains(text(),'m²') or contains(text(),'m2')]"),
        By.XPath(".//*[contains(text(),'m²') or contains(text(),'m2')]"),
        By.XPath(".//div[contains(@class,'surface') or contains(@class,'area')]"),
        By.XPath(".//*[contains(@aria-label,'powierzchnia') or contains(@data-cy,'surface')]"),
        By.CssSelector("span[class*='surface']"),
        By.CssSelector("div[class*='area']")
    };

    // Price filter clear buttons
    private readonly IReadOnlyCollection<By> _priceClearLocators = new List<By>
    {
        By.CssSelector("button[data-cy*='price-clear']"),
        By.CssSelector("button[aria-label*='Clear price']"),
        By.CssSelector("button[title*='Clear price']"),
        By.XPath("//button[contains(.,'Clear') or contains(.,'Wyczy??')]"),
        By.CssSelector(".filter-clear"),
        By.CssSelector("button.clear-filter")
    };

    // Surface filter input fields
    private readonly IReadOnlyCollection<By> _surfaceMinInputLocators = new List<By>
    {
        By.CssSelector("input[data-cy='search.form.surface.from']"),
        By.CssSelector("input[name='surfaceMin']"),
        By.CssSelector("input[name*='surface_from']"),
        By.CssSelector("input[placeholder*='powierzchnia']"),
        By.CssSelector("input[aria-label*='Powierzchnia od']"),
        By.CssSelector("input[data-testid*='surface-min']")
    };

    private readonly IReadOnlyCollection<By> _surfaceMaxInputLocators = new List<By>
    {
        By.CssSelector("input[data-cy='search.form.surface.to']"),
        By.CssSelector("input[name='surfaceMax']"),
        By.CssSelector("input[name*='surface_to']"),
        By.CssSelector("input[placeholder*='powierzchnia']"),
        By.CssSelector("input[aria-label*='Powierzchnia do']"),
        By.CssSelector("input[data-testid*='surface-max']")
    };

    // Search button
    private readonly IReadOnlyCollection<By> _searchButtonLocators = new List<By>
    {
        By.Id("search-form-submit"),
        By.CssSelector("button[type='submit']"),
        By.CssSelector("button[data-cy*='search']"),
        By.XPath("//button[contains(.,'Szukaj') or contains(.,'Search')]")
    };
    #endregion

    #region Wait Methods
    public bool WaitForListings(TimeSpan timeout)
    {
        Logger.Info("Waiting for listings to load");
        return AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                var driver = AqualityServices.Browser.Driver;
                return GetListingElements().Any();
            }
            catch { return false; }
        }, timeout);
    }

    private IList<IWebElement> GetListingElements()
    {
        var driver = AqualityServices.Browser.Driver;
        foreach (var locator in _listingLocators)
        {
            try
            {
                var elements = driver.FindElements(locator).Where(e => e.Displayed).ToList();
                if (elements.Any())
                {
                    Logger.Debug($"Found {elements.Count} listings using locator: {locator}");
                    return elements;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to find listings with locator: {locator}");
            }
        }
        return new List<IWebElement>();
    }
    #endregion

    #region Core Methods
    public (int min, int max) GetMinAndMaxSurfaceFromFirstPage()
    {
        Logger.Info("Analyzing surface areas from first page listings");
        
        // Wait for listings to be available
        if (!WaitForListings(DefaultWait))
        {
            Logger.Warn("No listings found, using default surface range");
            return (40, 120);
        }

        var surfaceValues = new List<int>();
        var listings = GetListingElements().Take(20).ToList(); // Analyze up to 20 listings
        
        Logger.Info($"Found {listings.Count} listings to analyze for surface area");

        foreach (var listing in listings)
        {
            try
            {
                var surfaceValue = ExtractSurfaceFromListing(listing);
                if (surfaceValue.HasValue)
                {
                    surfaceValues.Add(surfaceValue.Value);
                    Logger.Debug($"Extracted surface: {surfaceValue}m²");
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to extract surface from listing");
            }
        }

        if (!surfaceValues.Any())
        {
            Logger.Warn("No surface values found, using default range 40-120m²");
            return (40, 120);
        }

        var minSurface = surfaceValues.Min();
        var maxSurface = surfaceValues.Max();

        // Ensure reasonable range - expand if too narrow
        if (maxSurface - minSurface < 10)
        {
            Logger.Warn("Surface range too narrow, expanding by ±10m²");
            var avg = (minSurface + maxSurface) / 2;
            minSurface = Math.Max(10, avg - 10);
            maxSurface = Math.Min(500, avg + 10);
        }

        Logger.Info($"Surface range determined from {surfaceValues.Count} values: Min = {minSurface}m², Max = {maxSurface}m²");
        return (minSurface, maxSurface);
    }

    public (double min, double max) GetSurfaceRangeFromFirstPage()
    {
        var (minInt, maxInt) = GetMinAndMaxSurfaceFromFirstPage();
        return ((double)minInt, (double)maxInt);
    }

    private int? ExtractSurfaceFromListing(IWebElement listing)
    {
        foreach (var locator in _surfaceLocators)
        {
            try
            {
                var elements = listing.FindElements(locator);
                foreach (var element in elements.Where(e => e.Displayed))
                {
                    var text = element.Text?.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    var surface = ParseSurfaceFromText(text);
                    if (surface.HasValue)
                    {
                        Logger.Debug($"Found surface text: '{text}' -> {surface}m²");
                        return surface;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Error with surface locator: {locator}");
            }
        }
        return null;
    }

    private int? ParseSurfaceFromText(string text)
    {
        // Multiple regex patterns for different surface formats
        var patterns = new[]
        {
            @"(\d+(?:[.,]\d+)?)\s*m[²2]",           // "45 m²" or "45.5m2"
            @"(\d+(?:[.,]\d+)?)\s*m\s*²",          // "45 m ²"
            @"powierzchnia:?\s*(\d+(?:[.,]\d+)?)",  // "powierzchnia: 45"
            @"(\d+(?:[.,]\d+)?)\s*metr",           // "45 metr"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var valueStr = match.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    var intValue = (int)Math.Round(value);
                    // Validate reasonable surface area range
                    if (intValue >= 10 && intValue <= 500)
                    {
                        return intValue;
                    }
                }
            }
        }
        return null;
    }

    public void ClearPriceFilter()
    {
        Logger.Info("Clearing price filter");
        var driver = AqualityServices.Browser.Driver;
        bool cleared = false;

        // Try to find and click clear button
        foreach (var locator in _priceClearLocators)
        {
            try
            {
                var clearButton = driver.FindElements(locator).FirstOrDefault(e => e.Displayed && e.Enabled);
                if (clearButton != null)
                {
                    clearButton.Click();
                    Logger.Info($"Clicked price clear button using locator: {locator}");
                    cleared = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to click clear button with locator: {locator}");
            }
        }

        // Fallback: Clear price input fields directly
        if (!cleared)
        {
            Logger.Info("Clear button not found, clearing price input fields directly");
            try
            {
                var priceInputs = driver.FindElements(By.CssSelector("input[data-cy*='price'], input[name*='price']"));
                foreach (var input in priceInputs.Where(i => i.Displayed && i.Enabled))
                {
                    input.Clear();
                    Logger.Debug("Cleared price input field");
                    cleared = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to clear price input fields");
            }
        }

        if (cleared)
        {
            // Wait a moment for UI to update
            Thread.Sleep(500);
            Logger.Info("Price filter cleared successfully");
        }
        else
        {
            Logger.Warn("Could not clear price filter - no clear button or input fields found");
        }
    }

    public void SetSurfaceRange(int min, int max)
    {
        Logger.Info($"Setting surface range: {min} - {max} m²");
        var driver = AqualityServices.Browser.Driver;

        // Find and set minimum surface
        var minInput = FindFirstDisplayedElement(driver, _surfaceMinInputLocators);
        if (minInput != null)
        {
            try
            {
                minInput.Clear();
                minInput.SendKeys(min.ToString());
                Logger.Info($"Set minimum surface: {min}m²");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to set minimum surface value");
            }
        }
        else
        {
            Logger.Warn("Minimum surface input field not found");
        }

        // Find and set maximum surface
        var maxInput = FindFirstDisplayedElement(driver, _surfaceMaxInputLocators);
        if (maxInput != null)
        {
            try
            {
                maxInput.Clear();
                maxInput.SendKeys(max.ToString());
                Logger.Info($"Set maximum surface: {max}m²");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to set maximum surface value");
            }
        }
        else
        {
            Logger.Warn("Maximum surface input field not found");
        }

        // Small delay to allow UI to process the input
        Thread.Sleep(300);
    }

    public void SetSurfaceRange(double min, double max)
    {
        SetSurfaceRange((int)Math.Round(min), (int)Math.Round(max));
    }

    public void Search()
    {
        Logger.Info("Clicking search button to apply filters");
        var driver = AqualityServices.Browser.Driver;

        var searchButton = FindFirstDisplayedElement(driver, _searchButtonLocators);
        if (searchButton != null)
        {
            try
            {
                searchButton.Click();
                Logger.Info("Search button clicked successfully");
                
                // Wait for search to complete
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to click search button");
            }
        }
        else
        {
            Logger.Warn("Search button not found");
        }
    }

    private IWebElement? FindFirstDisplayedElement(IWebDriver driver, IEnumerable<By> locators)
    {
        foreach (var locator in locators)
        {
            try
            {
                var element = driver.FindElements(locator).FirstOrDefault(e => e.Displayed && e.Enabled);
                if (element != null)
                {
                    Logger.Debug($"Found element using locator: {locator}");
                    return element;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to find element with locator: {locator}");
            }
        }
        return null;
    }
    #endregion

    #region Validation Methods
    public bool AreAllPricesWithinRange(int min, int max)
    {
        Logger.Info($"Validating all prices are within range {min} - {max}");
        var listings = GetListingElements().Take(15).ToList();
        int validPrices = 0;
        int totalPrices = 0;

        foreach (var listing in listings)
        {
            try
            {
                var priceText = ExtractPriceFromListing(listing);
                if (!string.IsNullOrEmpty(priceText))
                {
                    var price = ParsePriceFromText(priceText);
                    if (price.HasValue)
                    {
                        totalPrices++;
                        if (price.Value >= min && price.Value <= max)
                        {
                            validPrices++;
                        }
                        else
                        {
                            Logger.Debug($"Price {price.Value} is outside range {min}-{max}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to validate price for listing");
            }
        }

        var isValid = totalPrices > 0 && validPrices == totalPrices;
        Logger.Info($"Price validation: {validPrices}/{totalPrices} prices within range");
        return isValid;
    }

    public bool AreAllSurfacesWithinRange(double min, double max)
    {
        Logger.Info($"Validating all surfaces are within range {min} - {max}m²");
        var listings = GetListingElements().Take(15).ToList();
        int validSurfaces = 0;
        int totalSurfaces = 0;

        foreach (var listing in listings)
        {
            try
            {
                var surface = ExtractSurfaceFromListing(listing);
                if (surface.HasValue)
                {
                    totalSurfaces++;
                    if (surface.Value >= min && surface.Value <= max)
                    {
                        validSurfaces++;
                    }
                    else
                    {
                        Logger.Debug($"Surface {surface.Value}m² is outside range {min}-{max}m²");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to validate surface for listing");
            }
        }

        var isValid = totalSurfaces > 0 && validSurfaces >= (totalSurfaces * 0.8); // Allow 80% tolerance
        Logger.Info($"Surface validation: {validSurfaces}/{totalSurfaces} surfaces within range");
        return isValid;
    }

    private string? ExtractPriceFromListing(IWebElement listing)
    {
        var priceSelectors = new[]
        {
            By.XPath(".//*[contains(text(),'PLN') or contains(text(),'z?')]"),
            By.CssSelector("span[class*='price']"),
            By.CssSelector("div[class*='price']")
        };

        foreach (var selector in priceSelectors)
        {
            try
            {
                var element = listing.FindElements(selector).FirstOrDefault(e => e.Displayed);
                if (element != null && !string.IsNullOrEmpty(element.Text))
                {
                    return element.Text.Trim();
                }
            }
            catch { }
        }
        return null;
    }

    private int? ParsePriceFromText(string text)
    {
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var price) && price > 1000)
        {
            return price;
        }
        return null;
    }

    public (int? price, double? surface, int? rooms) PickRandomOfferAndOpen()
    {
        Logger.Info("Picking random offer and opening details");
        var listings = GetListingElements().Take(10).ToList();
        
        if (!listings.Any())
        {
            Logger.Warn("No listings available to pick from");
            return (null, null, null);
        }

        var random = new Random();
        var selectedListing = listings[random.Next(listings.Count)];
        
        // Extract data before clicking
        var price = ParsePriceFromText(ExtractPriceFromListing(selectedListing) ?? "");
        var surface = ExtractSurfaceFromListing(selectedListing);
        
        try
        {
            // Find clickable link within the listing
            var link = selectedListing.FindElement(By.TagName("a"));
            link.Click();
            Logger.Info("Clicked on random offer");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to click on offer link");
        }

        return (price, surface.HasValue ? (double?)surface.Value : null, null);
    }
    #endregion
}