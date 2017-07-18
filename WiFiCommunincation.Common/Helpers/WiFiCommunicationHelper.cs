using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Credentials;

namespace WiFiCommunincation.Common.Helpers
{
    public class WiFiCommunicationHelper
    {
        public const string DefaultSsid = "HotspotName";
        public const string DefaultPassword = "ThePassword";
        public const string Rpi2HostName = "TheHostName";
        public const int DefaultPort = 9080;


        public static async Task<WiFiConnectionStatus> ConnectToWiFiNetwork(string ssid = DefaultSsid, string password= DefaultPassword)
        {
            var connectionStatus = WiFiConnectionStatus.NetworkNotAvailable;

            //Verify SSID and password
            if(!string.IsNullOrEmpty(ssid) && !string.IsNullOrEmpty(password))
            {
                //Verify that app has an access to WiFi functionality
                var hasAccess = await WiFiAdapter.RequestAccessAsync();

                //If so, sacn available networks using the first WiFi adapter
                if(hasAccess == WiFiAccessStatus.Allowed)
                {
                    //Get first WiFi adapter available
                    var wiFiAdapters = await WiFiAdapter.FindAllAdaptersAsync();
                    var firstWiFiAdapterAvailable = wiFiAdapters.FirstOrDefault();

                    if(firstWiFiAdapterAvailable != null)
                    {
                        //Scan networks
                        await firstWiFiAdapterAvailable.ScanAsync();

                        //FIlter the list of available networks by the SSID
                        var wiFiNetwork = firstWiFiAdapterAvailable.NetworkReport.AvailableNetworks.Where(network => network.Ssid == ssid).FirstOrDefault();

                        if(wiFiNetwork != null)
                        {
                            //Try to connect using password provided
                            var passwordCredential = new PasswordCredential()
                            {
                                Password = password
                            };

                            var connectionResult = await firstWiFiAdapterAvailable.ConnectAsync(wiFiNetwork, WiFiReconnectionKind.Automatic, passwordCredential);

                            //Return connection status
                            connectionStatus = connectionResult.ConnectionStatus;
                        }
                    }
                }
            }

            return connectionStatus;
        }

        public static async Task<StreamSocket> ConnectToHost(string hostName = Rpi2HostName, int port= DefaultPort)
        {
            var socket = new StreamSocket();
            await socket.ConnectAsync(new HostName(hostName), port.ToString());

            return socket;
        }
    }
}
