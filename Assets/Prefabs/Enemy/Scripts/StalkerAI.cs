using UnityEngine;
using System.Collections;

public class StalkerAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform graphics;
    public Animator animator;
    public Transform targetPoint;
    public Transform[] eyePoints;
    public LineRenderer[] lasers;

    [Header("Movement")]
    public float floatSpeed = 2f;
    public float floatDistance = 2.5f;
    public float floatHeight = 0.5f;
    public float floatFrequency = 1.5f;

    [Header("Detection")]
    public float detectionRange = 15f;

    [Header("Laser")]
    public float fireRate = 3f;
    public float chargeTime = 0.6f;
    public float trackTime = 0.5f;
    public float laserDuration = 0.05f;
    public int damage = 10;
    public GameObject hitEffectPrefab;

    private float lastFireTime;
    private bool isFiring;
    private Vector3 startPos;
    private int direction = 1;
    private float randomOffset;

    void Start()
    {
        startPos = transform.position;
        randomOffset = Random.Range(0f, 100f);
        lastFireTime = -fireRate; // prevents firing immediately on spawn
    }

    void Update()
    {
        if (player == null || graphics == null) return;

        float distance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(player.position.x, player.position.y)
        );

        if (distance < detectionRange)
        {
            // 🔴 DETECTED - player is close
            FacePlayer();

            if (!isFiring && Time.time > lastFireTime + fireRate)
            {
                StartCoroutine(LaserSequence());
                lastFireTime = Time.time;
            }
        }
        else
        {
            // 🟢 FLOAT FREELY - player is far
            FloatMovement();
            DisableAllLasers();
        }
    }

    void FloatMovement()
    {
        float variation = Mathf.Sin(Time.time * 0.8f + randomOffset) * 0.5f;
        float moveX = direction * (floatSpeed + variation) * Time.deltaTime;

        float time = Time.time + randomOffset;
        float waveY = Mathf.Sin(time * floatFrequency) * floatHeight * Time.deltaTime;

        transform.Translate(new Vector2(moveX, waveY));
        Flip(direction);

        if (Mathf.Abs(transform.position.x - startPos.x) >= floatDistance)
        {
            direction *= -1;
        }
    }

    IEnumerator LaserSequence()
    {
        isFiring = true;

        // Charge phase - lasers off
        DisableAllLasers();
        yield return new WaitForSeconds(chargeTime);

        // Tracking phase - lasers follow player
        float timer = 0f;
        while (timer < trackTime)
        {
            UpdateLasers(GetTargetPosition());
            timer += Time.deltaTime;
            yield return null;
        }

        // Fire phase - lock on and deal damage
        Vector3 finalTarget = GetTargetPosition();
        UpdateLasers(finalTarget);

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            Vector2 hitDir = (player.position - transform.position).normalized;
            ph.TakeDamage(damage, hitDir);
        }

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, finalTarget, Quaternion.identity);

        yield return new WaitForSeconds(laserDuration);

        DisableAllLasers();
        isFiring = false;
        lastFireTime = Time.time;
    }

    Vector3 GetTargetPosition()
    {
        return targetPoint != null ? targetPoint.position : player.position;
    }

    void UpdateLasers(Vector3 target)
    {
        for (int i = 0; i < lasers.Length; i++)
        {
            if (i >= eyePoints.Length) break;
            if (lasers[i] == null || eyePoints[i] == null) continue;

            lasers[i].enabled = true;
            lasers[i].SetPosition(0, eyePoints[i].position);
            lasers[i].SetPosition(1, target);
        }
    }

    void DisableAllLasers()
    {
        foreach (var l in lasers)
        {
            if (l != null)
                l.enabled = false;
        }
    }

    void FacePlayer()
    {
        float dir = player.position.x - transform.position.x;
        if (Mathf.Abs(dir) < 0.01f) return;
        Flip(Mathf.Sign(dir));
    }

    void Flip(float dir)
    {
        Vector3 s = graphics.localScale;
        s.x = Mathf.Abs(s.x) * (dir > 0 ? 1 : -1);
        graphics.localScale = s;
    }
}