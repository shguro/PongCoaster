using System.Drawing;
using System.Globalization;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Internal;
using PongCoasterServer.MQTT;

namespace PongCoasterServer;

public class Coaster : Disposable
{
    public string? Hostname { get; set; }
    public Color? Color { get; set; }
    public double? LastWeight { get; set; }
    public double? LastVoltage { get; set; }
    public string? UserName { get; set; }
    
    private MqttSimpleClient? Client { get; set; }

    public Coaster(string hostname, MqttSimpleClient? client)
    {
        Hostname = hostname;
        Client = client;
        client?.SubscribeAsync("weight/" + hostname);
        client?.SubscribeAsync("voltage/" + hostname);
        if (client != null) client.MessageReceived += OnMessageReceived;
    }

    public async Task Tare()
    {
        if (Client != null) await Client.PublishAsync("tare/" + Hostname, "");
    }

    protected override void Dispose(bool disposing){
        
        if (disposing)
        {
            Client?.UnsubscribeAsync("weight/" + Hostname);
            Client?.UnsubscribeAsync("voltage/" + Hostname);
            if (Client != null) Client.MessageReceived -= OnMessageReceived;
        }
        base.Dispose(disposing);    
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        if (topic == "weight/" + Hostname)
        {
            LastWeight = double.Parse(payload, CultureInfo.InvariantCulture);
            Console.WriteLine(payload, CultureInfo.InvariantCulture);
            Console.WriteLine(Hostname + "-Weight: " + LastWeight);
        }
        else if (topic == "voltage/" + Hostname)
        {
            LastVoltage = double.Parse(payload, CultureInfo.InvariantCulture);
            Console.WriteLine(Hostname + "-Voltage: " + LastVoltage);
        }

        return Task.CompletedTask;
    }
}