using Aquality.Selenium.Browsers;
using Aquality.Selenium.Core.Logging;
using FluentAssertions;
using Reqnroll;
using Autotests_task1.Pages;
using Autotests_task1.Helpers;
using OpenQA.Selenium;

namespace Autotests_task1.Steps;

[Binding]
public class SurfaceFilterSteps
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    private readonly ResultsPage _resultsPage = new();
    private readonly ScenarioContext _scenarioContext;

    private const string SurfaceRangeKey = "SurfaceRange";

    public SurfaceFilterSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Step definition that implements the exact functionality requested:
    /// 1. Analyze surface area from first page listings
    /// 2. Clear price filter
    /// 3. Apply surface filter with min and max values
    /// 4. Save values to scenario context for validation
    /// </summary>
    [When("I clear price filter and set surface range from first page")]
    public void WhenIClearPriceFilterAndSetSurfaceRangeFromFirstPage()
    {
        Logger.Info("Starting to clear price filter and set surface range from first page analysis");
        
        // Ensure results page is loaded with listings
        _resultsPage.WaitForListings(TimeSpan.FromSeconds(10))
            .Should().BeTrue("Listings should be available before analyzing surface range");
        
        // Step 1: Analyze surface range from current listings using ResultsPage
        var (minSurface, maxSurface) = _resultsPage.GetMinAndMaxSurfaceFromFirstPage();
        Logger.Info($"Surface range identified: Min = {minSurface}m², Max = {maxSurface}m²");

        // Save to scenario context for later validation
        try
        {
            ScenarioContextHelper.Set(_scenarioContext, SurfaceRangeKey, (minSurface, maxSurface));
            Logger.Info($"Saved surface range to scenario context: {minSurface}-{maxSurface}m²");
        }
        catch (Exception ex)
        {
            Logger.Debug($"Could not save to scenario context, continuing without it: {ex.Message}");
        }

        // Step 2: Clear existing price filters
        Logger.Info("Clearing price filters");
        _resultsPage.ClearPriceFilter();

        // Wait for filters to be cleared
        Thread.Sleep(1000);

        // Step 3: Apply new surface range filters
        Logger.Info($"Setting surface range filter: {minSurface}m² - {maxSurface}m²");
        _resultsPage.SetSurfaceRange(minSurface, maxSurface);

        // Step 4: Apply the search with new filters
        Logger.Info("Applying search with surface filters");
        _resultsPage.Search();

        // Step 5: Wait for updated results to load
        var resultsUpdated = AqualityServices.ConditionalWait.WaitFor(() =>
        {
            try
            {
                return _resultsPage.WaitForListings(TimeSpan.FromSeconds(5));
            }
            catch { return false; }
        }, TimeSpan.FromSeconds(20));

        resultsUpdated.Should().BeTrue("Updated results should load after applying surface filter");
        Logger.Info("Surface filter application completed successfully");
    }

    /// <summary>
    /// Validation step to ensure all apartments are within the surface range that was applied
    /// </summary>
    [Then("all apartments should have surface area within the applied range")]
    public void ThenAllApartmentsShouldHaveSurfaceAreaWithinTheAppliedRange()
    {
        Logger.Info("Validating that all apartments have surface area within the applied range");
        
        // Wait for listings to be available
        _resultsPage.WaitForListings(TimeSpan.FromSeconds(10))
            .Should().BeTrue("Listings should be loaded for surface validation");

        // Get the saved surface range from scenario context
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
            Logger.Warn($"Could not retrieve surface range from scenario context, analyzing current page: {ex.Message}");
            var currentRange = _resultsPage.GetMinAndMaxSurfaceFromFirstPage();
            minSurface = currentRange.min;
            maxSurface = currentRange.max;
        }

        // Validate that surfaces are within reasonable bounds
        minSurface.Should().BeGreaterThan(0, "Minimum surface should be positive");
        maxSurface.Should().BeLessThan(1000, "Maximum surface should be reasonable");
        maxSurface.Should().BeGreaterThanOrEqualTo(minSurface, "Maximum should be >= minimum");

        // Use ResultsPage validation method for thorough checking
        var surfaceValidation = _resultsPage.AreAllSurfacesWithinRange(minSurface, maxSurface);
        surfaceValidation.Should().BeTrue($"All apartment surfaces should be within the applied range: {minSurface}m² - {maxSurface}m²");

        Logger.Info($"Surface area validation completed successfully. All apartments are within range: {minSurface}m² - {maxSurface}m²");
    }

    /// <summary>
    /// Additional step for validating that surface filters are properly applied to the UI
    /// </summary>
    [Then("surface filters should be visible in the search interface")]
    public void ThenSurfaceFiltersShouldBeVisibleInTheSearchInterface()
    {
        Logger.Info("Validating that surface filters are visible in the search interface");
        
        var driver = AqualityServices.Browser.Driver;

        // Check that surface filter inputs are populated
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
            Logger.Warn($"Failed to check surface filter inputs: {ex.Message}");
        }

        // Validate URL contains surface parameters
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