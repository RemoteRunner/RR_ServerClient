using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Web;
using Android.App;
using Android.Webkit;
using Java.Lang;
using Newtonsoft.Json.Linq;
using RemoteRunner.Network;
using RemoteRunner.Network.WebService;

namespace RemoteRunner.Mobile
{
    public class HybridWebViewClient : WebViewClient
    {
        private readonly SocketManager socket;
        private readonly Activity _activity;
        private readonly WebService webService = new WebService();
        private readonly WebView webView;

        public HybridWebViewClient(WebView webView, SocketManager socket, Activity activity)
        {
            this.socket = socket;
            _activity = activity;
            this.webView = webView;
            socket.ReceivedMessage += Socket_ReceivedMessage;
            socket.ConnectedServer += Socket_ConnectedServer;
            socket.HostLost += Socket_HostLost;
            socket.HostRefused += Socket_HostRefused;
        }

        public void EnterLog(string ms)
        {
            _activity.RunOnUiThread(() =>
            {
                var js = $"ShowResult(\"{ms}\");";
                webView.EvaluateJavascript($"javascript: {js}", null);
            });
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
                    dynamic stuff = JObject.Parse(parameters["params"]);
                    JObject a = JObject.Parse(stuff.@params.ToString());
                    var @params = a.Children().Cast<object>().ToArray();
                    IDictionary<string, string> paramsDictionary = new Dictionary<string, string>();
                    foreach (JProperty param in @params)
                        paramsDictionary.Add(param.Name, param.Value.ToString());
                    var userName = paramsDictionary["user_name"];
                    var password = paramsDictionary["password"];
                    var user = AsyncHelpers.RunSync(() => webService.Login(userName, password));
                    if (user == null)
                    {
                        EnterLog("Incorrect username/password");
                    }
                    else
                    {
                        EnterLog("Success");
                        Variables.User = user;
                        if (!socket.Connect(user.host,user.port)) EnterLog("Connection timeout error");
                    }

                    break;
            }

            return true;
        }
    }
}