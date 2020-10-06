/// ****************************
/// FILE            : Program.cs (Server)
/// PROJECT         : RD-Assignment
/// PROGRAMMER      : Filipe Brito / Zandrin Joseph
/// FIRST VERSION   : 22/09/2020
/// LAST UPDATE     : 06/10/2020
/// ****************************
/*  *
    *                       ****************************************************************
    *   NAME            :	Program (Database Server)
    *   PURPOSE         :	This class listens to multiple clients through the use of sockets.
    *                       It's purpose is to receive commands and administer a database.
    *                       The input format is (usually) as follows:
    *                       [command] [firstname] [lastname] [dateofbirth]
    *                       The user may type 'help' if they need to be remined of commands.
    *                       Many clients can use this server at the same time.
    *                       ****************************************************************
    */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel.Design;
using System.Threading;

namespace Database_Server
{
    class Program
    {
        // This creates the socket of the server, and the socket list of clients.
        private static readonly Socket serverSideSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSideSockets = new List<Socket>();
        private const int PORT = 12345;
        private const int BUFFER_SIZE = 1024;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];


        // Main loop to the program. When user presses enter, program closes sockets from list and terminates itself.
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
            serverSideSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));     // starts the server
            serverSideSocket.Listen(0);                                     // starts listening for clients
            serverSideSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }


        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSideSockets)    // goes through all listed sockets and closes them
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSideSocket.Close();       // closes the server's socket
        }


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

            clientSideSockets.Add(socket);      // Adds socket to list
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);     // allows data to be received
            Console.WriteLine("Client connected, waiting for request...");
            serverSideSocket.BeginAccept(AcceptCallback, null);
        }


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

            int lineNumber = 0;  // keeps track of current line number
            byte[] receivedBuffer = new byte[dataReceived]; // creates an array of byte objects to receive
            byte[] response = new byte[1024];               
            Array.Copy(buffer, receivedBuffer, dataReceived);               // copies the buffer to the byte object
            string commandLine = Encoding.ASCII.GetString(receivedBuffer);  // converts the bytes received to characters
            Console.WriteLine("Received Text: " + commandLine);             // shows what was received

            string[] commandPiece = new string[5];          // creates an array of strings to store the command divided
            commandPiece = commandLine.Split(' ');          // splits the command sent into the array[]


            string filePath = @"..\..\database.txt";            // creates the file path for the fileIO to work with
            List<string> lineList = new List<string>();         // this will store all lines of the text file

            try
            {
                lineList = File.ReadAllLines(filePath).ToList();    // store all lines in the list
            }
            catch (System.IO.FileNotFoundException)
            {
                StreamWriter sq = File.CreateText(filePath);
                sq.Close();
            }



            switch (commandPiece[0].ToLower())
            {
                case "insert":
                    Console.WriteLine("Insert command received");

                    if (commandPiece.Count() < 4)
                    {
                        Console.WriteLine("Not enough arguments provided.");
                        response = Encoding.ASCII.GetBytes("Please check your command. Type the query again.");
                        currentSocket.Send(response);
                        break;
                    }
                    if (!DateValidation(commandPiece[3]))       // in case the date is not valid, sends the response to the client 
                    {
                        Console.WriteLine("Invalid date format.");
                        response = Encoding.ASCII.GetBytes("Please check your date format. Type the query again.");
                        currentSocket.Send(response);
                    }

                    foreach (string element in lineList) // goes through the whole local list and increments the counter to get the ID it's at
                    {
                        lineNumber++;
                    }
                    lineList.Add(lineNumber + "," + commandPiece[1] + "," + commandPiece[2] + "," + commandPiece[3]);   // adds the line to the end of the list
                    File.WriteAllLines(filePath, lineList);                                                             // writes to the txt file

                    response = Encoding.ASCII.GetBytes(commandPiece[1] + " " + commandPiece[2] + " was added to the database.");    // constructs response object
                    currentSocket.Send(response);                                                                                   // sends it
                    break;

                case "update":
                    Console.WriteLine("Update command received");

                    if (commandPiece.Count() < 5)               // checks for arguments validation
                    {
                        Console.WriteLine("Not enough arguments provided.");
                        response = Encoding.ASCII.GetBytes("Please check your command. Type the query again.");
                        currentSocket.Send(response);
                        break;
                    }
                    if (!DateValidation(commandPiece[4]))       // checks if the date is valid
                    {
                        Console.WriteLine("Invalid date format.");
                        response = Encoding.ASCII.GetBytes("Please check your date format. Type the query again.");
                        currentSocket.Send(response);
                        break;
                    }

                    int lineID = Int32.Parse(commandPiece[1]);

                    try
                    {
                        lineList[lineID] = commandPiece[1] + "," + commandPiece[2] + "," + commandPiece[3] + "," + commandPiece[4]; // replaces the line chosen with the updated info
                        File.WriteAllLines(filePath, lineList);                                                                     // writes everything to file
                        response = Encoding.ASCII.GetBytes("Updated successfully.");                                                // creates response object
                        currentSocket.Send(response);                                                                               // sends it
                        break;

                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Inexistent ID.");
                        response = Encoding.ASCII.GetBytes("No such ID exists.");
                        currentSocket.Send(response);
                        break;
                    }


                case "find":
                    Console.WriteLine("Find command received");

                    if (commandPiece.Count() < 2)       // checks if commands are valid
                    {
                        Console.WriteLine("Not enough arguments provided.");
                        response = Encoding.ASCII.GetBytes("Please check your command. Type the query again.");
                        currentSocket.Send(response);
                        break;
                    }

                    lineID = Int32.Parse(commandPiece[1]);      // gets int from string as ID

                    try
                    {
                        string[] resultPiece = lineList[lineID].Split(',');
                        response = Encoding.ASCII.GetBytes("ID " + resultPiece[0] + ": " + " " + resultPiece[1] + " " + resultPiece[2] + " " + resultPiece[3]);
                        currentSocket.Send(response);
                        break;
                    }
                    catch (System.ArgumentOutOfRangeException)      // ID was out of range
                    {
                        response = Encoding.ASCII.GetBytes("No such ID exists.");
                        currentSocket.Send(response);
                        break;
                    }


                case "quit":
                    currentSocket.Shutdown(SocketShutdown.Both);            // shuts down the socket
                    currentSocket.Close();                                  // closes it
                    clientSideSockets.Remove(currentSocket);                // removes it from the socket list
                    Console.WriteLine("Client requested to disconnect.");
                    break;

                case "help":
                    Console.WriteLine("Help text was sent.");
                    response = Encoding.ASCII.GetBytes("\nYou have three operations available.\n" +
                        "These are: insert, find and update.\n\n" +
                        "The insert function includes a new name into the database and generates a unique ID for it.\n" +
                        "To use insert, you should include a first name, a last name and a date of birth after the command, all separated by single spaces.\n" +
                        "\"insert Noman Atique 20/12/1985\"\n\n" +
                        "The find function searches for a specific ID and returns the contents of it.\n" +
                        "To use find, you should include an ID number.\n\n" +
                        "\"find 7\"\n\n" +
                        "The update function changes the contents of the entry.\n" +
                        "To use update, you should include an ID number, followed by a first name, a last name and a date of birth.\n" +
                        "\"update 3 Noman Atique 20/12/1984\"\n");
                currentSocket.Send(response);
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

        static bool DateValidation(string date)
        {
            bool val = Regex.IsMatch(date, @"(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[012])/((19|20)[\d]{2})"); // validates to format DD/MM/YYYY

            return val;
        }
    }
}
