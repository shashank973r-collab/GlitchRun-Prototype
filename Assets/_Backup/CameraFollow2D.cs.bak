using UnityEngine;
using GlitchRun.Player;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Smooth 2D side-scrolling camera controller with forward lead offset,
    /// level boundary clamping, background parallax tracking, and screen shake support.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        public static CameraFollow2D Instance { get; private set; }

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(2f, 1.2f, -10f);
        [SerializeField] private float smoothTime = 0.2f;

        [Header("Level Bounds")]
        [SerializeField] private bool clampBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-2f, -1f);
        [SerializeField] private Vector2 maxBounds = new Vector2(68f, 8f);

        [Header("Background Parallax")]
        [SerializeField] private Transform backgroundTransform;
        [SerializeField] private float backgroundParallaxFactor = 0.9f;

        private Vector3 currentVelocity;
        private Vector3 initialBackgroundPos;
        private float shakeTimer = 0f;
        private float shakeIntensity = 0.25f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
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

        public void TriggerShake(float duration = 0.2f, float intensity = 0.25f)
        {
            if (PlayerPrefs.GetInt("ScreenShake", 1) == 0) return;
            shakeTimer = duration;
            shakeIntensity = intensity;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
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

            Vector3 smoothPos = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * shakeIntensity,
                    Random.Range(-1f, 1f) * shakeIntensity,
                    0f
                );
                smoothPos += shakeOffset;
            }

            transform.position = smoothPos;

            // Parallax tracking
            if (backgroundTransform != null)
            {
                float bgX = initialBackgroundPos.x + (transform.position.x * backgroundParallaxFactor);
                float bgY = initialBackgroundPos.y + (transform.position.y * (backgroundParallaxFactor * 0.5f));
                backgroundTransform.position = new Vector3(bgX, bgY, initialBackgroundPos.z);
            }
        }
    }
}
