using System;

namespace ObstacleDodge
{
    public enum GameState { Menu, Playing, Paused, GameOver, LevelComplete }

    /// <summary>
    /// The game loop from the ads guide (Part 2.3): hit counter, Game Over,
    /// "continue after a rewarded ad" and restart (with an interstitial on every N-th round).
    /// On top of the guide it knows about levels and the finish line.
    /// </summary>
    public sealed class GameManager
    {
        public static GameManager Instance { get; private set; }

        public int MaxHits { get; set; } = 5;
        public int ContinueBonusLives { get; set; } = 3;
        public int InterstitialEveryNRounds { get; set; } = 3;

        // static: like in the guide, the value is not reset when a level is reloaded.
        static int restartCounter;

        int hits;
        bool continueUsed;
        readonly Action<int> loadLevel;

        public int Hits => hits;
        public int LivesLeft => Math.Max(0, MaxHits - hits);
        public int BonusLives => ContinueBonusLives;
        public bool IsGameOver => State == GameState.GameOver;
        public bool CanContinue => !continueUsed;

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

        public static void ResetRoundCounterForTests() => restartCounter = 0;

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
            RoundTime = 0f;
            loadLevel?.Invoke(Level);
            SetState(GameState.Playing);
        }

        public void Tick(float dt)
        {
            if (State == GameState.Playing) RoundTime += dt;
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
        }

        /// <summary>After a rewarded ad: the player keeps exactly ContinueBonusLives lives (one time per round).</summary>
        public void ContinueAfterReward()
        {
            if (State != GameState.GameOver) return;
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

        /// <summary>"Qayta o'ynash": restarts the level; every N-th round an interstitial is shown first.</summary>
        public void Restart() => AfterRound(BeginLevel);

        /// <summary>"Keyingi daraja".</summary>
        public void NextLevel()
        {
            Level++;
            AfterRound(BeginLevel);
        }

        void AfterRound(Action then)
        {
            restartCounter++;
            bool showAd = InterstitialEveryNRounds > 0 && restartCounter % InterstitialEveryNRounds == 0;
            if (showAd && AdManager.Instance != null)
                AdManager.Instance.ShowInterstitial(then);
            else
                then();
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
