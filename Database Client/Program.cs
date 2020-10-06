/// ****************************
/// FILE            : Program.cs (Client)
/// PROJECT         : RD-Assignment
/// PROGRAMMER      : Filipe Brito / Zandrin Joseph
/// FIRST VERSION   : 22/09/2020
/// LAST UPDATE     : 05/10/2020
/// ****************************
/*  *
    *                       ****************************************************************
    *   NAME            :	Program (Database Client)
    *   PURPOSE         :	This class connects to a Server through the use of sockets.
    *                       It's purpose is to add and check entries to a database.
    *                       The input format is (usually) as follows:
    *                       [command] [firstname] [lastname] [dateofbirth]
    *                       The user may type 'help' if they need to be remined of commands.
    *                       ****************************************************************
    */

///
/// ---------------------------------------
/// DISCLAIMER: This code was inspired by a
/// project by AbleOpus. Source can be found
/// in this link:
/// https://github.com/AbleOpus/NetworkingSamples
/// ---------------------------------------
/// 

using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Database_Client
{
    class Program
    {
        // Creates and configures the socket for the class
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int PORT = 12345;

        // Loop that will quit once MainLoop has finished running
        static void Main()
        {
            Console.Title = "Database Client";  // Title for the console
            Connect();                          // The starting point, makes the connection to the server
            MainLoop();                         // The logic of sending and receiving packets lays here
            Quit();                             // Closes the socket and terminates the program
        }



        private static void Connect()
        {
            int attempts = 1; // used to count the number of attempts

            while (!ClientSocket.Connected) // until the client connects, do this
            {
                try
                {
                    Console.WriteLine("Connection attempt " + attempts);
                    attempts++;
                    ClientSocket.Connect(IPAddress.Loopback, PORT);
                }
                catch (SocketException) // if an exception is caught, just clears the screen and tries again
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Connected");
        }

        private static void MainLoop()
        {
            // First info prompt
            Console.WriteLine("\n\nWrite your command (insert, update or find) followed by first name, last name and date of birth (DD/MM/YYYY).\n" +
                "Please separate each operation with a single space. Type 'quit' to quit the program.\n" +
                "E.g.: To insert an entry:\"insert John Doe 22/01/1956\"\n" +
                "to update an entry insert the ID before the name: \"update 34 John Doe 22/01/1966\"");

            while (true)        // infinite loop of sending and receiving information. stops when user types 'quit'
            {
                SendRequest();
                ReceiveResponse();
            }
        }

        
        private static void Quit()
        {
            SendString("quit");                         // Send quit command to server
            ClientSocket.Shutdown(SocketShutdown.Both); // terminates socket connection
            ClientSocket.Close();                       // basically the same
            Environment.Exit(0);                        // closes the window/program
        }

        private static void SendRequest()
        {
            string request = "";        // string to hold the commands
            while (request == "")       // loops until a command was written by the user
            {
                Console.WriteLine("Please type in your command. If you want help, type \"help\".");
                request = Console.ReadLine();
            }
            Console.Write("Sending a request: ");
            SendString(request);
            if (request.ToLower() == "quit") // if the request was a 'quit' one, calls Quit();
            {
                Quit();
            }
        }

 
        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);                  // creates a byte object and fills it with the contents of the parameter text
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);  // sends the by object
        }

        private static void ReceiveResponse()
        {
            var buffer = new byte[2048];                                    // creates a byte variable
            int received = ClientSocket.Receive(buffer, SocketFlags.None);  // stores how many bytes were received and stores the contents of it in the buffer
            if (received == 0) return;                                      // if a blank response was received
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);                   // transforms data to put into a string
            Console.WriteLine(text);                                        // displays the string
        }
    }
}