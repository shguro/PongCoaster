using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PongCoasterUI.Model;
using Windows.Devices.PointOfService;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PongCoasterUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WeightGame : Page
    {
        private App _app;
        private ObservableCollection<Coaster> _coasters;
        private DispatcherTimer _timer;
        private int _secondsLeft = 30;

        public ObservableCollection<Coaster> Coasters
        {
            get { return _coasters; }
            set
            {
                _coasters = value;
                //OnPropertyChanged(nameof(Coasters));
            }
        }

        public WeightGame()
        {
            InitializeComponent();

            _app = (App)Application.Current;
            Coasters = _app.CoasterList;
            this.InitializeComponent();
        }

        private void MainPage_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }

        private async void Start_OnClick(object sender, RoutedEventArgs e)
        {
            // Set timer start time and weight for each coaster
            foreach (var coaster in Coasters)
            {
                coaster.TimerStartTime = DateTime.Now;
                coaster.TimerStartWeight = coaster.LastWeight;
            }

            // Start timer and update UI every second
            var timer = new DispatcherTimer();
            var remainingSeconds = 10;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, args) =>
            {
                remainingSeconds--;

                if (remainingSeconds == 0)
                {
                    // Stop timer and calculate weight differences
                    timer.Stop();

                    foreach (var coaster in Coasters)
                    {
                        coaster.TimerEndWeight = coaster.LastWeight;
                    }

                    // Sort coasters by weight difference
                    Coasters = new ObservableCollection<Coaster>(Coasters.OrderByDescending(c => c.WeightDifference));
                    TimeDisplay.Visibility = Visibility.Collapsed;
                        // Update UI
                    OnPropertyChanged(nameof(Coasters));
                }
                else
                {
                    TimeDisplay.Visibility = Visibility.Visible;
                    // Update timer display
                    TimeDisplay.Text = $"{remainingSeconds} s";
                }
            };
            timer.Start();
        }



        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
