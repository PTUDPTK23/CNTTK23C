using Auction.Application.Abstractions;
using Auction.Application.Services;
using Auction.Infrastructure;
using BidValidation.Service.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

var port = int.Parse(Environment.GetEnvironmentVariable("GRPC_PORT")
                     ?? builder.Configuration["Grpc:Port"]
                     ?? "5101");

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(port, listen => listen.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 4 * 1024 * 1024;
});

builder.Services.AddAuctionPersistence(builder.Configuration);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<BidValidationEvaluator>();

var app = builder.Build();
app.MapGrpcService<BidValidationGrpcService>();

app.Logger.LogInformation("Bid Validation Service listening on gRPC port {Port}.", port);
await app.RunAsync();
