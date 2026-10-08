using UnityEngine;
using UnityEngine.SceneManagement;

// Ads guide, Part 2.3: hit counter, Game Over, "continue after a rewarded ad" and restart
// (an interstitial on every N-th restart). Added: levels, the finish line and saved progress.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] int maxHits = 5;
    [SerializeField] int continueBonusLives = 3;
    [SerializeField] int interstitialEveryNRestarts = 3;

    static int restartCounter = 0;

    // The level survives scene reloads (static) and app restarts (PlayerPrefs).
    const string LevelKey = "od_level";
    const string BestKey = "od_best";
    static int currentLevel = 0;

    int hits = 0;
    bool isGameOver = false;
    bool continueUsed = false;
    bool levelComplete = false;
    float roundTime = 0f;
    int stars = 0;

    public int Hits => hits;
    public int MaxHits => maxHits;
    public int BonusLives => continueBonusLives;
    public bool IsGameOver => isGameOver;
    public bool CanContinue => !continueUsed;
    public bool IsLevelComplete => levelComplete;
    public int Level => currentLevel;
    public int BestLevel => PlayerPrefs.GetInt(BestKey, 0);
    public float RoundTime => roundTime;
    public int Stars => stars;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (currentLevel <= 0) currentLevel = Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, 1));
    }

    void Update()
    {
        if (!isGameOver && !levelComplete) roundTime += Time.deltaTime;
    }

    public void RegisterHit()
    {
        if (isGameOver || levelComplete) return;
        hits++;
        Debug.Log("Hits: " + hits + " / " + maxHits);
        if (hits >= maxHits) EndGame();
    }

    void EndGame()
    {
        isGameOver = true;
        Time.timeScale = 0f;
    }

    public void ContinueAfterReward()
    {
        continueUsed = true;
        hits = Mathf.Max(0, maxHits - continueBonusLives);
        isGameOver = false;
        Time.timeScale = 1f;
    }

    // Called by FinishLine when the player reaches the end of the track.
    public void CompleteLevel()
    {
        if (isGameOver || levelComplete) return;
        levelComplete = true;
        stars = hits == 0 ? 3 : hits <= 2 ? 2 : 1;
        PlayerPrefs.SetInt(LevelKey, Mathf.Max(PlayerPrefs.GetInt(LevelKey, 1), currentLevel + 1));
        PlayerPrefs.SetInt(BestKey, Mathf.Max(BestLevel, currentLevel));
        PlayerPrefs.Save();
        Time.timeScale = 0f;
    }

    public void NextLevel()
    {
        currentLevel++;
        Restart();
    }

    public void Restart()
    {
        restartCounter++;
        bool showAd = restartCounter % interstitialEveryNRestarts == 0;
        if (showAd && AdManager.Instance != null)
            AdManager.Instance.ShowInterstitial(ReloadScene);
        else
            ReloadScene();
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
