///
/// 
/// 
///         PUT HEADER COMMENTS HERE
/// 
/// 
/// 
/// 
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;


namespace MultiServer
{
    class Program
    {
        private static readonly Socket serverSideSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSideSockets = new List<Socket>();
        private const int PORT = 12345;
        private const int BUFFER_SIZE = 1024;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        static void Main()
        {
            Console.Title = "Database Server";
            StartServer();
            Console.WriteLine("To close the server press ENTER.");
            Console.ReadLine(); // prompt input to close the sockets and the server
            CloseAllSockets();
        }

        private static void StartServer()
        {
            Console.WriteLine("Setting up server...");
            serverSideSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSideSocket.Listen(0);
            serverSideSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }

        /// <summary>
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        /// </summary>
        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSideSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSideSocket.Close();
        }

        /// <summary>
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        /// </summary>
        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSideSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            clientSideSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSideSocket.BeginAccept(AcceptCallback, null);
        }


        /// <summary>
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        /// </summary>
        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket currentSocket = (Socket)AR.AsyncState;
            int dataReceived = BUFFER_SIZE;

            try
            {
                dataReceived = currentSocket.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                currentSocket.Close();
                clientSideSockets.Remove(currentSocket);
                return;
            }
            catch (System.ObjectDisposedException)
            {
                Console.WriteLine("Client forcefully disconnected");
            }


            byte[] receivedBuffer = new byte[dataReceived];
            byte[] response = new byte[1024];
            Array.Copy(buffer, receivedBuffer, dataReceived);
            string commandLine = Encoding.ASCII.GetString(receivedBuffer);
            Console.WriteLine("Received Text: " + commandLine);
            string[] commands = new string[5];
            commands = commandLine.Split(' ');

            switch (commands[0].ToLower())
            {
                case "insert":
                    Console.WriteLine("Insert command received");
                    response = Encoding.ASCII.GetBytes("Insert successful.");
                    currentSocket.Send(response);
                    break;
                case "update":
                    Console.WriteLine("Update command received");
                    response = Encoding.ASCII.GetBytes("Update successful.");
                    currentSocket.Send(response);
                    break;
                case "find":
                    Console.WriteLine("Find command received");
                    response = Encoding.ASCII.GetBytes("Find successful.");
                    currentSocket.Send(response);
                    break;
                case "quit":
                    currentSocket.Shutdown(SocketShutdown.Both);
                    currentSocket.Close();
                    clientSideSockets.Remove(currentSocket);
                    Console.WriteLine("Client requested to disconnect.");
                    break;
                default:
                    Console.WriteLine("Received an invalid request");
                    byte[] data = Encoding.ASCII.GetBytes("Invalid request. Command should be: INSERT, UPDATE OR FIND.");
                    currentSocket.Send(data);
                    Console.WriteLine("Warning Sent");
                    break;
            }

            try
            {
                currentSocket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentSocket);
            }
            catch (System.ObjectDisposedException)
            {
                // I don't really know WHY this happens, but when I quit from the client it throws an exception.
            }

        }
    }
}