namespace ObstacleDodge
{
    /// <summary>
    /// AdMob ad unit IDs. These are Google's official TEST IDs: they always show "Test Ad"
    /// and are safe to click. Before publishing:
    ///   1. Create an app in admob.google.com (Android, package com.workhardpatience.obstacledodge).
    ///   2. Create 3 ad units: Banner, Interstitial, Rewarded.
    ///   3. Paste their IDs below and your App ID into Resources/values/strings.xml (admob_app_id).
    /// Never click your own real ads (the account gets banned) — use test devices instead.
    /// </summary>
    public static class AdIds
    {
        public const string Banner = "ca-app-pub-3940256099942544/6300978111";
        public const string Interstitial = "ca-app-pub-3940256099942544/1033173712";
        public const string Rewarded = "ca-app-pub-3940256099942544/5224354917";
    }
}
