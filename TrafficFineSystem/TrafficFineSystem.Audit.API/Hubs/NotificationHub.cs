using Microsoft.AspNetCore.SignalR;

namespace TrafficFineSystem.Audit.API.Hubs;

public class NotificationHub : Hub
{
    // Arayüz (WebApp) bu sınıfa bağlanacak. 
    // Şimdilik sadece sunucudan istemciye mesaj iteceğimiz (push) için burası boş kalabilir.
}