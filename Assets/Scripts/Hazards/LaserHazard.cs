using UnityEngine;
using GlitchRun.Player;

namespace GlitchRun.Hazards
{
    /// <summary>
    /// Lethal red laser barrier.
    /// Emits a pulsating laser beam. Cycling lasers periodically deactivate to allow timed passage.
    /// Standard damage lasers hurt the player even if Glitch Phase is active (unless canBeGlitched is explicitly set).
    /// </summary>
    public class LaserHazard : MonoBehaviour
    {
        [Header("Laser Mode")]
        [SerializeField] private bool isCycling = false;
        [SerializeField] private float activeTime = 2.5f;
        [SerializeField] private float inactiveTime = 1.5f;
        [SerializeField] private float initialDelay = 0f;
        [SerializeField] private bool canBeGlitched = false;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer laserRenderer;
        [SerializeField] private Collider2D laserCollider;
        [SerializeField] private Color activeColor = new Color(1f, 0.1f, 0.25f, 0.95f);   // Bright Neon Red
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0.1f, 0.35f);  // Dim / Warming up

        private float cycleTimer = 0f;
        private bool isLaserActive = true;
        private float pulseSpeed = 8f;

        public bool IsLaserActive => isLaserActive;
        public bool CanBeGlitched
        {
            get => canBeGlitched;
            set => canBeGlitched = value;
        }

        public Collider2D Collider => laserCollider;

        private void Awake()
        {
            if (laserRenderer == null) laserRenderer = GetComponent<SpriteRenderer>();
            if (laserCollider == null) laserCollider = GetComponent<Collider2D>();

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

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (laserRenderer == null) return;

            if (isLaserActive)
            {
                laserRenderer.enabled = true;
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * pulseSpeed);
                laserRenderer.color = new Color(activeColor.r, activeColor.g, activeColor.b, activeColor.a * pulse);
            }
            else
            {
                laserRenderer.enabled = true;
                laserRenderer.color = warningColor;
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

        private void OnTriggerExit2D(Collider2D other)
        {
            if (canBeGlitched)
            {
                var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
                if (player != null && player.Glitch != null && laserCollider != null)
                {
                    player.Glitch.UnregisterBarrierOverlap(laserCollider);
                }
            }
        }

        private void EvaluatePlayerCollision(Collider2D other)
        {
            if (!isLaserActive) return;

            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                if (canBeGlitched && player.Glitch != null && (player.Glitch.IsGlitching || player.Glitch.IsGraceActiveFor(gameObject)))
                {
                    if (laserCollider != null)
                    {
                        player.Glitch.RegisterBarrierOverlap(laserCollider);
                    }
                    return;
                }

                player.TryDamage("Laser Hazard");
            }
        }

        private void OnDisable()
        {
            if (canBeGlitched && laserCollider != null && Core.GameManager.Instance != null && Core.GameManager.Instance.ActivePlayer != null)
            {
                var glitch = Core.GameManager.Instance.ActivePlayer.Glitch;
                if (glitch != null)
                {
                    glitch.UnregisterBarrierOverlap(laserCollider);
                }
            }
        }
    }
}
