using Aquality.Selenium.Browsers;
using FluentAssertions;
using Reqnroll;
using Autotests_task1.Pages;
using NLog;
using Autotests_task1.Helpers;

namespace Autotests_task1.Steps;

[Binding]
public class SearchSteps
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ScenarioContext _scenarioContext;
    private readonly MainPage _mainPage = new();
    private readonly ResultsPage _resultsPage = new();
    private readonly OfferDetailsPage _offerDetailsPage = new();

    private const string PriceRangeKey = "PriceRange";
    private const string SurfaceRangeKey = "SurfaceRange";
    private const string OfferDataKey = "OfferData";

    private readonly string _location = "Warszawa";
    private readonly int _minPrice = 200000;
    private readonly int _maxPrice = 1000000;

    public SearchSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When("I set location and price filters and search")]
    public void WhenISetLocationAndPriceFiltersAndSearch()
    {
        Logger.Info("Setting location and price filters then searching");
        _mainPage.SetLocationWithEnter(_location); // ensure suggestion accepted
        _mainPage.SetPriceRange(_minPrice, _maxPrice);
        ScenarioContextHelper.Set(_scenarioContext, PriceRangeKey, (_minPrice, _maxPrice));

        // Single search attempt with longer wait
        Logger.Info("Attempting to click search button");
        var clicked = _mainPage.Search();
        clicked.Should().BeTrue("Search button should be clickable");

        // Wait for navigation to results page
        var navSucceeded = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                var url = AqualityServices.Browser.CurrentUrl;
                return url.Contains("/pl/") || url.Contains("search") || url.Contains("oferty");
            }
            catch
            {
                return false;
            }
        }, timeout: TimeSpan.FromSeconds(15));

        if (navSucceeded)
        {
            Logger.Info("Navigation succeeded, waiting for listings to load");
            var listingsLoaded = _resultsPage.WaitForListings(TimeSpan.FromSeconds(20));
            listingsLoaded.Should().BeTrue("Listings should load after search");

            var priceDataLoaded = _resultsPage.WaitForPriceData(TimeSpan.FromSeconds(15));
            priceDataLoaded.Should().BeTrue("Price data should load for validation");
        }
        else
        {
            Logger.Warn("Navigation did not succeed within timeout");
            navSucceeded.Should().BeTrue("Should navigate to results page after search");
        }
    }

    [Then("Search results should display apartments with price in selected range")]
    public void ThenSearchResultsShouldDisplayApartmentsWithPriceInSelectedRange()
    {
        _resultsPage.WaitForPriceData(TimeSpan.FromSeconds(15)).Should().BeTrue("Price data must be present before validation");
        var (min, max) = ScenarioContextHelper.Get<(int min, int max)>(_scenarioContext, PriceRangeKey);
        Logger.Info($"Verifying all prices are within {min}-{max}");
        _resultsPage.AreAllPricesWithinRange(min, max).Should().BeTrue("All prices should be within the expected range");
    }

    [When("I clear price filter and set surface range from first page")]
    public void WhenIClearPriceFilterAndSetSurfaceRangeFromFirstPage()
    {
        _resultsPage.WaitForListings(TimeSpan.FromSeconds(20)).Should().BeTrue("Listings should be available before deriving surface range");
        Logger.Info("Capturing surface range from first page and applying filter");
        var range = _resultsPage.GetSurfaceRangeFromFirstPage();
        ScenarioContextHelper.Set(_scenarioContext, SurfaceRangeKey, range);
        _resultsPage.ClearPriceFilter();
        _resultsPage.SetSurfaceRange(range.min, range.max);
        _resultsPage.Search();
        _resultsPage.WaitForListings(TimeSpan.FromSeconds(20)).Should().BeTrue("Listings should reload after applying surface filter");
    }

    [Then("Search results should display apartments with surface in selected range")]
    public void ThenSearchResultsShouldDisplayApartmentsWithSurfaceInSelectedRange()
    {
        _resultsPage.WaitForListings(TimeSpan.FromSeconds(20)).Should().BeTrue("Listings should be loaded before validating surface");
        var (min, max) = ScenarioContextHelper.Get<(double min, double max)>(_scenarioContext, SurfaceRangeKey);
        Logger.Info($"Verifying surfaces are within {min}-{max}");
        _resultsPage.AreAllSurfacesWithinRange(min, max).Should().BeTrue("All surfaces should be within derived range");
    }

    [When("I open random offer and save its details")]
    public void WhenIOpenRandomOfferAndSaveItsDetails()
    {
        _resultsPage.WaitForListings(TimeSpan.FromSeconds(20)).Should().BeTrue("Listings should be available before picking random offer");
        Logger.Info("Selecting random offer and saving parsed details");
        var data = _resultsPage.PickRandomOfferAndOpen();
        ScenarioContextHelper.Set(_scenarioContext, OfferDataKey, data);
        AqualityServices.ConditionalWait.WaitFor(() =>
            AqualityServices.Browser.CurrentUrl.Contains("/oferta") ||
            AqualityServices.Browser.CurrentUrl.Contains("/ad") ||
            AqualityServices.Browser.CurrentUrl.Contains("/listing"), timeout: TimeSpan.FromSeconds(30));
    }

    [Then("Offer details should match saved values")]
    public void ThenOfferDetailsShouldMatchSavedValues()
    {
        var (savedPrice, savedSurface, savedRooms) = ScenarioContextHelper.Get<(int? price, double? surface, int? rooms)>(_scenarioContext, OfferDataKey);
        Logger.Info("Reading details from offer page for validation");
        var (price, surface, rooms) = _offerDetailsPage.ReadDetails();

        if (savedPrice.HasValue) price.Should().Be(savedPrice.Value, "Price on details page should match saved list value");
        if (savedSurface.HasValue) surface.Should().BeApproximately(savedSurface.Value, 0.6, "Surface should match saved list value within tolerance");
        if (savedRooms.HasValue) rooms.Should().Be(savedRooms.Value, "Rooms count should match saved list value");
    }
}
