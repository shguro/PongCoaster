using MQTTnet;
using PongCoasterServer;
using PongCoasterServer.MQTT;

var coasters = new List<Coaster>();

var server = new MqttSimpleServer();
await server.StartAsync();

var client = new MqttSimpleClient();
await client.ConnectAsync("localhost", 1883);
await client.SubscribeAsync("weight/#");
await client.SubscribeAsync("voltage/#");
await client.SubscribeAsync("connected/#");


//connected
client.MessageReceived += (sender) =>
{ 
    var topic = sender.ApplicationMessage.Topic;
    var payload = sender.ApplicationMessage.ConvertPayloadToString();
    
    if(topic.Contains("connected/"))
    {
        var hostname = topic.Replace("connected/", "");
        Console.WriteLine("Connected: " + hostname);
        if(coasters.Find(coster => coster.Hostname == hostname) != null) return Task.CompletedTask;
        var coster = new Coaster(hostname, client);
        coasters.Add(coster);
    }

    return Task.CompletedTask;
};

//dsconnected coaster
server.ClientDisconnected += (sender) =>
{
    var hostname = sender.ClientId;
    Console.WriteLine("Disconnected: " + hostname);
    var coaster = coasters.Find(coster => coster.Hostname == hostname);
    if(coaster != null) coaster.Dispose();
    if (coaster != null) coasters.Remove(coaster);
    return Task.CompletedTask;
};
Console.WriteLine("Press Enter to exit.");
Console.ReadLine();
await coasters.FirstOrDefault()?.Tare()!;

Console.WriteLine("Press Enter to exit.");
Console.ReadLine();

await client.DisconnectAsync();
await server.StopAsync();
