using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using HondaCivicHybrid2008.Resources;
using Windows.Networking.Sockets;
using Windows.Networking.Proximity;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using BluetoothConnectionManager;
using System.Windows.Media;

namespace HondaCivicHybrid2008
{
    public partial class MainPage : PhoneApplicationPage
    {
        //private ConnectionManager connectionManager;

        private StateManager stateManager;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            //App.connectionManager = new ConnectionManager();
            //connectionManager = App.connectionManager;
            //connectionManager = new ConnectionManager();
            //connectionManager.MessageReceived += connectionManager_MessageReceived;
            stateManager = new StateManager();

        }

        async void connectionManager_MessageReceived(string message)
        {
            Debug.WriteLine("Message received:" + message);
            
            string[] messageArray = message.Split(':');
            switch (messageArray[0])
            {
                case "LED_RED":
                    stateManager.RedLightOn = messageArray[1] == "ON" ? true : false;
                    Dispatcher.BeginInvoke(delegate()
                    {
                        RedButton.Background = stateManager.RedLightOn ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                    });
                break;
                case "LED_GREEN":
                    stateManager.GreenLightOn = messageArray[1] == "ON" ? true : false;
                    Dispatcher.BeginInvoke(delegate()
                    {
                        GreenButton.Background = stateManager.GreenLightOn ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Black);
                    });
                break;
                case "LED_YELLOW":
                    stateManager.YellowLightOn = messageArray[1] == "ON" ? true : false;
                    Dispatcher.BeginInvoke(delegate()
                    {
                        YellowButton.Background = stateManager.YellowLightOn ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Black);
                    });
                break;
                case "PROXIMITY":
                    stateManager.BodyDetected = messageArray[1] == "DETECTED" ? true : false;
                    if (stateManager.BodyDetected)
                    {
                        Dispatcher.BeginInvoke(delegate()
                        {
                            BodyDetectionStatus.Text = "Intruder detected!!!";
                            BodyDetectionStatus.Foreground = new SolidColorBrush(Colors.Red);
                        });
                        await App.connectionManager.SendCommand("TURN_ON_RED");
                    } else {
                        Dispatcher.BeginInvoke(delegate()
                        {
                            BodyDetectionStatus.Text = "No body detected";
                            BodyDetectionStatus.Foreground = new SolidColorBrush(Colors.White);
                        });
                    }
                    break;
                case "Temperature":
                    Dispatcher.BeginInvoke(delegate()
                    {
                        BodyDetectionStatus.Text = messageArray[1];
                        //BodyDetectionStatus.Foreground = new SolidColorBrush(Colors.Red);
                    });
                    break;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.connectionManager.Initialize();
            stateManager.Initialize();
            AppToDevice();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //App.connectionManager.Terminate();
        }

        private void ConnectAppToDeviceButton_Click_1(object sender, RoutedEventArgs e)
        {
            AppToDevice();
        }

        private async void AppToDevice()
        {
            ConnectAppToDeviceButton.Content = "Connecting...";
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var pairedDevices = await PeerFinder.FindAllPeersAsync();

            if (pairedDevices.Count == 0)
            {
                Debug.WriteLine("No paired devices were found.");
            }
            else
            { 
                foreach (var pairedDevice in pairedDevices)
                {
                    if (pairedDevice.DisplayName == DeviceName.Text)
                    {
                        App.connectionManager.Connect(pairedDevice.HostName);
                        ConnectAppToDeviceButton.Content = "Connected";
                        DeviceName.IsReadOnly = true;
                        ConnectAppToDeviceButton.IsEnabled = false;
                        break;
                    }
                    continue;
                }

                if(ConnectAppToDeviceButton.Content.ToString() == "Connected")
                {
                    //NavigationService.Navigate(new Uri("/PanoramaPage.xaml", UriKind.Relative));
                }
            }
        }

        private async void RedButton_Click_1(object sender, RoutedEventArgs e)
        {
            //string command = stateManager.RedLightOn ? "TURN_OFF_RED" : "TURN_ON_RED";
            string command = "1";
            await App.connectionManager.SendCommand(command);

            Dispatcher.BeginInvoke(delegate()
            {
                RedButton.Background = new SolidColorBrush(Colors.Black);
                GreenButton.Background = new SolidColorBrush(Colors.Red);
            });

        }

        private async void GreenButton_Click_1(object sender, RoutedEventArgs e)
        {
            //string command = stateManager.GreenLightOn ? "TURN_OFF_GREEN" : "TURN_ON_GREEN";
            string command = "0";
            await App.connectionManager.SendCommand(command);
            Dispatcher.BeginInvoke(delegate()
            {
                RedButton.Background = new SolidColorBrush(Colors.Green);
                GreenButton.Background = new SolidColorBrush(Colors.Black);
                BodyDetectionStatus.Text = "Press Start";

            });
        }

        private async void YellowButton_Click_1(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/PanoramaPage.xaml", UriKind.Relative));
        }

        private void GridManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            double dY = e.TotalManipulation.Translation.Y;
            double dX = e.TotalManipulation.Translation.X;

            if (Math.Abs(dY) > Math.Abs(dX))
            {
                // Vertical
            }
            else
            {
                // Horizontal
                if (dX < 0)
                {
                    NavigationService.Navigate(new Uri("/ComfortPage.xaml", UriKind.Relative));
                }
            }
        }
    }
}