using Auction.Application.Abstractions;
using Auction.Application.Services;
using Auction.Infrastructure.Grpc;
using Auction.Infrastructure.Messaging;
using Auction.Infrastructure.Persistence;
using Auction.Infrastructure.Security;
using Auction.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auction.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuctionPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
                               ?? "Host=localhost;Port=5433;Database=auctiondb;Username=auction;Password=auction_dev_password";

        // Note: EnableRetryOnFailure is intentionally not combined with the explicit
        // user-initiated transactions used by the bid flow. Replaying an ambiguous commit
        // could duplicate a bid; durability is handled by the transactional outbox instead.
        services.AddDbContext<AuctionDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AuctionDbContext>());
        return services;
    }

    public static IServiceCollection AddAuctionSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        return services;
    }

    public static IServiceCollection AddAuctionMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection("RabbitMq"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitEventPublisher>();
        services.AddSingleton<OutboxSignal>();
        services.AddSingleton<IOutboxSignal>(sp => sp.GetRequiredService<OutboxSignal>());
        services.AddHostedService<OutboxPublisherService>();
        return services;
    }

    public static IServiceCollection AddBidValidationGrpc(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BidValidationOptions>(configuration.GetSection("BidValidation"));
        services.AddSingleton<IBidValidationClient, GrpcBidValidationClient>();
        return services;
    }

    public static IServiceCollection AddAuctionApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ProductService>();
        services.AddScoped<AuctionService>();
        services.AddScoped<BidService>();
        services.AddScoped<BidValidationEvaluator>();
        services.AddScoped<UserService>();
        services.AddScoped<AdminService>();
        services.AddScoped<AuctionLifecycleService>();
        services.AddHostedService<AuctionLifecycleWorker>();
        return services;
    }
}
