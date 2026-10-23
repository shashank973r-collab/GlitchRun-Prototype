using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GlitchRun.Core
{
    public enum GameState
    {
        Playing,
        GameOver,
        Victory
    }

    /// <summary>
    /// Central game manager coordinating game loop, state, collectibles, lives, and level progression.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Simulation Settings")]
        [Tooltip("Number of lives before simulation termination (Game Over).")]
        [SerializeField] private int maxLives = 3;

        [Tooltip("Minimum data fragments required to unlock the exit portal.")]
        [SerializeField] private int requiredFragments = 3;

        [Header("State (Runtime)")]
        [SerializeField] private GameState currentState = GameState.Playing;
        [SerializeField] private int currentLives;
        [SerializeField] private int collectedFragments = 0;
        [SerializeField] private int totalFragments = 0;
        [SerializeField] private float elapsedTime = 0f;

        // Events for UI and gameplay listeners
        public event Action<int, int> OnFragmentsUpdated; // (collected, required)
        public event Action<int, int> OnLivesUpdated;     // (current, max)
        public event Action OnGameOver;
        public event Action OnVictory;
        public event Action OnPlayerRespawn;

        public GameState CurrentState => currentState;
        public int CurrentLives => currentLives;
        public int MaxLives => maxLives;
        public int CollectedFragments => collectedFragments;
        public int RequiredFragments => requiredFragments;
        public int TotalFragments => totalFragments;
        public float ElapsedTime => elapsedTime;
        public bool IsObjectiveComplete => collectedFragments >= requiredFragments;

        public Vector3 SpawnPoint { get; set; } = Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentLives = maxLives;
            currentState = GameState.Playing;
        }

        private void Start()
        {
            // Discover all data fragments placed in the level
            var fragments = FindObjectsByType<Gameplay.DataFragment>(FindObjectsSortMode.None);
            totalFragments = fragments != null ? fragments.Length : 0;

            if (requiredFragments > totalFragments && totalFragments > 0)
            {
                requiredFragments = totalFragments;
            }

            OnFragmentsUpdated?.Invoke(collectedFragments, requiredFragments);
            OnLivesUpdated?.Invoke(currentLives, maxLives);
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                elapsedTime += Time.deltaTime;
            }
            else
            {
                // Quick restart shortcut in GameOver or Victory screen
                if (CheckRestartInput())
                {
                    RestartLevel();
                }
            }
        }

        private bool CheckRestartInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            if (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)) return true;
#endif
            return false;
        }

        public void CollectFragment(Gameplay.DataFragment fragment)
        {
            if (currentState != GameState.Playing) return;

            collectedFragments++;
            OnFragmentsUpdated?.Invoke(collectedFragments, requiredFragments);
            Debug.Log($"[GlitchRun] Data Fragment Collected: {collectedFragments}/{requiredFragments} (Total in sim: {totalFragments})");
        }

        public void PlayerDied()
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
                Debug.Log($"[GlitchRun] Player hit hazard! Lives remaining: {currentLives}/{maxLives}. Respawning...");
                OnPlayerRespawn?.Invoke();
            }
        }

        public void CompleteLevel()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.Victory;
            Debug.Log($"[GlitchRun] SIMULATION BREACHED! Victory reached in {elapsedTime:F1}s with {collectedFragments}/{totalFragments} fragments.");
            OnVictory?.Invoke();
        }

        private void TriggerGameOver()
        {
            currentState = GameState.GameOver;
            Debug.Log("[GlitchRun] SIMULATION PURGED! Player lives depleted.");
            OnGameOver?.Invoke();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
