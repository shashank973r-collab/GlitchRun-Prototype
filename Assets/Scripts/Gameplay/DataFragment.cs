using UnityEngine;
using GlitchRun.Core;
using GlitchRun.Player;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Neon-green data fragment collectible.
    /// Floats and pulses in the level. Collecting enough fragments unlocks the level exit portal.
    /// </summary>
    public class DataFragment : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float rotateSpeed = 45f;
        [SerializeField] private float scalePulseAmount = 0.08f;

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer fragmentRenderer;

        private Vector3 startPosition;
        private Vector3 baseScale;
        private bool isCollected = false;

        private void Awake()
        {
            if (fragmentRenderer == null)
            {
                fragmentRenderer = GetComponent<SpriteRenderer>();
            }

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            startPosition = transform.position;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (isCollected) return;

            // Smooth floating bob
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // Gentle rotation
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            // Subtle pulsing scale
            float pulse = 1f + Mathf.Sin(Time.time * bobSpeed * 1.5f) * scalePulseAmount;
            transform.localScale = baseScale * pulse;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            isCollected = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectFragment(this);
            }

            // Visual disable
            if (fragmentRenderer != null)
            {
                fragmentRenderer.enabled = false;
            }

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
