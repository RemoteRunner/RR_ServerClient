using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using RemoteRunner.Services;
using RemoteRunner.Services.Runner;
using RemoteRunner.Services.WebService;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace RemoteRunner
{
    internal class Program
    {
        private static readonly SocketManager Socket = new SocketManager(2048, 4199);
        private static readonly Runner Runner = new Runner();
        private static int clientCount;
        private static User user = null;

        private static async Task Main(string[] args)
        {
            WebService webService = new WebService();

            #region "register"
            //var regUser = new User()
            //{
            //    host = "192.168.124.56",
            //    password = "123456",
            //    user_name = "Vladislav",
            //    notifications = true,
            //    widgets = new List<string>(),
            //    port = 5234,
            //    role = Role.admin
            //};
            //var result = await webService.Register(regUser);
            //if (!result)
            //{
            //    Console.WriteLine("User creation was not successful :(");
            //}
            //else
            //{
            //    Console.WriteLine("User creation was successful");
            //}
            #endregion
            Console.WriteLine("Login");
            while (user == null)
            {
                Console.WriteLine("Enter username");
                var name = Console.ReadLine();
                Console.WriteLine("Enter password");
                var password = Console.ReadLine();
                user = await webService.Login(name, password);
                if (user == null)
                {
                    Console.WriteLine("Incorrect username/password");
                }
                else
                {
                    Console.WriteLine("Success");
                }
            }

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
            Timer t = new Timer(TimerCallback, webService, 0, 600000);
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

        private static async void TimerCallback(Object o)
        {
            var ws = o as WebService;
            var commands = await ws.GetUncomletedCommandsAsync(user.id);
            foreach (var message in commands)
            {
                EnterLog(message);
                dynamic stuff = JObject.Parse(message);
                var commandResultData = Runner.Run(message);
                EnterLog(commandResultData);
                var commandResult = new CommandResult
                {
                    data = commandResultData,
                    status = true,
                    record_id = Convert.ToInt32(stuff.record_id.ToString())
                };

                await ws.SendCommandResult(commandResult);
            }

            GC.Collect();
        }
    }
}