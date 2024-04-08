namespace MyApp.ApiService.Messaging.Events;

public class MessageReceived
{
    public string Text { get; init; }
    public DateTime ReceivedAt { get; set; }
}