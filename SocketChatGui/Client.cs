using Newtonsoft.Json;
using SocketMessageData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SocketChatGui
{
    public delegate void MessageReceivedEventHandler(MessageReceivedEventArgs args);

    public class MessageReceivedEventArgs : EventArgs
    {
        public Message Message { get; set; }
    }


    public class Client
    {
        public Socket Socket { get; set; }
        public int Id { get; set; }
        public string Username { get; set; }
        public event MessageReceivedEventHandler MessageReceived;
        public event DisconnectedEventHandler Disconnected;

        private byte[] _buffer;

        public Client(Socket socket, int id)
        {
            Socket = socket;
            Id = id;
        }

        public void Disconnect()
        {
            var message = new Message
            {
                Command = Command.Logout
            };
            Send(message);
            Socket.Disconnect(true);
            Disconnected();
        }

        #region Receiving
        public void StartReceiving()
        {
            try
            {
                _buffer = new byte[4];
                Socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (Socket.EndReceive(ar) > 1)
                {
                    _buffer = new byte[BitConverter.ToInt32(_buffer, 0)];
                    Socket.Receive(_buffer, _buffer.Length, SocketFlags.None);

                    var data = (Message)FromByteArray(_buffer);

                    switch (data.Command)
                    {
                        case Command.Message:
                            Console.WriteLine($"Received: '{data.Content}'\n\tNum of files: {data.Files.Length}");
                            
                            MessageReceived(new MessageReceivedEventArgs { Message = data });
                            break;
                        case Command.List:
                            var list = JsonConvert.DeserializeObject<List<User>>(data.Content);
                            Console.WriteLine("Received user list");
                            foreach (var user in list)
                            {
                                Console.WriteLine($"\t{user.Id} - {user.Username} - {user.JoinTime.ToLocalTime()}");
                            }
                            break;
                    }

                    StartReceiving();
                }
                else
                {
                    Disconnect();
                }
            }
            catch (Exception ex)
            {
                if (ex is SocketException sx)
                {
                    if (sx.ErrorCode == 10054)
                    {
                        Console.WriteLine("Server closed connection unexpectedly");
                        Disconnected();
                    }
                }
                else
                {
                    Console.WriteLine($"Exception: {ex}");
                    if (!Socket.Connected)
                    {
                        Disconnect();
                    }
                    else
                    {
                        StartReceiving();
                    }
                }
            }
        }
        #endregion

        #region Sending
        public void Send(Message message)
        {
            try
            {
                var data = ToByteArray(message);
                var fullPacket = new List<byte>();
                fullPacket.AddRange(BitConverter.GetBytes(data.Length));
                fullPacket.AddRange(data);

                Socket.Send(fullPacket.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        #endregion

        #region Convertions
        private static byte[] ToByteArray(object source)
        {
            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, source);
            return stream.ToArray();
        }

        private static object FromByteArray(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            stream.Write(bytes);
            stream.Position = 0;
            return formatter.Deserialize(stream);
        }
        #endregion

        public struct User
        {
            public int Id;
            public string Username;
            public DateTime JoinTime;
        }
    }
}
