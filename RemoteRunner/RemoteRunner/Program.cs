using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RemoteRunner.Network;
using RemoteRunner.Network.WebService;
using RemoteRunner.Services;

namespace RemoteRunner
{
    internal class Program
    {
        private static readonly SocketManager Socket = new SocketManager(int.MaxValue, 4199);
        private static readonly Runner Runner = new Runner();
        private static int clientCount;
        private static User user;

        private static async Task Main(string[] args)
        {
            var webService = new WebService();

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
                string pass = "";
                ConsoleKeyInfo key;
                do
                {
                    key = Console.ReadKey(true);

                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        pass += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                        {
                            pass = pass.Substring(0, (pass.Length - 1));
                            Console.Write("\b \b");
                        }
                    }
                }
                while (key.Key != ConsoleKey.Enter);
                Console.WriteLine();
                user = await webService.Login(name, pass);
                Console.WriteLine(user == null ? "Incorrect username/password" : "Success");
            }

            Socket.ClientConnected += Socket_ClientConnected;
            Socket.ClientDissconnected += Socket_ClientDisconnected;
            Socket.ReceivedMessage += Socket_ReceivedMessage;
            var localIpArray = Dns.GetHostAddresses(Dns.GetHostName());
            var ipAddress =
                localIpArray.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);

            Socket.Host();
            Socket.StartLisenClients();
            EnterLog($"Server started at {ipAddress}");

            Console.WriteLine("Enter action");
            var t = new Timer(TimerCallback, webService, 0, 10 * 60 * 1000); //10 минут
            while (true)
            {
                var c = Console.ReadLine();
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

        private static async void TimerCallback(object o)
        {
            var ws = o as WebService;
            await ws.SendUserData(new UserDiskInfo {user_id = user.id}, new UserProcessInfo {user_id = user.id});
            var commands = await ws.GetUncomletedCommandsAsync(user.id);
            foreach (var message in commands)
            {
                EnterLog(message);
                var commandResultData = Runner.Run(message);
                EnterLog(commandResultData);
                dynamic stuff = JObject.Parse(message);
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