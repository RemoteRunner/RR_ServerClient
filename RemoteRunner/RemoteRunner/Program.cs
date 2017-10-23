using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using RemoteRunner.Services;
using RemoteRunner.Services.Runner;

namespace Remote_Runner
{
    internal class Program
    {
        private static readonly SocketManager Socket = new SocketManager(2048, 4199);
        private static readonly Runner runner = new Runner();
        private static int clientCount;

        private static void Setup(string exeName)
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
                                exeName, AppDomain.CurrentDomain.FriendlyName)
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

        private static void Main(string[] args)
        {
            //Setup("run.bat");
            
            Socket.ClientConnected += socket_ClientConnected;
            Socket.ClientDissconnected += socket_ClientDisconnected;
            Socket.ReceivedMessage += socket_ReceivedMessage;
            IPAddress[] localIpArray = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress ipAddress =
                localIpArray.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);

            Socket.Host();
            Socket.StartLisenClients();
            EnterLog($"Server started at {ipAddress}:4199");

            Console.WriteLine("Enter action");
            while (true)
            {
                string c = Console.ReadLine();
               EnterLog(runner.Run("{ 'command': 'SetCursorPosition', 'params': [{ 'x': '5','y': '65','z': '75' }]}"));
                Action(c);
                EnterLog("Enter action");
            }
        }

        public static void EnterLog(string ms)
        {
            Console.WriteLine(ms);
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
                case "close":
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

        private static void socket_ReceivedMessage(string Message, TcpClient FromClient)
        {
            EnterLog(Message);
            EnterLog(runner.Run(Message));
        }

        private static void socket_ClientDisconnected(TcpClient Client)
        {
            clientCount--;
            EnterLog("Client disconnected");
            EnterLog("Clients:" + clientCount);
        }

        private static void socket_ClientConnected(TcpClient Client)
        {
            clientCount++;
            EnterLog("Client connected");
            EnterLog("Clients:" + clientCount);
        }
    }
}