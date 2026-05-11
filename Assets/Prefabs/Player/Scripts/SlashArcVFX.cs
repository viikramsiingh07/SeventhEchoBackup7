// SlashArcVFX.cs
// Attach to empty child "SlashArcVFX" on your Player prefab.
// Creates the sweeping double arc trail matching the Gemini reference image.
//
// SETUP:
// 1. Player prefab → create empty child "SlashArcVFX" → add this script
// 2. Inside SlashArcVFX, create two empty children: "TrailCore" and "TrailThin"
// 3. Add TrailRenderer to both, assign in Inspector
// 4. Create Additive material (see guide) → assign to trailMaterial
// 5. Position SlashArcVFX at the sword tip on your character sprite

using System.Collections;
using UnityEngine;

public class SlashArcVFX : MonoBehaviour
{
    [Header("Trail Renderers")]
    public TrailRenderer trailCore;   // Main thick arc
    public TrailRenderer trailThin;   // Thin offset arc (double-line effect)

    [Header("Material — must be Additive blend")]
    public Material trailMaterial;

    [Header("Arc Settings")]
    public float trailTime      = 0.28f;
    public float coreStartWidth = 0.45f;
    public float thinStartWidth = 0.12f;
    public float thinOffset     = 0.18f;  // Y offset for thin trail

    [Header("Impact Burst Prefab")]
    public GameObject impactBurstPrefab;

    void Awake() => InitTrails();

    void InitTrails()
    {
        if (trailCore == null || trailThin == null) return;
        ConfigureTrail(trailCore, coreStartWidth,
            new Color(1f,1f,1f,1f), new Color(0.8f,0.9f,1f,0f));
        ConfigureTrail(trailThin, thinStartWidth,
            new Color(0.9f,0.95f,1f,0.8f), new Color(0.7f,0.85f,1f,0f));
        trailThin.transform.localPosition = new Vector3(0f, -thinOffset, 0f);
        trailCore.emitting = false;
        trailThin.emitting = false;
    }

    void ConfigureTrail(TrailRenderer t, float startW, Color c0, Color c1)
    {
        t.time = trailTime; t.startWidth = startW; t.endWidth = 0f;
        t.minVertexDistance = 0.03f;
        t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        t.receiveShadows = false;
        if (trailMaterial != null) { t.material = trailMaterial; t.sharedMaterial = trailMaterial; }

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]{ new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
            new GradientAlphaKey[]{ new GradientAlphaKey(c0.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        t.colorGradient = g;

        AnimationCurve w = new AnimationCurve();
        w.AddKey(0f, 1f); w.AddKey(0.3f, 0.8f); w.AddKey(1f, 0f);
        t.widthCurve = w;
    }

    // Called by NinjaAttack.EnableSwordTrail() via Animation Event
    public void StartArc()
    {
        trailCore.Clear(); trailThin.Clear();
        trailCore.emitting = true; trailThin.emitting = true;
    }

    // Called by NinjaAttack.DisableSwordTrail() via Animation Event
    public void StopArc()
    {
        trailCore.emitting = false; trailThin.emitting = false;
        if (impactBurstPrefab != null)
            Destroy(Instantiate(impactBurstPrefab, transform.position, Quaternion.identity), 0.6f);
    }
}
