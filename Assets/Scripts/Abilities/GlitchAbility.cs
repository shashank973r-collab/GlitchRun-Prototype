using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GlitchRun.Managers;

namespace GlitchRun.Abilities
{
    /// <summary>
    /// Manages the player's core "Glitch" mechanic in GLITCH RUN.
    /// Activated via the 'E' key (or programmatically), the glitch temporarily destabilizes
    /// the simulation for 3 seconds, followed by an 8-second cooldown.
    /// Communicates with hazards and phase-shift platforms purely through event dispatch.
    /// </summary>
    public class GlitchAbility : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Ability Timings")]
        [Tooltip("Active duration of the glitch in seconds.")]
        [SerializeField] private float glitchDuration = 3.0f;

        [Tooltip("Cooldown period in seconds before the glitch can be activated again.")]
        [SerializeField] private float glitchCooldown = 8.0f;
        #endregion

        #region Internal State
        private bool isGlitchActive = false;
        private bool isOnCooldown = false;
        private float remainingGlitchTime = 0f;
        private float remainingCooldownTime = 0f;

        private Coroutine activeRoutine = null;
        #endregion

        #region Public Properties (Contract API)
        /// <summary>
        /// True while the simulation is actively glitched.
        /// </summary>
        public bool IsGlitchActive => isGlitchActive;

        /// <summary>
        /// True while the ability is recharging after a glitch has completed.
        /// </summary>
        public bool IsOnCooldown => isOnCooldown;

        /// <summary>
        /// True if the ability is ready to be activated (not active, not on cooldown, and playing).
        /// </summary>
        public bool IsReady => !isGlitchActive && !isOnCooldown && (GameManager.Instance == null || GameManager.Instance.IsPlaying);

        /// <summary>
        /// Remaining time in seconds for the current active glitch state. Returns 0 if inactive.
        /// </summary>
        public float RemainingGlitchTime => Mathf.Max(0f, remainingGlitchTime);

        /// <summary>
        /// Remaining time in seconds before the ability can be activated again. Returns 0 if ready.
        /// </summary>
        public float RemainingCooldownTime => Mathf.Max(0f, remainingCooldownTime);

        /// <summary>
        /// Normalized cooldown progress: 1.0 immediately upon entering cooldown, decaying to 0.0 when fully ready.
        /// </summary>
        public float CooldownProgressNormalized
        {
            get
            {
                if (!isOnCooldown || glitchCooldown <= 0f)
                {
                    return 0f;
                }
                return Mathf.Clamp01(remainingCooldownTime / glitchCooldown);
            }
        }
        #endregion

        #region Events (Publishers for Hazards & Platforms)
        /// <summary>
        /// Broadcast when the glitch state initiates. Hazards, platforms, and VFX subscribe here.
        /// </summary>
        public event Action OnGlitchStarted;

        /// <summary>
        /// Broadcast when the glitch state concludes (either naturally or forced).
        /// </summary>
        public event Action OnGlitchEnded;

        /// <summary>
        /// Broadcast when the cooldown completes and the ability becomes ready again.
        /// </summary>
        public event Action OnCooldownFinished;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Clamp timings to non-negative values at runtime.
            if (glitchDuration < 0f) glitchDuration = 0f;
            if (glitchCooldown < 0f) glitchCooldown = 0f;
        }

        private void OnValidate()
        {
            // Clamp timings in the Unity Inspector.
            if (glitchDuration < 0f)
            {
                glitchDuration = 0f;
            }

            if (glitchCooldown < 0f)
            {
                glitchCooldown = 0f;
            }
        }

        private void Update()
        {
            ReadInput();
        }

        private void OnDisable()
        {
            // Cleanly end any active glitch state and stop coroutines if disabled.
            CleanupAbilityState();
        }

        private void OnDestroy()
        {
            // Safeguard cleanup if destroyed while active.
            CleanupAbilityState();
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// Listens for the 'E' key using the New Input System (Keyboard.current).
        /// </summary>
        private void ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                TryActivateGlitch();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to trigger the Glitch ability.
        /// Returns true if successfully activated; false if on cooldown, already active, or game is paused/ended.
        /// </summary>
        public bool TryActivateGlitch()
        {
            // Prevent activation if already active or recharging.
            if (isGlitchActive || isOnCooldown)
            {
                return false;
            }

            // Respect GameManager state: only allow activation during active gameplay.
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return false;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            isGlitchActive = true;
            remainingGlitchTime = glitchDuration;
            Debug.Log($"[GlitchAbility] Glitch activated! Duration: {glitchDuration}s.");
            OnGlitchStarted?.Invoke();

            activeRoutine = StartCoroutine(ActiveGlitchRoutine());
            return true;
        }

        /// <summary>
        /// Immediately forces the active glitch to end and transitions into the cooldown phase.
        /// Routes through EndGlitchAndStartCooldown to ensure uniform state transitions.
        /// </summary>
        public void DeactivateGlitchEarly()
        {
            if (isGlitchActive)
            {
                Debug.Log("[GlitchAbility] Glitch deactivated early by request.");
                EndGlitchAndStartCooldown();
            }
        }

        /// <summary>
        /// Instantly resets any active cooldown, making the glitch immediately ready for use.
        /// Fires OnCooldownFinished exactly once.
        /// </summary>
        public void ResetCooldown()
        {
            if (isOnCooldown)
            {
                if (activeRoutine != null)
                {
                    StopCoroutine(activeRoutine);
                    activeRoutine = null;
                }

                isOnCooldown = false;
                remainingCooldownTime = 0f;
                Debug.Log("[GlitchAbility] Cooldown reset. Ability ready.");
                OnCooldownFinished?.Invoke();
            }
        }
        #endregion

        #region State Transitions & Coroutines
        /// <summary>
        /// Central transition point when the glitch phase concludes.
        /// Guarantees OnGlitchEnded is dispatched exactly once before starting cooldown.
        /// </summary>
        private void EndGlitchAndStartCooldown()
        {
            if (!isGlitchActive)
            {
                return;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            isGlitchActive = false;
            remainingGlitchTime = 0f;
            Debug.Log("[GlitchAbility] Glitch active phase ended.");
            OnGlitchEnded?.Invoke();

            StartCooldown();
        }

        /// <summary>
        /// Central entry point for entering the cooldown state.
        /// Ensures single coroutine ownership and sets cooldown state flags.
        /// </summary>
        private void StartCooldown()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            isOnCooldown = true;
            remainingCooldownTime = glitchCooldown;
            Debug.Log($"[GlitchAbility] Cooldown started: {glitchCooldown}s.");

            activeRoutine = StartCoroutine(CooldownRoutine());
        }

        /// <summary>
        /// Counts down the active glitch duration in unscaled realtime.
        /// Yields one frame initially to guarantee StartCoroutine returns and assigns activeRoutine.
        /// </summary>
        private IEnumerator ActiveGlitchRoutine()
        {
            // Yield one frame initially so StartCoroutine returns and assigns activeRoutine before routine completes.
            yield return null;

            while (remainingGlitchTime > 0f)
            {
                remainingGlitchTime -= Time.unscaledDeltaTime;
                yield return null;
            }

            EndGlitchAndStartCooldown();
        }

        /// <summary>
        /// Counts down the cooldown duration in unscaled realtime until the ability is restored.
        /// Yields one frame initially to guarantee StartCoroutine returns and assigns activeRoutine.
        /// </summary>
        private IEnumerator CooldownRoutine()
        {
            // Yield one frame initially so StartCoroutine returns and assigns activeRoutine before routine completes.
            yield return null;

            while (remainingCooldownTime > 0f)
            {
                remainingCooldownTime -= Time.unscaledDeltaTime;
                yield return null;
            }

            remainingCooldownTime = 0f;
            isOnCooldown = false;
            activeRoutine = null;

            Debug.Log("[GlitchAbility] Cooldown complete. Ability ready.");
            OnCooldownFinished?.Invoke();
        }

        /// <summary>
        /// Safely tears down active coroutines and ensures OnGlitchEnded is dispatched if disabled mid-glitch.
        /// </summary>
        private void CleanupAbilityState()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            if (isGlitchActive)
            {
                isGlitchActive = false;
                remainingGlitchTime = 0f;
                OnGlitchEnded?.Invoke();
            }

            isOnCooldown = false;
            remainingCooldownTime = 0f;
        }
        #endregion
    }
}
