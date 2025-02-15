var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.IncludeScopes = true; // Include the scope information in the log messages
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss zzz"; // Set the timestamp format for the log messages with timezone offset
    options.UseUtcTimestamp = true; // Set the timezone to GMT+7
});

// Add services to the container.
builder.Services.AddSingleton(provider => new CrawlingService.CrawlingService(provider.GetRequiredService<ILogger<CrawlingService.CrawlingService>>())
);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(myAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();