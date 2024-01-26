using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.DatabaseLayer.MySql.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Confluent.Kafka;
using ems_AuthService.Middlewares;
using ems_AuthServiceLayer.Contracts;
using ems_AuthServiceLayer.Service;
using Microsoft.Extensions.Options;
using ModalLayer;
using Newtonsoft.Json.Serialization;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IDb, Db>();
builder.Services.AddSingleton<ITimezoneConverter, TimezoneConverter>();

var kafkaServerDetail = new ProducerConfig();
builder.Configuration.Bind("KafkaServerDetail", kafkaServerDetail);
builder.Services.Configure<KafkaServiceConfig>(x => builder.Configuration.GetSection(nameof(KafkaServiceConfig)).Bind(x));
builder.Services.Configure<JwtSetting>(o => builder.Configuration.GetSection(nameof(JwtSetting)).Bind(o));

builder.Services.AddSingleton<ProducerConfig>(kafkaServerDetail);
builder.Services.AddSingleton<KafkaNotificationService>(x =>
{
    return new KafkaNotificationService(
        x.GetRequiredService<IOptions<KafkaServiceConfig>>(),
        x.GetRequiredService<ProducerConfig>(),
        x.GetRequiredService<ILogger<KafkaNotificationService>>(),
        builder.Environment.EnvironmentName == nameof(DefinedEnvironments.Development) ?
                        DefinedEnvironments.Development :
                        DefinedEnvironments.Production
    );
});

builder.Services.AddScoped<CurrentSession>(x =>
{
    return new CurrentSession
    {
        Environment = builder.Environment.EnvironmentName == nameof(DefinedEnvironments.Development) ?
                        DefinedEnvironments.Development :
                        DefinedEnvironments.Production
    };
});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.UseMiddleware<RequestMiddleware>();
app.UseAuthorization();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.MapControllers();
app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}