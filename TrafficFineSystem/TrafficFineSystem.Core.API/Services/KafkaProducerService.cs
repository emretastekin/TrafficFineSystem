using System.Text.Json;
using Confluent.Kafka;
using TrafficFineSystem.Shared.Events;

namespace TrafficFineSystem.Core.API.Services;

public class KafkaProducerService
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic = "fine-status-events"; // Kafka'daki kanalımızın adı

    public KafkaProducerService(IConfiguration configuration)
    {
        // docker-compose.yml dosyamızdaki Kafka adresini alıyoruz
        var producerconfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "127.0.0.1:9092"
        };
        
        _producer = new ProducerBuilder<Null, string>(producerconfig).Build();
    }

    public async Task ProduceFineStatusChangedEventAsync(FineStatusChangedEvent statusEvent)
    {
        var message = JsonSerializer.Serialize(statusEvent);
        Console.WriteLine($"----> KAFKA'YA MESAJ GÖNDERİLİYOR: {message}");

        try
        {
            // ProduceAsync sonucunu bir değişkene atayıp bekleyelim
            var result = await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = message });
            Console.WriteLine($"----> MESAJ KAFKA'YA BAŞARIYLA ULAŞTI! Partition: {result.Partition}, Offset: {result.Offset}");
        }
        catch (ProduceException<Null, string> e)
        {
            Console.WriteLine($"----> KAFKA GÖNDERİM HATASI: {e.Error.Reason}");
        }
    }
}