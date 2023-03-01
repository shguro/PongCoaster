using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Microsoft.UI;
using MQTTnet;
using PongCoasterUI.Model;
using PongCoasterUI.MQTT;

namespace PongCoasterUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public ObservableCollection<Coaster> CoasterList { get; set; } = new ObservableCollection<Coaster>();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        /// <remarks>
        /// If you're looking for App.xaml.cs, the file is present in each platform head
        /// of the solution.
        /// </remarks>
        public App()
        {
        }

        /// <summary>
        /// Gets the main window of the app.
        /// </summary>
        internal static Window MainWindow { get; private set; }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
#if DEBUG
		if (System.Diagnostics.Debugger.IsAttached)
		{
			// this.DebugSettings.EnableFrameRateCounter = true;
		}
#endif

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
		MainWindow = new Window();
		MainWindow.Activate();
#else
            MainWindow = Microsoft.UI.Xaml.Window.Current;
#endif

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (MainWindow.Content is not Frame rootFrame)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (args.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                MainWindow.Content = rootFrame;
            }

#if !(NET6_0_OR_GREATER && WINDOWS)
            if (args.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
#endif
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), args.Arguments);
                }
                // Ensure the current window is active
                MainWindow.Activate();
            }
            var server = new MqttSimpleServer();
            await server.StartAsync();

            var client = new MqttSimpleClient();
            await client.ConnectAsync("localhost", 1883);
            await client.SubscribeAsync("weight/#");
            await client.SubscribeAsync("voltage/#");
            await client.SubscribeAsync("connected/#");


            //connected
            client.MessageReceived += async (sender) =>
            {
                var topic = sender.ApplicationMessage.Topic;
                var payload = sender.ApplicationMessage.ConvertPayloadToString();

                if (topic.Contains("connected/"))
                {
                    var hostname = topic.Replace("connected/", "");
                    Console.WriteLine("Connected: " + hostname);
                    await MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        var oldCoaster = CoasterList.FirstOrDefault(coaster => coaster.Hostname == hostname);
                        if(oldCoaster != null)
                        {
                            CoasterList.Remove(oldCoaster);
                            client?.UnsubscribeAsync("weight/" + oldCoaster.Hostname);
                            client?.UnsubscribeAsync("voltage/" + oldCoaster.Hostname);
                        }

                        var coaster = new Coaster(hostname, client);
                        coaster.Color =
                            Helper.Colors.ColorsList[CoasterList.Count % Helper.Colors.ColorsList.Count];
                        client?.SubscribeAsync("weight/" + coaster.Hostname);
                        client?.SubscribeAsync("voltage/" + coaster.Hostname);
                        CoasterList.Add(coaster);
                        await client.PublishAsync("color/" + coaster.Hostname,
                            $"#{coaster.Color.Value.R:X2}{coaster.Color.Value.G:X2}{coaster.Color.Value.B:X2}");
                        
                    });
                }
                if (topic.Contains("weight"))
                {
                    await MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        var hostname = topic.Replace("weight/", "");
                        var coaster = CoasterList.FirstOrDefault(coaster => coaster.Hostname == hostname);
                        if (coaster != null) coaster.LastWeight = double.Parse(payload, CultureInfo.InvariantCulture);
                    });
                }
                else if (topic.Contains("voltage"))
                {
                    await MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        var hostname = topic.Replace("voltage/", "");
                        var coaster = CoasterList.FirstOrDefault(coaster => coaster.Hostname == hostname);
                        if (coaster != null) coaster.LastVoltage = double.Parse(payload, CultureInfo.InvariantCulture);
                    });
                }
            };

            //disconnected coaster
            server.ClientDisconnected += async (sender) =>
            {
                var hostname = sender.ClientId;
                Console.WriteLine("Disconnected: " + hostname);
                var coaster = CoasterList.FirstOrDefault(coster => coster.Hostname == hostname);
                if (coaster != null)
                {
                    await MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        CoasterList.Remove(coaster);
                    });
                }
            };
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save the application state and stop any background activity
            deferral.Complete();
        }
    }
}