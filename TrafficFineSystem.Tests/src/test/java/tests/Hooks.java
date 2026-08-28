package tests;

import io.cucumber.java.After;
import utils.Driver;

public class Hooks {

    // Her senaryonun sonunda otomatik olarak çalışır ve tarayıcıyı kapatır
    @After
    public void tearDown() {
        Driver.closeDriver();
    }
}