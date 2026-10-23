using UnityEngine;
using GlitchRun.Player;
using GlitchRun.Core;

namespace GlitchRun.Hazards
{
    /// <summary>
    /// Dedicated holographic Glitch Barrier (Phase Gate).
    /// Spans floor to above max jump height (unjumpable by design).
    /// Inflicts damage when touched normally, but allows harmless passage when Glitch Phase (Shift) is active.
    /// Integrates with GlitchAbility's grace window and safe-ejection logic to guarantee player never gets trapped or instakilled.
    /// </summary>
    public class GlitchBarrier : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Collider2D barrierCollider;
        [SerializeField] private SpriteRenderer barrierRenderer;

        [Header("Visual Styling")]
        [SerializeField] private Color normalColor = new Color(0.85f, 0.1f, 0.95f, 0.85f);  // Neon Magenta
        [SerializeField] private Color phasedColor = new Color(0.1f, 1.0f, 0.85f, 0.35f);  // Cyan ghost phase
        [SerializeField] private float pulseSpeed = 4f;

        public Collider2D Collider => barrierCollider;

        private void Awake()
        {
            if (barrierCollider == null) barrierCollider = GetComponent<Collider2D>();
            if (barrierRenderer == null) barrierRenderer = GetComponent<SpriteRenderer>();

            if (barrierCollider != null)
            {
                barrierCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (barrierRenderer == null) return;

            bool isPlayerPhasing = false;
            if (GameManager.Instance != null && GameManager.Instance.ActivePlayer != null)
            {
                var glitch = GameManager.Instance.ActivePlayer.Glitch;
                if (glitch != null && (glitch.IsGlitching || glitch.IsGraceActiveFor(gameObject)))
                {
                    isPlayerPhasing = true;
                }
            }

            if (isPlayerPhasing)
            {
                // Soft glowing cyan hologram
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * (pulseSpeed * 2f));
                barrierRenderer.color = new Color(phasedColor.r, phasedColor.g, phasedColor.b, phasedColor.a * pulse);
            }
            else
            {
                // Pulsate neon magenta security field
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * pulseSpeed);
                barrierRenderer.color = new Color(normalColor.r, normalColor.g, normalColor.b, normalColor.a * pulse);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EvaluatePlayerInteraction(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            EvaluatePlayerInteraction(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null && player.Glitch != null && barrierCollider != null)
            {
                player.Glitch.UnregisterBarrierOverlap(barrierCollider);
            }
        }

        private void EvaluatePlayerInteraction(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            var glitch = player.Glitch;
            if (glitch != null && (glitch.IsGlitching || glitch.IsGraceActiveFor(gameObject)))
            {
                // Safe passage while phasing or in post-phase grace window
                if (barrierCollider != null)
                {
                    glitch.RegisterBarrierOverlap(barrierCollider);
                }
                return;
            }

            // Normal contact without phase: apply single damage event (respecting player invulnerability cooldown)
            player.TryDamage("Glitch Barrier");
        }

        private void OnDisable()
        {
            // Ensure no lingering references in active player's ability
            if (GameManager.Instance != null && GameManager.Instance.ActivePlayer != null)
            {
                var glitch = GameManager.Instance.ActivePlayer.Glitch;
                if (glitch != null && barrierCollider != null)
                {
                    glitch.UnregisterBarrierOverlap(barrierCollider);
                }
            }
        }
    }
}
