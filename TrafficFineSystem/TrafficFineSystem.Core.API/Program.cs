using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Core.API.Data;
using TrafficFineSystem.Core.API.Repositories;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Core.API.Services;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Veritabanı bağlantısını sisteme tanıtıyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<ITrafficFineRepository, TrafficFineRepository>();

// Kafka servisimizi tekil (Singleton) olarak ekliyoruz
builder.Services.AddSingleton<KafkaProducerService>();

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();


app.Run();