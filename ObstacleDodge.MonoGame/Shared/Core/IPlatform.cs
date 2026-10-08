using System;

namespace ObstacleDodge
{
    /// <summary>
    /// Everything that is different between the phone (Android) and the computer (Desktop).
    /// The game itself only talks to this interface, so the same game code runs everywhere.
    /// </summary>
    public interface IPlatform
    {
        IAdsProvider Ads { get; }

        /// <summary>True on a phone: on-screen arrow buttons are shown.</summary>
        bool IsMobile { get; }

        /// <summary>Height (in real screen pixels) that the banner ad takes at the bottom of the screen.</summary>
        int BannerHeightPx { get; }

        /// <summary>Width (in real screen pixels) of the banner; it is centered at the bottom.</summary>
        int BannerWidthPx { get; }

        /// <summary>Folder where the save file is written.</summary>
        string SaveDirectory { get; }

        void Vibrate(int milliseconds);

        /// <summary>Leave the game (Android: back to the home screen).</summary>
        void Quit();

        /// <summary>Something went wrong: show the error text to the player instead of closing silently.</summary>
        void ReportError(string text);
    }

    /// <summary>
    /// One ad network (AdMob on Android, a stub on the computer).
    /// Callbacks may arrive on any thread; <see cref="AdManager"/> moves them to the game thread.
    /// </summary>
    public interface IAdsProvider
    {
        void Initialize();
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }

        /// <summary>Shows a rewarded ad. Exactly one of the two callbacks is called after the ad closes.</summary>
        void ShowRewarded(Action onRewarded, Action onFailed);

        /// <summary>Shows an interstitial. <paramref name="onClosed"/> is always called (even if nothing was shown).</summary>
        void ShowInterstitial(Action onClosed);

        void SetBannerVisible(bool visible);
    }

    /// <summary>
    /// The "Editor and PC: stub" from the ads guide (Part 3.2): shows no ads,
    /// but lets you test all of the game logic.
    /// </summary>
    public sealed class StubAdsProvider : IAdsProvider
    {
        public void Initialize() => Console.WriteLine("[Ads] Stub: ads are not shown");
        public bool IsRewardedReady => true;
        public bool IsInterstitialReady => true;

        public void ShowRewarded(Action onRewarded, Action onFailed)
        {
            Console.WriteLine("[Ads] Stub: reward given at once");
            onRewarded?.Invoke();
        }

        public void ShowInterstitial(Action onClosed)
        {
            Console.WriteLine("[Ads] Stub: interstitial skipped");
            onClosed?.Invoke();
        }

        public void SetBannerVisible(bool visible) { }
    }
}
