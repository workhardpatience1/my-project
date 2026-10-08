using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;

namespace ObstacleDodge
{
    /// <summary>
    /// The Android entry point. It starts the MonoGame game, puts the AdMob banner on top of it
    /// (bottom center) and gives the game what only a phone has: ads, vibration, the files folder.
    /// </summary>
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Exported = true,
        Icon = "@mipmap/appicon",
        Theme = "@style/AppTheme",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleTask, // not SingleInstance: the AdMob ad activity must open in the same task
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden |
                               ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.UiMode |
                               ConfigChanges.SmallestScreenSize)]
    public class MainActivity : AndroidGameActivity, IPlatform
    {
        ObstacleDodgeGame game;
        IAdsProvider ads;
        int bannerHeightPx, bannerWidthPx;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            HideSystemBars();

            var root = new FrameLayout(this);
#if ADMOB
            var adMob = new AdMobAds(this);
            ads = adMob;
            // A 320x50 dp banner; we know its size before it loads, so the buttons never jump.
            float density = Resources.DisplayMetrics.Density;
            bannerHeightPx = (int)(50 * density);
            bannerWidthPx = (int)(320 * density);
#else
            ads = new StubAdsProvider();
            bannerHeightPx = bannerWidthPx = 0;
#endif
            game = new ObstacleDodgeGame(this);
            var view = (View)game.Services.GetService(typeof(View));
            root.AddView(view, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
#if ADMOB
            adMob.AttachBanner(root);
#endif
            SetContentView(root);
            game.Run();
        }

        // ---- IPlatform ----
        public IAdsProvider Ads => ads;
        public bool IsMobile => true;
        public int BannerHeightPx => bannerHeightPx;
        public int BannerWidthPx => bannerWidthPx;
        public string SaveDirectory => FilesDir?.AbsolutePath;

        public void Vibrate(int milliseconds)
        {
            try
            {
                var vibrator = (Vibrator)GetSystemService(VibratorService);
                if (vibrator == null || !vibrator.HasVibrator) return;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    vibrator.Vibrate(VibrationEffect.CreateOneShot(milliseconds, VibrationEffect.DefaultAmplitude));
                else
#pragma warning disable CA1422, CS0618
                    vibrator.Vibrate(milliseconds);
#pragma warning restore CA1422, CS0618
            }
            catch (Exception) { /* no vibration on this device */ }
        }

        public void Quit() => RunOnUiThread(() => MoveTaskToBack(true));

        // ---- full screen ----

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus) HideSystemBars();
        }

        void HideSystemBars()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
                var controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                    controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
#pragma warning disable CA1422, CS0618
                Window.DecorView.SystemUiFlags =
                    SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation |
                    SystemUiFlags.LayoutStable | SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation;
#pragma warning restore CA1422, CS0618
            }
        }

        // ---- the banner follows the activity's life ----
#if ADMOB
        protected override void OnPause()
        {
            (ads as AdMobAds)?.Pause();
            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            (ads as AdMobAds)?.Resume();
        }

        protected override void OnDestroy()
        {
            (ads as AdMobAds)?.Destroy();
            base.OnDestroy();
        }
#endif
    }
}
