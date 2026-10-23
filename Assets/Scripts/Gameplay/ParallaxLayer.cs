using UnityEngine;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Infinite camera-relative parallax layer that seamlessly wraps horizontally.
    /// Purely driven by camera X position, eliminating pops, drift, or desync on respawn/reload.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Tracking")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float parallaxFactorX = 0.5f;
        [SerializeField] private float parallaxFactorY = 0.15f;
        [SerializeField] private float verticalOffset = 0f;

        [Header("Tiling Setup")]
        [SerializeField] private SpriteRenderer[] tileRenderers;

        private float tileWidth;
        private Vector3 startPosition;

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            startPosition = transform.position;

            if (tileRenderers != null && tileRenderers.Length > 0 && tileRenderers[0] != null)
            {
                tileWidth = tileRenderers[0].bounds.size.x;
                // If local scale was applied, take sprite size * scale
                if (tileWidth <= 0.01f && tileRenderers[0].sprite != null)
                {
                    tileWidth = tileRenderers[0].sprite.rect.width / tileRenderers[0].sprite.pixelsPerUnit * tileRenderers[0].transform.lossyScale.x;
                }
            }
            else
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    tileWidth = sr.bounds.size.x;
                }
            }

            if (tileWidth <= 0.01f)
            {
                tileWidth = 20.48f; // Default fallback for 2048px @ 100 PPU
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (Camera.main != null) cameraTransform = Camera.main.transform;
                else return;
            }

            float camX = cameraTransform.position.x;
            float camY = cameraTransform.position.y;

            // Compute distance moved with parallax scaling
            float distanceX = camX * parallaxFactorX;
            float distanceY = camY * parallaxFactorY;

            // Center the anchor around camera X with modulo offset
            float wrappedX = camX - ((camX * (1f - parallaxFactorX)) % tileWidth);

            transform.position = new Vector3(
                camX * parallaxFactorX,
                startPosition.y + distanceY + verticalOffset,
                startPosition.z
            );

            // Position 3 repeating tiles: center, left, and right around camera relative view
            if (tileRenderers != null && tileRenderers.Length >= 3)
            {
                float relativeOffset = (camX * (1f - parallaxFactorX)) % tileWidth;
                tileRenderers[0].transform.position = new Vector3(camX - relativeOffset - tileWidth, transform.position.y, transform.position.z);
                tileRenderers[1].transform.position = new Vector3(camX - relativeOffset, transform.position.y, transform.position.z);
                tileRenderers[2].transform.position = new Vector3(camX - relativeOffset + tileWidth, transform.position.y, transform.position.z);
            }
        }
    }
}
