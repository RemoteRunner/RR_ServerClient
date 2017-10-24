using System;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Web;
using Android.App;
using Android.OS;
using Android.Webkit;
using RemoteRunner.Mobile.Models;
using RemoteRunner.Mobile.Views;
using RemoteRunner.Services;

namespace RemoteRunner.Mobile
{
    public static class Variables
    {
        public static string Ip { get; set; }
    }

    [Activity(Label = "Remote Runner", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private readonly SocketManager socket = new SocketManager(2048, 4199);
        private WebView webView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            webView = FindViewById<WebView>(Resource.Id.webView);
            webView.Settings.JavaScriptEnabled = true;

            // Use subclassed WebViewClient to intercept hybrid native calls
            webView.SetWebViewClient(new HybridWebViewClient(socket, webView));

            // Render the view from the type generated from RazorView.cshtml
            var model = new Model1 {Text = "Command result"};
            var template = new RazorView {Model = model};
            string page = template.GenerateString();
            // Load the rendered HTML into the view with a base URL 
            // that points to the root of the bundled Assets folder
            webView.LoadDataWithBaseURL("file:///android_asset/", page, "text/html", "UTF-8", null);
        }

        protected override void OnPause()
        {
            base.OnPause();
            socket.Dissconnect();
        }

        protected override void OnResume()
        {
            base.OnResume();
            socket.Connect(Variables.Ip);
        }

        private class HybridWebViewClient : WebViewClient
        {
            private readonly SocketManager socket;
            private readonly WebView webView;

            public HybridWebViewClient(SocketManager socket, WebView webView)
            {
                this.socket = socket;
                this.webView = webView;
                socket.ReceivedMessage += socket_ReceivedMessage;
                socket.ConnectedServer += socket_ConnectedServer;
                socket.HostLost += socket_HostLost;
                socket.HostRefused += socket_HostRefused;
            }

            public void EnterLog(string ms)
            {
                string js = $"SetLabelText('{ms}');";
                webView.LoadUrl("javascript:" + js);
            }

            private void socket_HostRefused()
            {
                EnterLog("Server has been refused");
            }

            private void socket_HostLost()
            {
                EnterLog("Server crashed!");
            }

            private void socket_ConnectedServer()
            {
                EnterLog("Successfully connected");
            }

            private void socket_ReceivedMessage(string message, TcpClient fromClient)
            {
                EnterLog(message);
            }

            [Obsolete("deprecated")]
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                // If the URL is not our own custom scheme, just let the webView load the URL as usual
                var scheme = "hybrid:";

                if (!url.StartsWith(scheme))
                    return false;

                // This handler will treat everything between the protocol and "?"
                // as the method name.  The querystring has all of the parameters.
                string[] resources = url.Substring(scheme.Length).Split('?');
                string method = resources[0];
                NameValueCollection parameters = HttpUtility.ParseQueryString(resources[1]);

                if (method == "SendCommand")
                {
                    string command = parameters["command"];
                    socket.SendMessageToHost(command);
                }
                else if (method == "ConnectCommand")
                {
                    string ip = parameters["ip"];
                    socket.Connect(ip);
                    Variables.Ip = ip;
                }

                return true;
            }
        }
    }
}