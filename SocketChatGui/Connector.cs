using SocketMessageData;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketChatGui
{
    public delegate void ConnectedEventHandler();
    public delegate void DisconnectedEventHandler();

    public class Connector
    {
        private readonly Socket _socket;
        public readonly Client Client;
        private IPAddress _address;
        private int _port;
        private string _username;

        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;

        public Connector()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Client = new Client(_socket, 1);
            Client.Disconnected += () => Disconnected();
        }

        public void SetUp(IPAddress address, int port, string username)
        {
            _address = address;
            _port = port;
            _username = username;
        }

        public void Disconnect()
        {
            Client.Disconnect();
        }

        public void Send(Message message)
        {
            Client.Send(message);
        }

        public async Task TryToConnect()
        {
            while (!_socket.Connected)
            {
                try
                {
                    await _socket.ConnectAsync(new IPEndPoint(_address, _port));

                    var message = new Message
                    {
                        Command = Command.Login,
                        Content = _username
                    };
                    Client.Send(message);

                    Connected();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                    Disconnected();
                }

                Thread.Sleep(1000);
            }

            SetupForReceiving();
        }

        private void SetupForReceiving()
        {
            Client.StartReceiving();
        }
    }
}
