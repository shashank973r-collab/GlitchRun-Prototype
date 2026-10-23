using System.Collections;
using UnityEngine;
using GlitchRun.Abilities;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Represents a phase-shifted platform in GLITCH RUN.
    /// By default, the platform is non-solid and hidden (or faint).
    /// When the player activates GlitchAbility, it materializes into a solid, fully visible platform.
    /// Automatically retries locating GlitchAbility if missing on load, monitors for lost references
    /// if the ability is destroyed at runtime, and safely cleans up event subscriptions.
    /// </summary>
    public class GlitchPlatform : MonoBehaviour
    {
        #region Inspector Settings
        [Header("References")]
        [Tooltip("Optional reference to GlitchAbility. If unassigned, automatically finds it in the scene.")]
        [SerializeField] private GlitchAbility glitchAbility;

        [Tooltip("Optional Collider2D for platform collision. Auto-detected on this object if unassigned.")]
        [SerializeField] private Collider2D platformCollider;

        [Tooltip("Optional SpriteRenderer for visual representation. Auto-detected on this object if unassigned.")]
        [SerializeField] private SpriteRenderer platformRenderer;

        [Header("Visual & Phasing Settings")]
        [Tooltip("Tint color applied when the platform is phased out / normal state (low alpha recommended).")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 1f, 0.2f);

        [Tooltip("Tint color applied when the platform is solidified during an active Glitch.")]
        [SerializeField] private Color glitchActiveColor = new Color(0.2f, 1f, 0.9f, 1f);

        [Tooltip("If true, completely disables the SpriteRenderer when in the normal hidden state.")]
        [SerializeField] private bool hideRendererCompletely = false;

        [Tooltip("If true, inverts the behavior: solid normally, phased-out/pass-through during glitch.")]
        [SerializeField] private bool invertPhase = false;

        [Header("Retry Configuration")]
        [Tooltip("Interval in seconds to retry locating GlitchAbility if not found on initial load or lost at runtime.")]
        [SerializeField] private float retryInterval = 0.5f;
        #endregion

        #region Internal State
        private bool isSubscribed = false;
        private bool isSolid = false;
        private Coroutine retryRoutine = null;
        #endregion

        #region Public Properties
        /// <summary>
        /// True if the platform is currently solid and can be stood upon.
        /// </summary>
        public bool IsSolid => isSolid;

        /// <summary>
        /// True if the platform is inverted (solid when normal, non-solid during glitch).
        /// </summary>
        public bool InvertPhase
        {
            get => invertPhase;
            set
            {
                invertPhase = value;
                UpdatePlatformState(glitchAbility != null && glitchAbility.IsGlitchActive);
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Auto-detect attached components if left blank.
            if (platformCollider == null)
            {
                platformCollider = GetComponent<Collider2D>();
            }

            if (platformRenderer == null)
            {
                platformRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnValidate()
        {
            if (retryInterval < 0.1f)
            {
                retryInterval = 0.1f;
            }

            // Sync visual preview in the Editor Scene view outside Play Mode.
            if (!Application.isPlaying)
            {
                if (platformCollider == null) platformCollider = GetComponent<Collider2D>();
                if (platformRenderer == null) platformRenderer = GetComponent<SpriteRenderer>();
                UpdatePlatformState(isGlitchActive: false);
            }
        }

        private void OnEnable()
        {
            SubscribeToGlitchEvents();
            SyncCurrentState();
        }

        private void Start()
        {
            // Fallback check in Start if GlitchAbility awakened after this object's OnEnable.
            if (!isSubscribed)
            {
                SubscribeToGlitchEvents();
            }

            SyncCurrentState();
        }

        private void Update()
        {
            // Detect if the subscribed GlitchAbility reference was destroyed or unloaded while enabled.
            // Performs a simple null-check on the cached reference without doing a costly per-frame scene search.
            if (isSubscribed && glitchAbility == null)
            {
                HandleAbilityLost();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromGlitchEvents();
            // Ensure platform returns to its inactive state when disabled.
            UpdatePlatformState(isGlitchActive: false);
        }

        private void OnDestroy()
        {
            UnsubscribeFromGlitchEvents();
        }
        #endregion

        #region Event Subscriptions & Auto-Retry
        /// <summary>
        /// Safely subscribes to GlitchAbility events using Unity 6-compatible lookups.
        /// If GlitchAbility is not found, starts a periodic retry coroutine while enabled.
        /// Avoids duplicate subscriptions and never throws null-reference exceptions.
        /// </summary>
        private void SubscribeToGlitchEvents()
        {
            if (isSubscribed)
            {
                return;
            }

            // Find GlitchAbility in scene if unassigned.
            if (glitchAbility == null)
            {
                glitchAbility = FindFirstObjectByType<GlitchAbility>();
            }

            if (glitchAbility != null)
            {
                glitchAbility.OnGlitchStarted += HandleGlitchStarted;
                glitchAbility.OnGlitchEnded += HandleGlitchEnded;
                isSubscribed = true;

                StopRetryRoutine();
                SyncCurrentState();
            }
            else if (retryRoutine == null && isActiveAndEnabled)
            {
                retryRoutine = StartCoroutine(RetrySubscribeRoutine());
            }
        }

        /// <summary>
        /// Periodically checks the scene for GlitchAbility in unscaled realtime until found or disabled.
        /// Polls only at the specified interval rather than performing costly per-frame searches.
        /// </summary>
        private IEnumerator RetrySubscribeRoutine()
        {
            while (!isSubscribed)
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, retryInterval));

                if (glitchAbility == null)
                {
                    glitchAbility = FindFirstObjectByType<GlitchAbility>();
                }

                if (glitchAbility != null)
                {
                    glitchAbility.OnGlitchStarted += HandleGlitchStarted;
                    glitchAbility.OnGlitchEnded += HandleGlitchEnded;
                    isSubscribed = true;
                    SyncCurrentState();
                    break;
                }
            }

            retryRoutine = null;
        }

        /// <summary>
        /// Halts any running retry coroutine safely.
        /// </summary>
        private void StopRetryRoutine()
        {
            if (retryRoutine != null)
            {
                StopCoroutine(retryRoutine);
                retryRoutine = null;
            }
        }

        /// <summary>
        /// Safely unsubscribes from GlitchAbility events and cancels any pending retry routine.
        /// Handles cases where the ability object was destroyed or made unavailable.
        /// </summary>
        private void UnsubscribeFromGlitchEvents()
        {
            StopRetryRoutine();

            if (!isSubscribed)
            {
                return;
            }

            if (glitchAbility != null)
            {
                glitchAbility.OnGlitchStarted -= HandleGlitchStarted;
                glitchAbility.OnGlitchEnded -= HandleGlitchEnded;
            }

            isSubscribed = false;
        }

        /// <summary>
        /// Called when an existing GlitchAbility reference is destroyed or lost at runtime while the platform is enabled.
        /// Restores the platform to its normal state using invertPhase, clears subscription, and resumes polling for a replacement.
        /// </summary>
        private void HandleAbilityLost()
        {
            Debug.LogWarning($"[GlitchPlatform] '{gameObject.name}' lost its GlitchAbility reference. Resetting to normal state and retrying discovery.");

            isSubscribed = false;
            glitchAbility = null;

            // Restore platform to normal (non-glitched) state according to invertPhase setting.
            UpdatePlatformState(isGlitchActive: false);

            // Resume retrying for a replacement GlitchAbility (e.g. respawned player).
            if (retryRoutine == null && isActiveAndEnabled)
            {
                retryRoutine = StartCoroutine(RetrySubscribeRoutine());
            }
        }

        /// <summary>
        /// Synchronizes the initial solid/visual state based on whether a glitch is currently active.
        /// </summary>
        private void SyncCurrentState()
        {
            bool glitchActive = glitchAbility != null && glitchAbility.IsGlitchActive;
            UpdatePlatformState(glitchActive);
        }
        #endregion

        #region Event Callbacks
        private void HandleGlitchStarted()
        {
            UpdatePlatformState(isGlitchActive: true);
        }

        private void HandleGlitchEnded()
        {
            UpdatePlatformState(isGlitchActive: false);
        }
        #endregion

        #region State & Visual Updates
        /// <summary>
        /// Sets platform solidity and visual appearance based on whether glitch mode is active.
        /// </summary>
        /// <param name="isGlitchActive">True if simulation is currently glitched.</param>
        private void UpdatePlatformState(bool isGlitchActive)
        {
            // Standard: solid only when glitch is active.
            // Inverted: solid only when glitch is NOT active.
            isSolid = invertPhase ? !isGlitchActive : isGlitchActive;

            // Update collider solidity so player can or cannot stand on the platform.
            if (platformCollider != null)
            {
                platformCollider.enabled = isSolid;
            }

            // Update visual rendering.
            if (platformRenderer != null)
            {
                if (hideRendererCompletely && !isSolid)
                {
                    platformRenderer.enabled = false;
                }
                else
                {
                    platformRenderer.enabled = true;
                    platformRenderer.color = isSolid ? glitchActiveColor : normalColor;
                }
            }
        }
        #endregion
    }
}
