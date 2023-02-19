using MQTTnet;
using MQTTnet.Server;

namespace PongCoasterServer.MQTT;

public class MqttSimpleServer
{
    private readonly MqttServer _mqttServer;

    public MqttSimpleServer()
    {
        var factory = new MqttFactory();
        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint();
        _mqttServer = factory.CreateMqttServer(optionsBuilder.Build());
    }

    public async Task StartAsync()
    {
        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint();
        
        await _mqttServer.StartAsync();
    }

    public async Task StopAsync()
    {
        await _mqttServer.StopAsync();
    }
    
    public event Func<ClientDisconnectedEventArgs, Task> ClientDisconnected
    {
        add { _mqttServer.ClientDisconnectedAsync += value; }
        remove { _mqttServer.ClientDisconnectedAsync -= value; }
    }
}