using System;
using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>
    /// Ads for the Unity editor (and development builds): instead of a real network it shows a
    /// full screen "REKLAMA (TEST)" window and a test banner, so you can SEE exactly when each ad
    /// appears (every Game Over, every 3rd next level, the "+3 jon" button).
    /// </summary>
    public sealed class EditorTestAds : IAdsProvider
    {
        public sealed class Ad
        {
            public bool Rewarded;
            public float EndTime;
            public Action<bool> Done;
            public float SecondsLeft => Mathf.Max(0f, EndTime - Time.unscaledTime);
        }

        public Ad Current { get; private set; }
        public bool BannerVisible { get; private set; }
        float rewardedReadyAt, interstitialReadyAt;

        public void Initialize()
        {
            // like a real network: the first ads need a moment to load
            rewardedReadyAt = interstitialReadyAt = Time.unscaledTime + 2f;
            Debug.Log("[Ads] Unity editor: test ads (no real network)");
        }

        public bool IsRewardedReady => Current == null && Time.unscaledTime >= rewardedReadyAt;
        public bool IsInterstitialReady => Current == null && Time.unscaledTime >= interstitialReadyAt;

        public void ShowRewarded(Action onRewarded, Action onFailed)
        {
            Debug.Log("[Ads] Test rewarded ad");
            Current = new Ad
            {
                Rewarded = true,
                EndTime = Time.unscaledTime + 3f,
                Done = ok =>
                {
                    rewardedReadyAt = Time.unscaledTime + 2f;
                    (ok ? onRewarded : onFailed)?.Invoke();
                },
            };
        }

        public void ShowInterstitial(Action onClosed)
        {
            Debug.Log("[Ads] Test interstitial");
            Current = new Ad
            {
                Rewarded = false,
                EndTime = Time.unscaledTime + 2f,
                Done = _ =>
                {
                    interstitialReadyAt = Time.unscaledTime + 2f;
                    onClosed?.Invoke();
                },
            };
        }

        /// <summary>Called by the close / skip buttons of the test window.</summary>
        public void Close(bool watchedToTheEnd)
        {
            var ad = Current;
            Current = null;
            ad?.Done?.Invoke(watchedToTheEnd);
        }

        public void SetBannerVisible(bool visible) => BannerVisible = visible;
        public bool PrivacyOptionsRequired => false;
        public void ShowPrivacyOptions() { }
    }
}
