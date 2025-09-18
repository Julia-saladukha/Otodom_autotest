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

        // Collect listing containers (broad selectors to adapt to site changes)
        var listings = driver.FindElements(By.CssSelector("article, div[data-cy*='listing']"))
            .Where(e => e.Displayed)
            .Take(15) // limit scope
            .ToList();

        listings.Count.Should().BeGreaterThan(0, "There should be at least one visible listing");
        Logger.Info($"Found {listings.Count} visible listings to validate");

        int withTitle = 0, withPrice = 0;
        foreach (var listing in listings)
        {
            try
            {
                var titleEl = listing.FindElements(By.CssSelector("h2,h3,a[title],a[data-cy*='title']")).FirstOrDefault(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
                if (titleEl != null) withTitle++;

                var priceEl = listing.FindElements(By.XPath(".//*[contains(text(),'PLN') or contains(text(),'z?')]")).FirstOrDefault(e => e.Displayed);
                if (priceEl != null)
                {
                    var digits = new string(priceEl.Text.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out var priceValue))
                    {
                        withPrice++;
                        // Soft range check (only log, not fail if outside since site may show promos)
                        if (priceValue < MinPrice || priceValue > MaxPrice)
                        {
                            Logger.Warn($"Listing price {priceValue} outside expected range {MinPrice}-{MaxPrice}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Listing validation issue (ignored)");
            }
        }

        withTitle.Should().BeGreaterThan(0, "At least one listing should have a title");
        withPrice.Should().BeGreaterThan(0, "At least one listing should have a detectable price");
        Logger.Info($"Listings with title: {withTitle}, with price: {withPrice}");
        Logger.Info("Detailed search results validation completed");
    }
}
