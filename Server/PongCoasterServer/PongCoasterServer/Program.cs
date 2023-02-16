using System.Text;
using PongCoasterMqtt;


var server = new MqttSimpleServer();
await server.StartAsync();

var client = new MqttSimpleClient();
await client.ConnectAsync("localhost", 1883);
await client.SubscribeAsync("weight/#");
client.MessageReceived += (sender) =>
{
    Console.WriteLine($"Received message on topic {sender.ApplicationMessage.Topic}: {Encoding.UTF8.GetString(sender.ApplicationMessage.Payload)}");
    return Task.CompletedTask;
};
await client.PublishAsync("example/topic", "Hello, world!");


Console.WriteLine("Press Enter to exit.");
Console.ReadLine();
await client.DisconnectAsync();
await server.StopAsync();
