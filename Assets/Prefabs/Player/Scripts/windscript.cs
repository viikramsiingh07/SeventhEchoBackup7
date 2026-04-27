using UnityEngine;

public class WindGustController : MonoBehaviour
{
    public ParticleSystem wind;
    public Transform player;
    public Rigidbody2D playerRb;

    [Header("Base Wind")]
    public float baseWind = 4f;

    [Header("Player Influence")]
    public float speedMultiplier = 0.5f;

    private ParticleSystem.VelocityOverLifetimeModule velocity;

    void Start()
    {
        velocity = wind.velocityOverLifetime;
        velocity.enabled = true;

        wind.Play();
    }

    void Update()
    {
        float playerSpeed = Mathf.Abs(playerRb.linearVelocity.x);

        float finalWind = baseWind + (playerSpeed * speedMultiplier);

        velocity.x = -finalWind;
    }
}