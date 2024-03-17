using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ServerProject
{
    internal class MainLobby : Lobby
    {
        enum PacketType : int
        {
            FindLobby,
            CreateLobby
        }
        public MainLobby(string uniqueID, string? passWord, int ipv4Port, int ipv6Port)
        {
            UniqueID = uniqueID;
            PassWord = passWord;
            IPV4Port = ipv4Port;
            IPV6Port = ipv6Port;
            LocalServerName = uniqueID;
            Init();
        }
        protected override void HandleMemory(MemoryStream memory, ISocket socket)
        {
            using BinaryReader reader = new(memory);
            using MemoryStream reply = new();
            using BinaryWriter writer = new(reply);
            PacketType packetType = (PacketType)reader.ReadInt32();
            switch (packetType)
            {
                case PacketType.FindLobby:
                    {
                        if (LobbyManager.TryFindLobby(reader.ReadString(), out Lobby lobby))
                        {
                            writer.Write(true);
                            writer.Write(lobby.UniqueID);
                            switch (socket.RemoteAddress.Type)
                            {
                                case RemoteAddress.AddressType.IPV4:
                                    {
                                        writer.Write(lobby.IPV4Port);
                                        break;
                                    }
                                case RemoteAddress.AddressType.IPV6:
                                    {
                                        writer.Write(lobby.IPV6Port);
                                        break;
                                    }
                                case RemoteAddress.AddressType.NamedPipeStream:
                                    {
                                        writer.Write(lobby.LocalServerName);
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            writer.Write(false);
                        }
                        break;
                    }
                case PacketType.CreateLobby:
                    {
                        string[] createParamters = reader.ReadString().Split(" ");
                        if (LobbyManager.TryCreate(createParamters, out Lobby lobby))
                        {
                            lobby.Init();
                            writer.Write(0);
                            writer.Write(lobby.UniqueID);
                            switch (socket.RemoteAddress.Type)
                            {
                                case RemoteAddress.AddressType.IPV4:
                                    {
                                        writer.Write(lobby.IPV4Port);
                                        break;
                                    }
                                case RemoteAddress.AddressType.IPV6:
                                    {
                                        writer.Write(lobby.IPV6Port);
                                        break;
                                    }
                                case RemoteAddress.AddressType.NamedPipeStream:
                                    {
                                        writer.Write(lobby.LocalServerName);
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            if (lobby is not null)
                            {
                                writer.Write(1);
                                writer.Write(lobby.UniqueID);
                                switch (socket.RemoteAddress.Type)
                                {
                                    case RemoteAddress.AddressType.IPV4:
                                        {
                                            writer.Write(lobby.IPV4Port);
                                            break;
                                        }
                                    case RemoteAddress.AddressType.IPV6:
                                        {
                                            writer.Write(lobby.IPV6Port);
                                            break;
                                        }
                                    case RemoteAddress.AddressType.NamedPipeStream:
                                        {
                                            writer.Write(lobby.LocalServerName);
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                writer.Write(2);
                            }
                        }
                        break;
                    }
            }
        }
    }
}
