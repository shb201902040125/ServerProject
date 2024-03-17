using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ServerProject
{
    internal class Lobby
    {
        public string UniqueID;
        public string? PassWord;
        public HashSet<ISocket> tourist = [];
        public Dictionary<string, UserInfo> users = [];
        TcpListener _ipv4Listener, _ipv6Listener;
        NamedPipeServerStream _localListner;
        bool closed;
        public Lobby(string uniqueID, string? passWord, int ipv4Port, int ipv6Port, string pipeNameRoot)
        {
            UniqueID = uniqueID;
            PassWord = passWord;

            _ipv4Listener = new(IPAddress.Any, ipv4Port);
            _ipv4Listener.Start();
            new Thread(IPV4ListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", uniqueID, "IPV4ListenLoop")
            }.Start();
            _ipv6Listener = new(IPAddress.IPv6Any, ipv6Port);
            _ipv6Listener.Start();

            new Thread(IPV6ListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", uniqueID, "IPV6ListenLoop")
            }.Start();

            new Thread(LocalListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", uniqueID, "LocalListenLoop")
            }.Start();
        }
        public static bool TryCreate(out Lobby? lobby,string? uniqueID = null, string? passWord = null)
        {
            uniqueID ??= Guid.NewGuid().ToString();
            if(Utils.TryGetAvailablePortIPv4(49152,65535,out int ipv4Port)&&Utils.TryGetAvailablePortIPv6(49152, 65535, out int ipv6Port))
            {
                lobby = new(uniqueID, passWord, ipv4Port, ipv6Port, Program._localServerRoot);
                return true;
            }
            lobby = null;
            return false;
        }
        void IPV4ListenLoop()
        {
            while (!closed)
            {
                try
                {
                    AddSocket(new TCPSocket(_ipv4Listener.AcceptTcpClient()));
                }
                catch (Exception)
                {
                }
            }
            _ipv4Listener.Stop();
        }
        void IPV6ListenLoop()
        {
            while (!closed)
            {
                try
                {
                    AddSocket(new TCPSocket(_ipv6Listener.AcceptTcpClient()));
                }
                catch (Exception)
                {
                }
            }
            _ipv6Listener.Stop();
        }
        void LocalListenLoop()
        {
            while (!closed)
            {
                try
                {
                    _localListner = new(string.Join(".", Program._localServerRoot, UniqueID, "Listen"), PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances);
                    _localListner.WaitForConnection();
                    string pipeName = string.Join(".", Program._localServerRoot, UniqueID, "Listen", Guid.NewGuid());
                    _localListner.Write(Encoding.ASCII.GetBytes(pipeName));
                    AddSocket(new LocalSocket(pipeName));
                    _localListner.Close();
                }
                catch (Exception)
                {
                }
            }
            _localListner.Close();
        }
        private void AddSocket(ISocket socket)
        {
            tourist.Add(socket);
            Task.Factory.StartNew(() => SocketListenLoop(socket), TaskCreationOptions.LongRunning);
        }
        private void RemoveSocket(ISocket socket)
        {
            if(!tourist.Remove(socket))
            {
                string? removeKey = null;
                foreach (var pair in users)
                {
                    if (pair.Value.Socket == socket)
                    {
                        removeKey = pair.Key;
                    }
                }
                if (removeKey != null)
                {
                    users.Remove(removeKey);
                }
            }
        }
        private async void SocketListenLoop(ISocket socket)
        {
            socket.AsyncConnect();
            while (socket.IsConnected)
            {
                var result = await socket.AsyncRecive();
                if(result.Item1 == null)
                {
                    if (!"CRC Check Error".Equals(result.Item2))
                    {
                        RemoveSocket(socket);
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
                HandleMemory(result.Item1);
            }
        }
        private void HandleMemory(MemoryStream memory)
        {
            //TODO
        }
    }
}
