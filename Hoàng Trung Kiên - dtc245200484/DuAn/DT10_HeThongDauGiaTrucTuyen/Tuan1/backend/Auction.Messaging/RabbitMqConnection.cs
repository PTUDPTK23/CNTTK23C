using RabbitMQ.Client;

namespace Auction.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "auction";
    public string Password { get; set; } = "auction_dev_password";
    public string VirtualHost { get; set; } = "/";
    public bool Enabled { get; set; } = true;
    /// <summary>Optional prefix for queues, useful for isolated test runs.</summary>
    public string QueuePrefix { get; set; } = "";

    public string ResolveQueue(string baseName) =>
        string.IsNullOrWhiteSpace(QueuePrefix) ? baseName : $"{QueuePrefix}.{baseName}";
}

/// <summary>Lazily created, auto-recovering shared RabbitMQ connection.</summary>
public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(RabbitMqOptions options)
    {
        _options = options;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                try { await _connection.DisposeAsync(); } catch { /* ignore */ }
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = false,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(3),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            _connection = await factory.CreateConnectionAsync($"auction-service-{Environment.MachineName}", ct);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        return await connection.CreateChannelAsync(cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
        }

        _gate.Dispose();
    }
}
