package tests;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import org.junit.jupiter.api.Assertions;
import pages.DashboardPage;
import pages.LoginPage;
import utils.Driver;

public class DashboardSteps {

    LoginPage loginPage = new LoginPage();
    DashboardPage dashboardPage = new DashboardPage();

    @Given("Kullanıcı {string} ve şifresi ile sisteme giriş yapar")
    public void kullanici_ve_sifresi_ile_sisteme_giris_yapar(String email) throws InterruptedException {
        // C# projenin çalıştığı URL'ye gidiyoruz
        Driver.getDriver().get("http://localhost:5129/Auth/Login");

        Thread.sleep(2000);

        // Şifreyi sabit veriyoruz
        loginPage.performLogin(email, "123456");

    }

    @When("Kullanıcı Dashboard sayfasına yönlendirilir")
    public void kullanici_dashboard_sayfasina_yonlendirilir() throws InterruptedException {

        Thread.sleep(2000);

        String currentUrl = Driver.getDriver().getCurrentUrl();
        Assertions.assertTrue(currentUrl.equals("http://localhost:5129/") || currentUrl.contains("Home"),
                "Dashboard'a yönlendirilemedi!");
    }

    @Then("Sayfada Tahsil Edilen, Onay Bekleyen, Yeni Kesilen, İptal Edilen ve Kayıtlı Araçlar kartları görünür olmalıdır")
    public void sayfada_tahsil_edilen_onay_bekleyen_yeni_kesilen_iptal_edilen_ve_kayitli_araclar_kartlari_gorunur_olmalidir() throws InterruptedException {
        Assertions.assertTrue(dashboardPage.dashboardHeader.isDisplayed(), "Dashboard başlığı görünmüyor!");
        Assertions.assertTrue(dashboardPage.totalPaidCardTitle.isDisplayed(), "Tahsil Edilen kartı görünmüyor!");
        Assertions.assertTrue(dashboardPage.totalVehiclesCardTitle.isDisplayed(), "Kayıtlı Araçlar kartı görünmüyor!");

        Thread.sleep(2000);
    }
}