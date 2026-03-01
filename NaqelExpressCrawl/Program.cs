var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.IncludeScopes = true; // Include the scope information in the log messages
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss zzz"; // Set the timestamp format for the log messages with timezone offset
    options.UseUtcTimestamp = true; // Set the timezone to GMT+7
});

builder.Services.AddHttpClient();
// Add services to the container.
builder.Services.AddSingleton(provider =>
    new CrawlingService.CrawlingService(
        provider.GetRequiredService<ILogger<CrawlingService.CrawlingService>>(),
        provider.GetRequiredService<IHttpClientFactory>())
);

builder.Services.AddControllers();

const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:8080",
                "https://delivery.kiemtradoanhthuwesaam.info")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(myAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();