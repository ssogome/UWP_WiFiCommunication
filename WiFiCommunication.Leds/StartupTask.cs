using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using WiFiCommunication.Leds.LedsArray;
using Windows.Networking.Sockets;
using WiFiCommunication.Common.Helpers;
using Windows.UI;
using WiFiCommunincation.Common.Helpers;
using Windows.Devices.WiFi;
using System.Threading.Tasks;
using WiFiCommunication.Common.Enums;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace WiFiCommunication.Leds
{
    public sealed class StartupTask : IBackgroundTask
    {

        private BackgroundTaskDeferral taskDeferral;
        private LedArray ledArray;
        private StreamSocket streamSocket;
        private StreamSocketListener streamSocketListener;
        private volatile bool isCommunicationListenerStarted = false;

        public void Run(IBackgroundTaskInstance taskInstance)
        {

            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //

            taskDeferral = taskInstance.GetDeferral();
            InitializeLedArray();
            StartTcpService();
        }

        private async void InitializeLedArray()
        {
            const byte address = 0x46;
            var device = await I2cHelper.GetI2cDevice(address);
            if(device != null)
            {
                ledArray = new LedArray(device);
                ledArray.Reset(Colors.Black);
            }
            else
            {
                DiagnosticInfo.Display(null, " Led array unavailable");
            }
        }

        private async void StartTcpService()
        {
            var connectionStatus = await WiFiCommunicationHelper.ConnectToWiFiNetwork();

            if(connectionStatus == WiFiConnectionStatus.Success)
            {
                streamSocketListener = new StreamSocketListener();
                await streamSocketListener.BindServiceNameAsync(WiFiCommunicationHelper.DefaultPort.ToString());

                streamSocketListener.ConnectionReceived += StreamSocketListener_ConnectionReceived;
            }
            else
            {
                DiagnosticInfo.Display(null, "WiFi connection failed: " + connectionStatus.ToString());
            }
        }

        private void StreamSocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            DiagnosticInfo.Display(null, " Client has been connected.");
            streamSocket = args.Socket;

            StartCommunicationListener();
        }

        private void StartCommunicationListener()
        {
            if (!isCommunicationListenerStarted)
            {
                new Task(CommunicationListener).Start();
                isCommunicationListenerStarted = true;
            }
        }

        private async void CommunicationListener()
        {
            const int msSleepTime = 50;

            while (true)
            {
                var commandReceived = await SerialCommunicationHelper.ReadBytes(streamSocket.InputStream);
                try
                {
                    if(commandReceived.Length > 0)
                    {
                        ParseCommand(commandReceived);
                    }
                }
                catch(Exception ex)
                {
                    DiagnosticInfo.Display(null, ex.Message);
                }

                Task.Delay(msSleepTime).Wait();
            }
        }

        private void ParseCommand(byte[] command)
        {
            var errorCode = CommandHelper.VerifyCommand(command);
            if(errorCode == Common.Enums.ErrorCode.OK)
            {
                var commandId = (CommandId)command[CommandHelper.CommandIdIndex];
                switch (commandId)
                {
                    case CommandId.LedColor:
                        HandleLedColorCommand(command);
                        break;
                }
            }
        }

        private void HandleLedColorCommand(byte[] command)
        {
            var redChannel = command[CommandHelper.CommandDataBeginIndex];
            var greenChannel = command[CommandHelper.CommandDataBeginIndex + 1];
            var blueChannel = command[CommandHelper.CommandDataBeginIndex + 2];

            var color = Color.FromArgb(0, redChannel, greenChannel, blueChannel);

            if (ledArray != null)
            {
                ledArray.Reset(color);
            }

            DiagnosticInfo.Display(null, color.ToString() + " " + redChannel + " " + greenChannel + " " + blueChannel);
        }
    }
}
