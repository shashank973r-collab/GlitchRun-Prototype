using UnityEngine;
using GlitchRun.Player;

namespace GlitchRun.Hazards
{
    /// <summary>
    /// Lethal red laser barrier. Can be phased through unharmed while Glitch Ability is active.
    /// Supports constant beams or pulsating timed cycles.
    /// </summary>
    public class LaserHazard : MonoBehaviour
    {
        [Header("Laser Mode")]
        [Tooltip("If true, the laser periodically turns on and off.")]
        [SerializeField] private bool isCycling = false;
        [SerializeField] private float activeTime = 2.5f;
        [SerializeField] private float inactiveTime = 1.5f;
        [SerializeField] private float initialDelay = 0f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer laserRenderer;
        [SerializeField] private Collider2D laserCollider;
        [SerializeField] private Color activeColor = new Color(1f, 0.1f, 0.25f, 0.95f);   // Bright Neon Red
        [SerializeField] private Color bypassedColor = new Color(0.1f, 1f, 0.6f, 0.6f); // Glitched Neon Green
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0.1f, 0.4f);  // Amber warming up

        private float cycleTimer = 0f;
        private bool isLaserActive = true;
        private float pulseSpeed = 8f;
        private float bypassFeedbackTimer = 0f;

        public bool IsLaserActive => isLaserActive;

        private void Awake()
        {
            if (laserRenderer == null)
            {
                laserRenderer = GetComponent<SpriteRenderer>();
            }
            if (laserCollider == null)
            {
                laserCollider = GetComponent<Collider2D>();
            }

            if (laserCollider != null)
            {
                laserCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            cycleTimer = -initialDelay;
            UpdateVisuals();
        }

        private void Update()
        {
            if (isCycling)
            {
                cycleTimer += Time.deltaTime;
                if (cycleTimer >= 0f)
                {
                    float cycleTotal = activeTime + inactiveTime;
                    float cyclePos = cycleTimer % cycleTotal;

                    bool shouldBeActive = cyclePos < activeTime;
                    if (shouldBeActive != isLaserActive)
                    {
                        isLaserActive = shouldBeActive;
                        if (laserCollider != null)
                        {
                            laserCollider.enabled = isLaserActive;
                        }
                    }
                }
            }

            // Laser pulsation / hum visual
            if (bypassFeedbackTimer > 0f)
            {
                bypassFeedbackTimer -= Time.deltaTime;
                if (laserRenderer != null)
                {
                    laserRenderer.color = bypassedColor;
                }
            }
            else
            {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (laserRenderer == null) return;

            if (isLaserActive)
            {
                laserRenderer.enabled = true;
                // Pulsate brightness slightly
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * pulseSpeed);
                laserRenderer.color = new Color(activeColor.r, activeColor.g, activeColor.b, activeColor.a * pulse);
            }
            else
            {
                // Off state: slight dim flicker or disabled
                laserRenderer.enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EvaluatePlayerCollision(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            EvaluatePlayerCollision(other);
        }

        private void EvaluatePlayerCollision(Collider2D other)
        {
            if (!isLaserActive) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                if (player.Glitch != null && player.Glitch.IsGlitching)
                {
                    // Player successfully phased through laser!
                    bypassFeedbackTimer = 0.2f;
                }
                else
                {
                    // Player hit laser while unphased
                    player.TryDamage("Laser Hazard");
                }
            }
        }
    }
}
