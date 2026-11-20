using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Auction.API.Hubs;
using Auction.API.Infrastructure;
using Auction.API.Realtime;
using Auction.Application.Abstractions;
using Auction.Contracts;
using Auction.Infrastructure;
using Auction.Infrastructure.Persistence;
using Auction.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "Giá trị không hợp lệ." : e.ErrorMessage).ToArray());

            return new Microsoft.AspNetCore.Mvc.ObjectResult(new
            {
                code = "VALIDATION_ERROR",
                message = "Dữ liệu gửi lên không hợp lệ.",
                errors
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        };
    });

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                      ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters. Configure Jwt__Key.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Online Auction API",
        Version = "v1",
        Description = """
                      REST API for the Online Auction System.

                      Bid flow: REST → gRPC Bid Validation Service → PostgreSQL (serialized transaction)
                      → RabbitMQ (BidAccepted) → Hub Consumer → SignalR → clients, and → Audit Worker → audit_logs.

                      Authentication: `POST /api/auth/login`, then use the returned JWT as a Bearer token.
                      """
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "Auction.API.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuctionPersistence(builder.Configuration);
builder.Services.AddAuctionSecurity(builder.Configuration);
builder.Services.AddAuctionMessaging(builder.Configuration);
builder.Services.AddBidValidationGrpc(builder.Configuration);
builder.Services.AddAuctionApplication();

builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection("InternalApi"));
builder.Services.AddSingleton<AuctionBroadcaster>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Auction API v1");
        options.DocumentTitle = "Online Auction API";
    });
}

app.UseStaticFiles();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AuctionHub>("/hubs/auction");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Auction.API" }));

// ---------------------------------------------------------------------------
// Migrations + demo seed
// ---------------------------------------------------------------------------
if (builder.Configuration.GetValue("Database:AutoMigrate", true))
{
    await ApplyDatabaseAsync(app.Services, app.Logger);
}

app.Run();

static async Task ApplyDatabaseAsync(IServiceProvider services, ILogger logger)
{
    for (var attempt = 1; attempt <= 20; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
            await db.Database.MigrateAsync();

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await DbSeeder.SeedAsync(db, hasher, logger);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization attempt {Attempt}/20 failed.", attempt);
            if (attempt == 20) throw;
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

public partial class Program;
