using System;
using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>
    /// What is different on each device, for the shared game code (the Unity side of IPlatform).
    /// Ads: Android + LevelPlay package → LevelPlay; WebGL → Monetag; Unity editor / development
    /// build → visible test ads; otherwise no ads.
    /// </summary>
    public sealed class UnityPlatform : IPlatform
    {
        public IAdsProvider Ads { get; }
        public EditorTestAds TestAds { get; }
        public string LastError { get; private set; }
        readonly bool hasBanner;

        public UnityPlatform()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && LEVELPLAY_INSTALLED
            Ads = new LevelPlayAds();
            hasBanner = AdKeys.AreSet;
#elif UNITY_WEBGL && !UNITY_EDITOR
            Ads = WebAds.Create();
            hasBanner = false; // the Adsterra banner is outside the game, on the page
#elif UNITY_EDITOR || DEVELOPMENT_BUILD
            TestAds = new EditorTestAds();
            Ads = TestAds;
            hasBanner = true;
#else
            Ads = new StubAdsProvider();
            hasBanner = false;
#endif
        }

        public bool IsMobile => Application.isMobilePlatform;

        /// <summary>One "dp" in real pixels (Android: 160 dpi = 1).</summary>
        static float Density => Screen.dpi > 0 ? Screen.dpi / 160f : Mathf.Max(1f, Screen.height / 360f);

        // a 320 x 50 dp banner at the bottom center
        public int BannerHeightPx => hasBanner ? Mathf.RoundToInt(50f * BannerDensity) : 0;
        public int BannerWidthPx => hasBanner ? Mathf.RoundToInt(320f * BannerDensity) : 0;

        // in the editor the "phone" is the Game view, so use its height (a phone is about 360 dp high)
        static float BannerDensity => Application.isEditor ? Screen.height / 360f : Density;

        public string SaveDirectory => Application.persistentDataPath;

        public void Vibrate(int milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    vibrator?.Call("vibrate", (long)milliseconds);
                }
            }
            catch (Exception)
            {
                if (milliseconds >= 150) Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (milliseconds >= 150) Handheld.Vibrate();
#endif
        }

        public void Quit()
        {
            Application.Quit();
        }

        public void ReportError(string text)
        {
            LastError = text;
            Debug.LogError("[Obstacle Dodge] " + text);
        }

        public void OpenUrl(string url) => Application.OpenURL(url);
    }
}
