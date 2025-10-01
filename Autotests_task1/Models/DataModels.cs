using OpenQA.Selenium;

namespace Autotests_task1.Models;

public class ListingData
{
    public int Index { get; set; }
    public IWebElement Element { get; set; }
    public int? Price { get; set; }
    public int? Surface { get; set; }
    public int? Rooms { get; set; }
    public string? Title { get; set; }
}

public class OfferData
{
    public int? Price { get; set; }
    public double? Surface { get; set; }
    public int? Rooms { get; set; }
}

public class OfferValidationResult
{
    public int ListingIndex { get; set; }
    public ListingData ListingData { get; set; }
    public OfferData OfferData { get; set; }
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; }
}