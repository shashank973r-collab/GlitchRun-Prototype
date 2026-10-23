using System;
using System.Collections.Generic;
using UnityEngine;
using GlitchRun.Audio;
using UnityEngine.InputSystem;

namespace GlitchRun.Player
{
    /// <summary>
    /// Handles the player's signature Glitch Phase Ability:
    /// Enables phasing through holographic Glitch Barriers and phaseable lasers for 2.0 seconds.
    /// Features safe-exit ejection, 0.25s grace window, and strict cleanup on scene events.
    /// Does NOT provide immunity to standard physical hazards or unphaseable lasers.
    /// </summary>
    public class GlitchAbility : MonoBehaviour
    {
        [Header("Ability Timers")]
        [SerializeField] private float glitchDuration = 2.0f;
        [SerializeField] private float glitchCooldown = 3.5f;
        [SerializeField] private float graceWindowDuration = 0.25f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color glitchColor1 = new Color(0.0f, 1.0f, 0.85f, 0.90f); // Bright Cyan
        [SerializeField] private Color glitchColor2 = new Color(0.9f, 0.1f, 0.95f, 0.85f); // Neon Magenta
        [SerializeField] private Color glitchColor3 = new Color(0.2f, 0.8f, 1.0f, 0.95f);  // Hologram Blue
        [SerializeField] private Color glitchColor4 = new Color(1.0f, 1.0f, 1.0f, 0.70f);  // White Flash

        private Collider2D playerCollider;
        private Rigidbody2D rb;
        private float activeTimer = 0f;
        private float cooldownTimer = 0f;
        private float graceTimer = 0f;
        private Color originalColor = Color.white;
        private float flickerTimer = 0f;
        private int flickerIndex = 0;

        // Tracks barriers overlapping the player during glitch phase
        private readonly HashSet<Collider2D> activeOverlappingBarriers = new HashSet<Collider2D>();

        public bool IsGlitching { get; private set; } = false;
        public bool IsPhasing => IsGlitching;
        public bool IsGraceActive => graceTimer > 0f;

        public float CooldownTimer => cooldownTimer;
        public float GlitchDuration => glitchDuration;
        public float GlitchCooldown => glitchCooldown;
        public float RemainingActiveTime => Mathf.Max(0f, activeTimer);
        public float RemainingGraceTime => Mathf.Max(0f, graceTimer);
        public bool CanGlitch => !IsGlitching && cooldownTimer <= 0f;

        public float NormalizedCooldown => glitchCooldown > 0f ? Mathf.Clamp01(cooldownTimer / glitchCooldown) : 0f;
        public float NormalizedActiveTime => glitchDuration > 0f ? Mathf.Clamp01(activeTimer / glitchDuration) : 0f;

        public event Action OnGlitchStarted;
        public event Action OnGlitchEnded;

        private void Awake()
        {
            playerCollider = GetComponent<Collider2D>();
            rb = GetComponent<Rigidbody2D>();

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
                    BeginGraceOrEndGlitch();
                }
            }
            else if (graceTimer > 0f)
            {
                graceTimer -= Time.deltaTime;
                UpdateGraceVisuals();

                if (graceTimer <= 0f)
                {
                    ResolveGraceExpiry();
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
            graceTimer = 0f;
            activeOverlappingBarriers.Clear();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("glitch_on");
            }

            OnGlitchStarted?.Invoke();
            return true;
        }

        public void RegisterBarrierOverlap(Collider2D barrierCollider)
        {
            if (barrierCollider != null)
            {
                activeOverlappingBarriers.Add(barrierCollider);
            }
        }

        public void UnregisterBarrierOverlap(Collider2D barrierCollider)
        {
            if (barrierCollider != null)
            {
                activeOverlappingBarriers.Remove(barrierCollider);
            }
        }

        public bool IsGraceActiveFor(GameObject barrierObj)
        {
            if (!IsGraceActive) return false;
            if (barrierObj == null) return false;

            var col = barrierObj.GetComponent<Collider2D>();
            if (col != null && activeOverlappingBarriers.Contains(col))
            {
                return true;
            }

            return activeOverlappingBarriers.Count > 0;
        }

        private void BeginGraceOrEndGlitch()
        {
            activeTimer = 0f;
            // Prune null or disabled colliders
            activeOverlappingBarriers.RemoveWhere(c => c == null || !c.enabled);

            // Verify if player is actively overlapping any registered barrier
            bool isOverlappingBarrier = false;
            if (playerCollider != null)
            {
                foreach (var barrier in activeOverlappingBarriers)
                {
                    if (barrier != null && playerCollider.IsTouching(barrier))
                    {
                        isOverlappingBarrier = true;
                        break;
                    }
                }
            }

            if (isOverlappingBarrier)
            {
                // Enter grace window for the overlapping barrier
                IsGlitching = false;
                graceTimer = graceWindowDuration;
            }
            else
            {
                EndGlitch();
            }
        }

        private void ResolveGraceExpiry()
        {
            graceTimer = 0f;

            // If still overlapping a barrier at grace expiry, push player out safely to the nearer side
            if (playerCollider != null)
            {
                activeOverlappingBarriers.RemoveWhere(c => c == null || !c.enabled);

                foreach (var barrier in activeOverlappingBarriers)
                {
                    if (barrier != null && playerCollider.IsTouching(barrier))
                    {
                        PushOutOfBarrier(barrier);
                    }
                }
            }

            EndGlitch();
        }

        public void PushOutOfBarrier(Collider2D barrier)
        {
            if (barrier == null) return;

            Bounds barrierBounds = barrier.bounds;
            float playerWidth = playerCollider != null ? playerCollider.bounds.size.x : 0.85f;
            float playerHalfWidth = playerWidth * 0.5f;

            float currentX = transform.position.x;
            float leftEjectX = barrierBounds.min.x - playerHalfWidth - 0.15f;
            float rightEjectX = barrierBounds.max.x + playerHalfWidth + 0.15f;

            float distLeft = Mathf.Abs(currentX - leftEjectX);
            float distRight = Mathf.Abs(currentX - rightEjectX);

            float targetX = (distLeft <= distRight) ? leftEjectX : rightEjectX;

            Vector3 newPos = transform.position;
            newPos.x = targetX;
            transform.position = newPos;

            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
#else
                rb.velocity = new Vector2(0f, rb.velocity.y);
#endif
            }
        }

        private void EndGlitch()
        {
            IsGlitching = false;
            activeTimer = 0f;
            graceTimer = 0f;
            activeOverlappingBarriers.Clear();

            RestoreOriginalState();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("glitch_off");
            }

            OnGlitchEnded?.Invoke();
        }

        private void RestoreOriginalState()
        {
            if (targetRenderer != null)
            {
                targetRenderer.color = originalColor;
            }
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
                    case 3: targetRenderer.color = glitchColor4; break;
                }
            }
        }

        private void UpdateGraceVisuals()
        {
            if (targetRenderer == null) return;

            // Rapid cyan blinking during grace window
            float alpha = Mathf.PingPong(Time.time * 20f, 1f);
            targetRenderer.color = new Color(0f, 1f, 0.85f, 0.5f + 0.5f * alpha);
        }

        public void ResetAbility()
        {
            IsGlitching = false;
            cooldownTimer = 0f;
            activeTimer = 0f;
            graceTimer = 0f;
            activeOverlappingBarriers.Clear();
            RestoreOriginalState();
        }

        private void OnDisable()
        {
            ResetAbility();
        }
    }
}
