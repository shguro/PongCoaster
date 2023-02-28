#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Internal;
using PongCoasterUI.MQTT;
using Windows.UI.Core;

namespace PongCoasterUI.Model;

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
                if (Client != null)
                    if (_color != null)
                        Client.PublishAsync("color/" + Hostname,
                            $"#{_color.Value.R:X2}{_color.Value.G:X2}{_color.Value.B:X2}");
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

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        if (topic == "weight/" + Hostname)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                LastWeight = double.Parse(payload, CultureInfo.InvariantCulture);
            });
        }
        else if (topic == "voltage/" + Hostname)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                LastVoltage = double.Parse(payload, CultureInfo.InvariantCulture);
            });
        }
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