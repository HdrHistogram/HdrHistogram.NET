using System;
using System.Net;
using System.Net.Sockets;

namespace HdrHistogram.Examples
{
    /// <summary>
    /// A class used to test opening and closing a TCP socket.
    /// </summary>
    public class SocketTester
    {
        private readonly Lazy<AddressFamily> _addressFamily;
        public SocketTester(string url)
        {
            _addressFamily = new Lazy<AddressFamily>(() => GetAddressFamily(url));
        }
        
        public void CreateAndCloseDatagramSocket()
        {
            try
            {
                using (var socket = new Socket(_addressFamily.Value, SocketType.Stream, ProtocolType.Tcp))
                {
                }
            }
            catch (SocketException)
            {
            }
        }


        private static AddressFamily GetAddressFamily(string url)
        {
            var hostIpAddress = Dns.GetHostEntryAsync(url).GetAwaiter().GetResult().AddressList[0];
            var hostIpEndPoint = new IPEndPoint(hostIpAddress, 80);
            return hostIpEndPoint.AddressFamily;
        }
    }
}