using UnityEngine;

public class ProwlerAI : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;

    [Header("References")]
    public Transform graphics;
    public Animator animator;

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("Debug")]
    public bool showGizmos = true;

    private Rigidbody2D rb;
    private int direction = 1;
    private bool isGrounded = false;
    private bool wasAtEdge = false;
    private float turnCooldown = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (graphics == null)
            graphics = transform.Find("Graphics");

        if (animator == null && graphics != null)
            animator = graphics.GetComponent<Animator>();
        if (animator == null)
            animator = GetComponent<Animator>();

        if (groundLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Ground");
            if (idx >= 0) groundLayer = 1 << idx;
        }
    }

    Vector2 GetSpritePosition()
    {
        if (graphics != null)
            return new Vector2(graphics.position.x, graphics.position.y);
        return transform.position;
    }

    float GetActualDirection()
    {
        if (Mathf.Abs(rb.linearVelocity.x) < 0.01f) return direction;
        return rb.linearVelocity.x > 0 ? 1f : -1f;
    }

    bool IsEdgeAhead()
    {
        Vector2 spritePos = GetSpritePosition();
        float actualDir = GetActualDirection();

        // Step 1: find ground level directly below sprite
        RaycastHit2D groundBelow = Physics2D.Raycast(
            spritePos + Vector2.up,
            Vector2.down,
            20f,
            groundLayer
        );

        if (groundBelow.collider == null) return false;

        float groundY = groundBelow.point.y;

        // Step 2: check ahead at same ground height
        Vector2 aheadOrigin = new Vector2(
            spritePos.x + (actualDir * 2f),
            groundY + 2f
        );

        RaycastHit2D aheadHit = Physics2D.Raycast(
            aheadOrigin,
            Vector2.down,
            4f,
            groundLayer
        );

        return aheadHit.collider == null;
    }

    bool IsWallAhead()
    {
        Vector2 spritePos = GetSpritePosition();
        float actualDir = GetActualDirection();

        RaycastHit2D hit = Physics2D.Raycast(
            spritePos + Vector2.up * 0.5f,
            new Vector2(actualDir, 0),
            1.5f,
            groundLayer
        );
        return hit.collider != null;
    }

    void FixedUpdate()
    {
        CheckGrounded();

        if (turnCooldown > 0f)
            turnCooldown -= Time.fixedDeltaTime;

        if (!isGrounded) return;

        bool edgeAhead = IsEdgeAhead();
        bool wallAhead = IsWallAhead();

        if ((edgeAhead || wallAhead) && !wasAtEdge && turnCooldown <= 0f)
        {
            direction *= -1;
            wasAtEdge = true;
            turnCooldown = 0.5f;
        }
        else if (!edgeAhead && !wallAhead)
        {
            wasAtEdge = false;
        }

        rb.linearVelocity = new Vector2(direction * walkSpeed * -1f, rb.linearVelocity.y);
        UpdateFacing();
    }

    void CheckGrounded()
    {
        Vector2 spritePos = GetSpritePosition();
        RaycastHit2D hit = Physics2D.Raycast(
            spritePos + Vector2.up * 0.5f,
            Vector2.down,
            5f,
            groundLayer
        );
        isGrounded = hit.collider != null && hit.distance < 2f;
    }

    void UpdateFacing()
    {
        if (graphics == null) return;
        float actualDir = GetActualDirection();
        Vector3 s = graphics.localScale;
        s.x = Mathf.Abs(s.x) * (actualDir > 0 ? 1 : -1);
        graphics.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Transform gfx = graphics;
        if (gfx == null) gfx = transform.Find("Graphics");
        if (gfx == null) return;

        Vector2 spritePos = new Vector2(gfx.position.x, gfx.position.y);

        float dir = 1f;
        if (Application.isPlaying && rb != null)
            dir = rb.linearVelocity.x >= 0 ? 1f : -1f;

        // Green = ground check straight down
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            spritePos + Vector2.up,
            spritePos + Vector2.up + Vector2.down * 20f
        );

        // Red = edge check ahead
        Gizmos.color = Color.red;
        Vector2 aheadOrigin = new Vector2(
            spritePos.x + (dir * 2f),
            spritePos.y + 2f
        );
        Gizmos.DrawLine(
            aheadOrigin,
            aheadOrigin + Vector2.down * 4f
        );

        // Blue = wall check
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            spritePos + Vector2.up * 0.5f,
            spritePos + Vector2.up * 0.5f + new Vector2(dir * 1.5f, 0)
        );
    }
}