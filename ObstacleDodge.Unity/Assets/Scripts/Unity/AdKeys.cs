namespace ObstacleDodge
{
    /// <summary>
    /// Unity LevelPlay keys for the Android build (ads guide, Part 5.1 – 5.6).
    /// 1. Install "Unity LevelPlay" in Window → Package Manager (the project then switches to the real SDK).
    /// 2. On the LevelPlay website create the Android app (package com.workhardpatience.obstacledodge)
    ///    and 3 ad units: Rewarded, Interstitial, Banner.
    /// 3. Paste the App Key and the 3 Ad Unit IDs here (no spaces).
    /// In the Unity editor test ads are shown instead, so you can see when each ad appears.
    /// </summary>
    public static class AdKeys
    {
        public const string AppKey = "YOUR_APP_KEY";
        public const string RewardedAdUnitId = "YOUR_REWARDED_ID";
        public const string InterstitialAdUnitId = "YOUR_INTERSTITIAL_ID";
        public const string BannerAdUnitId = "YOUR_BANNER_ID";

        public static bool AreSet => !AppKey.StartsWith("YOUR_");
    }
}
