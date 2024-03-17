using System;
using System.Buffers;
using System.ComponentModel;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ServerProject
{
    public abstract class ISocket
    {
        public abstract RemoteAddress RemoteAddress { get; }
        public abstract bool IsConnected { get; }
        public abstract void AsyncConnect();
        public abstract void AsyncSend(byte[] data);
        public abstract Task<Tuple<MemoryStream?, object?>> AsyncRecive();
        public abstract void Close();
    }
    public class TCPSocket : ISocket
    {
        private TcpClient _connection;
        private TCPAddress _address;
        public TCPSocket(TcpClient client)
        {
            _connection = client;
            _connection.NoDelay = true;
            _address = client.Client.RemoteEndPoint is IPEndPoint iPEndPoint
                ? new TCPAddress(iPEndPoint.Address, iPEndPoint.Port)
                : throw new ArgumentException("In theory tcpClient addresses should not run here.");
        }
        public override RemoteAddress RemoteAddress => _address;
        public override bool IsConnected => _connection.Connected;
        public override void Close()
        {
            _connection.Close();
        }
        public override async void AsyncConnect()
        {
            if(IsConnected)
            {
                return;
            }
            await _connection.ConnectAsync(_address._address, _address._port);
        }
        public override void AsyncSend(byte[] data)
        {
            byte[] lenArray = ArrayPool<byte>.Shared.Rent(4);
            int len = data.Length;
            lenArray[0] = (byte)(len & 0b_1111_0000_0000_0000);
            lenArray[1] = (byte)(len & 0b_0000_1111_0000_0000);
            lenArray[2] = (byte)(len & 0b_0000_0000_1111_0000);
            lenArray[3] = (byte)(len & 0b_0000_0000_0000_1111);
            _connection.GetStream().Write(lenArray);
            ArrayPool<byte>.Shared.Return(lenArray);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            _connection.GetStream().BeginWrite(buffer, 0, buffer.Length,
                res =>
                {
                    (res.AsyncState as Action)?.Invoke();
                },
                () =>
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                });
        }
        public override async Task<Tuple<MemoryStream?,object?>> AsyncRecive()
        {
            try
            {
                if (!IsConnected)
                {
                    return Tuple.Create<MemoryStream?, object?>(null, "Socket Closed");
                }
                var stream = _connection.GetStream();
                byte[] array = ArrayPool<byte>.Shared.Rent(4);
                stream.Read(array);
                int length = BitConverter.ToInt32(array);
                ArrayPool<byte>.Shared.Return(array);
                using MemoryStream memory = new();
                using BinaryWriter writer = new(memory);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
                int len = 0, read = 0;
                while (len < length)
                {
                    read = await stream.ReadAsync(buffer, 0, length);
                    writer.Write(buffer, 0, read);
                    len += read;
                }
                ArrayPool<byte>.Shared.Return(buffer);
                return Tuple.Create<MemoryStream?, object?>(memory, "Safe Arrival");
            }
            catch (Exception ex)
            {
                return Tuple.Create<MemoryStream?, object?>(null, ex.ToString());
            }
        }
    }
    public class LocalSocket : ISocket
    {
        private NamedPipeServerStream _connection;
        private LocalAddress _address;
        public LocalSocket(string pipeName)
        {
            _connection = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
            _address = new LocalAddress(pipeName);
        }
        public override RemoteAddress RemoteAddress => _address;
        public override bool IsConnected => _connection.IsConnected;
        public override async void AsyncConnect()
        {
            if(IsConnected)
            {
                return;
            }
            await _connection.WaitForConnectionAsync();
        }
        public override async Task<Tuple<MemoryStream?, object?>> AsyncRecive()
        {
            try
            {
                if(!IsConnected)
                {
                    return Tuple.Create<MemoryStream?,object?>(null, "Socket Closed");
                }
                byte[] array = ArrayPool<byte>.Shared.Rent(4);
                _connection.Read(array);
                int length = BitConverter.ToInt32(array);
                _connection.Read(array);
                int check = BitConverter.ToInt32(array);
                ArrayPool<byte>.Shared.Return(array);
                using MemoryStream memory = new();
                using BinaryWriter writer = new(memory);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
                int len = 0, read = 0;
                while (len < length)
                {
                    read = await _connection.ReadAsync(buffer, 0, length);
                    writer.Write(buffer, 0, read);
                    len += read;
                }
                var crc = BitConverter.ToInt32(Utils.CalculateCRC(memory.ToArray()));
                if (crc != check)
                {
                    return Tuple.Create<MemoryStream?, object?>(null, "CRC Check Error");
                }
                ArrayPool<byte>.Shared.Return(buffer);
                return Tuple.Create<MemoryStream?, object?>(memory, "Safe Arrival");
            }
            catch(Exception ex)
            {
                return Tuple.Create<MemoryStream?, object?>(null, ex.ToString());
            }
        }
        public override void AsyncSend(byte[] data)
        {
            byte[] lenArray = ArrayPool<byte>.Shared.Rent(4);
            int len = data.Length;
            lenArray[0] = (byte)(len & 0b_1111_0000_0000_0000);
            lenArray[1] = (byte)(len & 0b_0000_1111_0000_0000);
            lenArray[2] = (byte)(len & 0b_0000_0000_1111_0000);
            lenArray[3] = (byte)(len & 0b_0000_0000_0000_1111);
            _connection.Write(lenArray);
            ArrayPool<byte>.Shared.Return(lenArray);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            var crc = Utils.CalculateCRC(buffer);
            _connection.Write(crc);
            _connection.BeginWrite(buffer, 0, buffer.Length,
                res =>
                {
                    (res.AsyncState as Action)?.Invoke();
                },
                () =>
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                });
        }
        public override void Close()
        {
            _connection.Close();
        }
    }
}
