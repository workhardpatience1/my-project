#if UNITY_ANDROID && !UNITY_EDITOR && LEVELPLAY_INSTALLED
using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>
    /// Unity LevelPlay for the Android build — the LevelPlay part of AdManager.cs from the ads guide
    /// (Part 3.2 and 5.9): rewarded, interstitial and a bottom banner. Keys are in AdKeys.cs.
    /// </summary>
    public sealed class LevelPlayAds : IAdsProvider
    {
        LevelPlayRewardedAd rewardedAd;
        LevelPlayInterstitialAd interstitialAd;
        LevelPlayBannerAd bannerAd;
        bool rewardEarned;
        bool bannerWanted = true;
        Action onRewarded, onRewardFailed, onInterstitialClosed;

        public void Initialize()
        {
            if (!AdKeys.AreSet)
            {
                Debug.LogWarning("[Ads] LevelPlay keys are not set in AdKeys.cs — no ads.");
                return;
            }
            LevelPlay.OnInitSuccess += OnLevelPlayReady;
            LevelPlay.OnInitFailed += err => Debug.Log("[Ads] Init failed: " + err);
            LevelPlay.Init(AdKeys.AppKey);
        }

        void OnLevelPlayReady(LevelPlayConfiguration config)
        {
            bannerAd = new LevelPlayBannerAd(AdKeys.BannerAdUnitId, LevelPlayAdSize.BANNER, LevelPlayBannerPosition.BottomCenter);
            bannerAd.OnAdLoaded += info => { if (!bannerWanted) bannerAd.HideAd(); };
            bannerAd.OnAdLoadFailed += err => Debug.Log("[Ads] Banner load failed: " + err);
            bannerAd.LoadAd();

            rewardedAd = new LevelPlayRewardedAd(AdKeys.RewardedAdUnitId);
            rewardedAd.OnAdRewarded += (info, reward) => rewardEarned = true;
            rewardedAd.OnAdClosed += info => OnRewardedClosed();
            rewardedAd.OnAdDisplayFailed += (info, err) => OnRewardedClosed();
            rewardedAd.OnAdLoadFailed += err => Debug.Log("[Ads] Rewarded load failed: " + err);
            rewardedAd.LoadAd();

            interstitialAd = new LevelPlayInterstitialAd(AdKeys.InterstitialAdUnitId);
            interstitialAd.OnAdClosed += info => OnInterstitialClosed();
            interstitialAd.OnAdDisplayFailed += (info, err) => OnInterstitialClosed();
            interstitialAd.OnAdLoadFailed += err => Debug.Log("[Ads] Interstitial load failed: " + err);
            interstitialAd.LoadAd();
        }

        public bool IsRewardedReady => rewardedAd != null && rewardedAd.IsAdReady();
        public bool IsInterstitialReady => interstitialAd != null && interstitialAd.IsAdReady();

        public void ShowRewarded(Action onRewarded, Action onFailed)
        {
            if (!IsRewardedReady) { onFailed?.Invoke(); return; }
            this.onRewarded = onRewarded;
            onRewardFailed = onFailed;
            rewardEarned = false;
            rewardedAd.ShowAd();
        }

        void OnRewardedClosed()
        {
            // the reward is given when the ad CLOSES, so the game does not go on behind it
            var ok = onRewarded;
            var fail = onRewardFailed;
            onRewarded = onRewardFailed = null;
            if (rewardEarned) ok?.Invoke(); else fail?.Invoke();
            rewardEarned = false;
            rewardedAd.LoadAd();
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady) { onClosed?.Invoke(); return; }
            onInterstitialClosed = onClosed;
            interstitialAd.ShowAd();
        }

        void OnInterstitialClosed()
        {
            var done = onInterstitialClosed;
            onInterstitialClosed = null;
            interstitialAd.LoadAd();
            done?.Invoke();
        }

        public void SetBannerVisible(bool visible)
        {
            bannerWanted = visible;
            if (bannerAd == null) return;
            if (visible) bannerAd.ShowAd(); else bannerAd.HideAd();
        }

        public bool PrivacyOptionsRequired => false;
        public void ShowPrivacyOptions() { }
    }
}
#endif
