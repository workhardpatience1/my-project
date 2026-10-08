using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR && LEVELPLAY_INSTALLED
using Unity.Services.LevelPlay;
#endif

// Ads guide, Part 3 (+ the banner from 5.9): one place for all ads.
//  - Android build + Unity LevelPlay package installed -> LevelPlay (rewarded, interstitial, banner)
//  - WebGL build -> Monetag through WebAds.jslib (Adsterra banners live in the HTML page)
//  - Editor, PC, or Android without the package -> a stub (no ads, the reward is given at once)
// LEVELPLAY_INSTALLED is added automatically by Editor/ProjectSetup.cs when you install the
// "Unity LevelPlay" package (Window -> Package Manager), so the project builds with or without it.
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("LevelPlay (Android only)")]
    [SerializeField] string appKey = "YOUR_APP_KEY";
    [SerializeField] string rewardedAdUnitId = "YOUR_REWARDED_ID";
    [SerializeField] string interstitialAdUnitId = "YOUR_INTERSTITIAL_ID";
    [SerializeField] string bannerAdUnitId = "YOUR_BANNER_ID";

    Action onRewarded;
    Action onRewardFailed;
    Action onInterstitialDone;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PlatformInit();
    }

    // ===== Public API (the game calls this) =====

    public bool IsRewardedReady()
    {
        return PlatformIsRewardedReady();
    }

    public void ShowRewarded(Action onReward, Action onFail = null)
    {
        if (!PlatformIsRewardedReady()) { onFail?.Invoke(); return; }
        onRewarded = onReward;
        onRewardFailed = onFail;
        PlatformShowRewarded();
    }

    public void ShowInterstitial(Action onDone)
    {
        onInterstitialDone = onDone;
        PlatformShowInterstitial();
    }

    // ===== Common "finish" methods =====

    void GrantReward()
    {
        Action a = onRewarded;
        onRewarded = null;
        onRewardFailed = null;
        a?.Invoke();
    }

    void FailReward()
    {
        Action a = onRewardFailed;
        onRewarded = null;
        onRewardFailed = null;
        a?.Invoke();
    }

    void FinishInterstitial()
    {
        Action a = onInterstitialDone;
        onInterstitialDone = null;
        a?.Invoke();
    }

#if UNITY_ANDROID && !UNITY_EDITOR && LEVELPLAY_INSTALLED
    // ===== Android: Unity LevelPlay =====
    LevelPlayRewardedAd rewardedAd;
    LevelPlayInterstitialAd interstitialAd;
    LevelPlayBannerAd bannerAd;
    bool rewardEarned;

    void PlatformInit()
    {
        LevelPlay.OnInitSuccess += OnLevelPlayReady;
        LevelPlay.OnInitFailed += err => Debug.Log("[Ads] Init failed: " + err);
        LevelPlay.Init(appKey);
    }

    void OnLevelPlayReady(LevelPlayConfiguration config)
    {
        CreateBanner(); // the banner shows up as soon as the SDK is ready

        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);
        rewardedAd.OnAdRewarded += (info, reward) => rewardEarned = true;
        rewardedAd.OnAdClosed += info => OnRewardedClosed();
        rewardedAd.OnAdDisplayFailed += (info, err) => OnRewardedClosed();
        rewardedAd.OnAdLoadFailed += err =>
            Debug.Log("[Ads] Rewarded load failed: " + err);
        rewardedAd.LoadAd();

        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);
        interstitialAd.OnAdClosed += info => OnInterstitialClosed();
        interstitialAd.OnAdDisplayFailed += (info, err) => OnInterstitialClosed();
        interstitialAd.OnAdLoadFailed += err =>
            Debug.Log("[Ads] Interstitial load failed: " + err);
        interstitialAd.LoadAd();
    }

    void CreateBanner()
    {
        // Bottom center, 320x50. displayOnLoad is true by default:
        // the banner shows by itself as soon as it has loaded.
        bannerAd = new LevelPlayBannerAd(bannerAdUnitId, LevelPlayAdSize.BANNER,
            LevelPlayBannerPosition.BottomCenter);
        bannerAd.OnAdLoaded += info => Debug.Log("[Ads] Banner loaded");
        bannerAd.OnAdLoadFailed += err =>
            Debug.Log("[Ads] Banner load failed: " + err);
        bannerAd.LoadAd();
    }

    void OnRewardedClosed()
    {
        if (rewardEarned) GrantReward(); else FailReward();
        rewardEarned = false;
        rewardedAd.LoadAd();
    }

    void OnInterstitialClosed()
    {
        interstitialAd.LoadAd();
        FinishInterstitial();
    }

    bool PlatformIsRewardedReady()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }

    void PlatformShowRewarded()
    {
        rewardEarned = false;
        rewardedAd.ShowAd();
    }

    void PlatformShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.IsAdReady())
            interstitialAd.ShowAd();
        else
            FinishInterstitial();
    }

#elif UNITY_WEBGL && !UNITY_EDITOR
    // ===== WebGL: Monetag through JavaScript (WebAds.jslib) =====
    [DllImport("__Internal")] static extern int WebAd_IsAvailable();
    [DllImport("__Internal")] static extern void WebAd_ShowRewarded();
    [DllImport("__Internal")] static extern void WebAd_ShowInterstitial();

    void PlatformInit() { }
    bool PlatformIsRewardedReady() { return WebAd_IsAvailable() == 1; }
    void PlatformShowRewarded() { WebAd_ShowRewarded(); }
    void PlatformShowInterstitial() { WebAd_ShowInterstitial(); }

#else
    // ===== Editor and PC: stub =====
    void PlatformInit()
    {
        Debug.Log("[Ads] Editor stub: ads are not shown");
    }

    bool PlatformIsRewardedReady() { return true; }

    void PlatformShowRewarded()
    {
        Debug.Log("[Ads] Editor stub: reward given at once");
        GrantReward();
    }

    void PlatformShowInterstitial()
    {
        Debug.Log("[Ads] Editor stub: interstitial skipped");
        FinishInterstitial();
    }
#endif

    // JavaScript (WebGL) calls these methods through SendMessage
    public void OnWebRewardedDone() { GrantReward(); }
    public void OnWebRewardedFailed() { FailReward(); }
    public void OnWebInterstitialDone() { FinishInterstitial(); }
}
