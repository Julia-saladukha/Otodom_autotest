using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using OpenQA.Selenium;
using NLog;
using Aquality.Selenium.Browsers;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Autotests_task1.Pages;

public class OfferDetailsPage : BasePage
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

    private ILabel PriceLabel => ElementFactory.GetLabel(By.CssSelector("span[data-cy='adPageHeaderPrice']"), "Offer price");
    private ILabel RoomsLabel => ElementFactory.GetLabel(By.XPath("//span[contains(@data-cy,'rooms-number')]"), "Rooms count");
    private ILabel SurfaceLabel => ElementFactory.GetLabel(By.XPath("//span[contains(@data-cy,'area')]"), "Surface");

    public OfferDetailsPage() : base(By.CssSelector("main"), "Offer Details Page") {}

    public (int? price, double? surface, int? rooms) ReadDetailsWithFallback()
    {
        Logger.Info("Reading offer details with fallback methods");
        
        var price = GetPriceWithFallback();
        var surface = GetSurfaceWithFallback();
        var rooms = GetRoomsWithFallback();

        Logger.Info($"Offer details read -> Price: {price}, Surface: {surface}, Rooms: {rooms}");
        return (price, surface, rooms);
    }

    private int? GetPriceWithFallback()
    {
        var priceSelectors = new[]
        {
            By.CssSelector("span[data-cy='adPageHeaderPrice']"),
            By.XPath("//span[contains(@class,'price') or contains(@data-cy,'price')]"),
            By.XPath("//*[contains(text(),'PLN') or contains(text(),'z?')]"),
            By.CssSelector(".price"),
            By.CssSelector("[class*='price']"),
            By.XPath("//strong[contains(text(),'PLN') or contains(text(),'z?')]")
        };

        var driver = AqualityServices.Browser.Driver;
        
        foreach (var selector in priceSelectors)
        {
            try
            {
                var elements = driver.FindElements(selector);
                foreach (var element in elements.Where(e => e.Displayed))
                {
                    var text = element.Text?.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    var price = ParsePriceFromText(text);
                    if (price.HasValue)
                    {
                        Logger.Info($"Found price using selector {selector}: {price}");
                        return price;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to get price with selector: {selector}");
            }
        }

        Logger.Warn("Could not extract price from offer page");
        return null;
    }

    private double? GetSurfaceWithFallback()
    {
        var surfaceSelectors = new[]
        {
            By.XPath("//span[contains(@data-cy,'area') or contains(@data-cy,'surface')]"),
            By.XPath("//*[contains(text(),'m²') or contains(text(),'m2')]"),
            By.XPath("//span[contains(@class,'area') or contains(@class,'surface')]"),
            By.XPath("//*[contains(@aria-label,'powierzchnia') or contains(@title,'powierzchnia')]"),
            By.CssSelector("[class*='surface']"),
            By.CssSelector("[class*='area']")
        };

        var driver = AqualityServices.Browser.Driver;
        
        foreach (var selector in surfaceSelectors)
        {
            try
            {
                var elements = driver.FindElements(selector);
                foreach (var element in elements.Where(e => e.Displayed))
                {
                    var text = element.Text?.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    var surface = ParseSurfaceFromText(text);
                    if (surface.HasValue)
                    {
                        Logger.Info($"Found surface using selector {selector}: {surface}m²");
                        return surface;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to get surface with selector: {selector}");
            }
        }

        Logger.Warn("Could not extract surface from offer page");
        return null;
    }

    private int? GetRoomsWithFallback()
    {
        var roomsSelectors = new[]
        {
            By.XPath("//span[contains(@data-cy,'rooms-number') or contains(@data-cy,'rooms')]"),
            By.XPath("//*[contains(text(),'pokoi') or contains(text(),'pokoje') or contains(text(),'rooms')]"),
            By.XPath("//span[contains(@class,'rooms') or contains(@aria-label,'pokoi')]"),
            By.CssSelector("[class*='rooms']"),
            By.XPath("//*[contains(@title,'pokoi') or contains(@title,'rooms')]")
        };

        var driver = AqualityServices.Browser.Driver;
        
        foreach (var selector in roomsSelectors)
        {
            try
            {
                var elements = driver.FindElements(selector);
                foreach (var element in elements.Where(e => e.Displayed))
                {
                    var text = element.Text?.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    var rooms = ParseRoomsFromText(text);
                    if (rooms.HasValue)
                    {
                        Logger.Info($"Found rooms using selector {selector}: {rooms}");
                        return rooms;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to get rooms with selector: {selector}");
            }
        }

        Logger.Warn("Could not extract rooms count from offer page");
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

    private double? ParseSurfaceFromText(string text)
    {
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
                    // Validate reasonable surface area range
                    if (value >= 10 && value <= 500)
                    {
                        return value;
                    }
                }
            }
        }
        return null;
    }

    private int? ParseRoomsFromText(string text)
    {
        var patterns = new[]
        {
            @"(\d+)\s*poko[ij]",           // "3 pokoi" or "3 pokoje"
            @"(\d+)\s*rooms?",            // "3 room" or "3 rooms"
            @"^(\d+)$"                    // Just a number if in context of rooms
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out var rooms))
                {
                    // Validate reasonable room count
                    if (rooms >= 1 && rooms <= 10)
                    {
                        return rooms;
                    }
                }
            }
        }
        return null;
    }

    // Legacy methods for backward compatibility
    public int GetPrice() => int.Parse(new string(PriceLabel.Text.Where(char.IsDigit).ToArray()));
    public int GetRooms() => int.Parse(new string(RoomsLabel.Text.Where(char.IsDigit).ToArray()));
    public double GetSurface() => double.Parse(new string(SurfaceLabel.Text.Replace(",", ".").Where(c=>char.IsDigit(c) || c=='.').ToArray()));
    public (int price, double surface, int rooms) ReadDetails()
    {
        var price = GetPrice();
        var surface = GetSurface();
        var rooms = GetRooms();
        Logger.Info($"Offer details read -> Price: {price}, Surface: {surface}, Rooms: {rooms}");
        return (price, surface, rooms);
    }
}
