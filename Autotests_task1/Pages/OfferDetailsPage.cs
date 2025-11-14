using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using OpenQA.Selenium;
using Aquality.Selenium.Core.Logging;
using Aquality.Selenium.Browsers;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Autotests_task1.Pages;

public class OfferDetailsPage : BasePage
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();

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
        var priceSelector = By.CssSelector("span[data-cy='adPageHeaderPrice']");
        var priceElements = Factory.FindElements<ILabel>(priceSelector, "Price labels");
        var element = priceElements.FirstOrDefault(e => e != null && SafeIsDisplayedAndAlive(e));

        if (element == null)
        {
            Logger.Warn("No visible and alive price element found");
            return null;
        }

        var text = element.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Logger.Warn("Price element text is empty");
            return null;
        }

        var price = ParsePriceFromText(text);
        if (price.HasValue)
        {
            Logger.Info($"Found price: {price}");
            return price;
        }

        Logger.Warn($"Failed to parse price from text: '{text}'");
        return null;
    }

    private bool SafeIsDisplayedAndAlive(IElement element)
    {
        return AqualityServices.ConditionalWait.WaitFor(() =>
        {
            return element.State.IsDisplayed && element.GetElement().TagName != null;
        }, TimeSpan.FromMilliseconds(100));
    }

    private double? GetSurfaceWithFallback()
    {
        var surfaceSelector = By.XPath("//*[@data-sentry-element='ItemGridContainer']//*[contains(translate(text(),'²М','2M'),'m2')]");
        var surfaceElements = Factory.FindElements<ILabel>(surfaceSelector, "Surface labels");
        var element = surfaceElements.FirstOrDefault(e => e != null && SafeIsDisplayedAndAlive(e));

        if (element == null)
        {
            Logger.Warn("No visible surface element found inside ItemGridContainer");
            return null;
        }

        var text = element.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Logger.Warn("Surface element text is empty");
            return null;
        }

        var surface = ParseSurfaceFromText(text);
        if (surface.HasValue)
        {
            Logger.Info($"Found surface: {surface} m²");
            return surface;
        }

        Logger.Warn($"Failed to parse surface from text: '{text}'");
        return null;
    }

    private int? GetRoomsWithFallback()
    {
        var roomsSelector = By.XPath("//*[contains(text(),'pokoi') or contains(text(),'pokoje') or contains(text(),'rooms')]");
        var roomsElements = Factory.FindElements<ILabel>(roomsSelector, "Rooms labels");
        var element = roomsElements.FirstOrDefault(e => e != null && SafeIsDisplayedAndAlive(e));

        if (element == null)
        {
            Logger.Warn("No visible and alive rooms element found");
            return null;
        }

        var text = element.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Logger.Warn("Rooms element text is empty");
            return null;
        }

        var rooms = ParseRoomsFromText(text);
        if (rooms.HasValue)
        {
            Logger.Info($"Found rooms: {rooms}");
            return rooms;
        }

        Logger.Warn($"Failed to parse rooms from text: '{text}'");
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
}
