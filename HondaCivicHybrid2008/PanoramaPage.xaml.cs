using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.IO;
using System.IO.IsolatedStorage;
using Windows.Networking.Sockets;
using Windows.Networking.Proximity;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BluetoothConnectionManager;

using Logger = CaledosLab.Portable.Logging.Logger;
using Microsoft.Phone.Tasks;

namespace HondaCivicHybrid2008
{
    public partial class PanoramaPage : PhoneApplicationPage
    {
#if DEBUG
        private static string bluetoothDevice = "4DELAHMAD1";
#else
        private static string bluetoothDevice = "OBDLink LX";
        //private static string bluetoothDevice = "HC-05";
#endif

        private enum BtStatus
        {
            Disabled,
            Enabled,
            Connected,
            Disconnected
        };

        private enum OBDstate
        {
            DISCONNECTED,
            CONNECTING,
            ATZ_SENT,
            ATZ_RESPONSE_RECEIVED,
            DEVICE_ID_RECEIVED,
            ATI_SENT,
            ATH_SENT,
            ATL_SENT,
            ATS_SENT,
            ATAL_SENT,
            ATSP_SENT,
            ATMA1_SENT,
            SEARCHING_FOR_PROTOCOL,
            ATMA1_STOPPED,
            ATCM_SENT,
            ATCF_SENT,
            ATMA_SENT
        };

        private enum ByteLocation:int
        {
            POS_1 = 0,
            POS_2 = 2,
            POS_3 = 4,
            POS_4 = 6,
            POS_5 = 8,
            POS_6 = 10,
            POS_7 = 12
        };

        private enum NumBytes:int
        {
            One = 2,
            Two = 4,
            Three = 6
        };

        private string[] can_ids = { "158 Speed Actual x100km/h",
                                     "161 ? Throttle",
                                     "1DC Engine RPM",
                                     "133 IMA Regen",
                                     "136 Accelerator Pedal Position",
                                     "13A ? Accelerator Pedal Position",
                                     "13F ? Transmission, RPM",
                                     "164 ? Cruise Control",
                                     "17C ? RPM",
                                     "183 Brakes",
                                     "1AA ? VSA, Brake Pedal?",
                                     "21E HVAC Controls",
                                     "324 HVAC, Coolant Temp",
                                     "37C ? HVAC",
                                     "405 ? Seat belt indicator, chime",
                                     
                                     "039 ? Constant",
                                     "143 ? IMA Constant",
                                     "166 ? IMA Constant",
                                     "1A4 ? VSA, Constant",
                                     "320 ? Constant",
                                     "40C ? Constant",
                                     "454 ? Constant",

                                     "096 Yaw-rate sensor",
                                     "18E Yaw-rate sensor",
                                     "191 Transmission",
                                     "1B0 Steering Wheel Angle",
                                     "1CF IMA Assist/Regen Indicators",
                                     "1D0 VSA LF Wheel Speed",
                                     "294 Odometer",
                                     "305 Seat Belt On/Off",
                                     "309 Speed Indicated x100km/h",
                                     "428 Odometer Indicated" };
                                     
        //private string[] can_ids = { "039 ? Constant",
        //                             "096 Yaw-rate sensor",
        //                             "133 IMA Regen",
        //                             "136 Accelerator Pedal Position",
        //                             "13A ? Accelerator Pedal Position",
        //                             "13F ? RPM, Gear stick, Engine load",
        //                             "143 ? IMA Constant",
        //                             "158 Speed Actual x100km/h",
        //                             "161 ? ",
        //                             "164 ? Cruise Control",
        //                             "166 ? IMA Constant",
        //                             "17C RPM, Parking Brake",
        //                             "183 ?",
        //                             "18E Yaw-rate sensor",
        //                             "191 Throttle Position",
        //                             "1A4 ? VSA, Constant",
        //                             "1AA ? VSA, Brake Pedal?",
        //                             "1B0 Steering Wheel Angle",
        //                             "1CF IMA Assist/Regen Indicators",
        //                             "1D0 VSA LF Wheel Speed",
        //                             "1DC Engine RPM",
        //                             "21E HVAC Controls",
        //                             "294 Odometer",
        //                             "305 Seat Belt On/Off",
        //                             "309 Speed Indicated x100km/h",
        //                             "320 ? Constant",
        //                             "324 HVAC, Coolant Temp",
        //                             "37C ? HVAC",
        //                             "405 ? Almost Constant",
        //                             "40C ? Constant",
        //                             "428 Odometer Indicated",
        //                             "454 ? Constant" };
        private int msg_counter;
        private int can_ids_counter;
        private bool scan_all;
        private bool buffer_full;

        private string deviceID;
        private string can_id;
        private StateManager stateManager;
        private BtStatus btStatus;
        private OBDstate obdState;
        private string canIdsFilter;
        private int nCanIdsSelected;
        private string displayText, displayTextOld, displayText2;
        private string[] canCommands;
        private string[] canCmdAndResponse;
        private int cmdCounter;
        private DispatcherTimer dispatcherTimer;
        private IsolatedStorageFile isolatedStorage;
        private StreamReader streamReader;
        private IsolatedStorageFileStream fileStream;

        private double speed;
        private double maf;
        private double km_l;
        private static double maf_to_speed_factor = 3600 / (14.7 * 7.02);

        public PanoramaPage()
        {
            InitializeComponent();
            for (int i = 0; i < can_ids.Length; i++)
            {
                lstCanIds.Items.Add(can_ids[i]);
            }

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            
            cmdCounter = 0;
            buffer_full = false;

            stateManager = new StateManager();
            btStatus = BtStatus.Enabled;
            msg_counter = 0;
            can_ids_counter = 0;

            lstCanIds.SummaryForSelectedItemsDelegate = GetSelectedCanIds;
            App.connectionManager.MessageReceived += connectionManager_MessageReceived;

            
            string logfile = DateTime.Now.ToString("yyyy.MM.d.HHmm") + ".txt";

            isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
            if (isolatedStorage.FileExists(logfile))
            {
                fileStream = isolatedStorage.OpenFile(logfile, FileMode.Open);
            }
            else
            {
                fileStream = isolatedStorage.CreateFile(logfile);
            }

            App.streamWriter = new StreamWriter(fileStream);
            App.streamWriter.AutoFlush = true;

            streamReader = new StreamReader(fileStream);
            Logger.Load(streamReader);

            Logger.WriteLine(App.streamWriter, "Initialized");
            

            speed = 0;
            maf = 0;
            km_l = 0;

            /*
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isolatedStorage.FileExists(LogFile))
                {
                    using (IsolatedStorageFileStream fs = isolatedStorage.OpenFile(LogFile, FileMode.Open))
                    {
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            Logger.Load(reader);
                        }
                    }
                }
            }
            */
        }

        private string GetSelectedCanIds(System.Collections.IList items)
        {
            if (items != null && items.Count > 0)
            {
                string selectedIds = "";
                for (int i = 0; i < items.Count; i++)
                {
                    selectedIds += (string)items[i];

                    // If not last item, add a comma to seperate them
                    if (i != items.Count - 1)
                        selectedIds += ", ";
                }

                return selectedIds;
            }
            else
                return "Nothing selected";
        }

        private void connectionManager_MessageReceived(string message)
        {
            Debug.WriteLine("Message received:" + message);
            HandleReceivedMessage(message);

            /*
            switch((int)message[0])
            {
                case 0:
                    SendCommand("1");
                break;
                case 1:
                    string temperature = message.Substring(1, 5);
                    string humidity = message.Substring(6, 5);

                    Dispatcher.BeginInvoke(delegate()
                    {
                        txtTemp.Text = temperature;
                        txtHumidity.Text = humidity;
                    });
                break;

                default:
                    //SendCommand("0");
                    //SendCommand("1");
                break;
            }
            */
        }
        /*
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
                        await connectionManager.SendCommand("TURN_ON_RED");
                    }
                    else
                    {
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
        */

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(btStatus == BtStatus.Enabled)
            {
                App.connectionManager.Initialize();
                //connectionManager.Initialize();
                stateManager.Initialize();
                //ConnectToBTDevice("HCH2008");
                ConnectToBTDevice(bluetoothDevice);
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            //SendCommand("0");
            //connectionManager.Terminate();
        }

        private void ConnectConnectToBTDeviceButton_Click_1(object sender, RoutedEventArgs e)
        {
            ConnectToBTDevice("HCH2008");
        }

        private void btnCanBusStart_Click(object sender, RoutedEventArgs e)
        {
            if (btnCanBusStart.Content.ToString() == "Start")
            {
                /*
                string logfile = DateTime.Now.ToString("yyyy.MM.d.HHmm") + ".txt";

                isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
                if (isolatedStorage.FileExists(logfile))
                {
                    fileStream = isolatedStorage.OpenFile(logfile, FileMode.Open);
                }
                else
                {
                    fileStream = isolatedStorage.CreateFile(logfile);
                }

                App.streamWriter = new StreamWriter(fileStream);
                App.streamWriter.AutoFlush = true;

                streamReader = new StreamReader(fileStream);
                Logger.Load(streamReader);

                Logger.WriteLine(App.streamWriter, "Initialized");
                */

                string cmdStr = App.canCommands + canIdsFilter + "STM:STM";
                canCommands = cmdStr.Split('\r');
                cmdCounter = 0;
                buffer_full = false;
                canCmdAndResponse = canCommands[cmdCounter++].Split(':');
                SendCommand(canCmdAndResponse[0]);
                txtDisplay.Text = "Starting...";
                txtNumCanMsgs.Text = "0";
                displayTextOld = "EMPTY";
                displayText2 = "";
                displayText = "";
            }
            else
            {
                btnCanBusStart.Content = "Start";
                txtDisplay.Text = "Stopped";
                SendCommand("");
                dispatcherTimer.Stop();
            }
        }

        private void btnCanBusSendLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EmailComposeTask mail = new EmailComposeTask();
                mail.Subject = "Honda Civic 2008 CAN Bus Log";
                mail.Body = Logger.GetStoredLog();

                if (mail.Body.Length > 32000) // max 32K
                {
                    mail.Body = mail.Body.Substring(mail.Body.Length - 32000);
                }

                mail.Show();
            }
            catch
            {
                MessageBox.Show("unable to create the email message");
            }
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            double km = Convert.ToDouble(txtKm.Text);
            double lp100km = Convert.ToDouble(txtLp100km.Text);

            const double tank_capacity = 40.00;
            double km_p_l = 100 / lp100km;
            double consumption = km / km_p_l;
            double remaining = tank_capacity - consumption;
            double max_km = remaining * km_p_l;

            lblKmPl.Text = "km/L: " + Convert.ToString(Math.Round(km_p_l, 2));
            lblConsumption.Text = "Consumption: " + Convert.ToString(Math.Round(consumption, 2)) + " Litres";
            lblRemaining.Text = "Remaining: " + Convert.ToString(Math.Round(remaining, 2)) + " Litres";
            lblMaxKm.Text = "Max Distance: " + Convert.ToString(Math.Round((max_km), 2)) + " km";
        }

        private async void ConnectToBTDevice(string device)
        {
            //ConnectConnectToBTDeviceButton.Content = "Connecting...";
            try
            {
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
                        if (pairedDevice.DisplayName == device)
                        {
                            App.connectionManager.Connect(pairedDevice.HostName);
                            Debug.WriteLine("Connecting to " + device + " ...");
                            Logger.WriteLine(App.streamWriter, "Connecting to " + device + " ...");
                            //ConnectConnectToBTDeviceButton.Content = "Connected";
                            //DeviceName.IsReadOnly = true;
                            //ConnectConnectToBTDeviceButton.IsEnabled = false;
                            btStatus = BtStatus.Connected;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x8007048F)
                {
                    btStatus = BtStatus.Disabled;
                }

            }
        }
        private async void SendCommand(string cmd)
        {
            await App.connectionManager.SendCommand(cmd + "\r\n");
            Logger.WriteLine(App.streamWriter, cmd);
        }

        private void HandleReceivedMessage(string message)
        {   /*
            Dispatcher.BeginInvoke(delegate()
            {
                txtRawOutput.Text = message;
            });
            */

            if (message == "BUFFER FULL")
            {
                Dispatcher.BeginInvoke(delegate()
                {
                    txtNumCanMsgs.Text = Convert.ToString( Convert.ToUInt32(txtNumCanMsgs.Text) + 1 );
                    txtDisplay.Text = message;
                });
                SendCommand("");
                buffer_full = true;
            }
            else if (message == "CAN ERROR")
            {
                Dispatcher.BeginInvoke(delegate()
                {
                    //btnCanBusStart.Content = "Start";
                    txtDisplay.Text = message;
                });
            }
            else if ( canCommands != null && cmdCounter <= canCommands.Length)
            {
                if(message.IndexOf(canCmdAndResponse[1]) >= 0 )
                {
                    if (cmdCounter == canCommands.Length)
                    {
                        // this was the response to the last command, most often "STM"
                        cmdCounter++;
                    }
                    else
                    {
                        canCmdAndResponse = canCommands[cmdCounter++].Split(':');

                        if (canCmdAndResponse[0].Length > 2)
                        {
                            SendCommand(canCmdAndResponse[0]);

                            Dispatcher.BeginInvoke(delegate()
                            {
                                if (canCmdAndResponse[0] == "ATMA" || canCmdAndResponse[0] == "STM")
                                {
                                    btnCanBusStart.Content = "Stop";
                                    txtDisplay.Text = "Scanning...";
                                    dispatcherTimer.Start();
                                }
                                else
                                {
                                    txtDisplay.Text = canCmdAndResponse[0];
                                }
                            });
                        }
                    }
                }
            }
            else if(buffer_full)
            {
                buffer_full = false;
                displayText = "Scanning...";
                /*
                Dispatcher.BeginInvoke(delegate()
                {
                    txtDisplay.Text = "Scanning...";
                });
                */
            }
            else
            {
                displayText = decodeMessage(message);

                /*
                if (displayText != displayTextOld)
                {
                    Dispatcher.BeginInvoke(delegate()
                    {
                        txtDisplay.Text = displayText;
                        displayTextOld = displayText;
                    });
                }
                */
            }

            Logger.WriteLine(App.streamWriter, message);
        }

        private string decodeMessage( string message )
        {
            string dispText = message;
            try
            {
                uint msgId = Convert.ToUInt32(message.Substring(0, 3), 16);
                message = message.Substring(3, message.Length - 5); // drop the ID and cheksum bytes
                dispText = message;
                for (int i = 2; i < dispText.Length; i += 3)
                    dispText = dispText.Insert(i, " ");

                dispText += "\n";

                switch (msgId)
                {
                    case 0x158: 
                        speed = Convert.ToUInt32(message.Substring(0, 4), 16); speed /= 100; 
                        break;

                    case 0x161: 
                        maf = Convert.ToUInt32(message.Substring(12, 2), 16);
                        km_l = Convert.ToUInt32(message.Substring(0, 4), 16);
                        km_l /= 100.00;
                        break;
                }

                if (nCanIdsSelected == 1 && msgId == Convert.ToUInt32(can_id, 16 ))
                {
                    switch (msgId)
                    {
                        case 0x039:
                            dispText += Convert.ToString(Convert.ToInt32(message.Substring(0, 2), 16));
                            break;

                        case 0x096: // Yaw-rate sensor
                            dispText += Convert.ToString(Convert.ToInt32(message.Substring(0, 4), 16) - 0x8000) + "\n" +
                                        Convert.ToString(Convert.ToInt32(message.Substring(4, 4), 16) - 0x0800);
                            break;

                        case 0x133: // IMA Regen request
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(0, 4), 16) / 1000);
                            break;

                        case 0x136: // Accelerator Pedal position
                            dispText += Convert.ToString(Convert.ToInt32(message.Substring(8, 2), 16));
                            break;

                        case 0x13A:
                            dispText += Convert.ToString(Convert.ToInt32(message.Substring(2, 2), 16));
                            break;

                        case 0x13F:
                            dispText += Convert.ToString(Convert.ToInt16(message.Substring(0, 4), 16)) + "\n" +
                                        Convert.ToString(Convert.ToInt16(message.Substring(4, 4), 16)) + "\n" +
                                        Convert.ToString(Convert.ToInt16(message.Substring(8, 4), 16)) + "\n" +
                                        message.Substring(10, 2);
                            break;

                        case 0x143:
                            break;
                        case 0x158: // Speed Actual
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(0, 4), 16)/100) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(8, 4), 16)/100);
                            break;
                        case 0x161:
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(0, 4), 16)) + "  " + Convert.ToString(Convert.ToUInt32(message.Substring(4, 4), 16)) + "\n" +
                                        message.Substring(8, 2) + "   " + message.Substring(10, 2) + "   " + Convert.ToString(Convert.ToUInt32(message.Substring(12, 2), 16)) + "\n" +
                                        Convert.ToString( Math.Round( 0.38 * maf , 2));
                            break;

                        case 0x164:
                            dispText += message.Substring(0, 2) + "   " + message.Substring(2, 2) + "   " + message.Substring(4, 2) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(6, 2), 16)) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(8, 2), 16)) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(10, 4), 16));
                            break;

                        case 0x166:
                            break;
                        case 0x17C:
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(4, 4), 16)) + "\n" +
                                        message.Substring(8, 2);
                            break;

                        case 0x183:
                            break;
                        case 0x18E: // Yaw-rate sensor 2
                            dispText += Convert.ToString(Convert.ToInt32(message.Substring(0, 4), 16));
                            break;

                        case 0x191:
                            dispText += message.Substring(0, 2) + "   " + message.Substring(2, 2) + "   " + message.Substring(4, 2) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(6, 2), 16)) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(8, 2), 16)) + "\n" +
                                        message.Substring(10, 2);
                            break;

                        case 0x1A4:
                            break;
                        case 0x1AA:
                            break;
                        case 0x1B0: // Steering Wheel Sensor
                            dispText += Convert.ToString(Convert.ToInt16(message.Substring(4, 4), 16));
                            break;

                        case 0x1CF: // IMA ASSIST/REGEN Indicators
                            dispText += Convert.ToString(Convert.ToInt32(message.Substring(0, 2), 16) - 0x80);
                            break;

                        case 0x1D0: // Left Front Wheel Speed
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(0, 4), 16) / 200) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(4, 4), 16) / 400) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(8, 4), 16)) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(12, 2), 16));
                            break;

                        case 0x1DC: // Engine RPM
                            dispText += message.Substring(0, 2) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(2, 4), 16));
                            break;

                        case 0x21E:
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(0, 4), 16)) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(4, 2), 16)) + "  " +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(6, 2), 16) - 40) + "\n" +
                                        message.Substring(8, 2) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(10, 1), 16)) + "  " +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(11, 1), 16));
                            break;

                        case 0x294:
                            break;
                        case 0x305:
                            break;
                        case 0x309:
                            break;
                        case 0x320:
                            break;
                        case 0x324:
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(0, 2), 16)) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(2, 2), 16) - 40) + "\n" +
                                        Convert.ToString(Convert.ToUInt32(message.Substring(4, 4), 16)) + "\n" +
                                        Convert.ToString(Convert.ToSByte(message.Substring(10, 2), 16));
                            break;

                        case 0x37C:
                            dispText += Convert.ToString(Convert.ToSByte(message.Substring(0, 2), 16)) + "\n";
                            dispText += Convert.ToString(Convert.ToSByte(message.Substring(4, 2), 16)) + "\n";
                            dispText += Convert.ToString(Convert.ToSByte(message.Substring(8, 2), 16));
                            break;

                        case 0x405:
                            break;
                        case 0x428: // Odometer Indicated
                            dispText += Convert.ToString(Convert.ToUInt32(message.Substring(6, 6), 16));
                            break;

                        case 0x40C:
                            break;
                        case 0x454:
                            break;

                    }
                }
                else if (msgId == 0x428)
                {   /*
                    double l_100km = 0;
                    if (km_l > 0)
                    {
                        l_100km = 100.00 / km_l;
                    }
                    */
                    displayText2 = string.Format("{0:0.00}", speed) + "  " + string.Format("{0:0}", (maf)) + "  " + string.Format("{0:0.00}", (0.38 * maf));

                    /*
                    dispText = Convert.ToString(Convert.ToUInt32(message.Substring(6, 6), 16));
                    double l_100km = 0;
                    if (maf > 0)
                    {
                        if (speed > 0)
                            l_100km = maf_to_speed_factor * maf / speed;
                        else
                            l_100km = maf_to_speed_factor * maf / 100.00;
                    }

                    displayText2 = dispText + "  " + string.Format("{0:0.00}", speed) + "  " +
                                   string.Format("{0:0.00}", (l_100km) );
                    */
                }
            }
            catch (Exception e)
            {
                dispText += message + "\n" + e.Message;
            }

            return dispText;
        }

        private void lstCanIds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPicker lpi = (sender as ListPicker);
            canIdsFilter = "";
            nCanIdsSelected = 0;
            if (lpi != null && lpi.SelectedItems != null && lpi.SelectedItems.Count > 0)
            {
                nCanIdsSelected = lpi.SelectedItems.Count;
                for (int i = 0; i < lpi.SelectedItems.Count; i++)
                {
                    canIdsFilter += "STFAP " + lpi.SelectedItems[i].ToString().Substring(0, 3) + ", FFF:OK\r";
                }

                // Add odometer to list if scanning more than one item
                if (lpi.SelectedItems.Count > 1 && canIdsFilter.IndexOf("428") <= 0)
                {
                    canIdsFilter += "STFAP 428, FFF:OK\r";
                    nCanIdsSelected++;
                }

                txtCanId.Text = lpi.SelectedItems[0].ToString().Substring(0, 3);
                can_id = txtCanId.Text;
                txtDisplay.Text = "Tap Start to start";
            }
            else
            {
                txtDisplay.Text = "Select CAN IDs above";
            }
        }

        private void btnCanBusSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CanSettingsPage.xaml", UriKind.Relative));
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (displayText != displayTextOld)
            {
                Dispatcher.BeginInvoke(delegate()
                {
                    txtDisplay.Text = displayText;
                    txtRawOutput.Text = displayText2;
                    displayTextOld = displayText;
                });
            }
        }

        async private void btnStartDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            // get Bluetooth devices list
            PeerFinder.Start();
            PeerFinder.AlternateIdentities["Bluetooth"] = "";
            var peers = await PeerFinder.FindAllPeersAsync();

            if (peers.Count == 0)
            {
                MessageBox.Show("Peer not found.");
            }
        }
    }
}