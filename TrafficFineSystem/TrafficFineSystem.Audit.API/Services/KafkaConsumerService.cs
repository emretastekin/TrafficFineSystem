using Microsoft.AspNetCore.SignalR;
using TrafficFineSystem.Audit.API.Hubs;
using System.Text.Json;
using Confluent.Kafka;
using TrafficFineSystem.Audit.API.Data;
using TrafficFineSystem.Shared.Entities;
using TrafficFineSystem.Shared.Events;
using TrafficFineSystem.Shared.Enums; // Enum'ları rahat kullanmak için ekledik

namespace TrafficFineSystem.Audit.API.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _topic = "fine-status-events";
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

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
            // BURASI ÇOK ÖNEMLİ: Eğer burada hard-coded (elle yazılmış) "host.docker.internal" varsa, onu "localhost:9092" yap.
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "audit-api-group-" + Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_topic);

        _logger.LogInformation("Kafka dinleyicisi başlatıldı. Topic: {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                var message = consumeResult.Message.Value;

                _logger.LogInformation("Kafka'dan yeni bir mesaj alındı: {Message}", message);

                // Gelen JSON string'i Event nesnemize çeviriyoruz
                var statusEvent = JsonSerializer.Deserialize<FineStatusChangedEvent>(message);

                if (statusEvent != null)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

                        // Yeni FineHistory nesnemizi oluşturuyoruz
                        var historyRecord = new FineHistory
                        {
                            TrafficFineId = statusEvent.TrafficFineId,
                            UserId = statusEvent.UserId,
                            ProcessDate = statusEvent.ProcessDate,
                            ProcessType = statusEvent.ProcessType,
                            Reason = statusEvent.Reason,
                            
                            // Event'ten gelen Enum değerlerini doğrudan aktarıyoruz
                            PreviousStatus = (TrafficFineSystem.Shared.Enums.FineStatus)statusEvent.PreviousStatus,
                            NewStatus = (TrafficFineSystem.Shared.Enums.FineStatus)statusEvent.NewStatus
                        };

                        dbContext.FineHistories.Add(historyRecord);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Log başarıyla FineHistories tablosuna kaydedildi.");
                        
                        
                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveFineNotification", 
                            $"Yeni bir işlem yapıldı! Ceza ID: {statusEvent.TrafficFineId}, İşlem: {statusEvent.ProcessType}", 
                            cancellationToken: stoppingToken);
                        
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError("Veritabanına kaydederken BÜYÜK HATA: {Mesaj} \n İç Hata: {IcMesaj}", 
                            dbEx.Message, dbEx.InnerException?.Message);
                    }
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