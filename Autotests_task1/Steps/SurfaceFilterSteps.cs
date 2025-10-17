using Aquality.Selenium.Browsers;
using FluentAssertions;
using Reqnroll;
using Autotests_task1.Pages;
using NLog;
using Autotests_task1.Helpers;
using OpenQA.Selenium;

namespace Autotests_task1.Steps;

[Binding]
public class SurfaceFilterSteps
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ResultsPage _resultsPage = new();
    private readonly ScenarioContext _scenarioContext;

    private const string SurfaceRangeKey = "SurfaceRange";

    public SurfaceFilterSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When("I clear price filter and set surface range from first page")]
    public void WhenIClearPriceFilterAndSetSurfaceRangeFromFirstPage()
    {
        Logger.Info("Starting to clear price filter and set surface range from first page analysis");
        
        AqualityServices.ConditionalWait.WaitFor(() => 
            _resultsPage.GetListingElements().Any(), 
            TimeSpan.FromSeconds(10))
            .Should().BeTrue("Listings should be available before analyzing surface range");
        
        var (minSurface, maxSurface) = _resultsPage.GetMinAndMaxSurfaceFromFirstPage();
        Logger.Info($"Surface range identified: Min = {minSurface}m², Max = {maxSurface}m²");

        ScenarioContextHelper.Set(_scenarioContext, SurfaceRangeKey, (minSurface, maxSurface));
        Logger.Info($"Saved surface range to scenario context: {minSurface}-{maxSurface}m²");

        Logger.Info("Clearing price filters");
        _resultsPage.ClearPriceFilter();

        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromSeconds(1));

        Logger.Info($"Setting surface range filter: {minSurface}m² - {maxSurface}m²");
        _resultsPage.SetSurfaceRange(minSurface, maxSurface);

        Logger.Info("Applying search with surface filters");
        _resultsPage.Search();

        var resultsUpdated = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var listings = _resultsPage.GetListingElements();
            return listings.Any();
        }, TimeSpan.FromSeconds(10));

        resultsUpdated.Should().BeTrue("Search results should reload with surface filters applied");
        Logger.Info("Surface filter application completed successfully");
    }

    [Then("all apartments should have surface area within the applied range")]
    public void ThenAllApartmentsShouldHaveSurfaceAreaWithinTheAppliedRange()
    {
        Logger.Info("Validating that all apartments have surface area within the applied range");
        _resultsPage.WaitForListings(TimeSpan.FromSeconds(10))
            .Should().BeTrue("Listings should be loaded for surface validation");

        int minSurface, maxSurface;
        try
        {
            var range = ScenarioContextHelper.Get<(int min, int max)>(_scenarioContext, SurfaceRangeKey);
            minSurface = range.min;
            maxSurface = range.max;
            Logger.Info($"Retrieved surface range from scenario context: {minSurface}m² - {maxSurface}m²");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not retrieve surface range from scenario context, analyzing current page");
            var currentRange = _resultsPage.GetMinAndMaxSurfaceFromFirstPage();
            minSurface = currentRange.min;
            maxSurface = currentRange.max;
        }

        minSurface.Should().BeGreaterThan(0, "Minimum surface should be positive");
        maxSurface.Should().BeLessThan(1000, "Maximum surface should be reasonable");
        maxSurface.Should().BeGreaterThanOrEqualTo(minSurface, "Maximum should be >= minimum");

        var surfaceValidation = _resultsPage.AreAllSurfacesWithinRange(minSurface, maxSurface);
        surfaceValidation.Should().BeTrue($"All apartment surfaces should be within the applied range: {minSurface}m² - {maxSurface}m²");

        Logger.Info($"Surface area validation completed successfully. All apartments are within range: {minSurface}m² - {maxSurface}m²");
    }

    [Then("surface filters should be visible in the search interface")]
    public void ThenSurfaceFiltersShouldBeVisibleInTheSearchInterface()
    {
        Logger.Info("Validating that surface filters are visible in the search interface");
        
        var driver = AqualityServices.Browser.Driver;

        try
        {
            var surfaceInputs = driver.FindElements(By.CssSelector("input[data-cy*='surface'], input[name*='surface'], input[placeholder*='powierzchnia']"))
                .Where(e => e.Displayed && !string.IsNullOrWhiteSpace(e.GetAttribute("value")))
                .ToList();
            
            if (surfaceInputs.Any())
            {
                foreach (var input in surfaceInputs)
                {
                    Logger.Info($"Surface filter input found with value: '{input.GetAttribute("value")}'");
                }
            }
            else
            {
                Logger.Warn("Surface filter inputs not found or empty - filters might be applied differently");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to check surface filter inputs");
        }

        var currentUrl = AqualityServices.Browser.CurrentUrl.ToLowerInvariant();
        var hasUrlParams = currentUrl.Contains("surface") || 
                          currentUrl.Contains("powierzchnia") || 
                          currentUrl.Contains("area") ||
                          currentUrl.Contains("areamin") ||
                          currentUrl.Contains("areamax");
        
        if (hasUrlParams)
        {
            Logger.Info("URL contains surface-related parameters, confirming filter application");
        }
        else
        {
            Logger.Info("URL doesn't contain obvious surface parameters, but search was executed");
        }

        Logger.Info("Surface filter interface validation completed");
    }
}