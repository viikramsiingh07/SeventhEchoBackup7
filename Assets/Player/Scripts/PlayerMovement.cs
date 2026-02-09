using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public Vector2 groundBoxSize = new Vector2(0.75f, 0.1f); // wider for edges
    public float groundBoxInset = 0.02f;                     // box slightly inside feet
    public float upwardUngroundThreshold = 0.05f;            // don't re-ground while going up
    public float groundedGraceTime = 0.08f;                  // coyote for edge flicker

    private Rigidbody2D rb;
    private CapsuleCollider2D capsule;
    private Animator animator;
    private PlayerInputActions input;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool crouchHeld;
    private bool isGrounded;

    private float lastGroundedTime;
    private Vector3 originalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        animator = GetComponentInChildren<Animator>(); // Graphics child

        rb.linearDamping = 0f;
        originalScale = transform.localScale;

        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Player.Enable();

        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMoveCancel;
        input.Player.Jump.performed += OnJump;
        input.Player.Crouch.performed += OnCrouch;
        input.Player.Crouch.canceled += OnCrouchCancel;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMoveCancel;
        input.Player.Jump.performed -= OnJump;
        input.Player.Crouch.performed -= OnCrouch;
        input.Player.Crouch.canceled -= OnCrouchCancel;

        input.Player.Disable();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
        HandleJump();
        UpdateAnimator();
        jumpPressed = false;
    }

    private void CheckGrounded()
    {
        // Cast starts slightly above feet, casts DOWN a tiny amount
        Vector2 origin = new Vector2(
            capsule.bounds.center.x,
            capsule.bounds.min.y + groundBoxInset + (groundBoxSize.y * 0.5f)
        );

        float castDist = 0.08f;

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            groundBoxSize,
            0f,
            Vector2.down,
            castDist,
            groundLayer
        );

        bool touchingTop = hit.collider != null && hit.normal.y >= 0.65f;

        if (touchingTop)
            lastGroundedTime = Time.time;

        // Going up: never grounded
        if (rb.linearVelocity.y > upwardUngroundThreshold)
        {
            isGrounded = false;
            return;
        }

        // Falling: NO grace (prevents “stuck in air” between platforms)
        if (rb.linearVelocity.y < -0.10f)
        {
            isGrounded = touchingTop;
            return;
        }

        // Flat/near-flat: allow grace for edges
        isGrounded = touchingTop || (Time.time - lastGroundedTime) <= groundedGraceTime;
    }

    private void HandleMovement()
    {
        float targetX = (crouchHeld && isGrounded) ? 0f : moveInput.x * moveSpeed;
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);

        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(moveInput.x) * originalScale.x,
                originalScale.y,
                originalScale.z
            );
        }
    }

    private void HandleJump()
    {
        if (!jumpPressed) return;
        if (!isGrounded) return;
        if (crouchHeld) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        bool isCrouching = crouchHeld && isGrounded;
        bool isRunning = Mathf.Abs(moveInput.x) > 0.1f && isGrounded && !isCrouching;

        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isRunning", isRunning);
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCancel(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;
    private void OnJump(InputAction.CallbackContext ctx) => jumpPressed = true;
    private void OnCrouch(InputAction.CallbackContext ctx) => crouchHeld = true;
    private void OnCrouchCancel(InputAction.CallbackContext ctx) => crouchHeld = false;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var cap = GetComponent<CapsuleCollider2D>();
        if (cap == null) return;

        Vector2 boxCenter = new Vector2(
            cap.bounds.center.x,
            cap.bounds.min.y + groundBoxInset
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(boxCenter, groundBoxSize);
    }
#endif
}