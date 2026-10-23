using UnityEngine;
using GlitchRun.Managers;
using GlitchRun.Player;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Represents the level exit threshold in GLITCH RUN.
    /// Remains locked until the player has collected all required Data Fragments.
    /// Upon contact with the player while unlocked, triggers the level victory sequence via GameManager.
    /// Supports both trigger zones and solid physics colliders.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitDoor : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Visual Feedback (Optional)")]
        [Tooltip("Optional SpriteRenderer reference. Auto-detected on this object if unassigned.")]
        [SerializeField] private SpriteRenderer doorRenderer;

        [Tooltip("Sprite displayed while the door remains locked.")]
        [SerializeField] private Sprite lockedSprite;

        [Tooltip("Sprite displayed once the door is unlocked.")]
        [SerializeField] private Sprite unlockedSprite;

        [Tooltip("Tint color applied when the door is locked.")]
        [SerializeField] private Color lockedColor = new Color(0.7f, 0.2f, 0.2f, 1f);

        [Tooltip("Tint color applied when the door is unlocked and ready for exit.")]
        [SerializeField] private Color unlockedColor = new Color(0.2f, 1f, 0.4f, 1f);

        [Header("Audio & Effects (Optional)")]
        [Tooltip("Optional sound effect played the moment all fragments are collected and the door opens.")]
        [SerializeField] private AudioClip doorUnlockSFX;

        [Tooltip("Optional sound effect played when the player enters the unlocked door.")]
        [SerializeField] private AudioClip victorySFX;

        [Tooltip("Optional particle effect spawned when the door unlocks.")]
        [SerializeField] private GameObject unlockVFX;
        #endregion

        #region Internal State
        private bool isUnlocked = false;
        private bool hasTriggeredVictory = false;
        private bool isSubscribed = false;
        #endregion

        #region Public Properties (Contract API)
        /// <summary>
        /// True if the required number of fragments has been collected and the door is accessible.
        /// </summary>
        public bool IsUnlocked => isUnlocked;

        /// <summary>
        /// True if the victory transition has already been dispatched.
        /// </summary>
        public bool HasTriggeredVictory => hasTriggeredVictory;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (doorRenderer == null)
            {
                doorRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            SubscribeToGameManager();
            CheckInitialUnlockState();
        }

        private void Start()
        {
            // Fallback check in Start in case GameManager Awake executed after OnEnable.
            if (!isSubscribed)
            {
                SubscribeToGameManager();
            }

            CheckInitialUnlockState();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameManager();
        }
        #endregion

        #region Event Subscriptions & State Checking
        /// <summary>
        /// Safely subscribes to the GameManager fragment update event.
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
                isSubscribed = true;
            }
        }

        /// <summary>
        /// Safely unsubscribes from GameManager events to prevent memory leaks and dangling callbacks.
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
            }

            isSubscribed = false;
        }

        /// <summary>
        /// Checks the current GameManager state upon initialization and updates the door state accordingly.
        /// </summary>
        private void CheckInitialUnlockState()
        {
            if (GameManager.Instance != null)
            {
                bool shouldBeUnlocked = GameManager.Instance.CollectedFragments >= GameManager.Instance.RequiredFragments;
                if (shouldBeUnlocked && !isUnlocked)
                {
                    Unlock(playEffects: false);
                }
                else if (!shouldBeUnlocked && isUnlocked)
                {
                    Lock();
                }
                else
                {
                    UpdateVisualState();
                }
            }
            else
            {
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Event listener invoked when GameManager.OnFragmentsUpdated fires.
        /// </summary>
        private void HandleFragmentsUpdated(int collected, int required)
        {
            if (collected >= required && !isUnlocked)
            {
                Unlock(playEffects: true);
            }
            else if (collected < required && isUnlocked)
            {
                Lock();
            }
        }
        #endregion

        #region Public Methods (Contract API)
        /// <summary>
        /// Unlocks the exit door, updating its visual state and playing optional unlock effects.
        /// </summary>
        public void Unlock(bool playEffects = true)
        {
            if (isUnlocked)
            {
                return;
            }

            isUnlocked = true;
            Debug.Log($"[ExitDoor] '{gameObject.name}' unlocked! Exit is now accessible.");

            UpdateVisualState();

            if (playEffects)
            {
                if (doorUnlockSFX != null)
                {
                    AudioSource.PlayClipAtPoint(doorUnlockSFX, transform.position);
                }

                if (unlockVFX != null)
                {
                    Instantiate(unlockVFX, transform.position, Quaternion.identity);
                }
            }
        }

        /// <summary>
        /// Resets the door to its locked state.
        /// </summary>
        public void Lock()
        {
            isUnlocked = false;
            hasTriggeredVictory = false;
            UpdateVisualState();
            Debug.Log($"[ExitDoor] '{gameObject.name}' locked.");
        }

        /// <summary>
        /// Attempts to trigger level completion if the door is unlocked.
        /// </summary>
        public void TryCompleteLevel()
        {
            if (!isUnlocked || hasTriggeredVictory)
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                hasTriggeredVictory = true;
                Debug.Log($"[ExitDoor] Player reached unlocked exit '{gameObject.name}'. Triggering level victory!");

                if (victorySFX != null)
                {
                    AudioSource.PlayClipAtPoint(victorySFX, transform.position);
                }

                GameManager.Instance.TriggerVictory();
            }
            else
            {
                Debug.LogWarning("[ExitDoor] Player reached unlocked door, but no GameManager was found to register victory.");
            }
        }
        #endregion

        #region Collision Handling
        /// <summary>
        /// Trigger collision callback (recommended setup: isTrigger = true).
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerContact(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandlePlayerContact(other.gameObject);
        }

        /// <summary>
        /// Solid collision callback (fallback if isTrigger = false).
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerContact(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandlePlayerContact(collision.gameObject);
        }

        /// <summary>
        /// Identifies whether the touching GameObject is the player and attempts level victory.
        /// </summary>
        private void HandlePlayerContact(GameObject target)
        {
            // If already completed or door is locked, do not process victory.
            if (!isUnlocked || hasTriggeredVictory)
            {
                return;
            }

            // Identify the player via the PlayerController component on the target or its parent.
            PlayerController player = target.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                TryCompleteLevel();
            }
        }
        #endregion

        #region Visual Helpers
        /// <summary>
        /// Updates the SpriteRenderer tint color and sprite according to current locked/unlocked state.
        /// </summary>
        private void UpdateVisualState()
        {
            if (doorRenderer == null)
            {
                return;
            }

            if (isUnlocked)
            {
                doorRenderer.color = unlockedColor;
                if (unlockedSprite != null)
                {
                    doorRenderer.sprite = unlockedSprite;
                }
            }
            else
            {
                doorRenderer.color = lockedColor;
                if (lockedSprite != null)
                {
                    doorRenderer.sprite = lockedSprite;
                }
            }
        }
        #endregion
    }
}
