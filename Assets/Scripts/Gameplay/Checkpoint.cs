using UnityEngine;
using GlitchRun.Core;
using GlitchRun.Audio;

namespace GlitchRun.Gameplay
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer beaconRenderer;

        [Header("Visual Colors")]
        [SerializeField] private Color inactiveColor = new Color(1f, 0.6f, 0.1f, 0.7f); // Amber
        [SerializeField] private Color activeColor = new Color(0.0f, 1f, 0.85f, 1f);     // Neon Cyan

        private bool isActivated = false;

        private void Awake()
        {
            if (beaconRenderer == null) beaconRenderer = GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated) return;

            if (other.CompareTag("Player") || other.GetComponent<Player.PlayerController>() != null)
            {
                Activate();
            }
        }

        private void Activate()
        {
            isActivated = true;
            UpdateVisuals();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(transform.position);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("checkpoint");
            }
        }

        private void UpdateVisuals()
        {
            if (beaconRenderer != null)
            {
                beaconRenderer.color = isActivated ? activeColor : inactiveColor;
            }
        }
    }
}
