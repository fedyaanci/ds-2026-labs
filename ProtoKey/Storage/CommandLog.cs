using System.Text.Json;

namespace ProtoKey.Storage;

public sealed class CommandLog
{
    private readonly string _path;
    private readonly ILogger<CommandLog> _logger;

    public CommandLog(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<CommandLog> logger)
    {
        string configuredPath = configuration["DataFile"] ?? "ProtoKey.data";
        _path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
        _logger = logger;
    }

    public IReadOnlyList<PersistedSet> ReadAll()
    {
        var commands = new List<PersistedSet>();
        if (!File.Exists(_path))
        {
            return commands;
        }

        int lineNumber = 0;
        foreach (string line in File.ReadLines(_path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                PersistedSet? command = JsonSerializer.Deserialize<PersistedSet>(line);
                if (command is not null && KeyValidator.IsValidKey(command.Key))
                {
                    commands.Add(command);
                }
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skipping damaged line {LineNumber} in {Path}",
                    lineNumber,
                    _path);
            }
        }

        return commands;
    }

    public async Task AppendAsync(
        IReadOnlyCollection<PersistedSet> commands,
        CancellationToken cancellationToken)
    {
        if (commands.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var stream = new FileStream(
            _path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        await using var writer = new StreamWriter(stream);
        foreach (PersistedSet command in commands)
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(command));
        }

        await writer.FlushAsync(cancellationToken);
    }
}
