using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RemoteRunner.Network
{
    public class SocketManager
    {
        private readonly int messageMaxLength;
        private TcpClient[] clients = new TcpClient[0];

        //Host
        private Thread[] clientThreads = new Thread[0];

        public bool Connected;

        private bool createdServer;

        //Client
        private TcpClient hostClient = new TcpClient();

        private int id;
        public string Ip;

        public bool IsHost;
        private Thread listener;
        private Thread listenHost;
        public bool ListeningClients;

        public int Port;
        private TcpListener tcpListener;

        public SocketManager(int maxLengthOfMessages, int port)
        {
            messageMaxLength = maxLengthOfMessages;
            Port = port;
        }

        public event ReceivedM ReceivedMessage;
        public event ClientC ClientConnected;
        public event ClientD ClientDissconnected;
        public event HostR HostRefused;
        public event ConnectedS ConnectedServer;
        public event HostL HostLost;

        public void Host()
        {
            if (Connected)
                return;
            if (createdServer)
                throw new ArgumentException("You are already hosted, you have to restart to host again", "original");
            try
            {
                tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
                listener = new Thread(StartListenClients);
                listener.Start();
                IsHost = true;
                Connected = true;
                createdServer = true;
            }
            catch
            {
                listener.Abort();
                IsHost = false;
                Connected = false;
            }
        }

        public void StartLisenClients()
        {
            if (!IsHost) return;
            if (ListeningClients)
                return;
            tcpListener.Start();
            ListeningClients = true;
        }

        public void StopLisenClients()
        {
            if (IsHost)
            {
                if (!ListeningClients)
                    return;
                tcpListener.Stop();
                ListeningClients = false;
            }
        }

        public void Connect(string ip)
        {
            if (Connected)
                return;
            Ip = ip;
            try
            {
                IsHost = false;
                hostClient.Connect(ip, Port);
                listenHost = new Thread(StartListenHost);
                listenHost.Start();
                Connected = true;
                ConnectedServer?.Invoke();
            }
            catch (Exception e)
            {
                var m = e.Message;
                if (m.Contains("target machine actively refused"))
                    HostRefused?.Invoke();
                IsHost = false;
                Connected = false;
            }
        }

        private void ReceivedMessagex(string m, TcpClient c)
        {
            ReceivedMessage?.Invoke(m, c);
        }

        public void SendMessageToHost(string m)
        {
            if (!Connected)
                return;
            if (IsHost)
                return;
            SendData(hostClient, m, 0);
        }

        public void SendMessageToHostAndAllClients(string m)
        {
            if (!Connected)
                return;
            if (IsHost)
                return;
            SendData(hostClient, m, 1);
        }

        public void SendMessageToAllClients(string message)
        {
            if (!Connected)
                return;
            if (!IsHost)
                return;

            foreach (var t in clients)
                if (t.Connected)
                    try
                    {
                        SendData(t, message, 0);
                    }
                    catch
                    {
                        // ignored
                    }
        }

        private void SendMessageToAllClientsExpectClient(string message, TcpClient cl)
        {
            if (!Connected)
                return;
            if (!IsHost)
                return;

            foreach (var t in clients)
                if (t != cl)
                    if (t.Connected)
                        try
                        {
                            SendData(t, message, 0);
                        }
                        catch
                        {
                            // ignored
                        }
        }

        public void SendMessageToAClient(string message, TcpClient c)
        {
            SendData(c, message, 0);
        }


        private void StartListenClients()
        {
            while (true)
                try
                {
                    try
                    {
                        tcpListener.Start();
                    }
                    catch (Exception e)
                    {
                    }
                    var c = tcpListener.AcceptTcpClient();
                    var clientThread = new Thread(() => HandleClient(id, c));
                    ClientConnected?.Invoke(c);

                    clientThreads = AddnewThreadToArray(clientThread, clientThreads);
                    clients = AddnewTcpClientToArray(c, clients);
                    clientThread.Start();
                    id++;
                }
                catch
                {
                    // ignored
                }
        }

        private void StartListenHost()
        {
            while (true)
                try
                {
                    var data = new byte[messageMaxLength];
                    hostClient.GetStream().Read(data, 0, data.Length);
                    data = ClearByteNulls(data);
                    if (data.Length == 0)
                    {
                        HostLost?.Invoke();
                        hostClient.Client.Disconnect(false);
                        Connected = false;
                        hostClient = new TcpClient();
                        break;
                    }
                    GettedMessageFromHost(GetString(data), hostClient);
                }
                catch (Exception e)
                {
                    if (e.Message ==
                        "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."
                    )
                    {
                        HostLost?.Invoke();
                        hostClient.Client.Disconnect(false);
                        Connected = false;
                        hostClient = new TcpClient();
                        break;
                    }
                }
        }

        public void Stop()
        {
            if (!IsHost)
                return;

            foreach (var t in clientThreads)
                try
                {
                    t.Abort();
                }
                catch
                {
                    // ignored
                }
            clientThreads = new Thread[0];
            foreach (var t in clients)
                try
                {
                    t.GetStream().Close();
                    t.Close();
                }
                catch
                {
                    // ignored
                }
            clients = new TcpClient[0];
            tcpListener.Stop();
            try
            {
                listener.Abort();
            }
            catch (Exception)
            {
                // ignored
            }
            listener = null;
            Connected = false;
        }

        public void Dissconnect()
        {
            if (!Connected)
                return;
            try
            {
                if (IsHost)
                    return;
                try
                {
                    listenHost.Abort();
                }
                catch
                {
                    // ignored
                }
                hostClient.GetStream().Close();
                try
                {
                    hostClient.Client.Disconnect(false);
                }
                catch
                {
                    // ignored
                }
                hostClient.Close();
                Connected = false;
                hostClient = new TcpClient();
            }
            catch
            {
                // ignored
            }
        }

        private void HandleClient(int clientId, TcpClient c)
        {
            while (true)
                try
                {
                    var data = new byte[messageMaxLength];
                    c.GetStream().Read(data, 0, data.Length);
                    data = ClearByteNulls(data);
                    if (data.Length == 0)
                    {
                        clientThreads[clientId].Abort();
                        break;
                    }
                    GettedMessageFromClient(GetString(data), c);
                }
                catch
                {
                    ClientDissconnected?.Invoke(c);

                    return;
                }
        }


        #region Functions

        private static Thread[] AddnewThreadToArray(Thread add, Thread[] array)
        {
            var c = new Thread[array.Length + 1];
            Array.Copy(array, c, array.Length);
            c[c.Length - 1] = add;
            return c;
        }

        private static TcpClient[] AddnewTcpClientToArray(TcpClient add, TcpClient[] array)
        {
            var c = new TcpClient[array.Length + 1];
            Array.Copy(array, c, array.Length);
            c[c.Length - 1] = add;
            return c;
        }

        private static byte[] ClearByteNulls(byte[] data)
        {
            return data.Where(t => t != 0).ToArray();
        }

        private void GettedMessageFromClient(string p, TcpClient c)
        {
            var cc = p.Substring(0, 1);
            var re = p.Substring(1, p.Length - 1);
            ReceivedMessagex(re, c);
            if (cc == "1")
                SendMessageToAllClientsExpectClient(re, c);
        }

        private void GettedMessageFromHost(string p, TcpClient c)
        {
            var re = p.Substring(1, p.Length - 1);
            ReceivedMessagex(re, c);
        }

        private static byte[] GetBytes(string str)
        {
            var toBytes = Encoding.ASCII.GetBytes(str);
            return toBytes;
        }

        private string GetString(byte[] bytes)
        {
            var something = Encoding.ASCII.GetString(bytes);
            return something;
        }

        private static void SendData(TcpClient c, string m, int statu)
        {
            try
            {
                var datas = GetBytes(statu + m);
                c.GetStream().Write(datas, 0, datas.Length);
            }
            catch
            {
                // ignored
            }
        }

        #endregion
    }
}