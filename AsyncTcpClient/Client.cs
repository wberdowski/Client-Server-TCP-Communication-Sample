using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AsyncTcpClient
{
    class Client
    {
        //Define a TCP client socket
        private static Socket _socket = new Socket(SocketType.Stream, ProtocolType.IP);
        //Define a receive buffer of size 1KB
        private static byte[] _recvBuffer = new byte[1024];
        //Define a socket endpoint at loopback address and port 54321
        private static IPEndPoint _serverEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54321);

        static void Main(string[] args)
        {
            Console.Title = "Client";
            Console.WriteLine(
                "TYPE 'exit' TO DISCONNECT AND EXIT CORRECTLY" + Environment.NewLine +
                "AFTER CONNECTING YOU CAN TYPE MESSAGES AND SEND THEM BY PRESSING 'Enter' KEY"
                );

            AttemptConnect();

            while (true)
            {
                //Read user message from console
                string text = Console.ReadLine();
                int sent = 0;

                if (text == "exit")
                    break;

                //Check if socket is connected to the server and message isn't empty
                if (_socket.Connected && text.Length > 0)
                {
                    //Encode user message using UTF8
                    byte[] encoded = Encoding.UTF8.GetBytes(text);

                    try
                    {
                        //Send encoded message to the server
                        sent = _socket.Send(encoded);
                    }
                    catch (SocketException ex)
                    {
                        //Connection with the server is lost
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }

                //Print how many bytes were sent
                Console.WriteLine($"Sent {sent} bytes");
            }

            //Cleanup
            _socket.Close();
            _socket.Dispose();
        }

        private static void AttemptConnect()
        {
            //Start connecting to the server endpoint asynchronously
            _socket.BeginConnect(_serverEp, OnConnect, null);
            Console.WriteLine($"Connecting to {_serverEp.ToString()} ...");
        }

        private static void OnConnect(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
            }
            catch (SocketException)
            {
                //Connection could not be established
                Console.WriteLine("Could not connect to the server. Repeating attempt in 10 seconds ...");
                //Wait 5 seconds
                Thread.Sleep(10000);
                AttemptConnect();
                return;
            }

            //Connection established
            Console.WriteLine($"Connected to {_serverEp.ToString()}");

            //Start waiting for the incomming messages from the server
            _socket.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, null);
        }

        private static void OnReceive(IAsyncResult ar)
        {
            int _len = 0;

            try
            {
                _len = _socket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                //Server is unreachable
                Console.WriteLine(ex.Message);
                return;
            }

            //Decode received bytes using UTF8
            string text = Encoding.UTF8.GetString(_recvBuffer, 0, _len);
            //Print received message
            Console.WriteLine($"Received from {_serverEp.ToString()}: {text}");

            //Start listening for future messages from client
            _socket.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, null);
        }
    }
}
