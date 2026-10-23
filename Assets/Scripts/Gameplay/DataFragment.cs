using UnityEngine;
using GlitchRun.Managers;
using GlitchRun.Player;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Represents a collectible simulation Data Fragment in GLITCH RUN.
    /// When gathered by the player, it registers with the GameManager to unlock the ExitDoor.
    /// Supports both trigger zones and solid physics colliders.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DataFragment : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Effects (Optional)")]
        [Tooltip("Optional audio clip played at the fragment's position when collected.")]
        [SerializeField] private AudioClip collectSFX;

        [Tooltip("Playback volume for the collection sound effect.")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        [Tooltip("Optional particle effect or visual prefab spawned upon collection.")]
        [SerializeField] private GameObject collectVFX;

        [Header("Component References (Optional)")]
        [Tooltip("Optional reference to the SpriteRenderer. Auto-detected if unassigned.")]
        [SerializeField] private SpriteRenderer fragmentRenderer;

        [Tooltip("Optional reference to the Collider2D. Auto-detected if unassigned.")]
        [SerializeField] private Collider2D fragmentCollider;
        #endregion

        #region Internal State
        private bool isCollected = false;
        #endregion

        #region Public Properties (Contract API)
        /// <summary>
        /// True once the fragment has been successfully gathered and processed by the GameManager.
        /// </summary>
        public bool IsCollected => isCollected;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (fragmentCollider == null)
            {
                fragmentCollider = GetComponent<Collider2D>();
            }

            if (fragmentRenderer == null)
            {
                fragmentRenderer = GetComponent<SpriteRenderer>();
            }
        }
        #endregion

        #region Collision & Pickup Detection
        /// <summary>
        /// Handles pickup when the fragment collider is configured as a trigger (isTrigger = true).
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other.gameObject);
        }

        /// <summary>
        /// Handles pickup when the fragment collider is configured as a solid obstacle (isTrigger = false).
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.gameObject);
        }

        /// <summary>
        /// Evaluates contact with another GameObject. If the player is identified, attempts collection.
        /// </summary>
        private void HandleContact(GameObject target)
        {
            // Ignore contact if this fragment has already been collected.
            if (isCollected)
            {
                return;
            }

            // Identify the player via the PlayerController component (on target or parent).
            PlayerController player = target.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                Collect();
            }
        }
        #endregion

        #region Collection Logic
        /// <summary>
        /// Executes collection logic, notifies the GameManager, plays feedback effects, and destroys the object.
        /// Can also be invoked programmatically for testing or cutscenes.
        /// </summary>
        public void Collect()
        {
            // Debounce guard to prevent multiple collection triggers from concurrent physics callbacks.
            if (isCollected)
            {
                return;
            }

            // Verify GameManager existence. Do not consume or destroy the fragment if the manager is missing.
            if (GameManager.Instance == null)
            {
                Debug.LogWarning($"[DataFragment] '{gameObject.name}' contacted by player, but no active GameManager was found. Fragment was not consumed.");
                return;
            }

            // Mark collected immediately to block subsequent collision calls.
            isCollected = true;

            // Notify GameManager to increment the collected fragment count.
            GameManager.Instance.RegisterFragmentCollected();
            Debug.Log($"[DataFragment] '{gameObject.name}' collected successfully.");

            // Spawn optional visual particle effect.
            if (collectVFX != null)
            {
                Instantiate(collectVFX, transform.position, Quaternion.identity);
            }

            // Play optional collection audio using PlayClipAtPoint so it finishes even after destruction.
            if (collectSFX != null)
            {
                AudioSource.PlayClipAtPoint(collectSFX, transform.position, sfxVolume);
            }

            // Disable visuals and collision immediately so the fragment disappears instantly.
            if (fragmentCollider != null)
            {
                fragmentCollider.enabled = false;
            }

            if (fragmentRenderer != null)
            {
                fragmentRenderer.enabled = false;
            }

            // Safely destroy the GameObject.
            Destroy(gameObject);
        }
        #endregion
    }
}
