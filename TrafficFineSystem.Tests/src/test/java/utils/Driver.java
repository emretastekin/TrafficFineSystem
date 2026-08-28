package utils;

import org.openqa.selenium.WebDriver;
import org.openqa.selenium.chrome.ChromeDriver;
import java.time.Duration;

public class Driver {

    // driver nesnemizi private ve static yaparak korumaya alıyoruz
    private static WebDriver driver;

    // Dışarıdan nesne üretilmesini engelliyoruz (Singleton Pattern)
    private Driver() {
    }

    public static WebDriver getDriver() {
        if (driver == null) {
            // Selenium 4, ChromeDriver'ı arka planda otomatik olarak ayarlayacaktır
            driver = new ChromeDriver();
            driver.manage().window().maximize();
            driver.manage().timeouts().implicitlyWait(Duration.ofSeconds(10));
        }
        return driver;
    }

    public static void closeDriver() {
        if (driver != null) {
            driver.quit();
            driver = null;
        }
    }
}