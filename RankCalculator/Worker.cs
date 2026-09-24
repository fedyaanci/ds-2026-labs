using System.Text.Json;
using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ShardStore;
using StackExchange.Redis;

namespace RankCalculator;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly ShardedRedisStore _store;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        ShardedRedisStore store)
    {
        _logger = logger;
        _configuration = configuration;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            Port = _configuration.GetValue("RabbitMq:Port", 5672),
            UserName = _configuration["RabbitMq:UserName"] ?? "lab",
            Password = _configuration["RabbitMq:Password"] ?? "lab",
            DispatchConsumersAsync = true
        };

        using IConnection connection = factory.CreateConnection();
        using IModel channel = connection.CreateModel();
        channel.QueueDeclare(
            queue: Messaging.RankQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var request = JsonSerializer.Deserialize<RankCalculationRequested>(
                    eventArgs.Body.Span)
                    ?? throw new InvalidDataException("Empty rank request");

                ShardContext? shard = await _store.LookupAsync(request.TextId);
                if (shard is null)
                {
                    throw new InvalidDataException($"Shard for {request.TextId} was not found");
                }

                _logger.LogInformation(
                    "LOOKUP: {TextId}, {Region}", request.TextId, shard.Region);
                RedisValue textValue = await shard.Database.StringGetAsync($"TEXT-{request.TextId}");
                if (!textValue.HasValue)
                {
                    throw new InvalidDataException($"Text {request.TextId} was not found");
                }

                string text = textValue.ToString();
                double rank = CalculateRank(text);
                await shard.Database.StringSetAsync($"RANK-{request.TextId}", rank);

                channel.ExchangeDeclare(
                    exchange: Messaging.EventsExchange,
                    type: ExchangeType.Topic,
                    durable: true);
                byte[] eventBody = JsonSerializer.SerializeToUtf8Bytes(
                    new RankCalculated(request.TextId, rank));
                IBasicProperties eventProperties = channel.CreateBasicProperties();
                eventProperties.Persistent = true;
                channel.BasicPublish(
                    exchange: Messaging.EventsExchange,
                    routingKey: Messaging.RankCalculatedRoutingKey,
                    basicProperties: eventProperties,
                    body: eventBody);

                _logger.LogInformation(
                    "Rank {Rank} calculated for text {TextId}", rank, request.TextId);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Rank calculation failed");
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(
            queue: Messaging.RankQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RankCalculator is waiting for messages");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    internal static double CalculateRank(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        int nonAlphabeticCount = text.Count(symbol =>
            !((symbol >= 'A' && symbol <= 'Z') ||
              (symbol >= 'a' && symbol <= 'z') ||
              (symbol >= 'А' && symbol <= 'Я') ||
              (symbol >= 'а' && symbol <= 'я') ||
              symbol == 'Ё' || symbol == 'ё'));

        return (double)nonAlphabeticCount / text.Length;
    }
}
