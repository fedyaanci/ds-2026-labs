namespace ProtoKey.Storage;

public sealed class StoreDispatcher
{
    private readonly StoreCommandQueue _commands;

    public StoreDispatcher(StoreCommandQueue commands) => _commands = commands;

    public async Task SetAsync(string key, int value, CancellationToken cancellationToken)
    {
        var completion = NewCompletion<bool>();
        await _commands.Channel.Writer.WriteAsync(
            new SetCommand(key, value, completion), cancellationToken);
        await completion.Task.WaitAsync(cancellationToken);
    }

    public async Task<int> GetAsync(string key, CancellationToken cancellationToken)
    {
        var completion = NewCompletion<int>();
        await _commands.Channel.Writer.WriteAsync(
            new GetCommand(key, completion), cancellationToken);
        return await completion.Task.WaitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> KeysAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        var completion = NewCompletion<IReadOnlyList<string>>();
        await _commands.Channel.Writer.WriteAsync(
            new KeysCommand(prefix, completion), cancellationToken);
        return await completion.Task.WaitAsync(cancellationToken);
    }

    private static TaskCompletionSource<T> NewCompletion<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
