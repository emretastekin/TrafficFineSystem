using Microsoft.AspNetCore.SignalR;
using TrafficFineSystem.Audit.API.Hubs;
using System.Text.Json;
using Confluent.Kafka;
using TrafficFineSystem.Audit.API.Data;
using TrafficFineSystem.Shared.Entities;
using TrafficFineSystem.Shared.Events;
using TrafficFineSystem.Shared.Enums; 

namespace TrafficFineSystem.Audit.API.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    // YENİ: Artık tek bir topic değil, iki ayrı topic dinleyeceğiz
    private readonly List<string> _topics = new() { "fine-status-events", "vehicle-events" };

    public KafkaConsumerService(
        IConfiguration configuration, 
        IServiceProvider serviceProvider, 
        ILogger<KafkaConsumerService> logger,
        IHubContext<NotificationHub> hubContext)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "audit-api-group-" + Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        
        // YENİ: Listeyi (iki kanalı birden) dinlemeye başlıyoruz
        consumer.Subscribe(_topics);

        _logger.LogInformation("Kafka dinleyicisi başlatıldı. Dinlenen Kanallar: {Topics}", string.Join(", ", _topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                var message = consumeResult.Message.Value;
                var currentTopic = consumeResult.Topic; // Mesajın HANGİ kanaldan geldiğini yakalıyoruz

                _logger.LogInformation("[{Topic}] kanalından mesaj alındı: {Message}", currentTopic, message);

                // 1. SENARYO: EĞER MESAJ CEZA KANALINDAN GELDİYSE
                if (currentTopic == "fine-status-events")
                {
                    var statusEvent = JsonSerializer.Deserialize<FineStatusChangedEvent>(message);
                    if (statusEvent != null)
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

                            var historyRecord = new FineHistory
                            {
                                TrafficFineId = statusEvent.TrafficFineId,
                                UserId = statusEvent.UserId,
                                ProcessDate = statusEvent.ProcessDate,
                                ProcessType = statusEvent.ProcessType,
                                Reason = statusEvent.Reason,
                                PreviousStatus = (TrafficFineSystem.Shared.Enums.FineStatus)statusEvent.PreviousStatus,
                                NewStatus = (TrafficFineSystem.Shared.Enums.FineStatus)statusEvent.NewStatus
                            };

                            dbContext.FineHistories.Add(historyRecord);
                            await dbContext.SaveChangesAsync(stoppingToken);
                            
                            // Web arayüzüne 'Ceza' tablosunu yenilemesi için sinyal gönder
                            await _hubContext.Clients.All.SendAsync(
                                "ReceiveFineNotification", 
                                $"Yeni işlem! Ceza ID: {statusEvent.TrafficFineId}, İşlem: {statusEvent.ProcessType}", 
                                cancellationToken: stoppingToken);
                        }
                        catch (Exception dbEx)
                        {
                            _logger.LogError("Veritabanına kaydederken HATA: {Mesaj}", dbEx.Message);
                        }
                    }
                }
                
                // 2. SENARYO: EĞER MESAJ ARAÇ KANALINDAN GELDİYSE (YENİ EKLENEN KISIM)
                else if (currentTopic == "vehicle-events")
                {
                    // Burada veritabanına log atmamıza gerek yok (şimdilik), doğrudan arayüze yenileme sinyali fırlatıyoruz!
                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveVehicleNotification", 
                        "Sisteme yeni bir araç eklendi!", 
                        cancellationToken: stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka dinlenirken bir hata oluştu.");
        }
    }
}