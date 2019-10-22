using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.DependencyInjection;

namespace Tello.AndroidUI
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            RequestedOrientation = ScreenOrientation.Landscape;
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation | SystemUiFlags.Immersive);

            var outputWindow = FindViewById<TextView>(Resource.Id.outputWindow);
            var logger = new OutputWindowLogger();
            logger.SetOutputWindow(outputWindow);

            var serviceProvider = new ServiceCollection()
                .AddSingleton<Tello.Logger>(logger)
                .BuildServiceProvider();

            var commandClient = new Tello.CommandClient(logger);
            var stateClient = new Tello.StateServer(logger);

            var inFlight = false;
            RegisterButton(Resource.Id.commandButton, async () =>
            {
                var wifiManager = (WifiManager)Application.Context.GetSystemService(WifiService);
                if (!wifiManager.IsWifiEnabled)
                {
                    logger.WriteInformationLine("Enabling WiFi");
                    logger.WriteInformationLine(wifiManager.SetWifiEnabled(true) ? "Enabled" : "Failed");
                }

                var initialiseSuccess = await commandClient.Initialise();
                if (!initialiseSuccess)
                {
                    logger.WriteErrorLine("Failed to initialise connection");
                }
                else if (!stateClient.IsListening)
                {
                    stateClient.Listen();
                }
            });
            RegisterButton(Resource.Id.forwardButton, async () => await commandClient.Forward(50));
            RegisterButton(Resource.Id.rotateLeftButton, async () => await commandClient.RotateLeft(30));
            RegisterButton(Resource.Id.rotateRightButton, async () => await commandClient.RotateRight(30));
            RegisterButton(Resource.Id.flipBackwardButton, async () => await commandClient.FlipBackward());
            RegisterButton(Resource.Id.takeOffLandButton, async () =>
                {
                    if (inFlight)
                    {
                        await commandClient.Land();
                    }
                    else
                    {
                        await commandClient.TakeOff();
                    }

                    inFlight = !inFlight;
                });
        }

        public void RegisterButton(int resourceId, Action action)
        {
            FindViewById<Button>(resourceId).Click += (object sender, EventArgs args) => action();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

