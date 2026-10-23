using UnityEngine;
using UnityEngine.UI;
using GlitchRun.Abilities;

namespace GlitchRun.UI
{
    /// <summary>
    /// Displays GlitchAbility status, countdown timers, and recharge progress on the gameplay HUD.
    /// Uses Unity's built-in UI (Text and Slider) with zero external dependencies.
    /// Handles missing or destroyed references safely with throttled scene discovery.
    /// Optimizes string formatting and UI updates to minimize per-frame allocations.
    /// </summary>
    public class GlitchHUD : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Ability Reference")]
        [Tooltip("Optional reference to GlitchAbility. Auto-detected in the scene if left unassigned.")]
        [SerializeField] private GlitchAbility glitchAbility;

        [Header("UI Display Components (Optional)")]
        [Tooltip("Text component displaying current state ('READY [E]', 'GLITCH ACTIVE', 'RECHARGING').")]
        [SerializeField] private Text statusText;

        [Tooltip("Text component displaying numeric countdown in seconds.")]
        [SerializeField] private Text timerText;

        [Tooltip("Slider visualizing active or cooldown progress (0 to 1).")]
        [SerializeField] private Slider progressSlider;

        [Header("Status Labels")]
        [SerializeField] private string readyStatusString = "GLITCH READY [E]";
        [SerializeField] private string activeStatusString = "SIMULATION GLITCHED";
        [SerializeField] private string cooldownStatusString = "SYSTEM RECHARGING...";

        [Header("Status Colors (Optional)")]
        [SerializeField] private Color readyColor = new Color(0.2f, 1f, 0.4f, 1f);
        [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 1f, 1f);
        [SerializeField] private Color cooldownColor = new Color(1f, 0.4f, 0.3f, 1f);
        #endregion

        #region Internal State
        private enum HUDState { Unassigned, Ready, Active, Cooldown }
        private HUDState lastState = HUDState.Unassigned;
        private int lastDisplayedTenths = -1;
        private float currentActiveDuration = 0f;

        // Throttled discovery settings
        private const float DiscoveryRetryInterval = 0.5f;
        private float nextDiscoveryTime = 0f;
        #endregion

        #region Public API
        /// <summary>
        /// Public access or manual assignment for GlitchAbility reference.
        /// Resets HUD state when reassigned.
        /// </summary>
        public GlitchAbility Ability
        {
            get => glitchAbility;
            set
            {
                if (glitchAbility != value)
                {
                    glitchAbility = value;
                    ResetCachedState();
                }
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
            }

            if (glitchAbility == null)
            {
                glitchAbility = FindFirstObjectByType<GlitchAbility>();
            }
        }

        private void OnEnable()
        {
            ResetCachedState();
            nextDiscoveryTime = 0f;
        }

        private void Update()
        {
            // Throttled fallback discovery if GlitchAbility is missing or was destroyed at runtime.
            if (glitchAbility == null)
            {
                if (Time.unscaledTime >= nextDiscoveryTime)
                {
                    nextDiscoveryTime = Time.unscaledTime + DiscoveryRetryInterval;
                    glitchAbility = FindFirstObjectByType<GlitchAbility>();

                    if (glitchAbility != null)
                    {
                        ResetCachedState();
                    }
                }

                if (glitchAbility == null)
                {
                    DisplayUnavailable();
                    return;
                }
            }

            UpdateHUDDisplays();
        }
        #endregion

        #region UI Update Logic
        /// <summary>
        /// Reads GlitchAbility state and updates UI elements.
        /// Minimizes string allocations by caching the last displayed tenth of a second.
        /// </summary>
        private void UpdateHUDDisplays()
        {
            if (glitchAbility.IsGlitchActive)
            {
                float remainingTime = glitchAbility.RemainingGlitchTime;
                int currentTenths = Mathf.CeilToInt(remainingTime * 10f);

                // Dynamically track the initial/maximum active duration without hardcoding numbers.
                if (lastState != HUDState.Active)
                {
                    lastState = HUDState.Active;
                    currentActiveDuration = remainingTime > 0f ? remainingTime : 1f;
                    SetStatusDisplay(activeStatusString, activeColor);
                }
                else if (remainingTime > currentActiveDuration)
                {
                    currentActiveDuration = remainingTime;
                }

                if (currentTenths != lastDisplayedTenths)
                {
                    lastDisplayedTenths = currentTenths;
                    SetTimerText($"{remainingTime:F1}s");
                }

                if (progressSlider != null)
                {
                    // Active glitch: slider drains towards 0 as time runs out
                    float progress = currentActiveDuration > 0.0001f
                        ? Mathf.Clamp01(remainingTime / currentActiveDuration)
                        : 0f;
                    progressSlider.value = progress;
                }
            }
            else if (glitchAbility.IsOnCooldown)
            {
                currentActiveDuration = 0f;
                float remainingTime = glitchAbility.RemainingCooldownTime;
                float cooldownProgress = glitchAbility.CooldownProgressNormalized; // 1.0 (start) -> 0.0 (ready)
                int currentTenths = Mathf.CeilToInt(remainingTime * 10f);

                if (lastState != HUDState.Cooldown)
                {
                    lastState = HUDState.Cooldown;
                    SetStatusDisplay(cooldownStatusString, cooldownColor);
                }

                if (currentTenths != lastDisplayedTenths)
                {
                    lastDisplayedTenths = currentTenths;
                    SetTimerText($"{remainingTime:F1}s");
                }

                if (progressSlider != null)
                {
                    // Cooldown: slider fills from 0 to 1 as ability recharges
                    progressSlider.value = Mathf.Clamp01(1f - cooldownProgress);
                }
            }
            else // Ready
            {
                currentActiveDuration = 0f;

                if (lastState != HUDState.Ready)
                {
                    lastState = HUDState.Ready;
                    lastDisplayedTenths = -1;
                    SetStatusDisplay(readyStatusString, readyColor);
                    SetTimerText(string.Empty);

                    if (progressSlider != null)
                    {
                        progressSlider.value = 1f;
                    }
                }
            }
        }

        private void DisplayUnavailable()
        {
            if (lastState != HUDState.Unassigned)
            {
                lastState = HUDState.Unassigned;
                currentActiveDuration = 0f;
                SetStatusDisplay("NO GLITCH MODULE", Color.gray);
                SetTimerText(string.Empty);

                if (progressSlider != null)
                {
                    progressSlider.value = 0f;
                }
            }
        }

        private void ResetCachedState()
        {
            lastState = HUDState.Unassigned;
            lastDisplayedTenths = -1;
            currentActiveDuration = 0f;
        }

        private void SetStatusDisplay(string text, Color color)
        {
            if (statusText != null)
            {
                statusText.text = text;
                statusText.color = color;
            }
        }

        private void SetTimerText(string text)
        {
            if (timerText != null)
            {
                timerText.text = text;
            }
        }
        #endregion
    }
}
