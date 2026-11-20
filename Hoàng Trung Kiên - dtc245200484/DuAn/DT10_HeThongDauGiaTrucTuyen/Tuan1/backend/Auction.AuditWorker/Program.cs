using Auction.Application.Abstractions;
using Auction.AuditWorker;
using Auction.Infrastructure;
using Auction.Messaging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? builder.Configuration["Port"] ?? "5103");
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddAuctionPersistence(builder.Configuration);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddOptions<RabbitMqOptions>().Bind(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value);
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddHostedService<AuditEventConsumer>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Auction.AuditWorker" }));

app.Run();
