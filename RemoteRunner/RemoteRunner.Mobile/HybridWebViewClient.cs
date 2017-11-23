using System;
using System.Collections.Specialized;
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
        private readonly WebView webView;
        WebService webService = new WebService();
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
            string js = $"ShowResult(\"{ms}\");";
            webView.EvaluateJavascript(string.Format("javascript: {0}", js), null);
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

            string[] resources = url.Substring(scheme.Length).Split('?');
            string method = resources[0];
            NameValueCollection parameters = HttpUtility.ParseQueryString(resources[1]);

            switch (method)
            {
                case "SendCommand":
                    string command = parameters["command"];
                    socket.SendMessageToHost(command);
                    break;

                case "LoginCommand":
                    string user_name = parameters["user-name"];
                    string password = parameters["password"];
                    User user = null;
                    user = AsyncHelpers.RunSync(() => webService.Login(user_name, password));
                    if (user == null)
                    {
                        EnterLog("Incorrect username/password");
                    }
                    else
                    {
                        EnterLog("Success");
                        Variables.User = user;
                    }

                    break;

                case "ConnectCommand":
                    string ip = parameters["ip"];
                    socket.Connect(ip);
                    Variables.Ip = ip;
                    break;
            }

            return true;
        }
    }
}