using System.Collections;
using UnityEngine;
using GlitchRun.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GlitchRun.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8.5f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 75f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 13.5f;
        [SerializeField] private float fallGravityMultiplier = 1.8f;
        [SerializeField] private float lowJumpGravityMultiplier = 2.2f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.15f);
        [SerializeField] private LayerMask groundLayer;

        [Header("Visuals")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private SpriteRenderer visualRenderer;

        [Header("Invulnerability Settings")]
        [SerializeField] private float respawnInvulnerabilityDuration = 1.5f;

        private Rigidbody2D rb;
        private GlitchAbility glitchAbility;
        private float defaultGravityScale;
        private Vector3 baseVisualScale = Vector3.one;

        private float horizontalInput = 0f;
        private bool jumpInputDown = false;
        private bool jumpInputHeld = false;

        private bool isGrounded = false;
        private float coyoteTimer = 0f;
        private float jumpBufferTimer = 0f;
        private bool isInvulnerable = false;
        private int facingDirection = 1;

        public bool IsGrounded => isGrounded;
        public GlitchAbility Glitch => glitchAbility;
        public bool IsInvulnerable => isInvulnerable;

        public float MoveSpeed => moveSpeed;
        public float JumpForce => jumpForce;
        public float GravityScale => defaultGravityScale;

        private Vector2 Velocity
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return rb.linearVelocity;
#else
                return rb.velocity;
#endif
            }
            set
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = value;
#else
                rb.velocity = value;
#endif
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            glitchAbility = GetComponent<GlitchAbility>();
            defaultGravityScale = rb.gravityScale;

            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }

            if (visualTransform != null)
            {
                baseVisualScale = visualTransform.localScale;
            }

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (groundCheckPoint == null)
            {
                var checkObj = new GameObject("GroundCheck");
                checkObj.transform.parent = transform;
                checkObj.transform.localPosition = new Vector3(0, -0.6f, 0);
                groundCheckPoint = checkObj.transform;
            }
        }

        private void Start()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.SetActivePlayer(this);
                Core.GameManager.Instance.OnPlayerRespawn += HandleRespawn;
            }
        }

        private void OnDestroy()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnPlayerRespawn -= HandleRespawn;
            }
        }

        private void Update()
        {
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.CurrentState != Core.GameState.Playing)
            {
                horizontalInput = 0f;
                return;
            }

            ReadInput();
            CheckGround();

            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }

            if (jumpInputDown)
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= Time.deltaTime;
            }

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                ExecuteJump();
            }

            UpdateFacingDirection();
        }

        private void FixedUpdate()
        {
            ApplyHorizontalMovement();
            ApplyJumpGravityModifiers();
        }

        private void ReadInput()
        {
            horizontalInput = 0f;
            jumpInputDown = false;
            jumpInputHeld = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontalInput -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontalInput += 1f;
                if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) jumpInputDown = true;
                if (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed) jumpInputHeld = true;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                float stickX = pad.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f) horizontalInput = stickX;
                if (pad.dpad.left.isPressed) horizontalInput = -1f;
                if (pad.dpad.right.isPressed) horizontalInput = 1f;
                if (pad.buttonSouth.wasPressedThisFrame) jumpInputDown = true;
                if (pad.buttonSouth.isPressed) jumpInputHeld = true;
            }
#endif
        }

        private void CheckGround()
        {
            Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position;

            Collider2D col;
            if (groundLayer != 0)
            {
                col = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
            }
            else
            {
                Collider2D[] hits = Physics2D.OverlapBoxAll(checkPos, groundCheckSize, 0f);
                col = null;
                foreach (var h in hits)
                {
                    if (h.gameObject != gameObject && !h.isTrigger)
                    {
                        col = h;
                        break;
                    }
                }
            }

            isGrounded = col != null;
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float currentSpeedX = Velocity.x;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

            float newSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeed, rate * Time.fixedDeltaTime);
            Velocity = new Vector2(newSpeedX, Velocity.y);
        }

        private void ExecuteJump()
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            Velocity = new Vector2(Velocity.x, jumpForce);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("jump");
            }
        }

        private void ApplyJumpGravityModifiers()
        {
            float currentVelY = Velocity.y;

            if (currentVelY < 0f)
            {
                rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
            }
            else if (currentVelY > 0f && !jumpInputHeld)
            {
                rb.gravityScale = defaultGravityScale * lowJumpGravityMultiplier;
            }
            else
            {
                rb.gravityScale = defaultGravityScale;
            }
        }

        private void UpdateFacingDirection()
        {
            if (horizontalInput > 0.1f) facingDirection = 1;
            else if (horizontalInput < -0.1f) facingDirection = -1;

            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(
                    facingDirection * Mathf.Abs(baseVisualScale.x),
                    baseVisualScale.y,
                    baseVisualScale.z
                );
            }
        }

        public bool TryDamage(string source)
        {
            if (isInvulnerable)
            {
                return false;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("damage");
            }

            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.PlayerTookDamage(source);
            }

            StartCoroutine(DamageFlashRoutine());
            return true;
        }

        private IEnumerator DamageFlashRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;
            float duration = 1.0f;
            float interval = 0.08f;

            while (elapsed < duration)
            {
                if (visualRenderer != null)
                {
                    visualRenderer.color = visualRenderer.color == Color.red ? Color.white : Color.red;
                }
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            if (visualRenderer != null)
            {
                visualRenderer.color = Color.white;
            }
            isInvulnerable = false;
        }

        private void HandleRespawn()
        {
            if (Core.GameManager.Instance != null)
            {
                transform.position = Core.GameManager.Instance.CurrentCheckpoint;
            }

            Velocity = Vector2.zero;

            if (glitchAbility != null)
            {
                glitchAbility.ResetAbility();
            }

            StartCoroutine(RespawnInvulnerabilityRoutine());
        }

        private IEnumerator RespawnInvulnerabilityRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;
            float blinkInterval = 0.1f;

            while (elapsed < respawnInvulnerabilityDuration)
            {
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = !visualRenderer.enabled;
                }
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = true;
                visualRenderer.color = Color.white;
            }
            isInvulnerable = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
            }
        }
    }
}
