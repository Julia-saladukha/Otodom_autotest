using Autotests_task1.Pages;
using Autotests_task1.Models;
using Aquality.Selenium.Browsers;
using NLog;
using OpenQA.Selenium;

namespace Autotests_task1.Examples;

/// <summary>
/// Example demonstrating how to collect all listings and validate them against offer details
/// </summary>
public class OfferValidationExample
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void RunExample()
    {
        Logger.Info("=== OFFER VALIDATION EXAMPLE ===");
        
        var mainPage = new MainPage();
        var resultsPage = new ResultsPage();
        var offerDetailsPage = new OfferDetailsPage();

        // 1. Navigate to search results
        mainPage.Open();
        mainPage.AcceptCookiesIfPresent();
        
        // 2. Perform search with filters
        mainPage.SetLocation("Warszawa");
        mainPage.SetPriceRange(200000, 1000000);
        mainPage.ClickSearchButton();
        
        // 3. Collect all listings data
        resultsPage.WaitForListings(TimeSpan.FromSeconds(10));
        var allListings = resultsPage.GetAllListingsData();
        
        Logger.Info($"Found {allListings.Count} listings on the page");
        
        // 4. Validate first 3 offers (limited for example)
        var validationResults = new List<OfferValidationResult>();
        
        foreach (var listing in allListings.Take(3))
        {
            Logger.Info($"Processing listing {listing.Index}: {listing.Title}");
            
            var result = new OfferValidationResult
            {
                ListingIndex = listing.Index,
                ListingData = listing
            };
            
            // Click on offer
            var link = listing.Element.FindElement(By.TagName("a"));
            ((IJavaScriptExecutor)AqualityServices.Browser.Driver)
                .ExecuteScript("arguments[0].removeAttribute('target');", link);
            link.Click();
            
            // Wait for navigation
            AqualityServices.ConditionalWait.WaitFor(() =>
                AqualityServices.Browser.CurrentUrl.Contains("/oferta"), 
                TimeSpan.FromSeconds(15));
            
            // Read offer details
            var (pagePrice, pageSurface, pageRooms) = offerDetailsPage.ReadDetailsWithFallback();
            
            result.OfferData = new OfferData
            {
                Price = pagePrice,
                Surface = pageSurface,
                Rooms = pageRooms
            };
            
            // Validate consistency
            var messages = new List<string>();
            
            if (listing.Price.HasValue && pagePrice.HasValue)
            {
                var priceDiff = Math.Abs(listing.Price.Value - pagePrice.Value);
                if (priceDiff > pagePrice.Value * 0.05) // 5% tolerance
                {
                    messages.Add($"Price mismatch: {listing.Price} vs {pagePrice}");
                }
            }
            
            if (listing.Surface.HasValue && pageSurface.HasValue)
            {
                var surfaceDiff = Math.Abs(listing.Surface.Value - pageSurface.Value);
                if (surfaceDiff > 5) // 5m² tolerance
                {
                    messages.Add($"Surface mismatch: {listing.Surface}m² vs {pageSurface}m²");
                }
            }
            
            if (listing.Rooms.HasValue && pageRooms.HasValue)
            {
                if (listing.Rooms.Value != pageRooms.Value)
                {
                    messages.Add($"Rooms mismatch: {listing.Rooms} vs {pageRooms}");
                }
            }
            
            result.IsValid = !messages.Any();
            result.ValidationMessage = result.IsValid ? "All data matches" : string.Join("; ", messages);
            
            validationResults.Add(result);
            
            Logger.Info($"Listing {listing.Index} validation: {(result.IsValid ? "PASS" : "FAIL")} - {result.ValidationMessage}");
            
            // Go back to results page
            AqualityServices.Browser.GoBack();
            resultsPage.WaitForListings(TimeSpan.FromSeconds(5));
        }
        
        // 5. Print summary
        var successRate = (double)validationResults.Count(r => r.IsValid) / validationResults.Count * 100;
        Logger.Info($"=== VALIDATION COMPLETE ===");
        Logger.Info($"Success rate: {successRate:F1}% ({validationResults.Count(r => r.IsValid)}/{validationResults.Count})");
        
        AqualityServices.Browser.Quit();
    }
}