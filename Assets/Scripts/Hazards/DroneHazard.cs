using UnityEngine;
using GlitchRun.Player;
using GlitchRun.Audio;

namespace GlitchRun.Hazards
{
    public class DroneHazard : MonoBehaviour
    {
        [Header("Patrol Settings")]
        [SerializeField] private Vector3 offsetA = new Vector3(-3f, 0f, 0f);
        [SerializeField] private Vector3 offsetB = new Vector3(3f, 0f, 0f);
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobSpeed = 3.0f;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 startPos;
        private Vector3 targetA;
        private Vector3 targetB;
        private bool movingToB = true;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            startPos = transform.position;
            targetA = startPos + offsetA;
            targetB = startPos + offsetB;
        }

        public void SetWaypoints(Vector3 posA, Vector3 posB)
        {
            targetA = posA;
            targetB = posB;
        }

        private void Update()
        {
            Vector3 target = movingToB ? targetB : targetA;
            Vector3 currentPos = transform.position;

            // Move horizontally
            float step = speed * Time.deltaTime;
            float newX = Mathf.MoveTowards(currentPos.x, target.x, step);

            // Subtle vertical hover bob
            float hoverY = (movingToB ? targetB.y : targetA.y) + Mathf.Sin(Time.time * bobSpeed) * bobHeight;

            transform.position = new Vector3(newX, hoverY, currentPos.z);

            // Sprite facing
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = (target.x < currentPos.x);
            }

            // Waypoint check
            if (Mathf.Abs(newX - target.x) < 0.05f)
            {
                movingToB = !movingToB;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckCollision(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckCollision(other);
        }

        private void CheckCollision(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.TryDamage("Security Drone");
            }
        }
    }
}
