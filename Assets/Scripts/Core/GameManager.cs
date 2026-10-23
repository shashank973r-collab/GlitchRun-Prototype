using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using GlitchRun.Player;
using GlitchRun.Audio;
// InputSystem package always available (activeInputHandler = 2, Both mode).
using UnityEngine.InputSystem;

namespace GlitchRun.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Respawning,
        Victory,
        GameOver
    }

    /// <summary>
    /// Central game manager coordinating game loop, state, collectibles, lives, and level progression.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Simulation Settings")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int requiredFragments = 3;

        [Header("State (Runtime)")]
        [SerializeField] private GameState currentState = GameState.Playing;
        [SerializeField] private int currentLives = 3;
        [SerializeField] private int collectedFragments = 0;
        [SerializeField] private int totalFragments = 0;
        [SerializeField] private float elapsedTime = 0f;
        [SerializeField] private int currentScore = 0;

        private Vector3 currentCheckpoint = Vector3.zero;
        private PlayerController activePlayer;

        public event Action<int, int> OnFragmentsUpdated; // (collected, required)
        public event Action<int, int> OnLivesUpdated;     // (current, max)
        public event Action<int> OnScoreUpdated;
        public event Action<GameState> OnStateChanged;
        public event Action OnPlayerRespawn;

        public GameState CurrentState => currentState;
        public int CurrentLives => currentLives;
        public int MaxLives => maxLives;
        public int CollectedFragments => collectedFragments;
        public int RequiredFragments => requiredFragments;
        public int TotalFragments => totalFragments;
        public float ElapsedTime => elapsedTime;
        public int CurrentScore => currentScore;
        public bool IsObjectiveComplete => collectedFragments >= requiredFragments;
        public Vector3 CurrentCheckpoint => currentCheckpoint;
        public PlayerController ActivePlayer => activePlayer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "MainMenu")
            {
                currentState = GameState.MainMenu;
            }
            else if (sceneName == "Victory")
            {
                currentState = GameState.Victory;
            }
            else if (sceneName == "GameOver")
            {
                currentState = GameState.GameOver;
            }
            else
            {
                currentState = GameState.Playing;
            }

            currentLives = maxLives;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            InitializeLevel();
        }

        public void InitializeLevel()
        {
            var fragments = FindObjectsByType<Gameplay.DataFragment>(FindObjectsInactive.Exclude);
            totalFragments = fragments != null ? fragments.Length : 0;

            if (requiredFragments > totalFragments && totalFragments > 0)
            {
                requiredFragments = totalFragments;
            }

            OnFragmentsUpdated?.Invoke(collectedFragments, requiredFragments);
            OnLivesUpdated?.Invoke(currentLives, maxLives);
            OnScoreUpdated?.Invoke(currentScore);

            if (AudioManager.Instance != null)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName == "MainMenu")
                {
                    AudioManager.Instance.PlayMusic("menu");
                }
                else
                {
                    AudioManager.Instance.PlayMusic("gameplay");
                }
            }
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                elapsedTime += Time.deltaTime;

                if (CheckPauseInput())
                {
                    TogglePause();
                }
            }
            else if (currentState == GameState.Paused)
            {
                if (CheckPauseInput())
                {
                    TogglePause();
                }
            }
            else if (currentState == GameState.GameOver || currentState == GameState.Victory)
            {
                if (CheckRestartInput())
                {
                    RestartLevel();
                }
            }
        }

        private bool CheckPauseInput()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) return true;

            var pad = Gamepad.current;
            if (pad != null && pad.startButton.wasPressedThisFrame) return true;

            // Legacy fallback
            if (kb == null && (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P)))
                return true;

            return false;
        }

        private bool CheckRestartInput()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            if (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)) return true;

            // Legacy fallback
            if (kb == null && UnityEngine.Input.GetKeyDown(KeyCode.R))
                return true;

            return false;
        }

        public void SetActivePlayer(PlayerController player)
        {
            activePlayer = player;
            if (currentCheckpoint == Vector3.zero && player != null)
            {
                currentCheckpoint = player.transform.position;
            }
        }

        public void SetCheckpoint(Vector3 pos)
        {
            currentCheckpoint = pos;
            Debug.Log($"[GlitchRun] Checkpoint updated to {pos}");
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                currentState = GameState.Paused;
                Time.timeScale = 0f;
                OnStateChanged?.Invoke(currentState);
            }
            else if (currentState == GameState.Paused)
            {
                currentState = GameState.Playing;
                Time.timeScale = 1f;
                OnStateChanged?.Invoke(currentState);
            }
        }

        public void CollectFragment(Gameplay.DataFragment fragment)
        {
            if (currentState != GameState.Playing) return;

            collectedFragments++;
            currentScore += 250;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("pickup");
            }

            OnFragmentsUpdated?.Invoke(collectedFragments, requiredFragments);
            OnScoreUpdated?.Invoke(currentScore);
        }

        public void PlayerTookDamage(string source)
        {
            if (currentState != GameState.Playing) return;

            currentLives--;
            OnLivesUpdated?.Invoke(currentLives, maxLives);

            if (currentLives <= 0)
            {
                TriggerGameOver();
            }
            else
            {
                Debug.Log($"[GlitchRun] Player damaged by {source}! Lives: {currentLives}/{maxLives}. Respawning...");
                OnPlayerRespawn?.Invoke();
            }
        }

        public void CompleteLevel()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.Victory;
            int timeBonus = Mathf.Max(0, (int)((180f - elapsedTime) * 10f));
            int livesBonus = currentLives * 500;
            currentScore += timeBonus + livesBonus;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("victory");
            }

            OnStateChanged?.Invoke(currentState);
            Debug.Log($"[GlitchRun] VICTORY! Final Score: {currentScore} (Time: {elapsedTime:F1}s)");
        }

        private void TriggerGameOver()
        {
            currentState = GameState.GameOver;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("death");
            }

            OnStateChanged?.Invoke(currentState);
            Debug.Log("[GlitchRun] GAME OVER! Neural simulation terminated.");
        }

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        public void StartGame()
        {
            currentLives = maxLives;
            collectedFragments = 0;
            elapsedTime = 0f;
            currentScore = 0;
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Gameplay");
        }

        public void RestartLevel()
        {
            currentLives = maxLives;
            collectedFragments = 0;
            elapsedTime = 0f;
            currentScore = 0;
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            currentState = GameState.MainMenu;
            SceneManager.LoadScene("MainMenu");
        }

        public void QuitGame()
        {
            Debug.Log("[GlitchRun] Quitting simulation application...");
            Application.Quit();
        }
    }
}
