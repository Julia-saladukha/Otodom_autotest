using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using OpenQA.Selenium;
using NLog;

namespace Autotests_task1.Pages;

public class OfferDetailsPage : BasePage
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

    private ILabel PriceLabel => ElementFactory.GetLabel(By.CssSelector("span[data-cy='adPageHeaderPrice']"), "Offer price");
    private ILabel RoomsLabel => ElementFactory.GetLabel(By.XPath("//span[contains(@data-cy,'rooms-number')]"), "Rooms count");
    private ILabel SurfaceLabel => ElementFactory.GetLabel(By.XPath("//span[contains(@data-cy,'area')]"), "Surface");

    public OfferDetailsPage() : base(By.CssSelector("main"), "Offer Details Page") {}

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
