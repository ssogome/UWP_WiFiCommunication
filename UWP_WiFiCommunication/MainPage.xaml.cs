using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WiFiCommunication.Common.Helpers;
using WiFiCommunication.Master;
using WiFiCommunincation.Common.Helpers;
using Windows.Devices.WiFi;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP_WiFiCommunication
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private ObservableCollection<string> diagnosticData = new ObservableCollection<string>();

        private SenseHatColor senseHatColor = new SenseHatColor();

        private StreamSocket streamSocket;

        public MainPage()
        {
            this.InitializeComponent();
        }

        public Task WiFiConnectionHelper { get; private set; }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await CloseStreamSocket();
                var connectionStatus = await WiFiCommunicationHelper.ConnectToWiFiNetwork();
                if(connectionStatus == WiFiConnectionStatus.Success)
                {
                    streamSocket = await WiFiCommunicationHelper.ConnectToHost();
                    DiagnosticInfo.Display(diagnosticData, "Connected to: " + WiFiCommunicationHelper.Rpi2HostName);
                }
            }
            catch(Exception ex)
            {
                DiagnosticInfo.Display(diagnosticData, ex.Message);
            }
        }

        private async Task CloseStreamSocket()
        {
            if(streamSocket != null)
            {
                await streamSocket.CancelIOAsync();
                streamSocket.Dispose();
                streamSocket = null;
            }
        }

        private async void ButtonSendColor_Click(object sender, RoutedEventArgs e)
        {
            if(streamSocket != null)
            {
                var commandData = CommandHelper.PrepareLedColorCommand(senseHatColor.Brush.Color);
                await SerialCommunicationHelper.WriteBytes(streamSocket.OutputStream, commandData);
                DiagnosticInfo.Display(diagnosticData, CommandHelper.CommandToString(commandData));
            }
            else
            {
                DiagnosticInfo.Display(diagnosticData, "No active connection.");
            }
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            diagnosticData.Clear();
        }
    }
}
