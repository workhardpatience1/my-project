#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>
    /// Monetag ads for the WebGL build through Plugins/WebGL/WebAds.jslib (ads guide, Part 6).
    /// It lives on a GameObject named exactly "AdManager", because JavaScript answers with
    /// SendMessage('AdManager', ...). Adsterra banners are in the HTML page, next to the game.
    /// </summary>
    public sealed class WebAds : MonoBehaviour, IAdsProvider
    {
        [DllImport("__Internal")] static extern int WebAd_IsAvailable();
        [DllImport("__Internal")] static extern void WebAd_ShowRewarded();
        [DllImport("__Internal")] static extern void WebAd_ShowInterstitial();

        Action onRewarded, onRewardFailed, onInterstitialClosed;

        public static WebAds Create()
        {
            var go = new GameObject("AdManager");
            DontDestroyOnLoad(go);
            return go.AddComponent<WebAds>();
        }

        public void Initialize() { }
        public bool IsRewardedReady => WebAd_IsAvailable() == 1;
        public bool IsInterstitialReady => true; // the page answers "done" at once if it has no ad

        public void ShowRewarded(Action onRewarded, Action onFailed)
        {
            this.onRewarded = onRewarded;
            onRewardFailed = onFailed;
            WebAd_ShowRewarded();
        }

        public void ShowInterstitial(Action onClosed)
        {
            onInterstitialClosed = onClosed;
            WebAd_ShowInterstitial();
        }

        // JavaScript calls these through SendMessage
        public void OnWebRewardedDone() { var a = onRewarded; onRewarded = onRewardFailed = null; a?.Invoke(); }
        public void OnWebRewardedFailed() { var a = onRewardFailed; onRewarded = onRewardFailed = null; a?.Invoke(); }
        public void OnWebInterstitialDone() { var a = onInterstitialClosed; onInterstitialClosed = null; a?.Invoke(); }

        public void SetBannerVisible(bool visible) { }
        public bool PrivacyOptionsRequired => false;
        public void ShowPrivacyOptions() { }
    }
}
#endif
