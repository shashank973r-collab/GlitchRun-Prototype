using System;
using UnityEngine;
using GlitchRun.Audio;
// InputSystem package always available (activeInputHandler = 2, Both mode).
using UnityEngine.InputSystem;

namespace GlitchRun.Player
{
    /// <summary>
    /// Handles the player's signature Glitch Phase Ability:
    /// Enables phasing through holographic Glitch Barriers for ~2.0 seconds with safe-exit collision verification.
    /// Note: Does not provide immunity to lethal lasers or physical hazards.
    /// </summary>
    public class GlitchAbility : MonoBehaviour
    {
        [Header("Ability Timers")]
        [SerializeField] private float glitchDuration = 2.0f;
        [SerializeField] private float glitchCooldown = 3.5f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color glitchColor1 = new Color(0.0f, 1.0f, 0.85f, 0.85f); // Cyan
        [SerializeField] private Color glitchColor2 = new Color(0.9f, 0.1f, 0.95f, 0.75f); // Neon Magenta
        [SerializeField] private Color glitchColor3 = new Color(0.1f, 1.0f, 0.5f, 0.85f);  // Acid Green

        private Collider2D playerCollider;
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

        public float NormalizedCooldown => glitchCooldown > 0f ? Mathf.Clamp01(cooldownTimer / glitchCooldown) : 0f;
        public float NormalizedActiveTime => glitchDuration > 0f ? Mathf.Clamp01(activeTimer / glitchDuration) : 0f;

        public event Action OnGlitchStarted;
        public event Action OnGlitchEnded;

        private void Awake()
        {
            playerCollider = GetComponent<Collider2D>();
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
            if (IsGlitching)
            {
                activeTimer -= Time.deltaTime;
                UpdateGlitchVisuals();

                if (activeTimer <= 0f)
                {
                    TryEndGlitchWithSafeExit();
                }
            }
            else if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer < 0f) cooldownTimer = 0f;
            }

            if (CheckGlitchInput())
            {
                TryActivateGlitch();
            }
        }

        private bool CheckGlitchInput()
        {
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

            // Legacy fallback
            if (Keyboard.current == null && UnityEngine.Input.GetKeyDown(KeyCode.LeftShift))
                return true;

            return false;
        }

        public bool TryActivateGlitch()
        {
            if (!CanGlitch) return false;

            IsGlitching = true;
            activeTimer = glitchDuration;
            cooldownTimer = glitchCooldown;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("glitch_on");
            }

            OnGlitchStarted?.Invoke();
            return true;
        }

        private void TryEndGlitchWithSafeExit()
        {
            // Safe-exit verification: Ensure player is not embedded inside a solid barrier
            if (playerCollider != null)
            {
                Collider2D[] overlapping = new Collider2D[4];
                ContactFilter2D filter = new ContactFilter2D();
                filter.useTriggers = false;
                int count = playerCollider.Overlap(filter, overlapping);

                for (int i = 0; i < count; i++)
                {
                    if (overlapping[i] != null && overlapping[i].gameObject != gameObject)
                    {
                        // Overlapping a solid obstacle; extend phase by 0.1s and nudge player slightly
                        activeTimer = 0.1f;
                        transform.position += Vector3.right * 0.15f;
                        return;
                    }
                }
            }

            EndGlitch();
        }

        private void EndGlitch()
        {
            IsGlitching = false;
            activeTimer = 0f;

            if (targetRenderer != null)
            {
                targetRenderer.color = originalColor;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("glitch_off");
            }

            OnGlitchEnded?.Invoke();
        }

        private void UpdateGlitchVisuals()
        {
            if (targetRenderer == null) return;

            flickerTimer += Time.deltaTime;
            if (flickerTimer >= 0.05f)
            {
                flickerTimer = 0f;
                flickerIndex = (flickerIndex + 1) % 4;

                switch (flickerIndex)
                {
                    case 0: targetRenderer.color = glitchColor1; break;
                    case 1: targetRenderer.color = glitchColor2; break;
                    case 2: targetRenderer.color = glitchColor3; break;
                    case 3: targetRenderer.color = new Color(1f, 1f, 1f, 0.4f); break;
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

        private void OnDisable()
        {
            ResetAbility();
        }
    }
}
