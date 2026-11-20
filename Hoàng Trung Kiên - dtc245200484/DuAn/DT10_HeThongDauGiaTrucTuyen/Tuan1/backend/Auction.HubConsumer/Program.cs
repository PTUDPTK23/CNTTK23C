using Auction.HubConsumer;
using Auction.Messaging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var options = builder.Configuration.GetSection("HubBroadcast").Get<HubBroadcastOptions>() ?? new HubBroadcastOptions();
var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? options.Port.ToString());

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.Configure<HubBroadcastOptions>(o =>
{
    builder.Configuration.GetSection("HubBroadcast").Bind(o);
    o.Port = port;
    o.ApiBaseUrl = Environment.GetEnvironmentVariable("AUCTION_API_URL") ?? o.ApiBaseUrl;
    o.InternalApiKey = Environment.GetEnvironmentVariable("INTERNAL_API_KEY") ?? o.InternalApiKey;
});

builder.Services.AddOptions<RabbitMqOptions>().Bind(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value);
builder.Services.AddSingleton<RabbitMqConnection>();

builder.Services.AddHttpClient<AuctionHubClient>((sp, http) =>
{
    var opts = sp.GetRequiredService<IOptions<HubBroadcastOptions>>().Value;
    http.BaseAddress = new Uri(opts.ApiBaseUrl);
    http.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<HubEventConsumer>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Auction.HubConsumer" }));

app.Run();
