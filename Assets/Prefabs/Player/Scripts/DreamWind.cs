// ============================================================
//  DreamWindVFX.cs  –  Seventh Echo  (v2 — fixed)
//
//  Changes from v1:
//  - Streaks spawn from taller edge, cover full screen height
//  - Dream Mist uses software alpha instead of broken URP shader
//  - All large square artifacts removed
//  - Softer, wider, more cinematic feel
// ============================================================
using UnityEngine;

public class DreamWindVFX : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static DreamWindVFX Instance { get; private set; }

    // ── References ────────────────────────────────────────────
    [Header("Player")]
    public Transform player;
    public Rigidbody2D playerRb;

    // ── Wind tuning ───────────────────────────────────────────
    [Header("Wind Feel")]
    public float baseWindSpeed = 4f;
    [Range(0f, 1f)]
    public float playerSpeedInfluence = 0.3f;

    [Header("Gust Timing")]
    public float gustIntervalMin = 5f;
    public float gustIntervalMax = 12f;
    public float gustStrength = 5f;
    public float gustDuration = 1.4f;
    public float dashSpeedThreshold = 14f;

    // ── Particle systems ──────────────────────────────────────
    private ParticleSystem _streaks;
    private ParticleSystem _dustMotes;
    private ParticleSystem _rockDebris;
    private ParticleSystem _dreamMist;

    private ParticleSystem.VelocityOverLifetimeModule _streaksVel;
    private ParticleSystem.VelocityOverLifetimeModule _motesVel;
    private ParticleSystem.VelocityOverLifetimeModule _debrisVel;

    // ── Fade ──────────────────────────────────────────────────
    private float _alpha = 0f;
    private float _fadeTarget = 0f;
    private float _fadeDur = 0.8f;
    private bool _fading = false;

    // ── Gust ──────────────────────────────────────────────────
    private float _gustTimer;
    private float _nextGust;
    private bool _gustActive;
    private float _gustCur;
    private float _gustTarget;

    // ── Palette ───────────────────────────────────────────────
    static readonly Color CStreak = new Color(0.88f, 0.95f, 1.00f);
    static readonly Color CMote = new Color(0.78f, 0.68f, 1.00f);
    static readonly Color CDebris = new Color(0.35f, 0.30f, 0.42f);
    static readonly Color CMist = new Color(0.55f, 0.38f, 0.80f);

    const float AStreak = 0.55f;
    const float AMote = 0.65f;
    const float ADebris = 0.40f;
    const float AMist = 0.18f;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildStreaks();
        BuildDustMotes();
        BuildRockDebris();
        BuildDreamMist();
        SetAllAlpha(0f);
        _nextGust = Random.Range(gustIntervalMin, gustIntervalMax);
    }

    void Update()
    {
        DoFade();
        if (_alpha <= 0.01f) return;
        DoGust();
        DriveVelocity();
    }

    void LateUpdate()
    {
        Follow(_dustMotes);
        Follow(_rockDebris);
        Follow(_dreamMist);
    }

    void Follow(ParticleSystem ps)
    {
        if (ps == null || player == null) return;
        Vector3 t = new Vector3(player.position.x, player.position.y, 0f);
        ps.transform.position = Vector3.Lerp(ps.transform.position, t, Time.deltaTime * 5f);
    }

    // ══ LAYER 1 — WIND STREAKS ════════════════════════════════
    void BuildStreaks()
    {
        _streaks = Make("WindStreaks");
        var m = _streaks.main;
        m.duration = 5f;
        m.loop = true;
        m.startLifetime = Rand(0.3f, 2.2f);      // wide range — some short snappy, some long lazy
        m.startSpeed = Rand(4f, 20f);           // big speed variance — chaotic feel
        m.startSize = Rand(0.008f, 0.06f);      // thin — hair-like streaks not rectangles
        m.startColor = A(CStreak, AStreak);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 300;
        m.gravityModifier = 0f;

        var e = _streaks.emission;
        e.rateOverTime = 80f;

        // Tall vertical edge far right — covers full screen height
        var s = _streaks.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        s.radius = 14f;                           // even taller — covers screen top to bottom
        s.position = new Vector3(16f, 0f, 0f);
        s.rotation = new Vector3(0f, 0f, 90f);

        _streaksVel = _streaks.velocityOverLifetime;
        _streaksVel.enabled = true;
        _streaksVel.space = ParticleSystemSimulationSpace.World;
        SetVelocity(_streaksVel, -8f, -20f, -1.2f, 1.2f);

        AlphaLife(_streaks, CStreak, new[] { 0f, 0.1f, 0.9f, 1f }, new[] { 0f, 1f, 0.8f, 0f });

        var n = _streaks.noise;
        n.enabled = true;
        n.strength = 1.2f;       // strong — streaks visibly curl and deviate
        n.frequency = 0.5f;       // medium frequency — waves not tiny jitter
        n.scrollSpeed = 0.4f;       // scrolls fast — constantly changing
        n.octaveCount = 2;          // two layers of noise — more organic
        n.octaveMultiplier = 0.5f;
        n.quality = ParticleSystemNoiseQuality.Medium;

        var r = Rend(_streaks);
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.12f;    // longer stretch
        r.lengthScale = 4.0f;     // much longer tails
        r.sortingOrder = 3;
        SetMat(r, CStreak, true);

        _streaks.Play();
    }

    // ══ LAYER 2 — DUST MOTES ══════════════════════════════════
    void BuildDustMotes()
    {
        _dustMotes = Make("DustMotes");
        var m = _dustMotes.main;
        m.duration = 5f;
        m.loop = true;
        m.startLifetime = Rand(3f, 7f);
        m.startSpeed = Rand(0.05f, 0.5f);
        m.startSize = Rand(0.02f, 0.07f);
        m.startColor = A(CMote, AMote);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 150;
        m.gravityModifier = -0.015f;

        var e = _dustMotes.emission;
        e.rateOverTime = 16f;

        var s = _dustMotes.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Box;
        s.scale = new Vector3(28f, 14f, 1f);

        _motesVel = _dustMotes.velocityOverLifetime;
        _motesVel.enabled = true;
        _motesVel.space = ParticleSystemSimulationSpace.World;
        SetVelocity(_motesVel, -0.8f, -2.0f, -0.1f, 0.35f);

        AlphaLife(_dustMotes, CMote, new[] { 0f, 0.2f, 0.8f, 1f }, new[] { 0f, 1f, 1f, 0f });

        var sOL = _dustMotes.sizeOverLifetime;
        sOL.enabled = true;
        var c = new AnimationCurve();
        c.AddKey(0f, 0.2f);
        c.AddKey(0.3f, 1.0f);
        c.AddKey(0.8f, 0.7f);
        c.AddKey(1f, 0.0f);
        sOL.size = new ParticleSystem.MinMaxCurve(1f, c);

        var n = _dustMotes.noise;
        n.enabled = true;
        n.strength = 0.45f;
        n.frequency = 0.12f;
        n.scrollSpeed = 0.08f;
        n.quality = ParticleSystemNoiseQuality.Medium;

        var r = Rend(_dustMotes);
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 2;
        SetMat(r, CMote, true);

        _dustMotes.Play();
    }

    // ══ LAYER 3 — ROCK DEBRIS ═════════════════════════════════
    void BuildRockDebris()
    {
        _rockDebris = Make("RockDebris");
        var m = _rockDebris.main;
        m.duration = 5f;
        m.loop = true;
        m.startLifetime = Rand(2f, 5f);
        m.startSpeed = Rand(0.1f, 1.0f);
        m.startSize = Rand(0.025f, 0.07f);   // small — never square-looking
        m.startColor = A(CDebris, ADebris);
        m.startRotation = Rand(0f, 360f * Mathf.Deg2Rad);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 60;
        m.gravityModifier = 0.03f;

        var e = _rockDebris.emission;
        e.rateOverTime = 7f;

        var s = _rockDebris.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Box;
        s.scale = new Vector3(22f, 9f, 1f);

        _debrisVel = _rockDebris.velocityOverLifetime;
        _debrisVel.enabled = true;
        _debrisVel.space = ParticleSystemSimulationSpace.World;
        SetVelocity(_debrisVel, -0.6f, -2.0f, -0.2f, 0.1f);

        var rot = _rockDebris.rotationOverLifetime;
        rot.enabled = true;
        rot.z = Rand(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);

        AlphaLife(_rockDebris, CDebris, new[] { 0f, 0.15f, 0.85f, 1f }, new[] { 0f, 1f, 0.5f, 0f });

        var n = _rockDebris.noise;
        n.enabled = true;
        n.strength = 0.18f;
        n.frequency = 0.25f;

        var r = Rend(_rockDebris);
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 1;
        SetMat(r, CDebris, false);

        _rockDebris.Play();
    }

    // ══ LAYER 4 — DREAM MIST ══════════════════════════════════
    // Uses many small overlapping particles instead of few large
    // ones — avoids the "square" look entirely.
    void BuildDreamMist()
    {
        _dreamMist = Make("DreamMist");
        var m = _dreamMist.main;
        m.duration = 8f;
        m.loop = true;
        m.startLifetime = Rand(4f, 9f);
        m.startSpeed = Rand(0.2f, 0.8f);
        m.startSize = Rand(0.3f, 1.2f);     // smaller — no squares
        m.startColor = A(CMist, AMist);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 60;                    // more particles, smaller size
        m.gravityModifier = -0.008f;

        var e = _dreamMist.emission;
        e.rateOverTime = 5f;

        var s = _dreamMist.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Box;
        s.scale = new Vector3(32f, 16f, 1f);

        var vel = _dreamMist.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        SetVelocity(vel, -0.4f, -1.2f, -0.05f, 0.15f);

        AlphaLife(_dreamMist, CMist, new[] { 0f, 0.3f, 0.7f, 1f }, new[] { 0f, 1f, 1f, 0f });

        var sOL = _dreamMist.sizeOverLifetime;
        sOL.enabled = true;
        var c = new AnimationCurve();
        c.AddKey(0f, 0.0f);
        c.AddKey(0.35f, 1.0f);
        c.AddKey(0.65f, 0.9f);
        c.AddKey(1f, 0.0f);
        sOL.size = new ParticleSystem.MinMaxCurve(1f, c);

        var n = _dreamMist.noise;
        n.enabled = true;
        n.strength = 0.6f;
        n.frequency = 0.08f;
        n.scrollSpeed = 0.04f;
        n.quality = ParticleSystemNoiseQuality.High;

        var r = Rend(_dreamMist);
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 0;
        SetMat(r, CMist, true);

        _dreamMist.Play();
    }

    // ══ RUNTIME ═══════════════════════════════════════════════

    void DoFade()
    {
        if (!_fading) return;
        _alpha = Mathf.MoveTowards(_alpha, _fadeTarget, Time.deltaTime / _fadeDur);
        SetAllAlpha(_alpha);
        if (Mathf.Approximately(_alpha, _fadeTarget)) _fading = false;
    }

    void DoGust()
    {
        _gustTimer += Time.deltaTime;
        float spd = playerRb ? Mathf.Abs(playerRb.linearVelocity.x) : 0f;
        if (!_gustActive && _gustTimer >= _nextGust) TriggerGust(gustStrength);
        if (!_gustActive && spd >= dashSpeedThreshold) TriggerGust(gustStrength * 2f);
        _gustCur = Mathf.Lerp(_gustCur, _gustTarget, Time.deltaTime * (_gustActive ? 9f : 4f));
        if (_gustActive && _gustTimer >= _nextGust + gustDuration)
        {
            _gustActive = false; _gustTarget = 0f;
            _nextGust = _gustTimer + Random.Range(gustIntervalMin, gustIntervalMax);
        }
    }

    void DriveVelocity()
    {
        float spd = playerRb ? Mathf.Abs(playerRb.linearVelocity.x) : 0f;
        float wind = (baseWindSpeed + spd * playerSpeedInfluence + _gustCur) * _alpha;
        SetVelocity(_streaksVel, -wind * 1.2f, -wind * 1.9f, -1.2f, 1.2f);
        SetVelocity(_motesVel, -wind * 0.25f, -wind * 0.55f, -0.1f, 0.35f);
        SetVelocity(_debrisVel, -wind * 0.35f, -wind * 0.75f, -0.2f, 0.1f);
    }

    void SetAllAlpha(float a)
    {
        PS(_streaks, CStreak, AStreak * a);
        PS(_dustMotes, CMote, AMote * a);
        PS(_rockDebris, CDebris, ADebris * a);
        PS(_dreamMist, CMist, AMist * a);
    }

    void PS(ParticleSystem ps, Color c, float a)
    {
        if (ps == null) return;
        var main = ps.main; main.startColor = A(c, a);
    }

    // ══ PUBLIC API ════════════════════════════════════════════

    public void FadeIn(float dur = 0.8f) { _fadeTarget = 1f; _fadeDur = dur; _fading = true; }
    public void FadeOut(float dur = 1.2f) { _fadeTarget = 0f; _fadeDur = dur; _fading = true; }

    public void TriggerGust(float strength)
    {
        _gustActive = true; _gustTarget = strength; _gustTimer = _nextGust;
    }

    // ══ HELPERS ═══════════════════════════════════════════════

    ParticleSystem Make(string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        return go.AddComponent<ParticleSystem>();
    }

    ParticleSystemRenderer Rend(ParticleSystem ps) =>
        ps.GetComponent<ParticleSystemRenderer>();

    static ParticleSystem.MinMaxCurve Rand(float a, float b) =>
        new ParticleSystem.MinMaxCurve(a, b);

    // Sets all 3 axes to TwoConstants mode at once — fixes "curves must be same mode"
    static void SetVelocity(ParticleSystem.VelocityOverLifetimeModule vel,
                            float xMin, float xMax,
                            float yMin, float yMax)
    {
        var zero = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.x = zero; vel.y = zero; vel.z = zero;   // prime all to TwoConstants first
        vel.x = new ParticleSystem.MinMaxCurve(xMin, xMax);
        vel.y = new ParticleSystem.MinMaxCurve(yMin, yMax);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    static Color A(Color c, float a) { c.a = a; return c; }

    void AlphaLife(ParticleSystem ps, Color col, float[] t, float[] a)
    {
        var mod = ps.colorOverLifetime;
        mod.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(col, 0f), new GradientColorKey(col, 1f) },
            System.Array.ConvertAll(t, (i) => new GradientAlphaKey(a[System.Array.IndexOf(t, i)], i))
        );
        mod.color = new ParticleSystem.MinMaxGradient(g);
    }

    void SetMat(ParticleSystemRenderer r, Color tint, bool additive)
    {
        // For stretched streaks, URP Particles/Unlit works fine.
        // For billboard particles (mist, motes, debris), Sprites/Default
        // is the most reliable in URP 2D projects — no square artifacts.
        Shader sh;
        if (r.renderMode == ParticleSystemRenderMode.Stretch)
        {
            sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
              ?? Shader.Find("Particles/Standard Unlit")
              ?? Shader.Find("Sprites/Default");
        }
        else
        {
            // Billboard particles — always use Sprites/Default in URP 2D
            sh = Shader.Find("Sprites/Default")
              ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        }

        if (sh == null) return;
        var mat = new Material(sh);
        mat.color = additive
            ? new Color(tint.r, tint.g, tint.b, 0.6f)
            : new Color(tint.r, tint.g, tint.b, 0.5f);

        if (additive && r.renderMode == ParticleSystemRenderMode.Stretch)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 3f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3000;
        }
        else
        {
            mat.renderQueue = 3000;
        }

        r.material = mat;
    }
}