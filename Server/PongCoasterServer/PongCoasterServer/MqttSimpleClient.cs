using MQTTnet;
using MQTTnet.Client;

namespace PongCoasterMqtt;

public class MqttSimpleClient
{
    private readonly IMqttClient _mqttClient;

    public MqttSimpleClient()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
    }

    public async Task ConnectAsync(string server, int port)
    {
        await _mqttClient.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(server, port)
            .Build());
    }

    public async Task SubscribeAsync(string topic)
    {
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .Build());
    }

    public async Task UnsubscribeAsync(string topic)
    {
        await _mqttClient.UnsubscribeAsync(topic);
    }

    public event Func<MqttApplicationMessageReceivedEventArgs, Task> MessageReceived
    {
        add { _mqttClient.ApplicationMessageReceivedAsync += value; }
        remove { _mqttClient.ApplicationMessageReceivedAsync -= value; }
    }

    public async Task PublishAsync(string topic, string payload, bool retain = false)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .Build();

        await _mqttClient.PublishAsync(message);
    }

    public async Task DisconnectAsync()
    {
        await _mqttClient.DisconnectAsync();
    }
}