using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using CloudNative.CloudEvents.Kafka;
using CloudNative.CloudEvents.Protobuf;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf;

namespace MyApp.Service.Messaging;

public class ProtobufProducer
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly ISchemaRegistryClient _schemaRegistryClient;

    public ProtobufProducer(IProducer<string, byte[]> producer, ISchemaRegistryClient schemaRegistryClient)
    {
        _producer = producer;
        _schemaRegistryClient = schemaRegistryClient;
    }

    public async Task ProduceAsync<T>(string topic, T message, string partitionKey, CancellationToken cancellationToken)
        where T : IMessage<T>, new()
    {
        var serializer = new ProtobufSerializer<T>(_schemaRegistryClient);
        var serializerContext = new SerializationContext(MessageComponentType.Value, topic);

        var cloudEventData = await serializer.SerializeAsync(message, serializerContext);

        var cloudEvent = new CloudEvent()
        {
            Id = Guid.NewGuid().ToString(),
            Type = topic,
            DataContentType = "application/protobuf",
            Source = new Uri("https://my-app.com/service/events/"),
            Time = DateTimeOffset.UtcNow,
            Data = cloudEventData
        };

        cloudEvent.SetPartitionKey(partitionKey);

        var kafkaMessage = cloudEvent.ToKafkaMessage(ContentMode.Binary, new ProtobufEventFormatter());

        await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
    }
}