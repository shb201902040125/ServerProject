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
    public class Lobby
    {
        public string UniqueID;
        public string? PassWord;
        public HashSet<ISocket> tourist = [];
        public Dictionary<string, UserInfo> users = [];
        public int IPV4Port { get; protected set; }
        public int IPV6Port { get; protected set; }
        public string LocalServerName { get; protected set; }
        protected TcpListener _ipv4Listener, _ipv6Listener;
        protected NamedPipeServerStream _localListner;
        public bool IsClosed;
        public virtual void Init()
        {
            StartIPV4Listen();
            StartIPV6Listen();
            StartLocalListen();
        }
        protected void StartIPV4Listen()
        {
            _ipv4Listener = new(IPAddress.Any, IPV4Port);
            _ipv4Listener.Start();
            new Thread(IPV4ListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", UniqueID, "IPV4ListenLoop")
            }.Start();
        }
        protected void StartIPV6Listen()
        {
            _ipv6Listener = new(IPAddress.IPv6Any, IPV6Port);
            _ipv6Listener.Start();
            new Thread(IPV6ListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", UniqueID, "IPV6ListenLoop")
            }.Start();
        }
        protected void StartLocalListen()
        {
            new Thread(LocalListenLoop)
            {
                IsBackground = true,
                Name = string.Join(".", UniqueID, "LocalListenLoop")
            }.Start();
        }
        protected virtual void IPV4ListenLoop()
        {
            while (!IsClosed)
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
        protected virtual void IPV6ListenLoop()
        {
            while (!IsClosed)
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
        protected virtual void LocalListenLoop()
        {
            while (!IsClosed)
            {
                try
                {
                    _localListner = new(string.Join(".", LocalServerName, "Listen"), PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances);
                    _localListner.WaitForConnection();
                    string pipeName = string.Join(".", LocalServerName, "Listen", Guid.NewGuid());
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
        protected virtual void AddSocket(ISocket socket)
        {
            tourist.Add(socket);
            Task.Factory.StartNew(() => SocketListenLoop(socket), TaskCreationOptions.LongRunning);
        }
        protected virtual void RemoveSocket(ISocket socket)
        {
            if (!tourist.Remove(socket))
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
            socket.Close();
        }
        protected virtual async void SocketListenLoop(ISocket socket)
        {
            socket.AsyncConnect();
            while (socket.IsConnected)
            {
                var result = await socket.AsyncRecive();
                if (result.Item1 == null)
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
                HandleMemory(result.Item1, socket);
            }
        }
        protected virtual void HandleMemory(MemoryStream memory, ISocket socket)
        {
            memory.Dispose();
        }
    }
}
