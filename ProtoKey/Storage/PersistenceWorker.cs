namespace ProtoKey.Storage;

public sealed class PersistenceWorker : BackgroundService
{
    private readonly PersistenceQueue _queue;
    private readonly CommandLog _commandLog;

    public PersistenceWorker(PersistenceQueue queue, CommandLog commandLog)
    {
        _queue = queue;
        _commandLog = commandLog;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var buffer = new List<PersistedSet>();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                Drain(buffer);
                await _commandLog.AppendAsync(buffer, stoppingToken);
                buffer.Clear();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is stopping. The final buffer is flushed below.
        }
        finally
        {
            Drain(buffer);
            await _commandLog.AppendAsync(buffer, CancellationToken.None);
        }
    }

    private void Drain(List<PersistedSet> buffer)
    {
        while (_queue.Channel.Reader.TryRead(out PersistedSet? command))
        {
            buffer.Add(command);
        }
    }
}
