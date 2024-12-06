using DistributedLock.Mongo.AspNetCore.Abstraction;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
const string connectionString = "mongodb://localhost:27017/"; // MongoDB connection string

var client = new MongoClient(connectionString);
builder.Services.AddSingleton<IMongoClient>(client);
builder.Services.AddMongoDistributedLock<Guid>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (IMongoLockFactory<Guid> lockerFactory) =>
    {
        var locker = lockerFactory.GenerateNewLock(Guid.Parse("019342fe-0ed2-743d-b64f-db3ec558302d"));
        var acq = await locker.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));
        var forecast = Array.Empty<WeatherForecast>();
        try
        {
            if (acq.Acquired)
            {
                forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        (
                            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]
                        ))
                    .ToArray();
            }
        }
        finally
        {
            // if (acquire.Acquired) no need to do it manually
            await locker.ReleaseAsync(acq);
        }
        return forecast;
    }).WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}