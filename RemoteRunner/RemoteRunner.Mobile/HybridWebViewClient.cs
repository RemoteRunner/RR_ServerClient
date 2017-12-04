using System;
using System.Net.Sockets;
using System.Web;
using Android.Webkit;
using RemoteRunner.Network;
using RemoteRunner.Network.WebService;

namespace RemoteRunner.Mobile
{
    public class HybridWebViewClient : WebViewClient
    {
        private readonly SocketManager socket;
        private readonly WebService webService = new WebService();
        private readonly WebView webView;

        public HybridWebViewClient(WebView webView, SocketManager socket)
        {
            this.socket = socket;
            this.webView = webView;
            socket.ReceivedMessage += Socket_ReceivedMessage;
            socket.ConnectedServer += Socket_ConnectedServer;
            socket.HostLost += Socket_HostLost;
            socket.HostRefused += Socket_HostRefused;
        }

        public void EnterLog(string ms)
        {
            var js = $"ShowResult(\"{ms}\");";
            webView.EvaluateJavascript($"javascript: {js}", null);
        }

        private void Socket_HostRefused()
        {
            EnterLog("Server has been refused");
        }

        private void Socket_HostLost()
        {
            EnterLog("Server crashed!");
        }

        private void Socket_ConnectedServer()
        {
            EnterLog("Successfully connected");
        }

        private void Socket_ReceivedMessage(string message, TcpClient fromClient)
        {
            EnterLog(message);
        }

        [Obsolete("deprecated")]
        public override bool ShouldOverrideUrlLoading(WebView view, string url)
        {
            var scheme = "hybrid:";
            if (!url.StartsWith(scheme))
                return false;

            var resources = url.Substring(scheme.Length).Split('?');
            var method = resources[0];
            var parameters = HttpUtility.ParseQueryString(resources[1]);

            switch (method)
            {
                case "SendCommand":
                    var command = parameters["params"];
                    socket.SendMessageToHost(command);
                    break;

                case "LoginCommand":
                    var userName = parameters["user-name"];
                    var password = parameters["password"];
                    var user = AsyncHelpers.RunSync(() => webService.Login(userName, password));
                    if (user == null)
                    {
                        EnterLog("Incorrect username/password");
                    }
                    else
                    {
                        EnterLog("Success");
                        Variables.User = user;
                        socket.Connect(user.host);
                        Variables.Ip = user.host;
                    }

                    break;
            }

            return true;
        }
    }
}