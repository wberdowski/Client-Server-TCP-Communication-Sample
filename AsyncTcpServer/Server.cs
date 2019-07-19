using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsyncTcpServer
{
    class Server
    {
        //Define a TCP client socket
        private static Socket _socket = new Socket(SocketType.Stream, ProtocolType.IP);
        //Define a receive buffer of size 1KB
        private static byte[] _recvBuffer = new byte[1024];
        //Define a socket endpoint at loopback address and port 54321
        private static IPEndPoint _localEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54321);
        //Define a list of all currently connected clients
        private static List<Socket> _clients = new List<Socket>();

        static void Main(string[] args)
        {
            Console.Title = "Server";

            //Tell a socket to use our local endpoint we defined earlier
            _socket.Bind(_localEp);
            //Set the maximum pending connections to 5 (i.e. connections waiting to be accepted)
            _socket.Listen(5);

            Console.WriteLine($"Listening at {_localEp.ToString()} for incomming connections ...");

            //Wait for the incomming connection asynchronously
            _socket.BeginAccept(OnAccept, null);

            while (true)
            {
                int sent = 0;
                string text = Console.ReadLine();

                if (text == "exit")
                    break;

                if (text.Length > 0)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(text);
                    //Broadcast a message to all connected clients
                    for (int i = 0; i < _clients.Count; i++)
                        sent += _clients[i].Send(bytes);
                }

                Console.WriteLine($"Sent {sent} bytes");
            }

            //Disconnect clients
            for (int i = 0; i < _clients.Count; i++)
                DisconnectClient(_clients[i]);

            //Cleanup
            _socket.Close();
            _socket.Dispose();
        }

        private static void OnAccept(IAsyncResult ar)
        {
            Socket _clientSocket = _socket.EndAccept(ar);
            Console.WriteLine($"Client {_clientSocket.RemoteEndPoint.ToString()} connected");
            //Start listening for a message from client asynchronously
            _clientSocket.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, _clientSocket);

            _clients.Add(_clientSocket);
            //Wait for the next incomming connection
            _socket.BeginAccept(OnAccept, null);
        }

        private static void OnReceive(IAsyncResult ar)
        {
            Socket _clientSocket = (Socket)ar.AsyncState;
            int _len = 0;

            try
            {
                _len = _clientSocket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
                DisconnectClient(_clientSocket);
                return;
            }

            //Check if message is empty
            if (_len == 0)
            {
                //Check if client socket is still reachable
                if (_clientSocket.Poll(1000, SelectMode.SelectWrite))
                {
                    Console.WriteLine($"Client {_clientSocket.RemoteEndPoint.ToString()} timed out");
                    DisconnectClient(_clientSocket);
                    return;
                }
            }
            else
            {
                //Decode received bytes using UTF8
                string text = Encoding.UTF8.GetString(_recvBuffer, 0, _len);
                //Print received message
                Console.WriteLine($"Received from {_clientSocket.RemoteEndPoint.ToString()}: {text}");
            }

            //Start listening for future messages from client
            _clientSocket.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, _clientSocket);
        }

        private static void DisconnectClient(Socket clientSocket)
        {
            Console.WriteLine($"Client {clientSocket.RemoteEndPoint.ToString()} disconnected");
            _clients.Remove(clientSocket);
            clientSocket.Close();
            clientSocket.Dispose();
        }
    }
}
