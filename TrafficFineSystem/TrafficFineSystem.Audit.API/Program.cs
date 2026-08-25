using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Audit.API.Data;
using TrafficFineSystem.Audit.API.Services;
using TrafficFineSystem.Audit.API.Hubs;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<KafkaConsumerService>();


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // WebApp'in çalıştığı adresi (Örn: localhost:5129) buraya yazmalısın
        policy.WithOrigins("http://localhost:5129", "https://localhost:5129") 
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // SignalR için zorunlu
    });
});



// ... (diğer servislerin altı) ...
builder.Services.AddSignalR();

// WebApp'in bağlanabilmesi için CORS ayarı
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder
        .SetIsOriginAllowed((host) => true) // Geliştirme ortamı için tüm portlara izin veriyoruz
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseCors("CorsPolicy");
app.MapControllers();

app.MapHub<NotificationHub>("/notificationHub");

app.Run();