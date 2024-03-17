using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace ServerProject
{
    internal class Program
    {
        internal static string _localServerRoot;
        internal static int ipv4Port, ipv6Port;
        static TcpListener _ipv4Listener, _ipv6Listener;
        static NamedPipeServerStream _localListner;
        private static void Main(string[] args)
        {
            string? line;
            Console.WriteLine("请输入本地管道名称");
            while ((line = Console.ReadLine()) != null)
            {
                _localServerRoot = line;
                break;
            }
            new Thread(LocalListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", nameof(ServerProject), "LocalListenLoop")
            }.Start();

            Console.WriteLine("请输入IPV4监听端口");
            while ((line = Console.ReadLine()) != null)
            {
                if (int.TryParse(line, out ipv4Port))
                {
                    break;
                }
            }
            _ipv4Listener = new(IPAddress.Any, ipv4Port);
            _ipv4Listener.Start();
            new Thread(IPV4ListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", nameof(ServerProject), "IPV4ListenLoop")
            }.Start();

            Console.WriteLine("请输入IPV6监听端口");
            while ((line = Console.ReadLine()) != null)
            {
                if (int.TryParse(line, out ipv6Port))
                {
                    break;
                }
            }
            _ipv4Listener = new(IPAddress.IPv6Any, ipv6Port);
            _ipv4Listener.Start();
            new Thread(IPV6ListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", nameof(ServerProject), "IPV6ListenLoop")
            }.Start();
        }
        static void IPV4ListenLoop()
        {
            while (true)
            {
                try
                {
                    Task.Factory.StartNew(() => HandleSocket(new TCPSocket(_ipv4Listener.AcceptTcpClient())));
                }
                catch (Exception)
                {
                }
            }
        }
        static void IPV6ListenLoop()
        {
            while (true)
            {
                try
                {
                    Task.Factory.StartNew(() => HandleSocket(new TCPSocket(_ipv6Listener.AcceptTcpClient())));
                }
                catch (Exception)
                {
                }
            }
        }
        static void LocalListenLoop()
        {
            while (true)
            {
                try
                {
                    _localListner = new(string.Join(".", nameof(ServerProject), "Listen"), PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances);
                    _localListner.WaitForConnection();
                    string pipeName = string.Join(".", nameof(ServerProject), "Listen", Guid.NewGuid());
                    _localListner.Write(Encoding.ASCII.GetBytes(pipeName));
                    Task.Factory.StartNew(() => HandleSocket(new LocalSocket(pipeName)));
                    _localListner.Close();
                }
                catch (Exception)
                {
                }
            }
        }
        static async void HandleSocket(ISocket socket)
        {
            socket.AsyncConnect();

            socket.AsyncConnect();
            while (socket.IsConnected)
            {
                var result = await socket.AsyncRecive();
                if (result.Item1 == null)
                {
                    if (!"CRC Check Error".Equals(result.Item2))
                    {
                        socket.Close();
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
                HandleMemory(result.Item1, socket);
            }
        }
        private static void HandleMemory(MemoryStream memory, ISocket socket)
        {
            //TODO
        }
    }
}
