using Android.App;
using Android.OS;
using Android.Webkit;
using RemoteRunner.Network;

namespace RemoteRunner.Mobile
{

    [Activity(Label = "@string/ApplicationName", MainLauncher = true, Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
    public class MainActivity : Activity
    {
        private readonly SocketManager socket = new SocketManager(2048, 4199);
        private WebView webView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            webView = FindViewById<WebView>(Resource.Id.webView);
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.DomStorageEnabled = true;
            webView.SetWebViewClient(new HybridWebViewClient(webView, socket));
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
            socket.Connect(Variables.Ip);
        }

        public override void OnBackPressed()
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Remote Runner");
            alert.SetMessage("Do you want to exit the application?");
            alert.SetPositiveButton("Yes", (sender, args) =>
            {
                FinishAndRemoveTask();
            });
            alert.SetNegativeButton("No", (sender, args) =>
            {
                return;
            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }
    }
}