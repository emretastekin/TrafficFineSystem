Feature: Dashboard ve Yetkilendirme Kontrolü

  Scenario: Yetkili bir Admin giriş yaptığında tüm istatistik kartlarını görmelidir
    Given Kullanıcı "admin@traffic.com" ve şifresi ile sisteme giriş yapar
    When Kullanıcı Dashboard sayfasına yönlendirilir
    Then Sayfada Tahsil Edilen, Onay Bekleyen, Yeni Kesilen, İptal Edilen ve Kayıtlı Araçlar kartları görünür olmalıdır