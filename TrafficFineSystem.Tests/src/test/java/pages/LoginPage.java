package pages;

import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.FindBy;
import org.openqa.selenium.support.PageFactory;
import utils.Driver;

public class LoginPage {

    public LoginPage() {
        // PageFactory, @FindBy notasyonlarını görünce driver üzerinden o elementleri otomatik bulur
        PageFactory.initElements(Driver.getDriver(), this);
    }

    // Görselden tespit ettiğimiz doğru ID
    @FindBy(id = "inputEmail")
    public WebElement emailBox;

    // Görselden tespit ettiğimiz doğru ID
    @FindBy(id = "inputPassword")
    public WebElement passwordBox;

    // Giriş Yap butonu (Genelde formlardaki submit butonu bu css ile rahatça bulunur)
    @FindBy(css = "button[type='submit']")
    public WebElement loginButton;

    // Arayüzü kullanacak aksiyon metodumuz
    public void performLogin(String email, String password) {
        emailBox.clear();
        emailBox.sendKeys(email);

        passwordBox.clear();
        passwordBox.sendKeys(password);

        loginButton.click();
    }
}