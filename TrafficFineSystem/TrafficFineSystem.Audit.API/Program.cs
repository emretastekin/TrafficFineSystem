using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Audit.API.Data;
using TrafficFineSystem.Audit.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();

app.Run();