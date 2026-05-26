using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public string attackTrigger = "attack";

    [Header("Block")]
    public bool lockMovementDuringBlock = true;

    [Header("Vertical Attack")]
    public string verticalAttackTrigger = "verticalAttack";
    [Tooltip("Normalized time (0-1) at which hold-V freezes the animation.\n6fr=0.16  8fr=0.12  10fr=0.10  12fr=0.08")]
    public float verticalAttackHoldFrameTime = 0.08f;
    [Tooltip("How long in seconds V must be held to trigger charged mode")]
    public float chargeThreshold = 2f;

    // ── Components ──
    private Animator animator;
    private PlayerInputActions input;
    private PlayerMovement movement;

    // ── Input flags ──
    private bool attackPressed;
    private bool blockHeld;
    private bool verticalAttackPressed;
    private bool verticalAttackHeld;

    // ── State ──
    private bool isAttacking;
    private bool isVerticalAttacking;
    private bool isBlocking;
    private bool isVerticalAttackHolding;
    private bool isChargedMode;
    private float verticalAttackHoldDuration;

    // ── Public accessors ──
    public bool IsAttacking => isAttacking;
    public bool IsBlocking => isBlocking;
    public bool IsVerticalAttacking => isVerticalAttacking;

    // ═══════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        movement = GetComponent<PlayerMovement>();
        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Player.Enable();

        try { input.Player.Attack.performed += OnAttack; } catch { }

        try
        {
            input.Player.Block.performed += OnBlockPerformed;
            input.Player.Block.canceled += OnBlockCanceled;
        }
        catch { }

        try
        {
            input.Player.VerticalAttack.performed += OnVerticalAttack;
            input.Player.VerticalAttack.canceled += OnVerticalAttackCanceled;
        }
        catch { }
    }

    private void OnDisable()
    {
        try { input.Player.Attack.performed -= OnAttack; } catch { }

        try
        {
            input.Player.Block.performed -= OnBlockPerformed;
            input.Player.Block.canceled -= OnBlockCanceled;
        }
        catch { }

        try
        {
            input.Player.VerticalAttack.performed -= OnVerticalAttack;
            input.Player.VerticalAttack.canceled -= OnVerticalAttackCanceled;
        }
        catch { }

        input.Player.Disable();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ClearAllInput();
    }

    private void ClearAllInput()
    {
        attackPressed = false;
        blockHeld = false;
        verticalAttackPressed = false;
        verticalAttackHeld = false;
        isVerticalAttackHolding = false;
        isChargedMode = false;
        verticalAttackHoldDuration = 0f;

        if (animator != null)
            animator.speed = 1f;
    }

    // ═══════════════════════════════════════════
    //  UPDATE — track V hold duration
    // ═══════════════════════════════════════════

    private void Update()
    {
        if (verticalAttackHeld)
            verticalAttackHoldDuration += Time.deltaTime;
        else
            verticalAttackHoldDuration = 0f;
    }

    // ═══════════════════════════════════════════
    //  FIXED UPDATE
    // ═══════════════════════════════════════════

    private void FixedUpdate()
    {
        if (animator != null)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            isVerticalAttacking = st.IsName("VerticalAttack");
            isAttacking = st.IsName("Attack") || st.IsName("SitAttack") || isVerticalAttacking;
        }
        else
        {
            isAttacking = false;
            isVerticalAttacking = false;
        }

        isBlocking = blockHeld
                     && (movement != null && movement.IsGrounded)
                     && !isAttacking
                     && (movement != null && !movement.IsDashing);

        HandleAttack();
        UpdateAnimator();

        attackPressed = false;
        verticalAttackPressed = false;
    }

    // ═══════════════════════════════════════════
    //  ATTACK LOGIC
    // ═══════════════════════════════════════════

    private void HandleAttack()
    {
        if (animator == null) return;
        if (isBlocking) return;

        // ── Vertical attack active ──
        if (isVerticalAttacking)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = st.normalizedTime % 1f;

            if (isChargedMode)
            {
                // Last 20% of animation plays very slowly
                if (normalizedTime >= 0.80f)
                    animator.speed = 0.15f;
                else
                    animator.speed = 2f;

                // Animation fully done
                if (normalizedTime >= 0.99f)
                {
                    animator.speed = 1f;
                    isChargedMode = false;
                }
            }
            else
            {
                // Normal V — freeze on frame 2 if held, tight window only
                if (verticalAttackHeld &&
                    normalizedTime >= verticalAttackHoldFrameTime &&
                    normalizedTime < verticalAttackHoldFrameTime + 0.05f &&
                    !isVerticalAttackHolding)
                {
                    animator.speed = 0f;
                    isVerticalAttackHolding = true;
                }

                if (isVerticalAttackHolding && !verticalAttackHeld)
                {
                    animator.speed = 1f;
                    isVerticalAttackHolding = false;
                }
            }

            return;
        }

        // Safety: restore animator speed when no longer in vertical attack
        if (animator.speed != 1f)
        {
            animator.speed = 1f;
            isChargedMode = false;
        }

        if (isAttacking) return;

        // ── Start vertical attack ──
        if (verticalAttackPressed)
        {
            isChargedMode = verticalAttackHoldDuration >= chargeThreshold;

            animator.ResetTrigger(verticalAttackTrigger);
            animator.SetTrigger(verticalAttackTrigger);
            attackPressed = false;
            return;
        }

        // ── Start normal attack ──
        if (!attackPressed) return;
        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(attackTrigger);
    }

    // ═══════════════════════════════════════════
    //  ANIMATOR
    // ═══════════════════════════════════════════

    private void UpdateAnimator()
    {
        if (animator == null) return;

        AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsName("Death")) return;

        SetAnimatorBoolIfExists("isBlocking", isBlocking);
        SetAnimatorBoolIfExists("isVerticalAttacking", isVerticalAttacking);
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

    // ═══════════════════════════════════════════
    //  INPUT CALLBACKS
    // ═══════════════════════════════════════════

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        attackPressed = true;
        if (animator != null && !isAttacking && !isBlocking)
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }
    }

    private void OnBlockPerformed(InputAction.CallbackContext ctx) => blockHeld = true;
    private void OnBlockCanceled(InputAction.CallbackContext ctx) => blockHeld = false;

    private void OnVerticalAttack(InputAction.CallbackContext ctx)
    {
        verticalAttackPressed = true;
        verticalAttackHeld = true;
        verticalAttackHoldDuration = 0f;
    }

    private void OnVerticalAttackCanceled(InputAction.CallbackContext ctx)
    {
        verticalAttackHeld = false;
    }
}