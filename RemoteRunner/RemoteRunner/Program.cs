using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
        private static readonly SocketManager Socket = new SocketManager(2048);
        private static readonly Runner Runner = new Runner();
        private static int clientCount;
        private static User user;

        private static void RegisterFireWall(string programPath)
        {
            var deleteRule = new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = "cmd.exe",
                Arguments = "/K " + string.Format("netsh advfirewall firewall delete rule name=\"{0}\"",
                                AppDomain.CurrentDomain.FriendlyName)
            };
            using (var proc = new Process())
            {
                proc.StartInfo = deleteRule;
                proc.Start();
                proc.WaitForExit(1000);
                //string output = proc.StandardOutput.ReadToEnd();

                //if (string.IsNullOrEmpty(output))
                //    output = proc.StandardError.ReadToEnd();
                //return output;
            }
            var procStartInfo = new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = "cmd.exe",
                Arguments = "/K " + string.Format(
                                "netsh advfirewall firewall add rule dir=in program=\"{0}\" name=\"{1}\" action=allow",
                                programPath, AppDomain.CurrentDomain.FriendlyName)
            };
            using (var proc = new Process())
            {
                proc.StartInfo = procStartInfo;
                proc.Start();
                proc.WaitForExit(1000);
                //string output = proc.StandardOutput.ReadToEnd();

                //if (string.IsNullOrEmpty(output))
                //    output = proc.StandardError.ReadToEnd();
                //return output;
            }
        }
        private static async Task Register(WebService webService)
        {
            var regUser = new User
            {
                host = "192.168.83.1",
                password = "1",
                user_name = "1",
                notifications = true,
                widgets = new List<string>(),
                port = 4199,
                role = Role.user
            };
            var result = await webService.Register(regUser);
            Console.WriteLine(!result ? "User creation was not successful :(" : "User creation was successful");
        }

        private static async Task Main()
        {
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                RegisterFireWall(Environment.GetCommandLineArgs()[0]);
                Process.Start(Environment.GetCommandLineArgs()[0]);
                Process.GetCurrentProcess().Kill();
            }

            var webService = new WebService();
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
            foreach (var ip in localIpArray.Where(address => address.AddressFamily == AddressFamily.InterNetwork).ToList())
            {
                Console.WriteLine(ip);
            }

            Console.WriteLine("Enter ipAdress");
            Socket.Ip = Console.ReadLine();
            Console.WriteLine("Enter port");
            Socket.Port = int.Parse(Console.ReadLine());
            Socket.Host();
            Socket.StartLisenClients();
            await webService.SendHostInfo(new HostInfo { host = Socket.Ip, port = Socket.Port, user_id = user.id });
            EnterLog($"Server started at {Socket.Ip}:{Socket.Port}");

            Console.WriteLine("Enter action");
            var t = new Timer(TimerCallback, webService, 0, 60 * 1000); //1 минута
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
            Console.WriteLine("ping web site");
            var webService = o as WebService;
            await webService.SendUserData(new UserDiskInfo { user_id = user.id }, new UserProcessInfo { user_id = user.id });
            var commands = await webService.GetUncomletedCommandsAsync(user.id);
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

                await webService.SendCommandResult(commandResult);
            }

            GC.Collect();
        }
    }
}