using FplBot.WebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWebApp();

var app = builder.Build();
app.UseWebApp();
app.Run($"http://*:{Environment.GetEnvironmentVariable("PORT") ?? "1337"}");
