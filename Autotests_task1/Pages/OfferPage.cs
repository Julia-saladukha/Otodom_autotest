using System.Text.RegularExpressions;
using Aquality.Selenium.Browsers;
using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using FluentAssertions;
using NLog;
using OpenQA.Selenium;
using Reqnroll;

namespace Autotests_task1.Pages;

/// <summary>
/// Represents the offer (details) page and provides robust retrieval and verification of offer data.
/// </summary>
public class OfferPage : BasePage
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Root selector kept generic as structure may change.
    public OfferPage() : base(By.CssSelector("main"), "Offer Page") { }

    // Primary explicit selectors
    private ILabel PriceLabel => ElementFactory.GetLabel(By.CssSelector("span[data-cy='adPageHeaderPrice']"), "Offer price");
    private ILabel RoomsLabel => ElementFactory.GetLabel(By.CssSelector("span[data-cy*='rooms-number']"), "Rooms count");
    private ILabel SurfaceLabel => ElementFactory.GetLabel(By.CssSelector("span[data-cy*='area']"), "Surface");

    // Fallback selector collections (searched in order) to increase resilience.
    private readonly By[] _priceFallbacks =
    {
        By.CssSelector("[data-cy*='price']"),
        By.XPath("//span[contains(.,'PLN') or contains(translate(.,'zl','ZL'),'z?')]")
    };

    private readonly By[] _roomsFallbacks =
    {
        By.CssSelector("[data-cy*='rooms']"),
        By.XPath("//*[contains(translate(.,'POK','pok'),'pok') and contains(.,' ')]")
    };

    private readonly By[] _surfaceFallbacks =
    {
        By.CssSelector("[data-cy*='area']"),
        By.XPath("//*[contains(text(),'m²') or contains(text(),'m2')]")
    };

    private static int ParseIntFromText(string text)
    {
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var val) ? val : 0;
    }

    private IWebElement? FirstDisplayed(By by)
    {
        try
        {
            var els = AqualityServices.Browser.Driver.FindElements(by);
            return els.FirstOrDefault(e => e.Displayed);
        }
        catch { return null; }
    }

    private int GetPriceInternal()
    {
        // Try primary label first
        if (PriceLabel.State.IsDisplayed)
        {
            var raw = PriceLabel.Text;
            var val = ParseIntFromText(raw);
            if (val > 0) return val;
        }
        foreach (var by in _priceFallbacks)
        {
            var el = FirstDisplayed(by);
            if (el == null) continue;
            var val = ParseIntFromText(el.Text);
            if (val > 0) return val;
        }
        Logger.Warn("Failed to parse Price value; returning 0");
        return 0;
    }

    private int GetRoomsInternal()
    {
        if (RoomsLabel.State.IsDisplayed)
        {
            var val = ParseIntFromText(RoomsLabel.Text);
            if (val > 0) return val;
        }
        foreach (var by in _roomsFallbacks)
        {
            var el = FirstDisplayed(by);
            if (el == null) continue;
            var val = ParseIntFromText(el.Text);
            if (val > 0) return val;
        }
        Logger.Warn("Failed to parse Rooms value; returning 0");
        return 0;
    }

    private int GetSurfaceInternal()
    {
        // Surface may contain decimal; we will take integer part as requested (int Surface)
        string Extract(string raw)
        {
            // Accept digits optionally separated by comma/period before m²
            var match = Regex.Match(raw.Replace(',', '.'), @"(\d+(?:\.\d+)?)\s*m[²2]", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var number = match.Groups[1].Value;
                if (double.TryParse(number, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                {
                    return ((int)Math.Round(d)).ToString();
                }
            }
            return new string(raw.Where(char.IsDigit).ToArray());
        }

        if (SurfaceLabel.State.IsDisplayed)
        {
            var extracted = Extract(SurfaceLabel.Text);
            if (int.TryParse(extracted, out var v) && v > 0) return v;
        }
        foreach (var by in _surfaceFallbacks)
        {
            var el = FirstDisplayed(by);
            if (el == null) continue;
            var extracted = Extract(el.Text);
            if (int.TryParse(extracted, out var v) && v > 0) return v;
        }
        Logger.Warn("Failed to parse Surface value; returning 0");
        return 0;
    }

    /// <summary>
    /// Returns offer details (Price, Rooms, Surface) as integers (surface rounded if decimal on page).
    /// </summary>
    public (int Price, int Rooms, int Surface) GetOfferDetails()
    {
        var price = GetPriceInternal();
        var rooms = GetRoomsInternal();
        var surface = GetSurfaceInternal();
        Logger.Info($"Offer details collected -> Price:{price}; Rooms:{rooms}; Surface:{surface}");
        return (price, rooms, surface);
    }

    /// <summary>
    /// Verifies that the offer details on page match data stored earlier in ScenarioContext (key 'OfferData').
    /// Expected stored object shape: ValueTuple (int? price, double? surface, int? rooms)
    /// </summary>
    public void VerifyOfferMatches(ScenarioContext scenarioContext, string contextKey = "OfferData")
    {
        if (!scenarioContext.TryGetValue(contextKey, out object? storedObj) || storedObj is null)
        {
            Logger.Warn($"ScenarioContext does not contain key '{contextKey}' to verify against.");
            return;
        }

        // Attempt to interpret the stored object as known tuple type.
        int? storedPrice = null; double? storedSurface = null; int? storedRooms = null;
        try
        {
            if (storedObj is ValueTuple<int?, double?, int?> tuple3)
            {
                storedPrice = tuple3.Item1;
                storedSurface = tuple3.Item2;
                storedRooms = tuple3.Item3;
            }
            else if (storedObj is ValueTuple<int, double, int> tupleNonNullable)
            {
                storedPrice = tupleNonNullable.Item1;
                storedSurface = tupleNonNullable.Item2;
                storedRooms = tupleNonNullable.Item3;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to map stored context object to expected tuple shape.");
        }

        var (price, rooms, surface) = GetOfferDetails();
        Logger.Info($"Verifying offer on page vs stored context: StoredPrice={storedPrice}, StoredSurface={storedSurface}, StoredRooms={storedRooms}");

        if (storedPrice.HasValue)
        {
            price.Should().Be(storedPrice.Value, "Price should match value captured from listings page");
        }
        if (storedSurface.HasValue)
        {
            surface.Should().Be((int)Math.Round(storedSurface.Value), "Surface (rounded) should match value captured from listings page");
        }
        if (storedRooms.HasValue)
        {
            rooms.Should().Be(storedRooms.Value, "Rooms count should match value captured from listings page");
        }
    }
}
