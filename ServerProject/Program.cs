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
        private static void Main(string[] args)
        {
            string? line;
            Console.WriteLine("请输入本地管道名称");
            while ((line = Console.ReadLine()) != null)
            {
                _localServerRoot = line;
                break;
            }

            Console.WriteLine("请输入IPV4监听端口");
            while ((line = Console.ReadLine()) != null)
            {
                if (int.TryParse(line, out ipv4Port))
                {
                    break;
                }
            }

            Console.WriteLine("请输入IPV6监听端口");
            while ((line = Console.ReadLine()) != null)
            {
                if (int.TryParse(line, out ipv6Port))
                {
                    break;
                }
            }

            MainLobby mainLobby = new(string.Join(".", nameof(ServerProject), _localServerRoot), null, ipv4Port, ipv6Port);
        }
    }
}
