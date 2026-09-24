using System.Text.Json;
using Contracts;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Valuator.Realtime;

public sealed class RankEventsNotifier : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IHubContext<ValuationHub> _hub;
    private readonly ILogger<RankEventsNotifier> _logger;

    public RankEventsNotifier(
        IConfiguration configuration,
        IHubContext<ValuationHub> hub,
        ILogger<RankEventsNotifier> logger)
    {
        _configuration = configuration;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string instanceId = _configuration["InstanceId"] ?? Environment.MachineName;
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
        channel.ExchangeDeclare(
            Contracts.Messaging.EventsExchange,
            ExchangeType.Topic,
            durable: true);

        string queueName = $"rank-notifications-{instanceId}";
        channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: true);
        channel.QueueBind(
            queueName,
            Contracts.Messaging.EventsExchange,
            Contracts.Messaging.RankCalculatedRoutingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                RankCalculated message = JsonSerializer.Deserialize<RankCalculated>(
                    eventArgs.Body.Span)!;
                await _hub.Clients.Group(ValuationHub.GroupName(message.TextId))
                    .SendAsync(
                        "rankCalculated",
                        message.TextId,
                        message.Rank,
                        cancellationToken: stoppingToken);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cannot send rank notification");
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(queueName, autoAck: false, consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
