using System.Net.Sockets;

namespace RemoteRunner.Network
{
    public delegate void ReceivedM(string message, TcpClient fromClient);

    public delegate void ClientC(TcpClient client);

    public delegate void ClientD(TcpClient client);

    public delegate void HostR();

    public delegate void ConnectedS();

    public delegate void HostL();
}