package pages;

import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.FindBy;
import org.openqa.selenium.support.PageFactory;
import utils.Driver;

public class DashboardPage {

    public DashboardPage() {
        PageFactory.initElements(Driver.getDriver(), this);
    }

    // Arayüzdeki "Sistem Özeti" başlığını yakalayarak sayfaya girdiğimizi doğrulayacağız
    @FindBy(xpath = "//h2[contains(text(),'Sistem Özeti')]")
    public WebElement dashboardHeader;

    // Tahsil Edilen ceza kartı başlığı
    @FindBy(xpath = "//h5[contains(text(),'Tahsil Edilen')]")
    public WebElement totalPaidCardTitle;

    // Kayıtlı Araçlar kartı
    @FindBy(xpath = "//h5[contains(text(),'Kayıtlı Araçlar')]")
    public WebElement totalVehiclesCardTitle;
}