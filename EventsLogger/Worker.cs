using System.Text.Json;
using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventsLogger;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string instanceId = _configuration["InstanceId"]
            ?? Environment.MachineName;
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            Port = _configuration.GetValue("RabbitMq:Port", 5672),
            UserName = _configuration["RabbitMq:UserName"]
                ?? throw new InvalidOperationException("RabbitMq:UserName is required"),
            Password = _configuration["RabbitMq:Password"]
                ?? throw new InvalidOperationException("RabbitMq:Password is required"),
            DispatchConsumersAsync = true
        };

        using IConnection connection = factory.CreateConnection();
        using IModel channel = connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: Messaging.EventsExchange,
            type: ExchangeType.Topic,
            durable: true);

        string queueName = $"events-logger-{instanceId}";
        channel.QueueDeclare(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true);
        channel.QueueBind(queueName, Messaging.EventsExchange, "*.calculated");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += (_, eventArgs) =>
        {
            try
            {
                if (eventArgs.RoutingKey == Messaging.RankCalculatedRoutingKey)
                {
                    RankCalculated value = JsonSerializer.Deserialize<RankCalculated>(
                        eventArgs.Body.Span)!;
                    _logger.LogInformation(
                        "RankCalculated: TextId={TextId}, Rank={Rank}",
                        value.TextId, value.Rank);
                }
                else if (eventArgs.RoutingKey == Messaging.SimilarityCalculatedRoutingKey)
                {
                    SimilarityCalculated value =
                        JsonSerializer.Deserialize<SimilarityCalculated>(eventArgs.Body.Span)!;
                    _logger.LogInformation(
                        "SimilarityCalculated: TextId={TextId}, Similarity={Similarity}",
                        value.TextId, value.Similarity);
                }

                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cannot process event {RoutingKey}", eventArgs.RoutingKey);
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }

            return Task.CompletedTask;
        };

        channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogInformation("EventsLogger {InstanceId} is subscribed", instanceId);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
