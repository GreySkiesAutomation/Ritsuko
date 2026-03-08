using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IncomingDiscordMessageQueue>();
builder.Services.AddSingleton<UnityHeartbeatState>();
builder.Services.AddSingleton<DiscordBotService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DiscordBotService>());

var app = builder.Build();

app.MapGet("/health", (DiscordBotService discordBotService, UnityHeartbeatState unityHeartbeatState) =>
{
    return Results.Ok(new HealthResponse
    {
        IsDiscordConnected = discordBotService.IsConnected,
        ConnectedUsername = discordBotService.ConnectedUsername,
        IsUnityHeartbeatFresh = unityHeartbeatState.IsHeartbeatFresh(TimeSpan.FromSeconds(10)),
        LastHeartbeatUtcIso8601 = unityHeartbeatState.GetLastHeartbeatUtc()?.ToString("O")
    });
});

app.MapGet("/poll-incoming", (IncomingDiscordMessageQueue incomingDiscordMessageQueue) =>
{
    return Results.Ok(incomingDiscordMessageQueue.DequeueAllMessages());
});

app.MapPost("/send-dm", async (SendDirectMessageRequest request, DiscordBotService discordBotService) =>
{
    if (request.UserId == 0)
    {
        return Results.BadRequest("UserId must be non-zero.");
    }

    if (string.IsNullOrWhiteSpace(request.MessageText))
    {
        return Results.BadRequest("MessageText must not be empty.");
    }

    await discordBotService.SendDirectMessageAsync(request.UserId, request.MessageText);
    return Results.Ok();
});

app.MapPost("/heartbeat", (UnityHeartbeatState unityHeartbeatState) =>
{
    unityHeartbeatState.RegisterHeartbeat();
    return Results.Ok(new HeartbeatResponse
    {
        Success = true,
        ReceivedUtcIso8601 = DateTime.UtcNow.ToString("O")
    });
});

app.Run("http://127.0.0.1:5099");

public sealed class DiscordBotService : IHostedService
{
    private readonly IncomingDiscordMessageQueue _incomingDiscordMessageQueue;
    private readonly UnityHeartbeatState _unityHeartbeatState;
    private readonly ILogger<DiscordBotService> _logger;
    private DiscordSocketClient? _discordSocketClient;

    public bool IsConnected { get; private set; }
    public string? ConnectedUsername { get; private set; }

    public DiscordBotService(
        IncomingDiscordMessageQueue incomingDiscordMessageQueue,
        UnityHeartbeatState unityHeartbeatState,
        ILogger<DiscordBotService> logger)
    {
        _incomingDiscordMessageQueue = incomingDiscordMessageQueue;
        _unityHeartbeatState = unityHeartbeatState;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var discordBotToken = Environment.GetEnvironmentVariable("DISCORD_BOT_API_KEY");

        if (string.IsNullOrWhiteSpace(discordBotToken))
        {
            throw new InvalidOperationException("DISCORD_BOT_API_KEY environment variable was missing or empty.");
        }

        _discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents =
                GatewayIntents.DirectMessages |
                GatewayIntents.MessageContent,
            AlwaysDownloadUsers = false
        });

        _discordSocketClient.Log += OnDiscordLogAsync;
        _discordSocketClient.MessageReceived += OnDiscordMessageReceivedAsync;
        _discordSocketClient.Connected += OnDiscordConnectedAsync;
        _discordSocketClient.Disconnected += OnDiscordDisconnectedAsync;
        _discordSocketClient.Ready += OnDiscordReadyAsync;

        _logger.LogInformation("Starting Discord sidecar...");

        await _discordSocketClient.LoginAsync(TokenType.Bot, discordBotToken);
        await _discordSocketClient.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_discordSocketClient == null)
        {
            return;
        }

        try
        {
            _discordSocketClient.Log -= OnDiscordLogAsync;
            _discordSocketClient.MessageReceived -= OnDiscordMessageReceivedAsync;
            _discordSocketClient.Connected -= OnDiscordConnectedAsync;
            _discordSocketClient.Disconnected -= OnDiscordDisconnectedAsync;
            _discordSocketClient.Ready -= OnDiscordReadyAsync;

            await _discordSocketClient.StopAsync();
            await _discordSocketClient.LogoutAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Exception while stopping Discord sidecar.");
        }
        finally
        {
            _discordSocketClient.Dispose();
            _discordSocketClient = null;
            IsConnected = false;
            ConnectedUsername = null;
        }
    }

    public async Task SendDirectMessageAsync(ulong userId, string messageText)
    {
        if (_discordSocketClient == null)
        {
            throw new InvalidOperationException("Discord client was not initialized.");
        }

        if (!IsConnected)
        {
            throw new InvalidOperationException("Discord client is not connected.");
        }

        var user = await _discordSocketClient.GetUserAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException($"Could not find Discord user with ID {userId}.");
        }

        await user.SendMessageAsync(messageText);

        _logger.LogInformation("Sent DM to {Username} ({UserId}): {MessageText}", user.Username, user.Id, messageText);
    }

    private Task OnDiscordLogAsync(LogMessage logMessage)
    {
        if (logMessage.Exception != null)
        {
            _logger.LogError(logMessage.Exception, "[Discord] {Severity} | {Source} | {Message}", logMessage.Severity, logMessage.Source, logMessage.Message);
        }
        else
        {
            _logger.LogInformation("[Discord] {Severity} | {Source} | {Message}", logMessage.Severity, logMessage.Source, logMessage.Message);
        }

        return Task.CompletedTask;
    }

    private Task OnDiscordConnectedAsync()
    {
        IsConnected = true;
        _logger.LogInformation("Discord gateway connected.");
        return Task.CompletedTask;
    }

    private Task OnDiscordDisconnectedAsync(Exception? exception)
    {
        IsConnected = false;

        if (exception != null)
        {
            _logger.LogWarning(exception, "Discord gateway disconnected with exception.");
        }
        else
        {
            _logger.LogInformation("Discord gateway disconnected.");
        }

        return Task.CompletedTask;
    }

    private Task OnDiscordReadyAsync()
    {
        ConnectedUsername = _discordSocketClient?.CurrentUser?.Username;
        _logger.LogInformation("Discord gateway ready as {ConnectedUsername}.", ConnectedUsername);
        return Task.CompletedTask;
    }

    private async Task OnDiscordMessageReceivedAsync(SocketMessage socketMessage)
    {
        if (socketMessage.Author.IsBot)
        {
            return;
        }

        if (socketMessage.Channel is not IDMChannel)
        {
            return;
        }

        var isUnityHeartbeatFresh = _unityHeartbeatState.IsHeartbeatFresh(TimeSpan.FromSeconds(10));

        if (!isUnityHeartbeatFresh)
        {
            _logger.LogWarning(
                "Unity heartbeat stale or missing. Auto-replying to {Username} ({UserId}) instead of queueing message.",
                socketMessage.Author.Username,
                socketMessage.Author.Id);

            await socketMessage.Channel.SendMessageAsync(
                "The Unity app is currently offline. Your message has been lost to the void.");

            return;
        }

        var incomingDiscordMessage = new IncomingDiscordMessage
        {
            UserId = socketMessage.Author.Id,
            Username = socketMessage.Author.Username,
            MessageText = socketMessage.Content,
            ReceivedUtcIso8601 = DateTime.UtcNow.ToString("O")
        };

        _incomingDiscordMessageQueue.EnqueueMessage(incomingDiscordMessage);

        _logger.LogInformation(
            "Queued DM from {Username} ({UserId}): {MessageText}",
            incomingDiscordMessage.Username,
            incomingDiscordMessage.UserId,
            incomingDiscordMessage.MessageText);
    }
}

public sealed class IncomingDiscordMessageQueue
{
    private readonly ConcurrentQueue<IncomingDiscordMessage> _incomingDiscordMessages = new();

    public void EnqueueMessage(IncomingDiscordMessage incomingDiscordMessage)
    {
        _incomingDiscordMessages.Enqueue(incomingDiscordMessage);
    }

    public List<IncomingDiscordMessage> DequeueAllMessages()
    {
        var dequeuedMessages = new List<IncomingDiscordMessage>();

        while (_incomingDiscordMessages.TryDequeue(out var incomingDiscordMessage))
        {
            dequeuedMessages.Add(incomingDiscordMessage);
        }

        return dequeuedMessages;
    }
}

public sealed class UnityHeartbeatState
{
    private readonly object _lock = new object();
    private DateTime? _lastHeartbeatUtc;

    public void RegisterHeartbeat()
    {
        lock (_lock)
        {
            _lastHeartbeatUtc = DateTime.UtcNow;
        }
    }

    public DateTime? GetLastHeartbeatUtc()
    {
        lock (_lock)
        {
            return _lastHeartbeatUtc;
        }
    }

    public bool IsHeartbeatFresh(TimeSpan timeout)
    {
        lock (_lock)
        {
            if (_lastHeartbeatUtc == null)
            {
                return false;
            }

            return (DateTime.UtcNow - _lastHeartbeatUtc.Value) <= timeout;
        }
    }
}

public sealed class IncomingDiscordMessage
{
    public ulong UserId { get; set; }
    public string Username { get; set; } = "";
    public string MessageText { get; set; } = "";
    public string ReceivedUtcIso8601 { get; set; } = "";
}

public sealed class SendDirectMessageRequest
{
    public ulong UserId { get; set; }
    public string MessageText { get; set; } = "";
}

public sealed class HealthResponse
{
    public bool IsDiscordConnected { get; set; }
    public string? ConnectedUsername { get; set; }
    public bool IsUnityHeartbeatFresh { get; set; }
    public string? LastHeartbeatUtcIso8601 { get; set; }
}

public sealed class HeartbeatResponse
{
    public bool Success { get; set; }
    public string ReceivedUtcIso8601 { get; set; } = "";
}