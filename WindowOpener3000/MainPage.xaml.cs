using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowOpener3000
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private I2cDevice _arduino; 
        private Timer _updateTimer;
        private int _controller = 0;
        private string _windowState;
        private int _high;
        private int _low;

        public MainPage()
        {
            this.InitializeComponent();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            InitializeConnection();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


        }

        private async Task InitializeConnection()
        {
            //sets up connection
            var settings = new I2cConnectionSettings(0x40);
            settings.BusSpeed = I2cBusSpeed.StandardMode;
            //looks for certain type of device
            string aqs = I2cDevice.GetDeviceSelector("I2c1");
            //Gets the first device (the only one)
            var devices = await DeviceInformation.FindAllAsync(aqs);
            _arduino = await I2cDevice.FromIdAsync(devices[0].Id, settings);
            _updateTimer = new Timer(DataReadCallback, null, 0, 1000);
        }

        private async void DataReadCallback(object state)
        {
            Uri uri;
            switch (_controller)
            {
                case 0:

                    SendData(2);
                    string tdata = ReadData();
                    if (tdata != null)
                    {
                        tdata = tdata.Remove(2);
                        uri = new Uri(Constants.SetTempURL + tdata);
                        int currTemp;
                        if(int.TryParse(tdata, out currTemp))
                        {
                            if(currTemp > _high)
                            {
                                SendData(0);
                            }
                            if(currTemp < _low)
                            {
                                SendData(1);
                            }
                        }

                        using (var httpClient = new Windows.Web.Http.HttpClient())
                        {
                            // Always catch network exceptions for async methods
                            try
                            {
                                await httpClient.GetAsync(uri);
                            }
                            catch (Exception ex) { }
                        }
                    }

                    break;


                case 1:
                    SendData(4);
                    string hdata = ReadData();
                    if (hdata != null)
                    {
                        hdata = hdata.Remove(2);
                        uri = new Uri(Constants.SetHumidURL + hdata);
                        using (var httpClient = new Windows.Web.Http.HttpClient())
                        {
                            // Always catch network exceptions for async methods
                            try
                            {
                                await httpClient.GetAsync(uri);
                            }
                            catch (Exception ex) { }
                        }
                    }
                    break;

                case 2:
                    uri = new Uri(Constants.GetManual);
                    using (var httpClient = new Windows.Web.Http.HttpClient())
                    {
                        // Always catch network exceptions for async methods
                        try
                        {
                            string result = httpClient.GetStringAsync(uri).GetResults();

                            if (_windowState != result)
                            {
                                _windowState = result;

                                switch (_windowState)
                                {
                                    case "Open":
                                        SendData(0);
                                        break;
                                    case "Close":
                                        SendData(1);
                                        break;
                                }


                            }

                        }
                        catch (Exception ex) { }
                    }
                    break;

                case 3:
                    Uri uriLow = new Uri(Constants.GetLowURL);
                    Uri uriHigh = new Uri(Constants.GetHighURL);
                    using (var httpClient = new Windows.Web.Http.HttpClient())
                    {
                        // Always catch network exceptions for async methods
                        try
                        {
                            int newHigh = 0;
                            string result1 = httpClient.GetStringAsync(uriHigh).GetResults();
                            if(int.TryParse(result1,out newHigh))
                            {
                                if(_high != newHigh)
                                {
                                    _high = newHigh;
                                }
                            }

                            int newLow = 0;
                            string result2 = httpClient.GetStringAsync(uriHigh).GetResults();
                            if(int.TryParse(result2, out newLow))
                            {
                                if(_low != newLow)
                                {
                                    _low = newLow;
                                }
                            }

                        }
                        catch (Exception ex) { }
                        break;
                    }

            }

            _controller++;

            if(_controller > 3) { _controller = 0; }

        }

        private void SendData(Byte code)
        {
            _arduino.Write(new byte[] { code });
        }

        private string ReadData()
        {
            byte[] RegAddrBuf = new byte[] { 0x40 };
            byte[] readBuf = new byte[6];
            try
            {
                //read the data
                _arduino.Read(readBuf);
            }
            catch (Exception f)
            {
                Debug.WriteLine(f.Message);
            }

            char[] cArray = System.Text.Encoding.UTF8.GetString(readBuf, 0, 6).ToCharArray();
            string data = new string(cArray);
            Debug.WriteLine(data);
            var task = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _txtTemp.Text = data;
            });
            return data;
        }


    }
}
