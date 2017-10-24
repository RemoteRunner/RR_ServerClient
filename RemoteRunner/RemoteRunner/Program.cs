using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using RemoteRunner.Services;
using RemoteRunner.Services.Runner;

namespace RemoteRunner
{
    internal class Program
    {
        private static readonly SocketManager Socket = new SocketManager(2048, 4199);
        private static readonly Runner Runner = new Runner();
        private static int clientCount;

        private static void Main(string[] args)
        {
            Socket.ClientConnected += Socket_ClientConnected;
            Socket.ClientDissconnected += Socket_ClientDisconnected;
            Socket.ReceivedMessage += Socket_ReceivedMessage;
            IPAddress[] localIpArray = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress ipAddress =
                localIpArray.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);

            Socket.Host();
            Socket.StartLisenClients();
            EnterLog($"Server started at {ipAddress}");

            Console.WriteLine("Enter action");
            while (true)
            {
                string c = Console.ReadLine();
                Action(c);
                EnterLog("Enter action");
            }
        }

        public static void EnterLog(string ms)
        {
            Console.WriteLine(ms);
            Socket.SendMessageToAllClients(ms);
        }

        private static void Action(string ms)
        {
            switch (ms)
            {
                case "StartListen":
                    Socket.StartLisenClients();
                    break;
                case "StopListen":
                    Socket.StopLisenClients();
                    break;
                case "Close":
                    try
                    {
                        Socket.StopLisenClients();
                    }
                    finally
                    {
                        try
                        {
                            Socket.Stop();
                        }
                        finally
                        {
                            Environment.Exit(0);
                        }
                    }
                    break;
                default:
                    EnterLog("No such command");
                    break;
            }
        }

        private static void Socket_ReceivedMessage(string message, TcpClient fromClient)
        {
            EnterLog(message);
            EnterLog(Runner.Run(message));
        }

        private static void Socket_ClientDisconnected(TcpClient client)
        {
            clientCount--;
            EnterLog("Client disconnected");
            EnterLog("Clients:" + clientCount);
        }

        private static void Socket_ClientConnected(TcpClient client)
        {
            clientCount++;
            EnterLog("Client connected");
            EnterLog("Clients:" + clientCount);
        }
    }
}