using UnityEngine;
using GlitchRun.Managers;
using GlitchRun.Player;
using GlitchRun.Abilities;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Represents a hazardous or lethal obstacle in GLITCH RUN (spikes, lasers, enemies).
    /// Eliminates the player upon physical contact.
    /// Can optionally freeze movement, physics, and lethality when the GlitchAbility is active.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Hazard Configuration")]
        [Tooltip("If true, contact with this hazard will eliminate the player.")]
        [SerializeField] private bool isLethal = true;

        [Tooltip("If true, this hazard freezes its motion and loses lethality during an active Glitch.")]
        [SerializeField] private bool canBeGlitchFrozen = true;

        [Header("Optional References")]
        [Tooltip("Optional reference to GlitchAbility. If left empty, will automatically find it in the scene.")]
        [SerializeField] private GlitchAbility glitchAbility;

        [Tooltip("Optional Rigidbody2D if this hazard is a moving platform, patrol enemy, or projectile.")]
        [SerializeField] private Rigidbody2D hazardRigidbody;

        [Tooltip("Optional SpriteRenderer to tint the hazard when frozen.")]
        [SerializeField] private SpriteRenderer hazardRenderer;

        [Tooltip("Optional Animator to pause when frozen.")]
        [SerializeField] private Animator hazardAnimator;

        [Header("Visual Indicators (Optional)")]
        [Tooltip("Normal tint color when the hazard is dangerous.")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Tint color applied when the hazard is frozen by a glitch.")]
        [SerializeField] private Color frozenColor = new Color(0.3f, 0.9f, 1f, 1f);
        #endregion

        #region Internal State
        private bool isFrozen = false;
        private bool isSubscribed = false;

        // Saved motion state for restoring physics after glitch ends
        private Vector2 savedLinearVelocity = Vector2.zero;
        private float savedAngularVelocity = 0f;
        private RigidbodyType2D savedBodyType = RigidbodyType2D.Dynamic;
        private float savedAnimatorSpeed = 1f;
        #endregion

        #region Public Properties (Contract API)
        /// <summary>
        /// True if the hazard is currently frozen by the glitch ability.
        /// </summary>
        public bool IsFrozen => isFrozen;

        /// <summary>
        /// True if contact with this hazard is currently lethal to the player.
        /// When frozen by a glitch, lethality is disabled.
        /// </summary>
        public bool IsLethal => isLethal && !isFrozen;

        /// <summary>
        /// Configures whether this hazard is lethal.
        /// </summary>
        public bool ConfiguredLethal
        {
            get => isLethal;
            set => isLethal = value;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Auto-detect attached components if not manually assigned in the Inspector.
            if (hazardRigidbody == null)
            {
                hazardRigidbody = GetComponent<Rigidbody2D>();
            }

            if (hazardRenderer == null)
            {
                hazardRenderer = GetComponent<SpriteRenderer>();
            }

            if (hazardAnimator == null)
            {
                hazardAnimator = GetComponent<Animator>();
            }

            if (hazardRenderer != null)
            {
                normalColor = hazardRenderer.color;
            }
        }

        private void OnEnable()
        {
            SubscribeToGlitchEvents();
        }

        private void Start()
        {
            // Ensure subscription if GlitchAbility wasn't ready during OnEnable.
            if (!isSubscribed)
            {
                SubscribeToGlitchEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromGlitchEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGlitchEvents();
        }
        #endregion

        #region Glitch Event Subscriptions
        /// <summary>
        /// Subscribes to GlitchAbility events safely without duplicate registrations.
        /// </summary>
        private void SubscribeToGlitchEvents()
        {
            if (isSubscribed)
            {
                return;
            }

            // Find GlitchAbility in scene if not assigned in Inspector.
            if (glitchAbility == null)
            {
                glitchAbility = FindFirstObjectByType<GlitchAbility>();
            }

            if (glitchAbility != null)
            {
                glitchAbility.OnGlitchStarted += FreezeHazard;
                glitchAbility.OnGlitchEnded += UnfreezeHazard;
                isSubscribed = true;

                // Sync state immediately if glitch is already active when enabled.
                if (glitchAbility.IsGlitchActive && canBeGlitchFrozen)
                {
                    FreezeHazard();
                }
            }
        }

        /// <summary>
        /// Safely unsubscribes from GlitchAbility events to prevent memory leaks and null callbacks.
        /// </summary>
        private void UnsubscribeFromGlitchEvents()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (glitchAbility != null)
            {
                glitchAbility.OnGlitchStarted -= FreezeHazard;
                glitchAbility.OnGlitchEnded -= UnfreezeHazard;
            }

            isSubscribed = false;
        }
        #endregion

        #region Freeze & Resume Logic
        /// <summary>
        /// Freezes hazard movement, pauses animations, and suppresses lethality during glitch.
        /// </summary>
        public void FreezeHazard()
        {
            if (!canBeGlitchFrozen || isFrozen)
            {
                return;
            }

            isFrozen = true;

            // Pause Rigidbody2D physics movement safely.
            if (hazardRigidbody != null)
            {
                savedLinearVelocity = hazardRigidbody.linearVelocity;
                savedAngularVelocity = hazardRigidbody.angularVelocity;
                savedBodyType = hazardRigidbody.bodyType;

                hazardRigidbody.linearVelocity = Vector2.zero;
                hazardRigidbody.angularVelocity = 0f;
                hazardRigidbody.bodyType = RigidbodyType2D.Kinematic;
            }

            // Pause Animator.
            if (hazardAnimator != null)
            {
                savedAnimatorSpeed = hazardAnimator.speed;
                hazardAnimator.speed = 0f;
            }

            // Update visual tint.
            if (hazardRenderer != null)
            {
                hazardRenderer.color = frozenColor;
            }

            Debug.Log($"[Hazard] '{gameObject.name}' frozen by Glitch.");
        }

        /// <summary>
        /// Resumes hazard movement, restores animations, and re-enables lethality after glitch ends.
        /// </summary>
        public void UnfreezeHazard()
        {
            if (!isFrozen)
            {
                return;
            }

            isFrozen = false;

            // Restore Rigidbody2D physics movement.
            if (hazardRigidbody != null)
            {
                hazardRigidbody.bodyType = savedBodyType;
                hazardRigidbody.linearVelocity = savedLinearVelocity;
                hazardRigidbody.angularVelocity = savedAngularVelocity;
            }

            // Resume Animator.
            if (hazardAnimator != null)
            {
                hazardAnimator.speed = savedAnimatorSpeed;
            }

            // Restore normal visual tint.
            if (hazardRenderer != null)
            {
                hazardRenderer.color = normalColor;
            }

            Debug.Log($"[Hazard] '{gameObject.name}' unfrozen. Lethality restored.");
        }
        #endregion

        #region Collision & Player Contact Detection
        /// <summary>
        /// Handles trigger-based collisions (e.g. laser beams, spikes configured as triggers).
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerContact(other.gameObject);
        }

        /// <summary>
        /// Handles continuous trigger overlap (in case player unfreezes or touches while already overlapping).
        /// </summary>
        private void OnTriggerStay2D(Collider2D other)
        {
            HandlePlayerContact(other.gameObject);
        }

        /// <summary>
        /// Handles solid physical collisions (e.g. solid spikes, patrol enemy bodies).
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerContact(collision.gameObject);
        }

        /// <summary>
        /// Handles continuous solid contact.
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            HandlePlayerContact(collision.gameObject);
        }

        /// <summary>
        /// Evaluates whether the colliding entity is the player and applies lethal defeat if applicable.
        /// </summary>
        private void HandlePlayerContact(GameObject target)
        {
            // If the hazard is currently inert (frozen by glitch) or configured as non-lethal, ignore contact.
            if (!IsLethal)
            {
                return;
            }

            // Detect the player using the PlayerController component on the target or its parent.
            PlayerController player = target.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                Debug.Log($"[Hazard] Player contacted lethal hazard '{gameObject.name}'. Triggering defeat.");

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerPlayerDeath();
                }
                else
                {
                    Debug.LogWarning("[Hazard] Player death triggered, but no GameManager instance was found in the scene.");
                }
            }
        }
        #endregion
    }
}
