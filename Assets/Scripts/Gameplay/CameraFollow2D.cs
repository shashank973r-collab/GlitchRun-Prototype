using UnityEngine;
using GlitchRun.Player;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Smooth 2D side-scrolling camera controller with forward lead offset,
    /// level boundary clamping, and optional background parallax tracking.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(2f, 1.2f, -10f);
        [SerializeField] private float smoothTime = 0.2f;

        [Header("Level Bounds")]
        [SerializeField] private bool clampBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-2f, -1f);
        [SerializeField] private Vector2 maxBounds = new Vector2(50f, 8f);

        [Header("Background Parallax")]
        [Tooltip("Optional background transform (e.g. BG_City_Animated) to scroll smoothly with the camera.")]
        [SerializeField] private Transform backgroundTransform;
        [SerializeField] private float backgroundParallaxFactor = 0.9f;

        private Vector3 currentVelocity;
        private Vector3 initialBackgroundPos;

        private void Start()
        {
            if (target == null)
            {
#if UNITY_6000_0_OR_NEWER
                var player = FindAnyObjectByType<PlayerController>();
#else
                var player = FindFirstObjectByType<PlayerController>();
#endif
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (backgroundTransform != null)
            {
                initialBackgroundPos = backgroundTransform.position;
            }

            if (target != null)
            {
                // Snap directly on start
                Vector3 targetPos = target.position + offset;
                if (clampBounds)
                {
                    targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
                    targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);
                }
                targetPos.z = offset.z;
                transform.position = targetPos;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
#if UNITY_6000_0_OR_NEWER
                var player = FindAnyObjectByType<PlayerController>();
#else
                var player = FindFirstObjectByType<PlayerController>();
#endif
                if (player != null) target = player.transform;
                else return;
            }

            Vector3 targetPos = target.position + offset;

            if (clampBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
                targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);
            }
            targetPos.z = offset.z;

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);

            // Parallax tracking for animated city background
            if (backgroundTransform != null)
            {
                float bgX = initialBackgroundPos.x + (transform.position.x * backgroundParallaxFactor);
                float bgY = initialBackgroundPos.y + (transform.position.y * (backgroundParallaxFactor * 0.5f));
                backgroundTransform.position = new Vector3(bgX, bgY, initialBackgroundPos.z);
            }
        }
    }
}
