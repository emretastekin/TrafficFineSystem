using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace TrafficFineSystem.Audit.API.Hubs;

public class NotificationHub : Hub
{
    // Sistemdeki aktif kullanıcıları ve onların açık olan sekmelerini/cihazlarını tutan eşzamanlı sözlük
    // Key: UserId (Firebase UID), Value: O kullanıcıya ait açık sekmelerin Connection ID listesi
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();

    // 1. Tarayıcı (WebApp) açıldığında kendi User ID'sini Hub'a kaydettirir
    public void RegisterUser(string userId)
    {
        var connectionId = Context.ConnectionId;

        UserConnections.AddOrUpdate(
            userId,
            new HashSet<string> { connectionId },
            (key, existingConnections) =>
            {
                lock (existingConnections)
                {
                    existingConnections.Add(connectionId);
                }
                return existingConnections;
            });
            
        Console.WriteLine($"[SignalR] Kullanıcı {userId} bağlandı. Toplam açık sekmesi: {UserConnections[userId].Count}");
    }

    // 2. Kullanıcı Çıkış Yap'a bastığında, DİĞER cihaz/sekmelerine sinyal gönderir
    public async Task NotifyLogout(string userId)
    {
        if (UserConnections.TryGetValue(userId, out var connectionIds))
        {
            List<string> otherConnections;
            lock (connectionIds)
            {
                // Çıkışa basan ana sekmeyi dışarıda bırakıp diğerlerini hedef alıyoruz
                otherConnections = connectionIds.Where(c => c != Context.ConnectionId).ToList();
            }

            if (otherConnections.Any())
            {
                // SADECE o kullanıcının diğer sekmelerine özel mesaj atıyoruz (Clients.All DEĞİL!)
                await Clients.Clients(otherConnections).SendAsync("ForceLogout");
            }
        }
    }

    // 3. Kullanıcı sekmeyi veya tarayıcıyı kapattığında defterden siler (Hafıza sızıntısını önler)
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        foreach (var user in UserConnections)
        {
            lock (user.Value)
            {
                if (user.Value.Contains(connectionId))
                {
                    user.Value.Remove(connectionId);
                    
                    // Eğer kullanıcının hiç açık sekmesi kalmadıysa ana sözlükten de uçur
                    if (!user.Value.Any())
                    {
                        UserConnections.TryRemove(user.Key, out _);
                    }
                    break;
                }
            }
        }

        return base.OnDisconnectedAsync(exception);
    }
}