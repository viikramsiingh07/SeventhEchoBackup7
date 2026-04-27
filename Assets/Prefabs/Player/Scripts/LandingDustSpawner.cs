using UnityEngine;

public class LandingDustSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject dustPrefab;

    [SerializeField] private float rayHeight = 5f;
    [SerializeField] private float maxDistance = 2f;

    private bool wasGrounded;

    void Update()
    {
        Vector2 origin = new Vector2(player.position.x, player.position.y + rayHeight);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayHeight * 2f, groundLayer);

        bool isGrounded = false;

        if (hit.collider != null)
        {
            float dist = player.position.y - hit.point.y;
            if (dist <= maxDistance)
                isGrounded = true;
        }

        // 🔥 LANDING DETECT
        if (!wasGrounded && isGrounded && rb.linearVelocity.y <= 0f)
        {
            SpawnDust(hit.point);
        }

        wasGrounded = isGrounded;
    }

    void SpawnDust(Vector2 position)
    {
        Instantiate(dustPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
    }
}