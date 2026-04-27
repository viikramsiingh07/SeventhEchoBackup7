using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public float moveDistance = 2f;
    public float speed = 1f;

    // expose current platform velocity so PlayerMovement can inherit it
    public Vector2 Velocity { get; private set; }

    private Rigidbody2D rb;
    private Vector2 startPos;
    private Vector2 lastPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        startPos = rb.position;
        lastPos = rb.position;
    }

    private void FixedUpdate()
    {
        // use FixedTime so the motion is stable in physics steps
        float x = Mathf.Sin(Time.fixedTime * speed) * moveDistance;
        Vector2 target = new Vector2(startPos.x + x, startPos.y);
        rb.MovePosition(target);

        // compute velocity for this physics frame
        Velocity = (rb.position - lastPos) / Time.fixedDeltaTime;
        lastPos = rb.position;
    }
}