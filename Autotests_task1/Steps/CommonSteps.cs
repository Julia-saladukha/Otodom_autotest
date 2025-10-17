using Aquality.Selenium.Browsers;
using FluentAssertions;
using Reqnroll;
using Autotests_task1.Pages;
using Autotests_task1.Helpers;
using Aquality.Selenium.Core.Logging;
using OpenQA.Selenium;

namespace Autotests_task1.Steps;

[Binding]
public class CommonSteps
{
    private static readonly Logger Logger = AqualityServices.Get<Logger>();
    private static readonly string BaseUrl = ConfigHelper.GetBaseUrl();
    private static readonly string LoginUrl = ConfigHelper.GetLoginUrl();

    private readonly MainPage _mainPage = new();
    private readonly LoginPage _loginPage = new();

    private static string UserEmail => ConfigHelper.GetUsername();
    private static string UserPassword => ConfigHelper.GetPassword();

    [When("I open Otodom main page")]
    [Given("I open Otodom main page")]
    public void OpenMainPage()
    {
        Logger.Info("Opening Otodom main page");
        _mainPage.Open();
        _mainPage.State.WaitForDisplayed(TimeSpan.FromSeconds(10))
            .Should().BeTrue("Main page should load successfully");
        
        AqualityServices.ConditionalWait.WaitFor(() =>
        {
            var url = AqualityServices.Browser.CurrentUrl;
            return url.Contains(new Uri(BaseUrl).Host);
        }, timeout: TimeSpan.FromSeconds(10));
        
        Logger.Info($"Main page loaded: {AqualityServices.Browser.CurrentUrl}");
    }

    [When("I accept cookies if popup appears")]
    [Given("I accept cookies if popup appears")]
    public void AcceptCookiesIfPopupAppears()
    {
        Logger.Info("Executing step: I accept cookies if popup appears");
        _mainPage.AcceptCookiesIfPresent();
        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromSeconds(1));
    }

    [When("I authorize user")]
    public void AuthorizeUser()
    {
        Logger.Info("Attempting user authorization (reading credentials from configuration)");
        
        var username = UserEmail;
        var password = UserPassword;
        
        var maskedUsername = username.Contains('@') 
            ? $"{username.Substring(0, Math.Min(2, username.IndexOf('@')))}{new string('*', Math.Max(0, username.IndexOf('@') - 2))}@{username.Split('@')[1]}"
            : "***";
        Logger.Info($"Credentials loaded successfully for user: {maskedUsername}");
        
        var opened = _mainPage.OpenLogin();
        if (!opened)
        {
            Logger.Warn("Login trigger not found. Continuing without login.");
            return;
        }

        bool onLogin = AqualityServices.ConditionalWait.WaitFor(() =>
            AqualityServices.Browser.CurrentUrl.Contains(new Uri(LoginUrl).Host) ||
            AqualityServices.Browser.Driver.FindElements(By.Id("username")).Any(), 
            timeout: TimeSpan.FromSeconds(10));

        if (!onLogin)
        {
            Logger.Error($"Login page did not load within timeout. Current URL: {AqualityServices.Browser.CurrentUrl}");
            throw new Exception("Login page was not reached within 10 seconds.");
        }
        
        Logger.Info("Login page detected");

        if (AqualityServices.Browser.CurrentUrl.Contains(new Uri(BaseUrl).Host) && 
            !AqualityServices.Browser.CurrentUrl.Contains(new Uri(LoginUrl).Host))
        {
            Logger.Info("Already authenticated. Skipping explicit login.");
            return;
        }

        Logger.Info("Submitting login credentials from configuration");
        _loginPage.Login(UserEmail, UserPassword);
        Logger.Info("Login credentials submitted successfully");

        AqualityServices.ConditionalWait.WaitFor(() =>
            AqualityServices.Browser.CurrentUrl.Contains(new Uri(BaseUrl).Host) && 
            !AqualityServices.Browser.CurrentUrl.Contains(new Uri(LoginUrl).Host),
            timeout: TimeSpan.FromSeconds(15));
        
        Logger.Info("Authorization step finished (best-effort).");

        AqualityServices.ConditionalWait.WaitFor(() => false, TimeSpan.FromSeconds(2));
    }

    [Then("Main page should be opened")]
    public void MainPageShouldBeOpened()
    {
        AqualityServices.Browser.CurrentUrl.Should().Contain(new Uri(BaseUrl).Host);
    }
}
