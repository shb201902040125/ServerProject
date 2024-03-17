using System.Net;
using System.Net.Sockets;

namespace ServerProject
{
    public abstract class RemoteAddress
    {
        public enum AddressType
        {
            IPV4,
            IPV6,
            NamedPipeStream
        }
        public AddressType Type { get; protected set; }
    }
    public class TCPAddress : RemoteAddress
    {
        internal readonly IPAddress _address;
        internal readonly int _port;
        public IPAddress IPAddress => new(_address.GetAddressBytes());
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
    public class LocalAddress : RemoteAddress
    {
        internal readonly string _label;
        public string Label => _label;
        public LocalAddress(string label)
        {
            _label = label;
            Type = AddressType.NamedPipeStream;
        }
    }
}
