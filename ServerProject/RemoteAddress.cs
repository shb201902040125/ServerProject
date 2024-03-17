using System.Net;
using System.Net.Sockets;

namespace ServerProject
{
    internal abstract class RemoteAddress
    {
        internal enum AddressType
        {
            IPV4,
            IPV6,
            NamedPipeStream
        }
        public AddressType Type { get; protected set; }
    }
    internal class TCPAddress : RemoteAddress
    {
        internal readonly IPAddress _address;
        internal readonly int _port;
        public TCPAddress(IPAddress address, int port)
        {
            Type = address.AddressFamily == AddressFamily.InterNetwork
                ? AddressType.IPV4
                : address.AddressFamily == AddressFamily.InterNetworkV6
                    ? AddressType.IPV6
                    : throw new ArgumentException($"Must be IPV4/IPV6 address:{address}");
            _address = address;
            _port = port;
        }
    }
    internal class LocalAddress : RemoteAddress
    {
        internal readonly string _label;
        public LocalAddress(string label)
        {
            _label = label;
            Type = AddressType.NamedPipeStream;
        }
    }
}
