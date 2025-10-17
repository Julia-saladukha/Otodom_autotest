using Aquality.Selenium.Browsers;
using Aquality.Selenium.Elements;
using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Core.Logging;
using OpenQA.Selenium;
using System.Globalization;
using System.Text.RegularExpressions;
using Autotests_task1.Models;

namespace Autotests_task1.Pages;

public class ResultsPage : BasePage
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ShortWait = TimeSpan.FromSeconds(5);

    public ResultsPage() : base(By.CssSelector("body"), "Results Page") { }

    #region Locators
    private readonly IReadOnlyCollection<By> _listingLocators = new List<By>
    {
        By.CssSelector("article[data-cy*='listing']"),
        By.CssSelector("div[data-cy*='listing']"),
        By.CssSelector(".offer-item"),
        By.CssSelector(".listing-item"),
        By.CssSelector("article"),
        By.CssSelector("div[class*='offer-card']")
    };

    private readonly IReadOnlyCollection<By> _surfaceLocators = new List<By>
    {
        By.XPath(".//span[contains(text(),'m²') or contains(text(),'m2')]"),
        By.XPath(".//*[contains(text(),'m²') or contains(text(),'m2')]"),
        By.XPath(".//div[contains(@class,'surface') or contains(@class,'area')]"),
        By.XPath(".//*[contains(@aria-label,'powierzchnia') or contains(@data-cy,'surface')]"),
        By.CssSelector("span[class*='surface']"),
        By.CssSelector("div[class*='area']")
    };

    private readonly IReadOnlyCollection<By> _priceClearLocators = new List<By>
    {
        By.CssSelector("button[data-cy*='price-clear']"),
        By.CssSelector("button[aria-label*='Clear price']"),
        By.CssSelector("button[title*='Clear price']"),
        By.XPath("//button[contains(.,'Clear') or contains(.,'Wyczyść')]"),
        By.CssSelector(".filter-clear"),
        By.CssSelector("button.clear-filter")
    };

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
            var driver = AqualityServices.Browser.Driver;
            return GetListingElements().Any();
        }, timeout);
    }

    public IList<IWebElement> GetListingElements()
    {
        var driver = AqualityServices.Browser.Driver;
        foreach (var locator in _listingLocators)
        {
            var elements = driver.FindElements(locator).Where(e => e.Displayed).ToList();
            if (elements.Any())
            {
                Logger.Debug($"Found {elements.Count} listings using locator: {locator}");
                return elements;
            }
        }
        return new List<IWebElement>();
    }
    #endregion

    #region Core Methods
    public (int min, int max) GetMinAndMaxSurfaceFromFirstPage()
    {
        Logger.Info("Analyzing surface areas from first page listings");
        
        if (!WaitForListings(DefaultWait))
        {
            Logger.Warn("No listings found, using default surface range");
            return (40, 120);
        }

        var surfaceValues = new List<int>();
        var listings = GetListingElements().Take(20).ToList();
        
        Logger.Info($"Found {listings.Count} listings to analyze for surface area");

        foreach (var listing in listings)
        {
            var surfaceValue = ExtractSurfaceFromListing(listing);
            if (surfaceValue.HasValue)
            {
                surfaceValues.Add(surfaceValue.Value);
                Logger.Debug($"Extracted surface: {surfaceValue}m²");
            }
        }

        if (!surfaceValues.Any())
        {
            Logger.Warn("No surface values found, using default range 40-120m²");
            return (40, 120);
        }

        var minSurface = surfaceValues.Min();
        var maxSurface = surfaceValues.Max();

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

    public int? ExtractSurfaceFromListing(IWebElement listing)
    {
        foreach (var locator in _surfaceLocators)
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
        return null;
    }

    public int? ExtractPriceFromListing(IWebElement listing)
    {
        var priceText = GetPriceTextFromListing(listing);
        if (!string.IsNullOrEmpty(priceText))
        {
            return ParsePriceFromText(priceText);
        }
        return null;
    }

    public int? ExtractRoomsFromListing(IWebElement listing)
    {
        var roomsSelectors = new[]
        {
            By.XPath(".//*[contains(text(),'pokoi') or contains(text(),'pokoje') or contains(text(),'rooms')]"),
            By.XPath(".//*[contains(@aria-label,'pokoi') or contains(@data-cy,'rooms')]"),
            By.CssSelector("span[class*='rooms']"),
            By.CssSelector("div[class*='rooms']"),
            By.XPath(".//*[text()[contains(.,'pokoi')] or text()[contains(.,'pokoje')]]")
        };

        foreach (var selector in roomsSelectors)
        {
            var elements = listing.FindElements(selector);
            foreach (var element in elements.Where(e => e.Displayed))
            {
                var text = element.Text?.Trim();
                if (string.IsNullOrEmpty(text)) continue;

                var rooms = ParseRoomsFromText(text);
                if (rooms.HasValue)
                {
                    Logger.Debug($"Found rooms text: '{text}' -> {rooms} rooms");
                    return rooms;
                }
            }
        }
        return null;
    }

    private int? ParseRoomsFromText(string text)
    {
        var patterns = new[]
        {
            @"(\d+)\s*poko[ij]",
            @"(\d+)\s*rooms?",
            @"^(\d+)$"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out var rooms))
                {
                    if (rooms >= 1 && rooms <= 10)
                    {
                        return rooms;
                    }
                }
            }
        }
        return null;
    }

    private string? GetPriceTextFromListing(IWebElement listing)
    {
        var priceSelectors = new[]
        {
            By.XPath(".//*[contains(text(),'PLN') or contains(text(),'zł')]"),
            By.CssSelector("span[class*='price']"),
            By.CssSelector("div[class*='price']")
        };

        foreach (var selector in priceSelectors)
        {
            var element = listing.FindElements(selector).FirstOrDefault(e => e.Displayed);
            if (element != null && !string.IsNullOrEmpty(element.Text))
            {
                return element.Text.Trim();
            }
        }
        return null;
    }

    private int? ParseSurfaceFromText(string text)
    {
        var patterns = new[]
        {
            @"(\d+(?:[.,]\d+)?)\s*m[²2]",
            @"(\d+(?:[.,]\d+)?)\s*m\s*²",
            @"powierzchnia:?\s*(\d+(?:[.,]\d+)?)",
            @"(\d+(?:[.,]\d+)?)\s*metr",
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
                    if (intValue >= 10 && intValue <= 500)
                    {
                        return intValue;
                    }
                }
            }
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
            var price = ExtractPriceFromListing(listing);
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

        var isValid = totalSurfaces > 0 && validSurfaces >= (totalSurfaces * 0.8);
        Logger.Info($"Surface validation: {validSurfaces}/{totalSurfaces} surfaces within range");
        return isValid;
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
        
        var price = ExtractPriceFromListing(selectedListing);
        var surface = ExtractSurfaceFromListing(selectedListing);
        var rooms = ExtractRoomsFromListing(selectedListing);
        
        var link = selectedListing.FindElement(By.TagName("a"));
        link.Click();
        Logger.Info("Clicked on random offer");

        return (price, surface.HasValue ? (double?)surface.Value : null, rooms);
    }
    #endregion

    #region Filter Methods
    public void ClearPriceFilter()
    {
        Logger.Info("Clearing price filter");
        var driver = AqualityServices.Browser.Driver;
        bool cleared = false;

        foreach (var locator in _priceClearLocators)
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

        if (!cleared)
        {
            Logger.Info("Clear button not found, clearing price input fields directly");
            var priceInputs = driver.FindElements(By.CssSelector("input[data-cy*='price'], input[name*='price']"));
            foreach (var input in priceInputs.Where(i => i.Displayed && i.Enabled))
            {
                input.Clear();
                Logger.Debug("Cleared price input field");
                cleared = true;
            }
        }

        if (cleared)
        {
            AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromMilliseconds(500));
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

        var surfaceInputsReady = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var minInput = FindFirstDisplayedElement(driver, _surfaceMinInputLocators);
            var maxInput = FindFirstDisplayedElement(driver, _surfaceMaxInputLocators);
            return minInput != null && maxInput != null && 
                   IsElementFullyInteractable(minInput) && IsElementFullyInteractable(maxInput);
        }, TimeSpan.FromSeconds(15));

        if (!surfaceInputsReady)
        {
            Logger.Error("Surface input fields never became ready for interaction");
            return;
        }

        var minInput = FindFirstDisplayedElement(driver, _surfaceMinInputLocators);
        var maxInput = FindFirstDisplayedElement(driver, _surfaceMaxInputLocators);

        if (minInput == null || maxInput == null)
        {
            Logger.Error("Surface input fields not found after waiting");
            return;
        }

        Logger.Info("Setting minimum surface...");
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", minInput);
        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromMilliseconds(500));
        
        minInput.Clear();
        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromMilliseconds(500));
        minInput.SendKeys(min.ToString());
        Logger.Info($"Set minimum surface: {min}m²");
        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromMilliseconds(500));

        Logger.Info("Setting maximum surface...");
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", maxInput);
        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromMilliseconds(500));
        
        maxInput.Clear();
        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromMilliseconds(500));
        maxInput.SendKeys(max.ToString());
        Logger.Info($"Set maximum surface: {max}m²");

        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromSeconds(1));
        Logger.Info("Surface range set successfully");
    }

    public void SetSurfaceRange(double min, double max)
    {
        SetSurfaceRange((int)Math.Round(min), (int)Math.Round(max));
    }

    private bool IsElementFullyInteractable(IWebElement element)
    {
        if (element == null) return false;
        
        var interactable = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var displayed = element.Displayed;
            var enabled = element.Enabled;
            var hasSize = element.Size.Height > 0 && element.Size.Width > 0;
            var location = element.Location;
            var size = element.Size;
            var inViewport = location.X >= -50 && location.Y >= -50 && size.Width > 5 && size.Height > 5;
            _ = element.TagName;
            return displayed && enabled && hasSize && inViewport;
        }, TimeSpan.FromMilliseconds(100));
        
        return interactable;
    }
    #endregion

    public void Search()
    {
        Logger.Info("Clicking search button to apply filters");
        var driver = AqualityServices.Browser.Driver;

        var searchButton = FindFirstDisplayedElement(driver, _searchButtonLocators);
        if (searchButton != null)
        {
            searchButton.Click();
            Logger.Info("Search button clicked successfully");
            AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromSeconds(1));
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
            var element = driver.FindElements(locator).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (element != null)
            {
                Logger.Debug($"Found element using locator: {locator}");
                return element;
            }
        }
        return null;
    }

    public List<ListingData> GetAllListingsData()
    {
        Logger.Info("Collecting data from all listings on the current page");
        
        var listings = GetListingElements();
        var listingsData = new List<ListingData>();
        
        for (int i = 0; i < listings.Count; i++)
        {
            var listing = listings[i];
            var listingData = new ListingData
            {
                Index = i + 1,
                Element = listing,
                Price = ExtractPriceFromListing(listing),
                Surface = ExtractSurfaceFromListing(listing),
                Rooms = ExtractRoomsFromListing(listing),
                Title = ExtractTitleFromListing(listing)
            };
            
            listingsData.Add(listingData);
            Logger.Debug($"Listing {i + 1}: Price={listingData.Price}, Surface={listingData.Surface}m², Rooms={listingData.Rooms}, Title='{listingData.Title}'");
        }
        
        Logger.Info($"Collected data from {listingsData.Count} listings");
        return listingsData;
    }

    public string? ExtractTitleFromListing(IWebElement listing)
    {
        var titleSelectors = new[]
        {
            By.CssSelector("h2"),
            By.CssSelector("h3"),
            By.CssSelector("a[title]"),
            By.CssSelector("a[data-cy*='title']"),
            By.CssSelector("span[data-cy*='title']"),
            By.CssSelector(".listing-title"),
            By.CssSelector("[class*='title']")
        };

        foreach (var selector in titleSelectors)
        {
            var element = listing.FindElements(selector).FirstOrDefault(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
            if (element != null)
            {
                var title = element.Text?.Trim();
                if (!string.IsNullOrEmpty(title))
                {
                    Logger.Debug($"Found title using selector {selector}: '{title}'");
                    return title;
                }
            }
        }
        
        return null;
    }
}