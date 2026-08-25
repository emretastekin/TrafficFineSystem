Traffic Fine Management System (TrafficFineSystem)

-->

.NET 9 teknolojisi, Microservices mimari yaklaşımları ve Event-Driven (Olay Tabanlı) iletişim modelleri kullanılarak geliştirilmiş kapsamlı bir Trafik Cezası Yönetim ve Denetim (Audit) sistemidir. Sistem, güçlü bir arka plan (Backend) mimarisi ile kullanıcı dostu, gerçek zamanlı güncellenen bir Yönetim Paneli (Frontend) sunar.


Mimari ve Kullanılan Teknolojiler

-->

Bu projede sorumlulukların ayrılması (SoC) ve servisler arası gevşek bağlılık (loose coupling) ilkeleri gözetilmiştir.

Backend & Frontend: .NET 9, ASP.NET Core Web API, ASP.NET Core MVC (Web Arayüzü)

Kimlik Doğrulama & Güvenlik: Firebase Authentication (JWT Token, Cookie ve Role/Permission tabanlı yetkilendirme)

Gerçek Zamanlı İletişim (Real-Time): SignalR (WebSocket)

ORM: Entity Framework Core (Code-First yaklaşımı)

Veritabanı: Azure SQL Edge / SQL Server

Mesaj Kuyruğu / Event Broker: Apache Kafka (Zookeeper destekli)

Önbellekleme (Caching): Redis

Konteynerleştirme: Docker & Docker Compose

Test Otomasyonu: Selenium WebDriver, JUnit, Cucumber, Appium




Sistem Bileşenleri

-->

Core.API (TrafficFineSystem.Core.API): Trafik cezalarının ve araçların yönetildiği ana servistir. Firebase üzerinden gelen JWT Token'ları doğrular (Authorization). Yeni bir ceza oluşturulduğunda veya durumu güncellendiğinde (Örn: Yeni -> Ödendi), veritabanını günceller, Redis önbelleğini temizler ve ilgili olayları Kafka broker'ına publish eder.

Audit.API (TrafficFineSystem.Audit.API): Arka planda çalışan tüketici (Consumer) servistir. Kafka üzerindeki fine-status-events kanalını dinler, gelen olayları yakalar ve FineHistories tablosuna güvenli bir şekilde loglar. Aynı zamanda SignalR Hub görevi görerek arayüze gerçek zamanlı güncellemeler fırlatır.

WebApp (TrafficFineSystem.WebApp): Yöneticilerin sistemi kullandığı MVC tabanlı kontrol panelidir. Firebase üzerinden kullanıcı girişi sağlar. Core.API ile HTTP Client üzerinden güvenli (Bearer Token) haberleşir. İçerisindeki SignalR dinleyicisi sayesinde, sistemde bir ceza kesildiğinde veya güncellendiğinde sayfayı manuel yenilemeye gerek kalmadan tabloları anında günceller.

Shared (TrafficFineSystem.Shared): Ortak kullanılan veritabanı modelleri, entity'ler ve event contract'larının bulunduğu katman.




Kurulum ve Çalıştırma Adımları

-->

Projeyi lokal ortamınızda ayağa kaldırmak için aşağıdaki adımları sırasıyla takip edebilirsiniz.

Ön Koşullar

Docker ve Docker Compose

.NET 9 SDK

JetBrains Rider veya Visual Studio / VS Code

Bir Firebase Projesi (Web API Key)


1. Repoyu Klonlayın

--> 

git clone https://github.com/kullaniciadin/TrafficFineSystem.git
cd TrafficFineSystem



2. Docker Servislerini Ayağa Kaldırın

Projenin ana dizininde bulunan docker-compose.yml dosyasını kullanarak SQL Edge, Redis, Zookeeper ve Kafka servislerini başlatın:

-->

docker-compose up -d


3. Veritabanı Migrations İşlemlerini Uygulayın

Core.API projesine ait veritabanı tablolarını ve migration'ları veritabanına yansıtın:

-->

dotnet ef database update --project TrafficFineSystem.Core.API


4. Firebase Ayarlarını Yapılandırın

-->

TrafficFineSystem.WebApp ve TrafficFineSystem.Core.API içerisindeki appsettings.json dosyalarına kendi Firebase API Key ve Project ID bilgilerinizi ekleyin.



5. Servisleri Çalıştırın

-->

Sistemin olay tabanlı (event-driven) ve gerçek zamanlı akışını test etmek için projeleri aynı anda çalıştırın:

TrafficFineSystem.Core.API (Producer & Main API)

TrafficFineSystem.Audit.API (Consumer & SignalR Hub)

TrafficFineSystem.WebApp (MVC UI)


