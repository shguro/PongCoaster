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

public class Coaster : INotifyPropertyChanged
{
    private string? _hostname;
    private Color? _color;
    private double? _lastWeight;
    private double? _lastVoltage;
    private string? _userName;

    //for weight game
    
    private DateTime _timerStartTime;
    private double? _timerStartWeight;
    private double? _timerEndWeight;

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


    public DateTime TimerStartTime
    {
        get => _timerStartTime;
        set
        {
            _timerStartTime = value;
            OnPropertyChanged();
        }
    }

    public double? TimerStartWeight
    {
        get => _timerStartWeight;
        set
        {
            _timerStartWeight = value;
            OnPropertyChanged();
        }
    }

    public double? TimerEndWeight
    {
        get => _timerEndWeight;
        set
        {
            _timerEndWeight = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WeightDifference));
        }
    }

    public double? WeightDifference => TimerEndWeight - TimerStartWeight;

    private MqttSimpleClient? Client { get; set; }

    public Coaster(string hostname, MqttSimpleClient? client)
    {
        Hostname = hostname;
        Client = client;
    }

    public async Task Tare()
    {
        if (Client != null) await Client.PublishAsync("tare/" + Hostname, "");
    }
    

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}