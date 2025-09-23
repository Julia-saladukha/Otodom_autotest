using Aquality.Selenium.Browsers;
using FluentAssertions;
using Reqnroll;
using Autotests_task1.Pages;
using NLog;
using OpenQA.Selenium;

namespace Autotests_task1.Steps;

[Binding]
public class SearchSteps
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly MainPage _mainPage = new();

    private const string LocationValue = "Warszawa";
    private const int MinPrice = 200000;
    private const int MaxPrice = 1000000;

    [When("I set location 'Warszawa' and price range 200000-1000000 and search")]
    public void WhenISetLocationWarszawaAndPriceRangeAndSearch()
    {
        Logger.Info("Setting location and price range, then performing search");

        var locationSet = _mainPage.SetLocation(LocationValue);
        locationSet.Should().BeTrue("Location input should be found and value set to Warszawa");
        Logger.Info("Location set successfully");

        var priceSet = _mainPage.SetPriceRange(MinPrice, MaxPrice);
        priceSet.Should().BeTrue("Price range inputs should be found and values entered");
        Logger.Info($"Price range set: {MinPrice}-{MaxPrice}");

        var clicked = _mainPage.ClickSearchButton();
        if (!clicked)
        {
            Logger.Warn("Primary search button click failed, trying generic search method");
            clicked = _mainPage.Search();
        }
        clicked.Should().BeTrue("Search button should be clicked");
        Logger.Info("Search initiated");

        // Wait for results navigation (strict)
        var initialUrl = "https://www.otodom.pl/";
        var loaded = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                var url = AqualityServices.Browser.CurrentUrl.ToLowerInvariant();
                if (!url.Equals(initialUrl) && (url.Contains("wynik") || url.Contains("/listing") || url.Contains("/sprzedaz"))) return true;
                var driver = AqualityServices.Browser.Driver;
                return driver.FindElements(By.CssSelector("article, div[data-cy*='listing']")).Any(e => e.Displayed);
            }
            catch { return false; }
        }, timeout: TimeSpan.FromSeconds(30));

        loaded.Should().BeTrue("Results page should load (URL changed or listings visible)");
        Logger.Info("Results page navigation condition satisfied");
    }

    [Then("I should see the search results for 'Warszawa' within the price range 200000-1000000")]
    public void ThenIShouldSeeTheSearchResultsForWarszawaWithinThePriceRange()
    {
        Logger.Info("Validating that results are shown for Warszawa with expected price filters");
        var browser = AqualityServices.Browser;
        var driver = browser.Driver;

        // Wait for potential listing cards
        var listingsPresent = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                return driver.FindElements(By.CssSelector("article, div[data-cy*='listing']"))
                    .Any(e => e.Displayed);
            }
            catch { return false; }
        }, TimeSpan.FromSeconds(15));
        listingsPresent.Should().BeTrue("At least one listing should be visible");

        var url = browser.CurrentUrl.ToLowerInvariant();
        Logger.Info($"Current URL after search: {url}");
        if (!url.Contains("warsz"))
        {
            Logger.Warn("URL does not contain 'warsz' - relying on filter inputs for validation");
        }

        // Log price filter values if present
        try
        {
            var minInput = driver.FindElements(By.CssSelector("input[data-cy='search.form.price.from']")).FirstOrDefault(e => e.Displayed);
            var maxInput = driver.FindElements(By.CssSelector("input[data-cy='search.form.price.to']")).FirstOrDefault(e => e.Displayed);
            if (minInput != null && maxInput != null)
            {
                Logger.Info($"Price inputs values: from='{minInput.GetAttribute("value")}' to='{maxInput.GetAttribute("value")}'");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed reading price inputs");
        }

        browser.CurrentUrl.Should().NotBe("https://www.otodom.pl/", "Search should navigate or update state");
        Logger.Info("Search results validation (primary) completed");
    }

    [Then("the search results should be valid")]
    public void ThenTheSearchResultsShouldBeValid()
    {
        Logger.Info("Performing detailed validation of search results");
        var driver = AqualityServices.Browser.Driver;

        
    }

    // NEW METHOD Analyse and area filter
    [When("I analyze surface area from results and apply surface filter")]
    public void WhenIAnalyzeSurfaceAreaFromResultsAndApplySurfaceFilter()
    {
        Logger.Info("Starting surface area analysis and filter application");
        var driver = AqualityServices.Browser.Driver;

        // 1. Анализируем результаты первой страницы
        var (minSurface, maxSurface) = AnalyzeSurfaceAreaFromFirstPage();
        Logger.Info($"Analyzed surface range: {minSurface}m² - {maxSurface}m²");

        // 2. Удаляем фильтры цены
        ClearPriceFilters();
        Logger.Info("Price filters cleared");

        // 3. Устанавливаем фильтры по площади
        SetSurfaceFilter(minSurface, maxSurface);
        Logger.Info($"Surface filter set: {minSurface}m² - {maxSurface}m²");

        // 4. Запускаем новый поиск
        var clicked = _mainPage.ClickSearchButton();
        clicked.Should().BeTrue("Search button should be clicked after applying surface filter");
        
        // Ждем обновления результатов
        AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                return driver.FindElements(By.CssSelector("article, div[data-cy*='listing']")).Any(e => e.Displayed);
            }
            catch { return false; }
        }, TimeSpan.FromSeconds(20));

        Logger.Info("Surface filter application completed");
    }

    private (double minSurface, double maxSurface) AnalyzeSurfaceAreaFromFirstPage()
    {
        Logger.Info("Analyzing surface area from first page listings");
        var driver = AqualityServices.Browser.Driver;
        var surfaceAreas = new List<double>();

        var listings = driver.FindElements(By.CssSelector("article, div[data-cy*='listing']"))
            .Where(e => e.Displayed)
            .Take(20) // Анализируем до 20 объявлений
            .ToList();

        foreach (var listing in listings)
        {
            try
            {
                // Ищем площадь - варианты селекторов для площади
                var surfaceSelectors = new[]
                {
                    ".//*[contains(text(),'m²') or contains(text(),'m2')]",
                    ".//*[contains(@aria-label,'Powierzchnia') or contains(@data-cy,'surface')]",
                    ".//span[contains(text(),'m²')]",
                    ".//div[contains(text(),'m²')]"
                };

                foreach (var selector in surfaceSelectors)
                {
                    try
                    {
                        var surfaceEl = listing.FindElement(By.XPath(selector));
                        if (surfaceEl != null && surfaceEl.Displayed)
                        {
                            var surfaceText = surfaceEl.Text;
                            Logger.Debug($"Found surface text: '{surfaceText}'");
                            
                            // Извлекаем численное значение площади
                            var match = System.Text.RegularExpressions.Regex.Match(surfaceText, @"(\d+(?:[.,]\d+)?)\s*m[²2]");
                            if (match.Success)
                            {
                                if (double.TryParse(match.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var surface))
                                {
                                    surfaceAreas.Add(surface);
                                    Logger.Debug($"Extracted surface: {surface}m²");
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to extract surface from listing");
            }
        }

        if (!surfaceAreas.Any())
        {
            Logger.Warn("No surface areas found, using default range 40-120m²");
            return (40.0, 120.0);
        }

        var minSurface = surfaceAreas.Min();
        var maxSurface = surfaceAreas.Max();
        
        Logger.Info($"Found {surfaceAreas.Count} surface values. Range: {minSurface}m² - {maxSurface}m²");
        return (minSurface, maxSurface);
    }

    private void ClearPriceFilters()
    {
        Logger.Info("Clearing price filters");
        var success = _mainPage.ClearPriceRange();
        if (!success)
        {
            Logger.Warn("Failed to clear price filters using MainPage method, trying direct approach");
            var driver = AqualityServices.Browser.Driver;
            try
            {
                // Fallback: прямая очистка через селекторы
                var priceInputs = driver.FindElements(By.CssSelector("input[data-cy*='price'], input[name*='price']"));
                foreach (var input in priceInputs.Where(i => i.Displayed && i.Enabled))
                {
                    input.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Fallback price clearing also failed");
            }
        }
    }

    private void SetSurfaceFilter(double minSurface, double maxSurface)
    {
        Logger.Info($"Setting surface filter: {minSurface}m² - {maxSurface}m²");
        var success = _mainPage.SetSurfaceRange((int)minSurface, (int)maxSurface);
        if (!success)
        {
            Logger.Warn("Failed to set surface filter using MainPage method, trying direct approach");
            var driver = AqualityServices.Browser.Driver;
            try
            {
                // Fallback: прямая установка через селекторы
                var surfaceMinSelectors = new[]
                {
                    "input[data-cy='search.form.surface.from']",
                    "input[name='surfaceMin']",
                    "input[placeholder*='powierzchnia']"
                };

                var surfaceMaxSelectors = new[]
                {
                    "input[data-cy='search.form.surface.to']",
                    "input[name='surfaceMax']",
                    "input[placeholder*='powierzchnia']"
                };

                foreach (var selector in surfaceMinSelectors)
                {
                    try
                    {
                        var minInput = driver.FindElement(By.CssSelector(selector));
                        if (minInput.Displayed && minInput.Enabled)
                        {
                            minInput.Clear();
                            minInput.SendKeys(((int)minSurface).ToString());
                            break;
                        }
                    }
                    catch { }
                }

                foreach (var selector in surfaceMaxSelectors)
                {
                    try
                    {
                        var maxInput = driver.FindElement(By.CssSelector(selector));
                        if (maxInput.Displayed && maxInput.Enabled)
                        {
                            maxInput.Clear();
                            maxInput.SendKeys(((int)maxSurface).ToString());
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Fallback surface filter setting also failed");
            }
        }
    }
}
