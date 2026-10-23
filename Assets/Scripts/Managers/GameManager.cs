using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlitchRun.Managers
{
    /// <summary>
    /// Represents the high-level states of the gameplay loop.
    /// </summary>
    public enum GameState
    {
        Playing,
        Dead,
        Victory
    }

    /// <summary>
    /// Central manager orchestrating game state, data fragment tracking,
    /// player death/respawn flow, and level completion in GLITCH RUN.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>
        /// Global static access point to the active GameManager instance.
        /// </summary>
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            // Clamp required fragments to non-negative values.
            if (requiredFragments < 0)
            {
                Debug.LogWarning($"[GameManager] Required fragments was set to {requiredFragments}. Clamping to 0.");
                requiredFragments = 0;
            }

            // Enforce a strict singleton pattern within the scene.
            // If another instance already exists, destroy this duplicate immediately.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GameManager] Duplicate GameManager detected on GameObject '{gameObject.name}'. Destroying duplicate component.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnValidate()
        {
            // Prevent negative values in the Unity Inspector.
            if (requiredFragments < 0)
            {
                requiredFragments = 0;
            }
        }

        private void OnDestroy()
        {
            // Clear static reference when this instance is unloaded to prevent memory leaks / stale references.
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Inspector Settings
        [Header("Simulation Objectives")]
        [Tooltip("The number of data fragments the player must gather to unlock the exit door.")]
        [SerializeField] private int requiredFragments = 3;

        [Header("Lifecycle Timings")]
        [Tooltip("Delay in seconds before the level reloads after the player dies.")]
        [SerializeField] private float respawnDelay = 1.0f;
        #endregion

        #region Internal State
        private int collectedFragments = 0;
        private Coroutine reloadRoutine;
        #endregion

        #region Public Properties
        /// <summary>
        /// The current overall state of gameplay (Playing, Dead, or Victory).
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.Playing;

        /// <summary>
        /// Current number of collected data fragments.
        /// </summary>
        public int CollectedFragments => collectedFragments;

        /// <summary>
        /// Target number of fragments required to unlock the exit.
        /// </summary>
        public int RequiredFragments => requiredFragments;

        /// <summary>
        /// Returns true if the player has triggered level victory.
        /// </summary>
        public bool IsGameWon => CurrentState == GameState.Victory;

        /// <summary>
        /// Returns true if the player has died and the game over sequence is underway.
        /// </summary>
        public bool IsGameOver => CurrentState == GameState.Dead;

        /// <summary>
        /// Returns true if the game is currently in the active playing state.
        /// </summary>
        public bool IsPlaying => CurrentState == GameState.Playing;
        #endregion

        #region Events
        /// <summary>
        /// Invoked whenever the fragment count changes.
        /// Arguments: (int currentCount, int targetCount)
        /// </summary>
        public event Action<int, int> OnFragmentsUpdated;

        /// <summary>
        /// Invoked once when the player dies and the death sequence begins.
        /// </summary>
        public event Action OnPlayerDied;

        /// <summary>
        /// Invoked once when the player reaches the exit and wins the level.
        /// </summary>
        public event Action OnGameWon;
        #endregion

        #region Public API
        /// <summary>
        /// Registers the pickup of a single data fragment.
        /// Will not increment past RequiredFragments or execute outside of active play.
        /// </summary>
        public void RegisterFragmentCollected()
        {
            // Ignore collection events if the player is dead or has already won.
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            // Cap collection count at the required target to prevent overflowing.
            if (collectedFragments >= requiredFragments)
            {
                return;
            }

            collectedFragments++;
            Debug.Log($"[GameManager] Fragment collected: {collectedFragments}/{requiredFragments}");

            // Notify UI and listeners of the updated count.
            OnFragmentsUpdated?.Invoke(collectedFragments, requiredFragments);
        }

        /// <summary>
        /// Triggers player defeat, halts progression, and schedules a scene reload.
        /// Safe to call multiple times; ignores subsequent calls after the initial trigger.
        /// </summary>
        public void TriggerPlayerDeath()
        {
            // Prevent multiple death triggers during an in-progress death sequence.
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            CurrentState = GameState.Dead;
            Debug.Log("[GameManager] Player defeat triggered. Halting gameplay progression.");

            // Notify gameplay listeners (e.g., sound effects, death animation, hazard states).
            OnPlayerDied?.Invoke();

            // Begin reload countdown coroutine.
            if (reloadRoutine == null)
            {
                reloadRoutine = StartCoroutine(ReloadSceneAfterDelay(respawnDelay));
            }
        }

        /// <summary>
        /// Triggers victory when the player enters the unlocked exit portal.
        /// Freezes state transitions and broadcasts the victory event for UI.
        /// </summary>
        public void TriggerVictory()
        {
            // Only transition to Victory from the active Playing state.
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            CurrentState = GameState.Victory;
            Debug.Log("[GameManager] Level completed! Simulation decrypted.");

            // Notify UI and audio listeners.
            OnGameWon?.Invoke();
        }

        /// <summary>
        /// Semantic alias for TriggerVictory to support alternative level completion naming conventions.
        /// </summary>
        public void CompleteLevel()
        {
            TriggerVictory();
        }

        /// <summary>
        /// Immediately reloads the currently active scene.
        /// Can be bound to a restart key (e.g., 'R') or invoked directly.
        /// </summary>
        public void RestartLevel()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Debug.Log($"[GameManager] Reloading scene: {activeScene.name} (Index: {activeScene.buildIndex})");

            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Waits for the specified delay in unscaled realtime before restarting the current scene.
        /// Uses unscaled realtime so reload proceeds even if Time.timeScale is 0.
        /// </summary>
        private IEnumerator ReloadSceneAfterDelay(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            RestartLevel();
        }
        #endregion
    }
}
