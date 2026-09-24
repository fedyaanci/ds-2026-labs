namespace ProtoKey.Storage;

public sealed class StoreWorker : BackgroundService
{
    private readonly StoreCommandQueue _commands;
    private readonly PersistenceQueue _persistence;
    private readonly CommandLog _commandLog;
    private readonly ILogger<StoreWorker> _logger;
    private readonly Dictionary<string, int> _values = new(StringComparer.Ordinal);

    public StoreWorker(
        StoreCommandQueue commands,
        PersistenceQueue persistence,
        CommandLog commandLog,
        ILogger<StoreWorker> logger)
    {
        _commands = commands;
        _persistence = persistence;
        _commandLog = commandLog;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (PersistedSet command in _commandLog.ReadAll())
        {
            _values[command.Key] = command.Value;
        }

        _logger.LogInformation("Restored {Count} keys from disk", _values.Count);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (StoreCommand command in
            _commands.Channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                switch (command)
                {
                    case SetCommand set:
                        _values[set.Key] = set.Value;
                        await _persistence.Channel.Writer.WriteAsync(
                            new PersistedSet(set.Key, set.Value), stoppingToken);
                        set.Completion.TrySetResult(true);
                        break;

                    case GetCommand get:
                        get.Completion.TrySetResult(
                            _values.TryGetValue(get.Key, out int value) ? value : 0);
                        break;

                    case KeysCommand keys:
                        IReadOnlyList<string> matches = _values.Keys
                            .Where(key => key.StartsWith(keys.Prefix, StringComparison.Ordinal))
                            .ToArray();
                        keys.Completion.TrySetResult(matches);
                        break;
                }
            }
            catch (Exception exception)
            {
                CompleteWithException(command, exception);
            }
        }
    }

    private static void CompleteWithException(StoreCommand command, Exception exception)
    {
        switch (command)
        {
            case SetCommand set:
                set.Completion.TrySetException(exception);
                break;
            case GetCommand get:
                get.Completion.TrySetException(exception);
                break;
            case KeysCommand keys:
                keys.Completion.TrySetException(exception);
                break;
        }
    }
}
