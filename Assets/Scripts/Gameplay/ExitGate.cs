using UnityEngine;
using GlitchRun.Core;
using GlitchRun.Player;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Level exit portal. Remains locked until the required number of data fragments is collected,
    /// then powers up into a vibrant neon green escape portal.
    /// </summary>
    public class ExitGate : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private SpriteRenderer portalRenderer;
        [SerializeField] private Color lockedColor = new Color(0.7f, 0.2f, 0.1f, 0.6f);
        [SerializeField] private Color unlockedColor = new Color(0f, 1f, 0.5f, 1f);
        [SerializeField] private float pulseSpeed = 4f;

        private bool isUnlocked = false;

        public bool IsUnlocked => isUnlocked;

        private void Awake()
        {
            if (portalRenderer == null)
            {
                portalRenderer = GetComponent<SpriteRenderer>();
            }

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated += HandleFragmentsUpdated;
                UpdateGateState(GameManager.Instance.CollectedFragments, GameManager.Instance.RequiredFragments);
            }
            else
            {
                SetUnlocked(false);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated -= HandleFragmentsUpdated;
            }
        }

        private void Update()
        {
            if (portalRenderer != null)
            {
                // Pulsing glow effect
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * pulseSpeed);
                Color baseCol = isUnlocked ? unlockedColor : lockedColor;
                portalRenderer.color = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * pulse);
            }
        }

        private void HandleFragmentsUpdated(int collected, int required)
        {
            UpdateGateState(collected, required);
        }

        private void UpdateGateState(int collected, int required)
        {
            bool shouldUnlock = collected >= required;
            if (shouldUnlock != isUnlocked)
            {
                SetUnlocked(shouldUnlock);
            }
        }

        private void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            if (isUnlocked)
            {
                Debug.Log("[GlitchRun] TERMINAL BREACH GATE UNLOCKED! Enter to escape the simulation.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isUnlocked) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CompleteLevel();
                }
            }
        }
    }
}
