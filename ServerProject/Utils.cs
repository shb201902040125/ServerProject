using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace ServerProject
{
    internal class Utils
    {
        public static bool TryGetAvailablePortIPv4(int start, int end, out int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            for (int i = start; i <= end; i++)
            {
                if (!IsPortInUseIPv4(i, tcpConnInfoArray))
                {
                    port = i;
                    return true;
                }
            }

            port = 0;
            return false;
        }
        private static bool IsPortInUseIPv4(int port, IPEndPoint[] tcpConnInfoArray)
        {
            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port && endpoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool TryGetAvailablePortIPv6(int start, int end, out int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            for (int i = start; i <= end; i++)
            {
                if (!IsPortInUseIPv6(i, tcpConnInfoArray))
                {
                    port = i;
                    return true;
                }
            }

            port = 0;
            return false;
        }
        private static bool IsPortInUseIPv6(int port, IPEndPoint[] tcpConnInfoArray)
        {
            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port && endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return true;
                }
            }
            return false;
        }
        public static byte[] CalculateCRC(byte[] data)
        {
            using (var crcAlgorithm = new Crc32())
            {
                return crcAlgorithm.ComputeHash(data);
            }
        }
        public class Crc32 : HashAlgorithm
        {
            private const uint DefaultPolynomial = 0xedb88320;
            private uint _dwPolynomial;
            private uint[] _crc32Table;
            private uint _crc;

            public Crc32()
            {
                _dwPolynomial = DefaultPolynomial;
                _crc32Table = InitializeCrc32Table(_dwPolynomial);
                _crc = 0xffffffff;
            }

            public override void Initialize()
            {
                _crc = 0xffffffff;
            }

            protected override void HashCore(byte[] array, int ibStart, int cbSize)
            {
                _crc = ~CalculateCrc32(_crc, _crc32Table, array, ibStart, cbSize);
            }

            protected override byte[] HashFinal()
            {
                byte[] hash = BitConverter.GetBytes(~_crc);
                Array.Reverse(hash);
                return hash;
            }

            public override int HashSize { get { return 32; } }

            private static uint[] InitializeCrc32Table(uint dwPolynomial)
            {
                uint[] crc32Table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint crc = i;
                    for (int j = 8; j > 0; j--)
                    {
                        if ((crc & 1) == 1)
                            crc = (crc >> 1) ^ dwPolynomial;
                        else
                            crc >>= 1;
                    }
                    crc32Table[i] = crc;
                }
                return crc32Table;
            }

            private static uint CalculateCrc32(uint crc, uint[] crc32Table, byte[] buffer, int offset, int size)
            {
                crc = ~crc;
                for (int i = offset; i < size; i++)
                {
                    unchecked
                    {
                        crc = (crc >> 8) ^ crc32Table[buffer[i] ^ crc & 0xff];
                    }
                }
                return ~crc;
            }
        }
    }
}
