using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PongCoasterUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private App _app;
        private ObservableCollection<Coaster> _coasters;

        public ObservableCollection<Coaster> Coasters
        {
            get { return _coasters; }
            set
            {
                _coasters = value;
                OnPropertyChanged(nameof(Coasters));
            }
        }
        public SettingsPage()
        {
            InitializeComponent();

            _app = (App)Application.Current;
            Coasters = _app.CoasterList;

            Coasters.CollectionChanged += Coasters_CollectionChanged;
        }


        private void MainPage_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }

        private void Coasters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                    foreach (Coaster item in e.NewItems)
                    {
                        item.PropertyChanged += Coaster_PropertyChanged;
                    }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                    foreach (Coaster item in e.OldItems)
                    {
                        item.PropertyChanged -= Coaster_PropertyChanged;
                    }
            }
        }

        private void Coaster_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Handle property changed events for the Coaster objects
            // This code will be executed whenever a property value changes
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
