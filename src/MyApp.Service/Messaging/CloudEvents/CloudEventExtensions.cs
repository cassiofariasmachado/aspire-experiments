using CloudNative.CloudEvents;
using Google.Protobuf;

namespace MyApp.Service.Messaging.CloudEvents;

public static class CloudEventExtensions
{
    private const int SchemaRegistryMessageBytesStartIndex = 6;

    public static TMessage GetMessage<TMessage>(this CloudEvent cloudEvent) where TMessage : IMessage<TMessage>
    {
        if (cloudEvent.Data is null)
            return default;

        var messageBytes = (cloudEvent.Data as byte[] ?? []).AsSpan(SchemaRegistryMessageBytesStartIndex);
        var parser = new MessageParser<TMessage>(Activator.CreateInstance<TMessage>);

        return parser.ParseFrom(messageBytes);
    }
}