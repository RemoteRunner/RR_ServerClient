using Android.App;
using Android.OS;
using Android.Webkit;
using RemoteRunner.Network;

namespace RemoteRunner.Mobile
{
    [Activity(Label = "@string/ApplicationName", MainLauncher = true,
        Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
    public class MainActivity : Activity
    {
        private readonly SocketManager socket = new SocketManager(2048);
        private WebView webView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            webView = FindViewById<WebView>(Resource.Id.webView);
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.DomStorageEnabled = true;
            webView.SetWebViewClient(new HybridWebViewClient(webView, socket, this));
            webView.SetWebChromeClient(new WebChromeClient());

            webView.LoadUrl("file:///android_asset/index.html");
        }

        protected override void OnPause()
        {
            base.OnPause();
            socket.Dissconnect();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (Variables.User != null)
                socket.Connect(Variables.User.host, Variables.User.port);
        }

        public override void OnBackPressed()
        {
            var alert = new AlertDialog.Builder(this);
            alert.SetTitle("Remote Runner");
            alert.SetMessage("Do you want to exit the application?");
            alert.SetPositiveButton("Yes", (sender, args) => { FinishAndRemoveTask(); });
            alert.SetNegativeButton("No", (sender, args) => { });

            Dialog dialog = alert.Create();
            dialog.Show();
        }
    }
}