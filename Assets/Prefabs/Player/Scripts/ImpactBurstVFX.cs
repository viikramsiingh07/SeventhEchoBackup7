// ImpactBurstVFX.cs
// Attach to an "ImpactBurst" prefab.
// Spawned automatically by SlashArcVFX.StopArc() at the blade tip.
// Creates the flash + spark burst visible in the Gemini reference image.
//
// PREFAB SETUP:
// 1. Create empty GameObject "ImpactBurst"
// 2. Add this script
// 3. Create child "CoreFlash" → add SpriteRenderer (white circle sprite, Additive material)
// 4. Create child "Sparks" → add ParticleSystem (see settings in script comments)
// 5. Save as prefab → assign to SlashArcVFX.impactBurstPrefab

using System.Collections;
using UnityEngine;

public class ImpactBurstVFX : MonoBehaviour
{
    [Header("Core Flash")]
    public SpriteRenderer coreFlash;
    public float flashDuration = 0.15f;
    public float flashMaxScale = 1.4f;

    [Header("Sparks Particle System")]
    public ParticleSystem sparks;

    // ── Particle System settings to configure in Inspector ──
    // Duration: 0.1  |  Looping: OFF  |  Start Lifetime: 0.25
    // Start Speed: 7  |  Start Size: 0.06  |  Start Color: White
    // Max Particles: 20
    // [Emission] Burst: Time=0, Count=12
    // [Shape] Shape=Circle, Radius=0.05, Emit from Edge=YES
    // [Renderer] Material=AdditiveWhite, Render Mode=Stretched Billboard
    // [Stretched Billboard] Speed Scale=0.25
    // [Color over Lifetime] White → Transparent
    // [Size over Lifetime] 1 → 0

    void Start()
    {
        if (coreFlash != null)
            StartCoroutine(FlashCore());

        if (sparks != null)
            sparks.Play();
    }

    IEnumerator FlashCore()
    {
        float t = 0f;
        Transform tr = coreFlash.transform;
        tr.localScale = Vector3.zero;

        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float p = t / flashDuration;
            float s = Mathf.Sin(p * Mathf.PI) * flashMaxScale;
            tr.localScale = new Vector3(s, s, 1f);
            Color c = coreFlash.color;
            c.a = 1f - p;
            coreFlash.color = c;
            yield return null;
        }

        coreFlash.enabled = false;
    }
}
