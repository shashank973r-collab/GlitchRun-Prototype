using UnityEngine;
using GlitchRun.Player;
using GlitchRun.Core;

namespace GlitchRun.Hazards
{
    /// <summary>
    /// Holographic security barrier that completely blocks normal movement,
    /// but can be passed through only while the player's Glitch Phase ability is active.
    /// </summary>
    public class GlitchBarrier : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Collider2D barrierCollider;
        [SerializeField] private SpriteRenderer barrierRenderer;

        [Header("Visual Styling")]
        [SerializeField] private Color normalColor = new Color(0.85f, 0.1f, 0.95f, 0.85f); // Neon Magenta
        [SerializeField] private Color phasedColor = new Color(0.1f, 1f, 0.85f, 0.25f);  // Cyan ghost phase
        [SerializeField] private float pulseSpeed = 4f;

        private void Awake()
        {
            if (barrierCollider == null) barrierCollider = GetComponent<Collider2D>();
            if (barrierRenderer == null) barrierRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (barrierRenderer == null) return;

            // Check if player in scene is actively glitching
            bool isPlayerGlitching = false;
            if (GameManager.Instance != null && GameManager.Instance.ActivePlayer != null)
            {
                var glitch = GameManager.Instance.ActivePlayer.Glitch;
                if (glitch != null && glitch.IsGlitching)
                {
                    isPlayerGlitching = true;
                }
            }

            if (isPlayerGlitching)
            {
                barrierRenderer.color = phasedColor;
                if (barrierCollider != null) barrierCollider.isTrigger = true;
            }
            else
            {
                // Pulsate magenta glow
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * pulseSpeed);
                barrierRenderer.color = new Color(normalColor.r, normalColor.g, normalColor.b, normalColor.a * pulse);
                if (barrierCollider != null) barrierCollider.isTrigger = false;
            }
        }
    }
}
