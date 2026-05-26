using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Jump Feel")]
    public float fallMultiplier = 2.2f;
    public float jumpCutMultiplier = 2.0f;
    public float apexDownBoost = 0.5f;

    [Header("Air Control")]
    [Range(0f, 1f)] public float airControlLerp = 0.20f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.16f;
    [Range(0f, 1f)] public float minGroundNormalY = 0.35f;

    [Header("Coyote Time")]
    public float groundedGraceTime = 0.18f;
    public float jumpGroundedMaxRelativeYSpeed = 3.0f;
    public float coyoteGroundedMaxRelativeYSpeed = 3.0f;

    [Header("Input Drift Fix")]
    public float inputDeadzone = 0.1f;
    public bool forceStopXWhenIdle = true;
    public float idleStopVelEpsilon = 0.2f;

    [Header("Slope Stick")]
    public bool stopSlidingWhenIdle = true;
    public float idleSlopeStickVelocity = 0.05f;

    [Header("Physics Material")]
    public PhysicsMaterial2D noFrictionMaterial;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashMinDuration = 0.4f;
    public float dashCooldown = 0.8f;
    public float doubleTapWindow = 0.25f;
    public float dashParticleVelocityX = 15f;

    [Header("Animator")]
    public string didJumpBool = "didJump";

    [Header("Debug")]
    public bool debugGround = false;

    // ── Components ──
    private Rigidbody2D rb;
    private CapsuleCollider2D capsule;
    private Animator animator;
    private Transform graphics;
    private PlayerInputActions input;
    private ParticleSystem dashParticles;
    private PlayerAttack playerAttack;

    // ── Input flags ──
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool crouchHeld;

    // ── State ──
    private bool isGrounded;
    private bool didJump;
    private float lastGroundedTime;

    // ── Ground tracking ──
    private Vector2 groundNormal = Vector2.up;
    private Collider2D currentGroundCollider;
    private Rigidbody2D currentGroundRigidbody;
    private Vector2 currentGroundVelocity;

    // ── Dash ──
    private bool isDashing = false;
    private float dashDirection = 0f;
    private float dashCooldownRemaining = 0f;
    private float dashTimeRemaining = 0f;

    // ── Double tap tracking ──
    private float lastRightTapTime = -999f;
    private float lastLeftTapTime = -999f;
    private bool prevRightHeld = false;
    private bool prevLeftHeld = false;

    // ── Public accessors ──
    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsCrouching => crouchHeld && isGrounded;
    public bool DidJump => didJump;
    public float MoveInputX => moveInput.x;
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
    public Vector2 GroundVelocity => currentGroundVelocity;

    // ═══════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        playerAttack = GetComponent<PlayerAttack>();

        Transform graphicsChild = transform.Find("Graphics");
        if (graphicsChild != null)
            graphics = graphicsChild;
        else
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            graphics = sr != null ? sr.transform : transform;
        }

        dashParticles = GetComponent<ParticleSystem>();
        if (dashParticles == null)
            dashParticles = GetComponentInChildren<ParticleSystem>();

        input = new PlayerInputActions();
        rb.linearDamping = 0f;

        if (groundLayer.value == 0)
        {
            int groundIndex = LayerMask.NameToLayer("Ground");
            if (groundIndex >= 0)
                groundLayer = 1 << groundIndex;
        }

        if (noFrictionMaterial != null && capsule != null)
            capsule.sharedMaterial = noFrictionMaterial;
    }

    private void OnEnable()
    {
        input.Player.Enable();

        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMoveCancel;

        input.Player.Jump.performed += OnJump;
        input.Player.Jump.canceled += OnJumpCancel;

        input.Player.Crouch.performed += OnCrouch;
        input.Player.Crouch.canceled += OnCrouchCancel;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMoveCancel;

        input.Player.Jump.performed -= OnJump;
        input.Player.Jump.canceled -= OnJumpCancel;

        input.Player.Crouch.performed -= OnCrouch;
        input.Player.Crouch.canceled -= OnCrouchCancel;

        input.Player.Disable();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ClearAllInput();
    }

    private void ClearAllInput()
    {
        moveInput = Vector2.zero;
        jumpPressed = false;
        jumpHeld = false;
        crouchHeld = false;
        prevRightHeld = false;
        prevLeftHeld = false;
    }

    // ═══════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════

    private void Update()
    {
        bool rightHeld = moveInput.x > 0.5f;
        bool leftHeld = moveInput.x < -0.5f;

        if (rightHeld && !prevRightHeld)
        {
            if (Time.time - lastRightTapTime <= doubleTapWindow)
                TryStartDash(1f);
            else
                lastRightTapTime = Time.time;
        }

        if (leftHeld && !prevLeftHeld)
        {
            if (Time.time - lastLeftTapTime <= doubleTapWindow)
                TryStartDash(-1f);
            else
                lastLeftTapTime = Time.time;
        }

        prevRightHeld = rightHeld;
        prevLeftHeld = leftHeld;
    }

    // ═══════════════════════════════════════════
    //  FIXED UPDATE
    // ═══════════════════════════════════════════

    private void FixedUpdate()
    {
        CheckGrounded();

        if (dashCooldownRemaining > 0f)
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.fixedDeltaTime);

        HandleDash();
        HandleMovement();
        HandleJump();
        ApplyBetterGravity();
        UpdateAnimator();

        jumpPressed = false;
    }

    // ═══════════════════════════════════════════
    //  DASH
    // ═══════════════════════════════════════════

    private void TryStartDash(float dir)
    {
        if (isDashing) return;
        if (!isGrounded) return;
        if (dashCooldownRemaining > 0f) return;
        if (crouchHeld) return;

        // Ask PlayerAttack if it's safe to dash
        if (playerAttack != null && playerAttack.IsAttacking) return;
        if (playerAttack != null && playerAttack.IsBlocking) return;

        dashDirection = dir;
        isDashing = true;
        dashTimeRemaining = dashMinDuration;
        dashCooldownRemaining = dashCooldown;

        if (dashParticles != null)
        {
            var vel = dashParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(dir > 0f ? -dashParticleVelocityX : dashParticleVelocityX);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            dashParticles.Play();
        }
    }

    private void HandleDash()
    {
        if (!isDashing) return;

        // Stop dash immediately if attack fires
        if (playerAttack != null && playerAttack.IsAttacking)
        {
            isDashing = false;
            dashTimeRemaining = 0f;
            if (dashParticles != null) dashParticles.Stop();
            return;
        }

        if (!isGrounded)
        {
            isDashing = false;
            if (dashParticles != null) dashParticles.Stop();
            return;
        }

        dashTimeRemaining -= Time.fixedDeltaTime;

        float rawX = moveInput.x;
        bool stillHolding = (dashDirection > 0f && rawX > 0.1f) ||
                            (dashDirection < 0f && rawX < -0.1f);

        if (dashTimeRemaining > 0f || stillHolding)
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
        else
        {
            isDashing = false;
            if (dashParticles != null) dashParticles.Stop();
        }
    }

    // ═══════════════════════════════════════════
    //  GROUND CHECK
    // ═══════════════════════════════════════════

    private void CheckGrounded()
    {
        if (capsule == null)
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            currentGroundCollider = null;
            currentGroundRigidbody = null;
            currentGroundVelocity = Vector2.zero;
            return;
        }

        RaycastHit2D hit = Physics2D.CapsuleCast(
            capsule.bounds.center,
            capsule.bounds.size,
            capsule.direction,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        bool validSurface = hit.collider != null && hit.normal.y >= minGroundNormalY;

        if (validSurface)
        {
            Vector2 hitGroundVelocity = GetGroundVelocity(hit.collider, hit.rigidbody);
            float relativeY = rb.linearVelocity.y - hitGroundVelocity.y;

            if (relativeY <= jumpGroundedMaxRelativeYSpeed)
            {
                isGrounded = true;
                lastGroundedTime = Time.time;
                didJump = false;
                groundNormal = hit.normal;
                currentGroundCollider = hit.collider;
                currentGroundRigidbody = hit.rigidbody;
                currentGroundVelocity = hitGroundVelocity;

                if (debugGround)
                    Debug.Log($"[GROUND] grounded=true hit={hit.collider.name} dist={hit.distance:F4} normal={hit.normal} playerVy={rb.linearVelocity.y:F3} groundVy={currentGroundVelocity.y:F3} relY={relativeY:F3}", this);

                return;
            }
        }

        float relativeYDuringGrace = rb.linearVelocity.y - currentGroundVelocity.y;
        bool withinGrace = (Time.time - lastGroundedTime) <= groundedGraceTime;
        isGrounded = withinGrace && relativeYDuringGrace <= coyoteGroundedMaxRelativeYSpeed;

        if (isGrounded)
            didJump = false;
        else
        {
            groundNormal = Vector2.up;
            currentGroundCollider = null;
            currentGroundRigidbody = null;
            currentGroundVelocity = Vector2.zero;
        }

        if (debugGround)
        {
            if (hit.collider != null)
            {
                Vector2 hgv = GetGroundVelocity(hit.collider, hit.rigidbody);
                float relY = rb.linearVelocity.y - hgv.y;
                Debug.Log($"[GROUND] grounded={isGrounded} hit={hit.collider.name} dist={hit.distance:F4} normal={hit.normal} playerVy={rb.linearVelocity.y:F3} groundVy={hgv.y:F3} relY={relY:F3}", this);
            }
            else
                Debug.Log($"[GROUND] grounded={isGrounded} hit=null playerVy={rb.linearVelocity.y:F3} groundVy={currentGroundVelocity.y:F3}", this);
        }
    }

    private Vector2 GetGroundVelocity(Collider2D hitCollider, Rigidbody2D hitRb)
    {
        if (hitCollider == null) return Vector2.zero;

        MovingRockUpDown movingRock = hitCollider.GetComponent<MovingRockUpDown>();
        if (movingRock == null) movingRock = hitCollider.GetComponentInParent<MovingRockUpDown>();
        if (movingRock != null) return movingRock.Velocity;

        MovingPlatform movingPlatform = hitCollider.GetComponent<MovingPlatform>();
        if (movingPlatform == null) movingPlatform = hitCollider.GetComponentInParent<MovingPlatform>();
        if (movingPlatform != null) return movingPlatform.Velocity;

        if (hitRb != null) return hitRb.linearVelocity;

        return Vector2.zero;
    }

    // ═══════════════════════════════════════════
    //  MOVEMENT
    // ═══════════════════════════════════════════

    private void HandleMovement()
    {
        if (isDashing) return;

        bool attacking = playerAttack != null && playerAttack.IsAttacking;
        bool blocking = playerAttack != null && playerAttack.IsBlocking;

        if (blocking)
        {
            rb.linearVelocity = new Vector2(currentGroundVelocity.x, rb.linearVelocity.y);
            return;
        }

        if (attacking)
        {
            rb.linearVelocity = new Vector2(currentGroundVelocity.x, rb.linearVelocity.y);
            return;
        }

        float rawX = moveInput.x;
        if (Mathf.Abs(rawX) < inputDeadzone) rawX = 0f;

        float targetX = (crouchHeld && isGrounded) ? 0f : rawX * moveSpeed;

        if (isGrounded)
        {
            float finalX = targetX + currentGroundVelocity.x;
            rb.linearVelocity = new Vector2(finalX, rb.linearVelocity.y);

            if (stopSlidingWhenIdle && rawX == 0f)
            {
                Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x).normalized;
                float tangentSpeed = Vector2.Dot(rb.linearVelocity - currentGroundVelocity, tangent);
                rb.linearVelocity -= tangent * tangentSpeed;

                if (Mathf.Abs(rb.linearVelocity.y - currentGroundVelocity.y) < idleSlopeStickVelocity)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentGroundVelocity.y);
            }

            if (forceStopXWhenIdle && rawX == 0f && Mathf.Abs(rb.linearVelocity.x - currentGroundVelocity.x) <= idleStopVelEpsilon)
                rb.linearVelocity = new Vector2(currentGroundVelocity.x, rb.linearVelocity.y);
        }
        else
        {
            float newX = Mathf.Lerp(rb.linearVelocity.x, targetX, airControlLerp);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        if (graphics != null && rawX != 0f)
        {
            Vector3 s = graphics.localScale;
            s.x = Mathf.Sign(rawX) * Mathf.Abs(s.x);
            graphics.localScale = s;
        }
    }

    // ═══════════════════════════════════════════
    //  JUMP
    // ═══════════════════════════════════════════

    private void HandleJump()
    {
        if (!jumpPressed) return;

        bool blocking = playerAttack != null && playerAttack.IsBlocking;
        bool attacking = playerAttack != null && playerAttack.IsAttacking;

        if (blocking) return;

        if (isDashing)
        {
            isDashing = false;
            dashTimeRemaining = 0f;
            if (dashParticles != null) dashParticles.Stop();
        }

        float relativeY = rb.linearVelocity.y - currentGroundVelocity.y;
        bool withinGrace = (Time.time - lastGroundedTime) <= groundedGraceTime;
        bool canUseCoyote = withinGrace && relativeY <= coyoteGroundedMaxRelativeYSpeed;

        if (!(isGrounded || canUseCoyote)) return;
        if (crouchHeld) return;
        if (attacking) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
        didJump = true;
        currentGroundCollider = null;
        currentGroundRigidbody = null;
        currentGroundVelocity = Vector2.zero;
        groundNormal = Vector2.up;
    }

    // ═══════════════════════════════════════════
    //  GRAVITY
    // ═══════════════════════════════════════════

    private void ApplyBetterGravity()
    {
        if (isDashing) return;

        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (jumpCutMultiplier - 1f) * Time.fixedDeltaTime;
        else if (Mathf.Abs(rb.linearVelocity.y) < 0.1f && !isGrounded)
            rb.linearVelocity += Vector2.down * apexDownBoost * Time.fixedDeltaTime;
    }

    // ═══════════════════════════════════════════
    //  ANIMATOR
    // ═══════════════════════════════════════════

    private void UpdateAnimator()
    {
        if (animator == null) return;

        AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsName("Death")) return;

        bool attacking = playerAttack != null && playerAttack.IsAttacking;
        bool blocking = playerAttack != null && playerAttack.IsBlocking;
        bool isCrouching = crouchHeld && isGrounded;
        bool allowRun = !attacking && !blocking;
        bool isRunning = allowRun && Mathf.Abs(moveInput.x) > 0.1f && isGrounded && !isCrouching;

        SetAnimatorBoolIfExists("isGrounded", isGrounded);
        SetAnimatorBoolIfExists("isCrouching", isCrouching);
        SetAnimatorBoolIfExists("isRunning", isRunning);
        SetAnimatorBoolIfExists("isDashing", isDashing);
        SetAnimatorBoolIfExists(didJumpBool, didJump);
        SetAnimatorFloatIfExists("yVelocity", rb.linearVelocity.y);
    }

    private void SetAnimatorBoolIfExists(string param, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(param)) return;
        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter p = animator.GetParameter(i);
            if (p.name == param && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(param, value);
                return;
            }
        }
    }

    private void SetAnimatorFloatIfExists(string param, float value)
    {
        if (animator == null || string.IsNullOrEmpty(param)) return;
        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter p = animator.GetParameter(i);
            if (p.name == param && p.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(param, value);
                return;
            }
        }
    }

    // ═══════════════════════════════════════════
    //  INPUT CALLBACKS
    // ═══════════════════════════════════════════

    private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCancel(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void OnJump(InputAction.CallbackContext ctx) { jumpPressed = true; jumpHeld = true; }
    private void OnJumpCancel(InputAction.CallbackContext ctx) => jumpHeld = false;

    private void OnCrouch(InputAction.CallbackContext ctx) => crouchHeld = true;
    private void OnCrouchCancel(InputAction.CallbackContext ctx) => crouchHeld = false;

    // ═══════════════════════════════════════════
    //  EDITOR GIZMOS
    // ═══════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        CapsuleCollider2D cap = GetComponent<CapsuleCollider2D>();
        if (cap == null) return;

        Gizmos.color = Color.green;
        Vector2 origin  = cap.bounds.center;
        Vector2 size    = cap.bounds.size;
        Vector2 castEnd = origin + Vector2.down * groundCheckDistance;

        Gizmos.DrawWireCube(origin, size);
        Gizmos.DrawWireCube(castEnd, size);
        Gizmos.DrawLine(new Vector2(origin.x - size.x * 0.5f, origin.y), new Vector2(castEnd.x - size.x * 0.5f, castEnd.y));
        Gizmos.DrawLine(new Vector2(origin.x + size.x * 0.5f, origin.y), new Vector2(castEnd.x + size.x * 0.5f, castEnd.y));
    }
#endif
}