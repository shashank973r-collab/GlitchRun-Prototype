using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GlitchRun.Player
{
    /// <summary>
    /// Handles the player's signature Glitch Ability:
    /// Briefly phases through red laser hazards and digital defenses, with visual distortion and cooldown.
    /// </summary>
    public class GlitchAbility : MonoBehaviour
    {
        [Header("Ability Timers")]
        [Tooltip("Duration in seconds that the glitch phase remains active.")]
        [SerializeField] private float glitchDuration = 2.0f;

        [Tooltip("Cooldown period in seconds before the glitch can be activated again.")]
        [SerializeField] private float glitchCooldown = 3.5f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color glitchColor1 = new Color(0.2f, 1f, 0.5f, 0.85f); // Pale neon green
        [SerializeField] private Color glitchColor2 = new Color(0f, 0.9f, 1f, 0.75f);  // Cyan
        [SerializeField] private Color glitchColor3 = new Color(0.9f, 0.2f, 1f, 0.75f); // Magenta glitch accent


        private float activeTimer = 0f;
        private float cooldownTimer = 0f;
        private Color originalColor = Color.white;
        private float flickerTimer = 0f;
        private int flickerIndex = 0;

        public bool IsGlitching { get; private set; } = false;
        public float CooldownTimer => cooldownTimer;
        public float GlitchDuration => glitchDuration;
        public float GlitchCooldown => glitchCooldown;
        public float RemainingActiveTime => Mathf.Max(0f, activeTimer);
        public bool CanGlitch => !IsGlitching && cooldownTimer <= 0f;

        /// <summary>
        /// Returns 1 when on full cooldown, 0 when ready to use.
        /// </summary>
        public float NormalizedCooldown => glitchCooldown > 0f ? Mathf.Clamp01(cooldownTimer / glitchCooldown) : 0f;

        /// <summary>
        /// Returns fraction of remaining active glitch duration (1 -> 0).
        /// </summary>
        public float NormalizedActiveTime => glitchDuration > 0f ? Mathf.Clamp01(activeTimer / glitchDuration) : 0f;

        public event Action OnGlitchStarted;
        public event Action OnGlitchEnded;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (targetRenderer != null)
            {
                originalColor = targetRenderer.color;
            }
        }

        private void Update()
        {
            // Update timers
            if (IsGlitching)
            {
                activeTimer -= Time.deltaTime;
                UpdateGlitchVisuals();

                if (activeTimer <= 0f)
                {
                    EndGlitch();
                }
            }
            else if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer < 0f) cooldownTimer = 0f;
            }

            // Input check (Shift key or Gamepad shoulder)
            if (CheckGlitchInput())
            {
                TryActivateGlitch();
            }
        }

        private bool CheckGlitchInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
            {
                return true;
            }

            var pad = Gamepad.current;
            if (pad != null && (pad.rightShoulder.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame))
            {
                return true;
            }
#endif
            return false;
        }

        public bool TryActivateGlitch()
        {
            if (!CanGlitch) return false;

            IsGlitching = true;
            activeTimer = glitchDuration;
            cooldownTimer = glitchCooldown;

            Debug.Log("[GlitchRun] GLITCH ABILITY ACTIVATED! Phasing through hazards for 2.0s.");
            OnGlitchStarted?.Invoke();
            return true;
        }

        private void EndGlitch()
        {
            IsGlitching = false;
            activeTimer = 0f;

            if (targetRenderer != null)
            {
                targetRenderer.color = originalColor;
            }

            Debug.Log("[GlitchRun] Glitch phase ended. Recharging system...");
            OnGlitchEnded?.Invoke();
        }

        private void UpdateGlitchVisuals()
        {
            if (targetRenderer == null) return;

            // Rapid digital color flicker
            flickerTimer += Time.deltaTime;
            if (flickerTimer >= 0.05f)
            {
                flickerTimer = 0f;
                flickerIndex = (flickerIndex + 1) % 4;

                switch (flickerIndex)
                {
                    case 0:
                        targetRenderer.color = glitchColor1;
                        break;
                    case 1:
                        targetRenderer.color = glitchColor2;
                        break;
                    case 2:
                        targetRenderer.color = glitchColor3;
                        break;
                    case 3:
                        targetRenderer.color = new Color(1f, 1f, 1f, 0.4f);
                        break;
                }
            }
        }

        public void ResetAbility()
        {
            if (IsGlitching)
            {
                EndGlitch();
            }
            cooldownTimer = 0f;
            activeTimer = 0f;
        }
    }
}
