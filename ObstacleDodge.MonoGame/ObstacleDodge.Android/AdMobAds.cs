#if ADMOB
using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Google.Android.Gms.Ads;
using Google.Android.Gms.Ads.Initialization;
using Google.Android.Gms.Ads.Interstitial;
using Google.Android.Gms.Ads.Rewarded;
using Xamarin.Google.UserMesssagingPlatform; // (sic: the binding's namespace has three "s")

namespace ObstacleDodge
{
    /// <summary>
    /// Google AdMob for the Android version: banner (always at the bottom), interstitial
    /// (between rounds) and rewarded (the "Reklama ko'rish: +3 jon" button).
    /// It does the same job as the LevelPlay part of AdManager.cs in the ads guide (Part 3 and 5).
    /// Before any ad, Google's consent tool (UMP) asks for consent where the law needs it
    /// (EU/UK, guide Part 11.2); everywhere else it does nothing. All calls run on the UI thread.
    /// </summary>
    public sealed class AdMobAds : IAdsProvider
    {
        readonly Activity activity;
        AdView banner;
        volatile InterstitialAd interstitial;
        volatile RewardedAd rewarded;
        bool loadingInterstitial, loadingRewarded, initialized, adsStarted;
        IConsentInformation consent;
        volatile bool privacyOptionsRequired; // read every frame by the menu, updated on the UI thread
        int interstitialRetry, rewardedRetry;

        public AdMobAds(Activity activity) { this.activity = activity; }

        public void AttachBanner(FrameLayout root)
        {
            banner = new AdView(activity) { AdUnitId = AdIds.Banner, AdSize = AdSize.Banner };
            var lp = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent,
                GravityFlags.Bottom | GravityFlags.CenterHorizontal);
            root.AddView(banner, lp);
        }

        public void Initialize()
        {
            activity.RunOnUiThread(() =>
            {
                try { RequestConsentThenStart(); }
                catch (Exception e)
                {
                    Console.WriteLine("[Ads] Consent tool failed: " + e.Message);
                    StartAds();
                }
            });
        }

        // ---------------------------------------------------------------- consent (UMP)

        void RequestConsentThenStart()
        {
            consent = UserMessagingPlatform.GetConsentInformation(activity);
            var parameters = new ConsentRequestParameters.Builder().Build();
            consent.RequestConsentInfoUpdate(activity, parameters,
                new ConsentUpdated(() =>
                {
                    try
                    {
                        // shows the form only if the user is in a region that needs it and has not answered yet
                        UserMessagingPlatform.LoadAndShowConsentFormIfRequired(activity, new ConsentFormDismissed(error =>
                        {
                            if (error != null) Console.WriteLine("[Ads] Consent form: " + error.Message);
                            StartAdsIfAllowed();
                        }));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[Ads] Consent form failed: " + e.Message);
                        StartAdsIfAllowed();
                    }
                }),
                new ConsentUpdateFailed(error =>
                {
                    Console.WriteLine("[Ads] Consent update failed: " + error?.Message);
                    StartAdsIfAllowed();
                }));

            // consent from the last launch is still valid: start loading ads right away
            UpdatePrivacyFlag();
            if (consent.CanRequestAds()) StartAds();
        }

        void StartAdsIfAllowed()
        {
            UpdatePrivacyFlag();
            bool allowed;
            try { allowed = consent == null || consent.CanRequestAds(); } catch { allowed = true; }
            if (allowed) StartAds();
        }

        void StartAds()
        {
            if (adsStarted) return;
            adsStarted = true;
            try
            {
                MobileAds.Initialize(activity, new InitListener(OnInitialized));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Ads] Init failed: " + e.Message);
            }
        }

        public bool PrivacyOptionsRequired => privacyOptionsRequired;

        void UpdatePrivacyFlag()
        {
            try
            {
                var status = consent?.PrivacyOptionsRequirementStatus;
                privacyOptionsRequired = status != null && status.Equals(ConsentInformationPrivacyOptionsRequirementStatus.Required);
            }
            catch { privacyOptionsRequired = false; }
        }

        public void ShowPrivacyOptions()
        {
            activity.RunOnUiThread(() =>
            {
                try
                {
                    UserMessagingPlatform.ShowPrivacyOptionsForm(activity, new ConsentFormDismissed(error =>
                    {
                        if (error != null) Console.WriteLine("[Ads] Privacy options: " + error.Message);
                    }));
                }
                catch (Exception e) { Console.WriteLine("[Ads] Privacy options failed: " + e.Message); }
            });
        }

        void OnInitialized()
        {
            try { OnInitializedCore(); }
            catch (Exception e) { Console.WriteLine("[Ads] After init: " + e.Message); }
        }

        void OnInitializedCore()
        {
            if (initialized) return;
            initialized = true;
            Console.WriteLine("[Ads] AdMob ready");
            try { banner?.LoadAd(new AdRequest.Builder().Build()); } catch (Exception e) { Console.WriteLine("[Ads] Banner: " + e.Message); }
            LoadInterstitial();
            LoadRewarded();
        }

        // ---------------------------------------------------------------- interstitial

        void LoadInterstitial()
        {
            if (loadingInterstitial || interstitial != null) return;
            loadingInterstitial = true;
            try
            {
            InterstitialAd.Load(activity, AdIds.Interstitial, new AdRequest.Builder().Build(), new InterstitialLoad(
                ad =>
                {
                    loadingInterstitial = false;
                    interstitialRetry = 0;
                    interstitial = ad;
                },
                error =>
                {
                    loadingInterstitial = false;
                    Console.WriteLine("[Ads] Interstitial load failed: " + error?.Message);
                    RetryLater(ref interstitialRetry, LoadInterstitial);
                }));
            }
            catch (Exception e)
            {
                loadingInterstitial = false;
                Console.WriteLine("[Ads] Interstitial load error: " + e.Message);
            }
        }

        public bool IsInterstitialReady => interstitial != null;

        public void ShowInterstitial(Action onClosed)
        {
            activity.RunOnUiThread(() =>
            {
                var ad = interstitial;
                interstitial = null;
                if (ad == null) { onClosed?.Invoke(); LoadInterstitial(); return; }
                bool done = false;
                void Finish()
                {
                    if (done) return;
                    done = true;
                    onClosed?.Invoke();
                    LoadInterstitial(); // load the next one right away
                }
                ad.FullScreenContentCallback = new FullScreenCallback(Finish, Finish);
                try { ad.Show(activity); } catch (Exception) { Finish(); }
            });
        }

        // ---------------------------------------------------------------- rewarded

        void LoadRewarded()
        {
            if (loadingRewarded || rewarded != null) return;
            loadingRewarded = true;
            try
            {
            RewardedAd.Load(activity, AdIds.Rewarded, new AdRequest.Builder().Build(), new RewardedLoad(
                ad =>
                {
                    loadingRewarded = false;
                    rewardedRetry = 0;
                    rewarded = ad;
                },
                error =>
                {
                    loadingRewarded = false;
                    Console.WriteLine("[Ads] Rewarded load failed: " + error?.Message);
                    RetryLater(ref rewardedRetry, LoadRewarded);
                }));
            }
            catch (Exception e)
            {
                loadingRewarded = false;
                Console.WriteLine("[Ads] Rewarded load error: " + e.Message);
            }
        }

        public bool IsRewardedReady => rewarded != null;

        public void ShowRewarded(Action onRewarded, Action onFailed)
        {
            activity.RunOnUiThread(() =>
            {
                var ad = rewarded;
                rewarded = null;
                if (ad == null) { onFailed?.Invoke(); LoadRewarded(); return; }

                bool earned = false, done = false;
                void Closed()
                {
                    if (done) return;
                    done = true;
                    // the reward is given when the ad is CLOSED, so the game does not go on behind the ad
                    if (earned) onRewarded?.Invoke(); else onFailed?.Invoke();
                    LoadRewarded();
                }
                void FailedToShow()
                {
                    earned = false;
                    Closed();
                }
                ad.FullScreenContentCallback = new FullScreenCallback(Closed, FailedToShow);
                try { ad.Show(activity, new RewardListener(() => earned = true)); }
                catch (Exception) { FailedToShow(); }
            });
        }

        // ---------------------------------------------------------------- banner + lifecycle

        public void SetBannerVisible(bool visible)
        {
            activity.RunOnUiThread(() =>
            {
                try { if (banner != null) banner.Visibility = visible ? ViewStates.Visible : ViewStates.Gone; } catch { }
            });
        }

        public void Pause() { try { banner?.Pause(); } catch { } }
        public void Resume() { try { banner?.Resume(); } catch { } }
        public void Destroy() { try { banner?.Destroy(); } catch { } }

        /// <summary>No ad (no internet, no fill)? Try again after 10, 20, 40... seconds (max 2 minutes).</summary>
        void RetryLater(ref int attempt, Action load)
        {
            attempt++;
            int seconds = Math.Min(120, 10 * (1 << Math.Min(attempt - 1, 4)));
            var handler = new Android.OS.Handler(Android.OS.Looper.MainLooper);
            handler.PostDelayed(load, seconds * 1000L);
        }

        // ---------------------------------------------------------------- Java callback classes

        sealed class InitListener : Java.Lang.Object, IOnInitializationCompleteListener
        {
            readonly Action done;
            public InitListener(Action done) { this.done = done; }
            public void OnInitializationComplete(IInitializationStatus status) => done();
        }

        sealed class ConsentUpdated : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateSuccessListener
        {
            readonly Action done;
            public ConsentUpdated(Action done) { this.done = done; }
            public void OnConsentInfoUpdateSuccess() => done();
        }

        sealed class ConsentUpdateFailed : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateFailureListener
        {
            readonly Action<FormError> failed;
            public ConsentUpdateFailed(Action<FormError> failed) { this.failed = failed; }
            public void OnConsentInfoUpdateFailure(FormError error) => failed(error);
        }

        sealed class ConsentFormDismissed : Java.Lang.Object, IConsentFormOnConsentFormDismissedListener
        {
            readonly Action<FormError> dismissed;
            public ConsentFormDismissed(Action<FormError> dismissed) { this.dismissed = dismissed; }
            public void OnConsentFormDismissed(FormError error) => dismissed(error);
        }

        sealed class InterstitialLoad : InterstitialAdLoadCallback
        {
            readonly Action<InterstitialAd> loaded;
            readonly Action<LoadAdError> failed;
            public InterstitialLoad(Action<InterstitialAd> loaded, Action<LoadAdError> failed) { this.loaded = loaded; this.failed = failed; }
            public override void OnAdLoaded(InterstitialAd ad) => loaded(ad);
            public override void OnAdFailedToLoad(LoadAdError error) => failed(error);
        }

        sealed class RewardedLoad : RewardedAdLoadCallback
        {
            readonly Action<RewardedAd> loaded;
            readonly Action<LoadAdError> failed;
            public RewardedLoad(Action<RewardedAd> loaded, Action<LoadAdError> failed) { this.loaded = loaded; this.failed = failed; }
            public override void OnAdLoaded(RewardedAd ad) => loaded(ad);
            public override void OnAdFailedToLoad(LoadAdError error) => failed(error);
        }

        sealed class FullScreenCallback : FullScreenContentCallback
        {
            readonly Action dismissed, failedToShow;
            public FullScreenCallback(Action dismissed, Action failedToShow) { this.dismissed = dismissed; this.failedToShow = failedToShow; }
            public override void OnAdDismissedFullScreenContent() => dismissed();
            public override void OnAdFailedToShowFullScreenContent(AdError error)
            {
                Console.WriteLine("[Ads] Failed to show: " + error?.Message);
                failedToShow();
            }
        }

        sealed class RewardListener : Java.Lang.Object, IOnUserEarnedRewardListener
        {
            readonly Action earned;
            public RewardListener(Action earned) { this.earned = earned; }
            public void OnUserEarnedReward(IRewardItem reward) => earned();
        }
    }
}
#endif
