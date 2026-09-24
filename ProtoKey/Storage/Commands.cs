using System.Threading.Channels;

namespace ProtoKey.Storage;

public abstract record StoreCommand;

public sealed record SetCommand(
    string Key,
    int Value,
    TaskCompletionSource<bool> Completion) : StoreCommand;

public sealed record GetCommand(
    string Key,
    TaskCompletionSource<int> Completion) : StoreCommand;

public sealed record KeysCommand(
    string Prefix,
    TaskCompletionSource<IReadOnlyList<string>> Completion) : StoreCommand;

public sealed record PersistedSet(string Key, int Value);

public sealed class StoreCommandQueue
{
    public Channel<StoreCommand> Channel { get; } =
        System.Threading.Channels.Channel.CreateUnbounded<StoreCommand>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
}

public sealed class PersistenceQueue
{
    public Channel<PersistedSet> Channel { get; } =
        System.Threading.Channels.Channel.CreateUnbounded<PersistedSet>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
}
