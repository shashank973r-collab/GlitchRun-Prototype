using UnityEngine;

namespace GlitchRun.Gameplay
{
    /// <summary>
    /// Camera-relative infinite-wrapping parallax layer.
    /// Movement driven purely by camera X delta — zero drift on respawn/reload/teleport.
    /// Three sibling SpriteRenderers tile horizontally with no seams or pops.
    /// The parent GameObject should NOT be moved by this or any other script.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Tracking")]
        [Tooltip("Leave null → resolved to Camera.main at Start.")]
        [SerializeField] private Transform cameraTransform;

        [Range(0f, 1f)]
        [Tooltip("0 = locked to world (no movement), 1 = moves with camera (no parallax).")]
        [SerializeField] private float parallaxFactorX = 0.5f;

        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactorY = 0.0f;

        [Header("Tiling")]
        [Tooltip("Exactly 3 SpriteRenderers arranged as [left, center, right] tiles.")]
        [SerializeField] private SpriteRenderer[] tileRenderers;

        // Derived at runtime
        private float tileWidth;

        // We track our own accumulated horizontal scroll offset to
        // avoid floating-point drift over long levels.
        private float scrollOffset;

        // Baseline camera position captured at Start so that movement
        // is always relative — not absolute — preventing respawn pops.
        private float cameraStartX;
        private float cameraStartY;
        private float baseY;

        // The three tiles are centered at offsets: -tileWidth, 0, +tileWidth
        // relative to the layer's own position (which never moves).
        private static readonly float[] kTileOffsets = { -1f, 0f, 1f };

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            ResolveTileWidth();

            if (cameraTransform != null)
            {
                cameraStartX = cameraTransform.position.x;
                cameraStartY = cameraTransform.position.y;
            }

            baseY = transform.position.y;

            // Place tiles at their canonical positions immediately so there's
            // no 1-frame flicker on scene load.
            PositionTiles(0f, 0f);
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                    cameraStartX = cameraTransform.position.x;
                    cameraStartY = cameraTransform.position.y;
                }
                else return;
            }

            float camDeltaX = cameraTransform.position.x - cameraStartX;
            float camDeltaY = cameraTransform.position.y - cameraStartY;

            PositionTiles(camDeltaX, camDeltaY);
        }

        private void PositionTiles(float camDeltaX, float camDeltaY)
        {
            if (tileRenderers == null || tileRenderers.Length < 3 || tileWidth <= 0f)
                return;

            // How far has this layer scrolled from its start position?
            float layerScrollX = camDeltaX * parallaxFactorX;
            float layerScrollY = camDeltaY * parallaxFactorY;

            // The "center" tile anchor in world space.
            float layerBaseX = transform.position.x + layerScrollX;
            float layerY = baseY + layerScrollY;

            // Snap the anchor to the nearest tile grid so we always know
            // which three tiles to show without a running counter.
            float snapped = Mathf.Floor(layerBaseX / tileWidth) * tileWidth;

            for (int i = 0; i < 3; i++)
            {
                if (tileRenderers[i] == null) continue;
                float tileX = snapped + (i - 1) * tileWidth;

                // Shift left/right by one tile-width when the camera wanders off.
                // This keeps exactly one tile to the left, one under, and one to the right.
                while (tileX + tileWidth < layerBaseX) tileX += tileWidth * 3f;
                while (tileX > layerBaseX + tileWidth) tileX -= tileWidth * 3f;

                tileRenderers[i].transform.position = new Vector3(
                    tileX,
                    layerY,
                    tileRenderers[i].transform.position.z
                );
            }
        }

        private void ResolveTileWidth()
        {
            if (tileRenderers != null && tileRenderers.Length > 0 && tileRenderers[0] != null)
            {
                // Use bounds (accounts for sprite scale) – valid after Awake
                tileWidth = tileRenderers[0].bounds.size.x;

                if (tileWidth < 0.01f && tileRenderers[0].sprite != null)
                {
                    // Fallback: derive from sprite rect & PPU
                    float spritePixelWidth = tileRenderers[0].sprite.rect.width;
                    float ppu = tileRenderers[0].sprite.pixelsPerUnit;
                    float scale = tileRenderers[0].transform.lossyScale.x;
                    tileWidth = (spritePixelWidth / ppu) * scale;
                }
            }

            if (tileWidth < 0.01f)
            {
                // Absolute last resort – fits a 2048×720 sprite at 100 PPU
                tileWidth = 20.48f;
                Debug.LogWarning($"[ParallaxLayer] {gameObject.name}: Could not determine tile width; defaulting to {tileWidth}. Assign tileRenderers in the Inspector.", this);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (tileWidth <= 0.01f) ResolveTileWidth();
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 pos = transform.position;
            for (int i = -1; i <= 1; i++)
            {
                Gizmos.DrawWireCube(new Vector3(pos.x + i * tileWidth, pos.y, pos.z),
                    new Vector3(tileWidth, 5f, 0f));
            }
        }
#endif
    }
}
