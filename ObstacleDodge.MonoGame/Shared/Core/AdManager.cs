using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ObstacleDodge
{
    /// <summary>
    /// One place for all ads (the AdManager from the ads guide, Part 3).
    /// The game only asks three things: "is a rewarded ad ready?", "show a rewarded ad"
    /// and "show an interstitial". Which network shows it is the provider's job.
    /// </summary>
    public sealed class AdManager
    {
        public static AdManager Instance { get; private set; }

        /// <summary>An interstitial is never shown more often than this (guide 11.4).</summary>
        public double MinSecondsBetweenInterstitials { get; set; } = 60;

        readonly IAdsProvider provider;
        readonly ConcurrentQueue<Action> gameThreadQueue = new ConcurrentQueue<Action>();
        readonly Stopwatch clock = Stopwatch.StartNew();
        double lastInterstitialTime = double.NegativeInfinity;

        /// <summary>True while a full screen ad is open: the game is paused and ignores input.</summary>
        public bool IsShowingAd { get; private set; }

        public AdManager(IAdsProvider provider)
        {
            this.provider = provider ?? new StubAdsProvider();
            Instance = this;
            try { this.provider.Initialize(); }
            catch (Exception e) { Console.WriteLine("[Ads] Init failed: " + e.Message); }
        }

        // ===== Public API (the game calls this) =====

        public bool IsRewardedReady()
        {
            if (IsShowingAd) return false;
            try { return provider.IsRewardedReady; }
            catch { return false; }
        }

        public void ShowRewarded(Action onReward, Action onFail = null)
        {
            if (!IsRewardedReady()) { onFail?.Invoke(); return; }
            IsShowingAd = true;
            try
            {
                provider.ShowRewarded(
                    () => Post(() => { IsShowingAd = false; onReward?.Invoke(); }),
                    () => Post(() => { IsShowingAd = false; onFail?.Invoke(); }));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Ads] Rewarded failed: " + e.Message);
                IsShowingAd = false;
                onFail?.Invoke();
            }
        }

        /// <summary>
        /// Shows an interstitial between rounds. If one was shown less than
        /// <see cref="MinSecondsBetweenInterstitials"/> ago, or none is loaded, the game just goes on.
        /// </summary>
        public void ShowInterstitial(Action onDone)
        {
            double now = clock.Elapsed.TotalSeconds;
            bool tooSoon = now - lastInterstitialTime < MinSecondsBetweenInterstitials;
            bool ready;
            try { ready = provider.IsInterstitialReady; } catch { ready = false; }
            if (tooSoon || !ready || IsShowingAd) { onDone?.Invoke(); return; }

            lastInterstitialTime = now;
            IsShowingAd = true;
            try
            {
                provider.ShowInterstitial(() => Post(() => { IsShowingAd = false; onDone?.Invoke(); }));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Ads] Interstitial failed: " + e.Message);
                IsShowingAd = false;
                onDone?.Invoke();
            }
        }

        public void SetBannerVisible(bool visible)
        {
            try { provider.SetBannerVisible(visible); } catch { }
        }

        /// <summary>Runs the ad callbacks on the game thread. Called once per frame from Game.Update.</summary>
        public void Update()
        {
            while (gameThreadQueue.TryDequeue(out var action)) action();
        }

        void Post(Action action) => gameThreadQueue.Enqueue(action);
    }
}
