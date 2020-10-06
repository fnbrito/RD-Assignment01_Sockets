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
using System.IO;
using System.Linq;

namespace MultiServer
{
    class Program
    {
        private static readonly Socket serverSideSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSideSockets = new List<Socket>();
        private const int PORT = 12345;
        private const int BUFFER_SIZE = 1024;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        public static int counter;
        public static int idNumber;
        public static int lineNumber;

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
            string filePath = @"C:\Users\Acer\Desktop\data.txt";

            StreamReader cou = new StreamReader(@"C:\Users\Acer\Desktop\data.txt");
            string line2 = cou.ReadLine();
            Program.idNumber = 0;
            while (line2 != null)
            {
               
                line2 = cou.ReadLine();
                Program.idNumber++;
            }
            cou.Close();

            switch (commands[0].ToLower())
            {
                case "insert":
                    
                    List<string> lines = new List<string>();
                    lines = File.ReadAllLines(filePath).ToList();
                    lines.Add(Program.idNumber + "," + commands[1] + "," + commands[2] + "," + commands[3]);
                    Program.idNumber++;
                    File.WriteAllLines(filePath, lines);
                    Console.WriteLine("Insert command received");
                    response = Encoding.ASCII.GetBytes("\nInsert successful.");
                    currentSocket.Send(response);
                    break;
                case "update":
                    
                    lineChanger(Program.lineNumber + "," +commands[1] + "," + commands[2] + "," + commands[3], @"C:\Users\Acer\Desktop\data.txt", Program.lineNumber + 1);
                    
                    Console.WriteLine("Update command received");
                    response = Encoding.ASCII.GetBytes("\nUpdate successful.");
                    currentSocket.Send(response);
                    break;
                case "find":
                    StreamReader sr = new StreamReader(@"C:\Users\Acer\Desktop\data.txt");
                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        if (line.Contains(commands[1]))
                        {
                            Program.lineNumber = Program.counter;
                            Program.counter = 0;
                            Console.WriteLine(line);
                            response = Encoding.ASCII.GetBytes(line);
                            currentSocket.Send(response);
                            break;
                        }
                        line = sr.ReadLine();
                        Program.counter++;
                    }
                    sr.Close();
                    Console.WriteLine("Find command received");
                    response = Encoding.ASCII.GetBytes("\nFind successful.");
                    //currentSocket.Send(response);
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
        static void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }
    }
}
