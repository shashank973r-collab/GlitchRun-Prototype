using UnityEngine;
using UnityEngine.InputSystem;
using GlitchRun.Managers;

namespace GlitchRun.Player
{
    /// <summary>
    /// Handles 2D player movement, jumping, and ground collision detection
    /// for GLITCH RUN using Unity 6 2D physics and the New Input System (Keyboard.current).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Movement & Jump")]
        [Tooltip("Horizontal movement speed in units per second.")]
        [SerializeField] private float moveSpeed = 8f;

        [Tooltip("Upward velocity applied immediately when jumping.")]
        [SerializeField] private float jumpForce = 14f;

        [Tooltip("Duration in seconds that a jump press is buffered before landing or expiring.")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Ground Detection")]
        [Tooltip("Child transform positioned at the player's feet used to check for ground contact.")]
        [SerializeField] private Transform groundCheckPoint;

        [Tooltip("Radius of the overlap circle cast from the ground check point.")]
        [SerializeField] private float groundCheckRadius = 0.2f;

        [Tooltip("Layers recognized as solid ground.")]
        [SerializeField] private LayerMask groundLayer;
        #endregion

        #region Components & Internal State
        private Rigidbody2D rb;
        private Collider2D bodyCollider;

        private float horizontalInput = 0f;
        private float jumpBufferCounter = 0f;
        private bool isGrounded = false;
        #endregion

        #region Public Properties (Contract API)
        /// <summary>
        /// True if the player's ground-check overlap currently detects the ground layer.
        /// </summary>
        public bool IsGrounded => isGrounded;

        /// <summary>
        /// The current linear physics velocity of the player's Rigidbody2D.
        /// </summary>
        public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;

        /// <summary>
        /// Flag controlling whether player input is accepted. Can be disabled during cutscenes or transitions.
        /// </summary>
        public bool CanMove { get; set; } = true;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();

            // Freeze rotation on Z to prevent the player sprite from tipping over on collisions.
            if (rb != null)
            {
                rb.freezeRotation = true;
            }

            // Ensure critical Inspector settings are clamped to non-negative values at runtime.
            if (groundCheckRadius < 0f) groundCheckRadius = 0f;
            if (jumpForce < 0f) jumpForce = 0f;
            if (moveSpeed < 0f) moveSpeed = 0f;
            if (jumpBufferTime < 0f) jumpBufferTime = 0f;
        }

        private void OnValidate()
        {
            // Clamp Inspector fields to non-negative values in the Editor.
            if (groundCheckRadius < 0f)
            {
                groundCheckRadius = 0f;
            }

            if (jumpForce < 0f)
            {
                jumpForce = 0f;
            }

            if (moveSpeed < 0f)
            {
                moveSpeed = 0f;
            }

            if (jumpBufferTime < 0f)
            {
                jumpBufferTime = 0f;
            }
        }

        private void Update()
        {
            // Gather input and update the jump buffer counter each frame.
            ReadInput();
        }

        private void FixedUpdate()
        {
            // Perform ground detection and apply movement in FixedUpdate for consistent physics.
            PerformGroundCheck();
            ApplyMovement();
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// Reads keyboard inputs using the New Input System (Keyboard.current).
        /// Buffers jump presses and safely handles null Keyboard states.
        /// </summary>
        private void ReadInput()
        {
            // Reset horizontal input by default.
            horizontalInput = 0f;

            // If movement is prohibited externally or by GameManager, expire any buffered jump and exit.
            if (!CanMove || (GameManager.Instance != null && !GameManager.Instance.IsPlaying))
            {
                jumpBufferCounter = 0f;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                // Count down existing buffer even if keyboard is disconnected.
                if (jumpBufferCounter > 0f)
                {
                    jumpBufferCounter -= Time.deltaTime;
                }
                return;
            }

            // Read horizontal input axes (A/D or Left/Right Arrow).
            bool leftPressed = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool rightPressed = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

            if (leftPressed && !rightPressed)
            {
                horizontalInput = -1f;
            }
            else if (rightPressed && !leftPressed)
            {
                horizontalInput = 1f;
            }

            // Jump Buffer: When Space is pressed, queue a jump for up to jumpBufferTime seconds.
            // Decay the timer each frame so expired presses do not linger.
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else if (jumpBufferCounter > 0f)
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }
        #endregion

        #region Physics & Movement
        /// <summary>
        /// Checks if the groundCheckPoint overlaps with any solid geometry on the groundLayer.
        /// </summary>
        private void PerformGroundCheck()
        {
            if (groundCheckPoint != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
            }
            else if (bodyCollider != null)
            {
                // Fallback ground check from the bottom of the body collider if no check point is assigned.
                Vector2 fallbackPoint = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);
                isGrounded = Physics2D.OverlapCircle(fallbackPoint, groundCheckRadius, groundLayer);
            }
            else
            {
                isGrounded = false;
            }
        }

        /// <summary>
        /// Applies horizontal velocity and consumes buffered jumps when grounded using Unity 6 Rigidbody2D.linearVelocity.
        /// </summary>
        private void ApplyMovement()
        {
            if (rb == null)
            {
                return;
            }

            // If movement is prohibited (e.g., dead or won in GameManager), zero horizontal movement.
            if (!CanMove || (GameManager.Instance != null && !GameManager.Instance.IsPlaying))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                jumpBufferCounter = 0f;
                return;
            }

            // Apply horizontal speed while maintaining current vertical velocity.
            float targetXVelocity = horizontalInput * moveSpeed;
            float currentYVelocity = rb.linearVelocity.y;

            // Execute jump only if a jump request is buffered AND the player is grounded.
            // This prevents mid-air jumps while ensuring jump inputs right before landing are honored.
            if (jumpBufferCounter > 0f && isGrounded)
            {
                currentYVelocity = jumpForce;
                jumpBufferCounter = 0f; // Consume the buffered jump immediately.
            }

            rb.linearVelocity = new Vector2(targetXVelocity, currentYVelocity);
        }
        #endregion

        #region Public Methods (Contract API)
        /// <summary>
        /// Resets the player's position in the scene and cancels all existing momentum.
        /// </summary>
        /// <param name="spawnPosition">Target coordinates in world space.</param>
        public void ResetPosition(Vector2 spawnPosition)
        {
            transform.position = spawnPosition;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            jumpBufferCounter = 0f;
            horizontalInput = 0f;
        }

        /// <summary>
        /// Disables player input and stops horizontal velocity.
        /// </summary>
        public void DisableMovement()
        {
            CanMove = false;

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            jumpBufferCounter = 0f;
            horizontalInput = 0f;
        }

        /// <summary>
        /// Restores player movement control.
        /// </summary>
        public void EnableMovement()
        {
            CanMove = true;
        }
        #endregion

        #region Editor Gizmos
        private void OnDrawGizmosSelected()
        {
            // Draw a green sphere in the Scene view to visualize the ground detection radius.
            Gizmos.color = isGrounded ? Color.green : Color.red;

            if (groundCheckPoint != null)
            {
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
            }
            else if (bodyCollider != null)
            {
                Vector2 fallbackPoint = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);
                Gizmos.DrawWireSphere(fallbackPoint, groundCheckRadius);
            }
        }
        #endregion
    }
}
