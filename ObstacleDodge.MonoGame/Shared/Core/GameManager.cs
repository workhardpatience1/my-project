using System;

namespace ObstacleDodge
{
    public enum GameState { Menu, Playing, Paused, GameOver, LevelComplete }

    /// <summary>
    /// The game loop from the ads guide (Part 2.3): hit counter, Game Over,
    /// "continue after a rewarded ad" and restart. On top of the guide it knows about levels and
    /// the finish line. Ads: an interstitial after EVERY Game Over (all lives are gone),
    /// and one before every N-th next level.
    /// </summary>
    public sealed class GameManager
    {
        public static GameManager Instance { get; private set; }

        public int MaxHits { get; set; } = 5;
        public int ContinueBonusLives { get; set; } = 3;

        /// <summary>Show an interstitial every time all lives are gone (Game Over).</summary>
        public bool AdOnGameOver { get; set; } = true;
        /// <summary>Seconds between the last hit and that ad, so the player sees what happened.</summary>
        public float GameOverAdDelay { get; set; } = 1.0f;
        /// <summary>An interstitial before every N-th "next level" (0 = never).</summary>
        public int InterstitialEveryNLevels { get; set; } = 3;

        // static: like the guide's restartCounter, it is not reset when a level is reloaded.
        static int levelsCompleted;

        float gameOverAdTimer = -1f;
        bool waitingForGameOverAd;

        int hits;
        bool continueUsed;
        readonly Action<int> loadLevel;

        public int Hits => hits;
        public int LivesLeft => Math.Max(0, MaxHits - hits);
        public int BonusLives => ContinueBonusLives;
        public bool IsGameOver => State == GameState.GameOver;
        public bool CanContinue => !continueUsed;

        /// <summary>True from the Game Over moment until its ad has closed: the buttons wait.</summary>
        public bool GameOverAdPending => gameOverAdTimer >= 0f || waitingForGameOverAd;

        public GameState State { get; private set; } = GameState.Menu;
        public int Level { get; private set; } = 1;
        public float RoundTime { get; private set; }
        public int LastStars { get; private set; }
        public bool LastWasNewBest { get; private set; }
        public SaveData Save { get; }

        /// <summary>Fired after every counted hit (sound, vibration, camera shake).</summary>
        public event Action HitRegistered;
        public event Action<GameState> StateChanged;
        /// <summary>Fired when the player continues after a rewarded ad.</summary>
        public event Action Continued;

        public GameManager(SaveData save, Action<int> loadLevel)
        {
            Save = save ?? new SaveData();
            this.loadLevel = loadLevel;
            Level = Math.Max(1, Save.Level);
            Instance = this;
        }

        public static void ResetRoundCounterForTests() => levelsCompleted = 0;

        public void SelectLevel(int level)
        {
            Level = Math.Clamp(level, 1, Math.Max(1, Save.Level));
        }

        /// <summary>"O'ynash" in the menu.</summary>
        public void StartGame() => BeginLevel();

        void BeginLevel()
        {
            hits = 0;
            continueUsed = false;
            gameOverAdTimer = -1f;
            waitingForGameOverAd = false;
            RoundTime = 0f;
            loadLevel?.Invoke(Level);
            SetState(GameState.Playing);
        }

        /// <summary>Called every frame (also outside of play, for the Game Over ad timer).</summary>
        public void Tick(float dt)
        {
            if (State == GameState.Playing) RoundTime += dt;
            if (gameOverAdTimer >= 0f)
            {
                gameOverAdTimer -= dt;
                if (gameOverAdTimer < 0f) ShowGameOverAd();
            }
        }

        /// <summary>Called by an obstacle the first time the player touches it (ObjectHit).</summary>
        public void RegisterHit()
        {
            if (State != GameState.Playing) return;
            hits++;
            Console.WriteLine("Hits: " + hits + " / " + MaxHits);
            HitRegistered?.Invoke();
            if (hits >= MaxHits) EndGame();
        }

        void EndGame()
        {
            SetState(GameState.GameOver);
            if (AdOnGameOver && AdManager.Instance != null)
                gameOverAdTimer = Math.Max(0f, GameOverAdDelay);
        }

        void ShowGameOverAd()
        {
            gameOverAdTimer = -1f;
            if (State != GameState.GameOver || AdManager.Instance == null) return;
            waitingForGameOverAd = true;
            // every Game Over gets its ad, even if another ad was shown a moment ago
            AdManager.Instance.ShowInterstitial(() => waitingForGameOverAd = false, ignoreCooldown: true);
        }

        /// <summary>After a rewarded ad: the player keeps exactly ContinueBonusLives lives (one time per round).</summary>
        public void ContinueAfterReward()
        {
            if (State != GameState.GameOver || GameOverAdPending) return;
            continueUsed = true;
            hits = Math.Max(0, MaxHits - ContinueBonusLives);
            SetState(GameState.Playing);
            Continued?.Invoke();
        }

        /// <summary>The player reached the finish line.</summary>
        public void CompleteLevel()
        {
            if (State != GameState.Playing) return;
            LastStars = hits == 0 ? 3 : hits <= 2 ? 2 : 1;
            LastWasNewBest = Level > Save.BestLevel;
            Save.BestLevel = Math.Max(Save.BestLevel, Level);
            Save.Level = Math.Max(Save.Level, Level + 1);
            Save.Write();
            SetState(GameState.LevelComplete);
        }

        /// <summary>"Qayta o'ynash": restarts the level (the Game Over ad was already shown).</summary>
        public void Restart()
        {
            if (GameOverAdPending) return;
            BeginLevel();
        }

        /// <summary>"Keyingi daraja": every N-th time an interstitial is shown first.</summary>
        public void NextLevel()
        {
            if (State != GameState.LevelComplete) return;
            Level++;
            levelsCompleted++;
            bool showAd = InterstitialEveryNLevels > 0 && levelsCompleted % InterstitialEveryNLevels == 0;
            if (showAd && AdManager.Instance != null)
                AdManager.Instance.ShowInterstitial(BeginLevel);
            else
                BeginLevel();
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void GoToMenu()
        {
            if (GameOverAdPending) return;
            Level = Math.Clamp(Level, 1, Math.Max(1, Save.Level));
            SetState(GameState.Menu);
        }

        void SetState(GameState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
