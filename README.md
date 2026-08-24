Traffic Fine Management System (TrafficFineSystem)

.NET 9 teknolojisi, Microservices mimari yaklaşımları ve Event-Driven (Olay Tabanlı) iletişim modelleri kullanılarak geliştirilmiş kapsamlı bir Trafik Cezası Yönetim ve Denetim (Audit) sistemidir.


--> Mimari ve Kullanılan Teknolojiler

Bu projede sorumlulukların ayrılması (SoC) ve servisler arası gevşek bağlılık (loose coupling) ilkeleri gözetilmiştir.

Backend: .NET 9, ASP.NET Core Web API

ORM: Entity Framework Core (Code-First yaklaşımı)

Veritabanı: Azure SQL Edge / SQL Server

Mesaj Kuyruğu / Event Broker: Apache Kafka (Zookeeper destekli)

Önbellekleme (Caching): Redis

Konteynerleştirme: Docker & Docker Compose

Test Otomasyonu: Selenium WebDriver, JUnit, Cucumber, Appium




--> Sistem Bileşenleri

Core.API (TrafficFineSystem.Core.API): Trafik cezalarının ve araçların yönetildiği ana servis. Yeni bir ceza oluşturulduğunda (Fine) ilgili olayları Kafka broker'ına publish eder.

Audit.API (TrafficFineSystem.Audit.API): Arka planda çalışan tüketici (Consumer) servis. Kafka üzerindeki fine-status-events kanalını dinler, gelen olayları yakalar ve FineHistories tablosuna güvenli bir şekilde loglar.

Shared (TrafficFineSystem.Shared): Ortak kullanılan veritabanı modelleri, entity'ler ve event contract'larının bulunduğu katman.




--> Kurulum ve Çalıştırma Adımları

Projeyi kendi lokal ortamınızda ayağa kaldırmak için aşağıdaki adımları sırasıyla takip edebilirsiniz.

Ön Koşullar
Docker ve Docker Compose

.NET 9 SDK

JetBrains Rider veya Visual Studio / VS Code






1. Repoyu Klonlayın

git clone https://github.com/kullaniciadin/TrafficFineSystem.git
cd TrafficFineSystem



2. Docker Servislerini Ayağa Kaldırın

Projenin ana dizininde bulunan docker-compose.yml dosyasını kullanarak SQL Edge, Redis, Zookeeper ve Kafka servislerini başlatın:

docker-compose up -d



3. Veritabanı Migrations İşlemlerini Uygulayın
   
Core.API projesine ait veritabanı tablolarını ve migration'ları veritabanına yansıtın:

dotnet ef database update --project TrafficFineSystem.Core.API



4. Servisleri Çalıştırın
   
Sistemin olay tabanlı (event-driven) akışını test etmek için servisleri şu sıra ile çalıştırmanız önerilir:

--> Audit.API'yi Başlatın (Consumer):

dotnet run --project TrafficFineSystem.Audit.API



--> Core.API'yi Başlatın (Producer) ve Test Edin:

Core.API projesini çalıştırın ve Swagger arayüzüne (/swagger) gidin.

Önce /api/Vehicles endpoint'ini kullanarak sisteme bir araç kaydedin.

Ardından /api/Fines endpoint'ini kullanarak yeni bir ceza oluşturun.

Core.API mesajı Kafka'ya iletecek, Audit.API ise bu mesajı anında yakalayarak FineHistories tablosuna işleyecektir.


<img width="846" height="342" alt="image" src="https://github.com/user-attachments/assets/a8723542-4bce-4cd6-b4a7-75d21003577e" />
