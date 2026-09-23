using System.Text;
using System.Text.Json;
using Contracts;
using RabbitMQ.Client;

namespace Valuator.Messaging;

public sealed class MessagePublisher : IDisposable
{
    private readonly IConnection _connection;

    public MessagePublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"] ?? "localhost",
            Port = configuration.GetValue("RabbitMq:Port", 5672),
            UserName = configuration["RabbitMq:UserName"] ?? "lab",
            Password = configuration["RabbitMq:Password"] ?? "lab"
        };

        _connection = factory.CreateConnection();
    }

    public void Publish(string textId)
    {
        using IModel channel = _connection.CreateModel();
        channel.QueueDeclare(
            queue: Contracts.Messaging.RankQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(
            new RankCalculationRequested(textId));
        IBasicProperties properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: Contracts.Messaging.RankQueue,
            basicProperties: properties,
            body: body);
    }

    public void PublishSimilarityCalculated(string textId, double similarity)
    {
        using IModel channel = _connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: Contracts.Messaging.EventsExchange,
            type: ExchangeType.Topic,
            durable: true);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(
            new SimilarityCalculated(textId, similarity));
        IBasicProperties properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: Contracts.Messaging.EventsExchange,
            routingKey: Contracts.Messaging.SimilarityCalculatedRoutingKey,
            basicProperties: properties,
            body: body);
    }

    public void Dispose() => _connection.Dispose();
}
