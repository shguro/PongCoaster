﻿using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Internal;
using PongCoasterServer.MQTT;

namespace PongCoasterServer;

public class Coaster : Disposable, INotifyPropertyChanged
{
    private string? _hostname;
    private Color? _color;
    private double? _lastWeight;
    private double? _lastVoltage;
    private string? _userName;

    public string? Hostname
    {
        get => _hostname;
        set
        {
            if (_hostname != value)
            {
                _hostname = value;
                OnPropertyChanged(nameof(Hostname));
            }
        }
    }

    public Color? Color
    {
        get => _color;
        set
        {
            if (_color != value)
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }
    }

    public double? LastWeight
    {
        get => _lastWeight;
        set
        {
            if (_lastWeight != value)
            {
                _lastWeight = value;
                OnPropertyChanged(nameof(LastWeight));
            }
        }
    }

    public double? LastVoltage
    {
        get => _lastVoltage;
        set
        {
            if (_lastVoltage != value)
            {
                _lastVoltage = value;
                OnPropertyChanged(nameof(LastVoltage));
            }
        }
    }

    public string? UserName
    {
        get => _userName;
        set
        {
            if (_userName != value)
            {
                _userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}