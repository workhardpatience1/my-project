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
        double adStartedAt;
        int adToken;           // which show the callbacks belong to
        Action pendingFailure; // what to do if the ad never reports back

        /// <summary>If an ad never reports that it closed, the game goes on after this many seconds.</summary>
        public double AdTimeoutSeconds { get; set; } = 150;

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
            int token = BeginAd(onFail);
            try
            {
                provider.ShowRewarded(
                    () => Post(() => { if (EndAd(token)) onReward?.Invoke(); }),
                    () => Post(() => { if (EndAd(token)) onFail?.Invoke(); }));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Ads] Rewarded failed: " + e.Message);
                if (EndAd(token)) onFail?.Invoke();
            }
        }

        /// <summary>
        /// Shows an interstitial between rounds. If one was shown less than
        /// <see cref="MinSecondsBetweenInterstitials"/> ago (unless <paramref name="ignoreCooldown"/>),
        /// or none is loaded, the game just goes on.
        /// </summary>
        public void ShowInterstitial(Action onDone, bool ignoreCooldown = false)
        {
            double now = clock.Elapsed.TotalSeconds;
            bool tooSoon = !ignoreCooldown && now - lastInterstitialTime < MinSecondsBetweenInterstitials;
            bool ready;
            try { ready = provider.IsInterstitialReady; } catch { ready = false; }
            if (tooSoon || !ready || IsShowingAd) { onDone?.Invoke(); return; }

            lastInterstitialTime = now;
            int token = BeginAd(onDone);
            try
            {
                provider.ShowInterstitial(() => Post(() => { if (EndAd(token)) onDone?.Invoke(); }));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Ads] Interstitial failed: " + e.Message);
                if (EndAd(token)) onDone?.Invoke();
            }
        }

        public void SetBannerVisible(bool visible)
        {
            try { provider.SetBannerVisible(visible); } catch { }
        }

        public bool PrivacyOptionsRequired
        {
            get { try { return provider.PrivacyOptionsRequired; } catch { return false; } }
        }

        public void ShowPrivacyOptions()
        {
            try { provider.ShowPrivacyOptions(); } catch (Exception e) { Console.WriteLine("[Ads] Privacy options: " + e.Message); }
        }

        /// <summary>Runs the ad callbacks on the game thread. Called once per frame from Game.Update.</summary>
        public void Update()
        {
            while (gameThreadQueue.TryDequeue(out var action)) action();

            // safety net: never leave the game frozen behind an ad that did not report back
            if (IsShowingAd && clock.Elapsed.TotalSeconds - adStartedAt > AdTimeoutSeconds)
            {
                Console.WriteLine("[Ads] No answer from the ad, continuing the game");
                var failure = pendingFailure;
                EndAd(adToken);
                failure?.Invoke();
            }
        }

        int BeginAd(Action onFailure)
        {
            IsShowingAd = true;
            adStartedAt = clock.Elapsed.TotalSeconds;
            pendingFailure = onFailure;
            return ++adToken;
        }

        /// <summary>Closes the current ad; false if this callback is late or doubled (then it is ignored).</summary>
        bool EndAd(int token)
        {
            if (!IsShowingAd || token != adToken) return false;
            IsShowingAd = false;
            pendingFailure = null;
            return true;
        }

        void Post(Action action) => gameThreadQueue.Enqueue(action);
    }
}
