using UnityEngine;
using UnityEngine.UI;
using GlitchRun.Managers;

namespace GlitchRun.UI
{
    /// <summary>
    /// Manages in-game user interface displays for GLITCH RUN.
    /// Handles fragment collection counters, gameplay HUD toggling,
    /// death notification overlays, and victory screen panels.
    /// Uses Unity built-in UI components (Text and GameObject panels) for zero-dependency integration.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        #region Inspector Settings
        [Header("HUD Elements (In-Game)")]
        [Tooltip("Root container GameObject holding in-game gameplay HUD elements. Hidden during game over/victory.")]
        [SerializeField] private GameObject hudContainer;

        [Tooltip("Text component displaying collected and required fragments count.")]
        [SerializeField] private Text fragmentText;

        [Tooltip("Format string for fragment readout. {0} = collected, {1} = required.")]
        [SerializeField] private string fragmentFormat = "FRAGMENTS: {0} / {1}";

        [Header("State Overlays")]
        [Tooltip("Overlay panel shown when the player dies. Scene will reload automatically after the delay.")]
        [SerializeField] private GameObject deathOverlay;

        [Tooltip("Overlay panel shown when the player clears the level.")]
        [SerializeField] private GameObject victoryOverlay;
        #endregion

        #region Internal State
        private bool isSubscribed = false;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            SubscribeToGameManager();
            SyncUIWithManagerState();
        }

        private void Start()
        {
            // Fallback subscription check in Start in case GameManager awakened after this object's OnEnable.
            if (!isSubscribed)
            {
                SubscribeToGameManager();
            }

            SyncUIWithManagerState();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
        }
        #endregion

        #region Event Subscriptions
        /// <summary>
        /// Subscribes safely to GameManager events without duplicate listeners.
        /// </summary>
        private void SubscribeToGameManager()
        {
            if (isSubscribed)
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated += HandleFragmentsUpdated;
                GameManager.Instance.OnPlayerDied += HandlePlayerDied;
                GameManager.Instance.OnGameWon += HandleGameWon;
                isSubscribed = true;
            }
        }

        /// <summary>
        /// Unsubscribes cleanly from GameManager events to prevent memory leaks and dangling callbacks.
        /// </summary>
        private void UnsubscribeFromGameManager()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated -= HandleFragmentsUpdated;
                GameManager.Instance.OnPlayerDied -= HandlePlayerDied;
                GameManager.Instance.OnGameWon -= HandleGameWon;
            }

            isSubscribed = false;
        }
        #endregion

        #region State Synchronization & Event Callbacks
        /// <summary>
        /// Synchronizes all UI elements with current GameManager properties.
        /// Ensures correct displays even if events fired prior to subscription.
        /// </summary>
        public void SyncUIWithManagerState()
        {
            if (GameManager.Instance == null)
            {
                // Standalone test scene fallback
                UpdateFragmentDisplay(0, 3);
                SetElementActive(hudContainer, true);
                SetElementActive(deathOverlay, false);
                SetElementActive(victoryOverlay, false);
                return;
            }

            // Sync fragment text readout
            UpdateFragmentDisplay(GameManager.Instance.CollectedFragments, GameManager.Instance.RequiredFragments);

            // Sync overlays based on current discrete state
            switch (GameManager.Instance.CurrentState)
            {
                case GameState.Playing:
                    SetElementActive(hudContainer, true);
                    SetElementActive(deathOverlay, false);
                    SetElementActive(victoryOverlay, false);
                    break;

                case GameState.Dead:
                    SetElementActive(hudContainer, false);
                    SetElementActive(deathOverlay, true);
                    SetElementActive(victoryOverlay, false);
                    break;

                case GameState.Victory:
                    SetElementActive(hudContainer, false);
                    SetElementActive(deathOverlay, false);
                    SetElementActive(victoryOverlay, true);
                    break;
            }
        }

        private void HandleFragmentsUpdated(int collected, int required)
        {
            UpdateFragmentDisplay(collected, required);
        }

        private void HandlePlayerDied()
        {
            SetElementActive(hudContainer, false);
            SetElementActive(deathOverlay, true);
        }

        private void HandleGameWon()
        {
            SetElementActive(hudContainer, false);
            SetElementActive(victoryOverlay, true);
        }
        #endregion

        #region Helpers & Public Actions
        /// <summary>
        /// Formats and updates the fragment count text element.
        /// Safely ignores missing text component.
        /// </summary>
        private void UpdateFragmentDisplay(int collected, int required)
        {
            if (fragmentText != null)
            {
                fragmentText.text = string.Format(fragmentFormat, collected, required);
            }
        }

        /// <summary>
        /// Safely activates or deactivates a UI GameObject with null-checking.
        /// </summary>
        private void SetElementActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        /// <summary>
        /// Public helper for UI Button OnClick events to restart the current level.
        /// Can be bound to a "Retry" or "Play Again" button in the Inspector.
        /// </summary>
        public void RestartLevel()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }
        #endregion
    }
}
