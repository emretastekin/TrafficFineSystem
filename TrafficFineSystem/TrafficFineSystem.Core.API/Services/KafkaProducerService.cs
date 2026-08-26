using System.Text.Json;
using Confluent.Kafka;
using TrafficFineSystem.Shared.Events;

namespace TrafficFineSystem.Core.API.Services;

public class KafkaProducerService
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _fineTopic = "fine-status-events"; // Ceza olayları kanalı
    private readonly string _vehicleTopic = "vehicle-events"; // YENİ: Araç olayları kanalı

    public KafkaProducerService(IConfiguration configuration)
    {
        // docker-compose.yml dosyamızdaki Kafka adresini alıyoruz
        var producerconfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
        
        _producer = new ProducerBuilder<Null, string>(producerconfig).Build();
    }

    // 1. MEVCUT METOT: Cezalar için
    public async Task ProduceFineStatusChangedEventAsync(FineStatusChangedEvent statusEvent)
    {
        var message = JsonSerializer.Serialize(statusEvent);
        Console.WriteLine($"----> KAFKA'YA MESAJ GÖNDERİLİYOR (Ceza): {message}");

        try
        {
            var result = await _producer.ProduceAsync(_fineTopic, new Message<Null, string> { Value = message });
            Console.WriteLine($"----> MESAJ KAFKA'YA BAŞARIYLA ULAŞTI! Partition: {result.Partition}, Offset: {result.Offset}");
        }
        catch (ProduceException<Null, string> e)
        {
            Console.WriteLine($"----> KAFKA GÖNDERİM HATASI (Ceza): {e.Error.Reason}");
        }
    }

    // 2. YENİ EKLENEN METOT: Araçlar için
    public async Task ProduceVehicleCreatedEventAsync(VehicleCreatedEvent vehicleEvent)
    {
        var message = JsonSerializer.Serialize(vehicleEvent);
        Console.WriteLine($"----> KAFKA'YA MESAJ GÖNDERİLİYOR (Araç): {message}");

        try
        {
            // Burada mesajı _vehicleTopic kanalına gönderiyoruz
            var result = await _producer.ProduceAsync(_vehicleTopic, new Message<Null, string> { Value = message });
            Console.WriteLine($"----> MESAJ KAFKA'YA BAŞARIYLA ULAŞTI (Araç)! Partition: {result.Partition}, Offset: {result.Offset}");
        }
        catch (ProduceException<Null, string> e)
        {
            Console.WriteLine($"----> KAFKA GÖNDERİM HATASI (Araç): {e.Error.Reason}");
        }
    }
}