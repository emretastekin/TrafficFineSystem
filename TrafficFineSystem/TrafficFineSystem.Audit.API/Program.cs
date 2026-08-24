using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Audit.API.Data;
using TrafficFineSystem.Audit.API.Services;
using TrafficFineSystem.Audit.API.Hubs;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<KafkaConsumerService>();


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


var app = builder.Build();

app.UseCors("CorsPolicy");

app.MapHub<NotificationHub>("/notificationHub");

app.Run();