using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Elements;
using OpenQA.Selenium;

namespace Autotests_task1.Pages;

public class LoginPage : BasePage
{
    private readonly ITextBox _email = ElementFactory.GetTextBox(By.Id("username"), "Email");
    private readonly ITextBox _password = ElementFactory.GetTextBox(By.Id("password"), "Password");
    private readonly IButton _submit = ElementFactory.GetButton(By.XPath("//button[@type='submit']"), "Submit");

    public LoginPage() : base(By.CssSelector("form"), "Login Page") { }

    public void Login(string email, string password)
    {
        _email.ClearAndType(email);
        _password.ClearAndType(password);
        _submit.Click();
    }
}
