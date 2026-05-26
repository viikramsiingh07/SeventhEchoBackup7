// ============================================================
//  DreamWindVFX.cs  –  Seventh Echo
//  Dark Cold Dream — Built-in Pipeline — Camera Size ~9.6
// ============================================================
using UnityEngine;

public class DreamWindVFX : MonoBehaviour
{
    public static DreamWindVFX Instance { get; private set; }

    [Header("Player")]
    public Transform player;
    public Rigidbody2D playerRb;

    [Header("Wind Feel")]
    public float baseWindSpeed = 5f;
    [Range(0f, 1f)]
    public float playerSpeedInfluence = 0.25f;

    [Header("Gust Timing")]
    public float gustIntervalMin = 6f;
    public float gustIntervalMax = 14f;
    public float gustStrength = 6f;
    public float gustDuration = 1.6f;
    public float dashSpeedThreshold = 14f;

    // ── Particle Systems ──
    private ParticleSystem _coldStreaks;
    private ParticleSystem _dreamFireflies;
    private ParticleSystem _iceMotes;
    private ParticleSystem _voidMist;

    private ParticleSystem.VelocityOverLifetimeModule _streaksVel;
    private ParticleSystem.VelocityOverLifetimeModule _firefliesVel;
    private ParticleSystem.VelocityOverLifetimeModule _motesVel;

    // ── Fade ──
    private float _alpha = 0f;
    private float _fadeTarget = 0f;
    private float _fadeDur = 1f;
    private bool _fading = false;

    // ── Gust ──
    private float _gustTimer;
    private float _nextGust;
    private bool _gustActive;
    private float _gustCur;
    private float _gustTarget;

    // ── Shared soft circle texture ──
    private Texture2D _softCircleTex;

    // ── Cold Dream Palette ──
    static readonly Color CColdStreak = new Color(0.72f, 0.88f, 1.00f);
    static readonly Color CFirefly = new Color(0.60f, 0.95f, 1.00f);
    static readonly Color CIceMote = new Color(0.85f, 0.93f, 1.00f);
    static readonly Color CVoidMist = new Color(0.20f, 0.15f, 0.35f);

    const float AStreak = 0.55f;
    const float AFirefly = 1.0f;
    const float AMote = 0.80f;
    const float AVoid = 0.35f;

    // ═══════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _softCircleTex = CreateSoftCircleTexture(64);
    }

    void Start()
    {
        BuildColdStreaks();
        BuildDreamFireflies();
        BuildIceMotes();
        BuildVoidMist();
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
        Follow(_dreamFireflies);
        Follow(_iceMotes);
        Follow(_voidMist);
    }

    void Follow(ParticleSystem ps)
    {
        if (ps == null || player == null) return;
        Vector3 t = new Vector3(player.position.x, player.position.y, 0f);
        ps.transform.position = Vector3.Lerp(ps.transform.position, t, Time.deltaTime * 4f);
    }

    // ═══════════════════════════════════════════
    //  LAYER 1 — COLD WIND STREAKS
    // ═══════════════════════════════════════════

    void BuildColdStreaks()
    {
        _coldStreaks = Make("ColdStreaks");
        _coldStreaks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var m = _coldStreaks.main;
        m.duration = 6f;
        m.loop = true;
        m.startLifetime = Rand(0.4f, 1.8f);
        m.startSpeed = Rand(8f, 22f);
        m.startSize = Rand(0.04f, 0.18f);
        m.startColor = A(CColdStreak, AStreak);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 250;
        m.gravityModifier = 0f;

        var e = _coldStreaks.emission;
        e.rateOverTime = 60f;

        var s = _coldStreaks.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        s.radius = 22f;
        s.position = new Vector3(22f, 0f, 0f);
        s.rotation = new Vector3(0f, 0f, 90f);

        _streaksVel = _coldStreaks.velocityOverLifetime;
        _streaksVel.enabled = true;
        _streaksVel.space = ParticleSystemSimulationSpace.World;
        SetVel(_streaksVel, -10f, -25f, -0.8f, 0.8f);

        AlphaLife(_coldStreaks, CColdStreak,
            new[] { 0f, 0.05f, 0.85f, 1f },
            new[] { 0f, 1f, 0.6f, 0f });

        var n = _coldStreaks.noise;
        n.enabled = true;
        n.strength = 0.6f;
        n.frequency = 0.3f;
        n.scrollSpeed = 0.5f;
        n.octaveCount = 2;
        n.quality = ParticleSystemNoiseQuality.Medium;

        var r = Rend(_coldStreaks);
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.08f;
        r.lengthScale = 6f;
        r.sortingOrder = 5;
        r.sortingLayerName = "Default";

        Shader sh = Shader.Find("Particles/Additive")
                 ?? Shader.Find("Legacy Shaders/Particles/Additive")
                 ?? Shader.Find("Sprites/Default");
        if (sh != null)
        {
            var mat = new Material(sh);
            mat.color = new Color(CColdStreak.r, CColdStreak.g, CColdStreak.b, 0.9f);
            r.material = mat;
        }

        _coldStreaks.Play();
    }

    // ═══════════════════════════════════════════
    //  LAYER 2 — DREAM FIREFLIES
    // ═══════════════════════════════════════════

    void BuildDreamFireflies()
    {
        _dreamFireflies = Make("DreamFireflies");
        _dreamFireflies.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var m = _dreamFireflies.main;
        m.duration = 6f;
        m.loop = true;
        m.startLifetime = Rand(3f, 7f);
        m.startSpeed = Rand(0.02f, 0.25f);
        m.startSize = Rand(0.15f, 0.40f);
        m.startColor = A(CFirefly, AFirefly);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 180;
        m.gravityModifier = -0.006f;

        var e = _dreamFireflies.emission;
        e.rateOverTime = 22f;

        var s = _dreamFireflies.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Box;
        s.scale = new Vector3(40f, 22f, 1f);

        _firefliesVel = _dreamFireflies.velocityOverLifetime;
        _firefliesVel.enabled = true;
        _firefliesVel.space = ParticleSystemSimulationSpace.World;
        SetVel(_firefliesVel, -0.1f, -0.4f, -0.06f, 0.06f);

        var sOL = _dreamFireflies.sizeOverLifetime;
        sOL.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.10f, 1f);
        sizeCurve.AddKey(0.25f, 0.2f);
        sizeCurve.AddKey(0.40f, 0.9f);
        sizeCurve.AddKey(0.55f, 0.15f);
        sizeCurve.AddKey(0.70f, 0.8f);
        sizeCurve.AddKey(0.85f, 0.1f);
        sizeCurve.AddKey(1f, 0f);
        sOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var col = _dreamFireflies.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(CFirefly,                 0f),
                new GradientColorKey(new Color(0.9f, 1f, 1f), 0.4f),
                new GradientColorKey(CFirefly,                 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f,   0f),
                new GradientAlphaKey(1f,   0.10f),
                new GradientAlphaKey(0.1f, 0.25f),
                new GradientAlphaKey(1f,   0.40f),
                new GradientAlphaKey(0.1f, 0.55f),
                new GradientAlphaKey(0.9f, 0.70f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var n = _dreamFireflies.noise;
        n.enabled = true;
        n.strength = 0.5f;
        n.frequency = 0.3f;
        n.scrollSpeed = 0.2f;
        n.octaveCount = 2;
        n.quality = ParticleSystemNoiseQuality.Medium;

        var r = Rend(_dreamFireflies);
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 4;
        r.sortingLayerName = "Default";
        SetCircleMat(r, CFirefly, true);

        _dreamFireflies.Play();
    }

    // ═══════════════════════════════════════════
    //  LAYER 3 — ICE MOTES
    // ═══════════════════════════════════════════

    void BuildIceMotes()
    {
        _iceMotes = Make("IceMotes");
        _iceMotes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var m = _iceMotes.main;
        m.duration = 6f;
        m.loop = true;
        m.startLifetime = Rand(3f, 8f);
        m.startSpeed = Rand(0.02f, 0.3f);
        m.startSize = Rand(0.08f, 0.25f);
        m.startColor = A(CIceMote, AMote);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 200;
        m.gravityModifier = -0.005f;

        var e = _iceMotes.emission;
        e.rateOverTime = 25f;

        var s = _iceMotes.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Box;
        s.scale = new Vector3(40f, 22f, 1f);

        _motesVel = _iceMotes.velocityOverLifetime;
        _motesVel.enabled = true;
        _motesVel.space = ParticleSystemSimulationSpace.World;
        SetVel(_motesVel, -0.3f, -1.2f, -0.08f, 0.08f);

        var sOL = _iceMotes.sizeOverLifetime;
        sOL.enabled = true;
        var c = new AnimationCurve();
        c.AddKey(0f, 0f);
        c.AddKey(0.15f, 1f);
        c.AddKey(0.5f, 0.5f);
        c.AddKey(0.75f, 1f);
        c.AddKey(1f, 0f);
        sOL.size = new ParticleSystem.MinMaxCurve(1f, c);

        AlphaLife(_iceMotes, CIceMote,
            new[] { 0f, 0.1f, 0.5f, 0.9f, 1f },
            new[] { 0f, 1f, 0.5f, 1f, 0f });

        var n = _iceMotes.noise;
        n.enabled = true;
        n.strength = 0.25f;
        n.frequency = 0.2f;
        n.scrollSpeed = 0.1f;
        n.quality = ParticleSystemNoiseQuality.Medium;

        var r = Rend(_iceMotes);
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 3;
        r.sortingLayerName = "Default";
        SetCircleMat(r, CIceMote, true);

        _iceMotes.Play();
    }

    // ═══════════════════════════════════════════
    //  LAYER 4 — VOID MIST
    // ═══════════════════════════════════════════

    void BuildVoidMist()
    {
        _voidMist = Make("VoidMist");
        _voidMist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var m = _voidMist.main;
        m.duration = 8f;
        m.loop = true;
        m.startLifetime = Rand(4f, 9f);
        m.startSpeed = Rand(0.1f, 0.4f);
        m.startSize = Rand(0.5f, 1.8f);
        m.startColor = A(CVoidMist, AVoid);
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 100;
        m.gravityModifier = -0.003f;

        var e = _voidMist.emission;
        e.rateOverTime = 10f;

        var s = _voidMist.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Box;
        s.scale = new Vector3(40f, 4f, 1f);
        s.position = new Vector3(0f, -3f, 0f);

        var vel = _voidMist.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        SetVel(vel, -0.3f, -1.0f, -0.05f, 0.05f);

        var sOL = _voidMist.sizeOverLifetime;
        sOL.enabled = true;
        var c = new AnimationCurve();
        c.AddKey(0f, 0f);
        c.AddKey(0.2f, 1f);
        c.AddKey(0.8f, 0.7f);
        c.AddKey(1f, 0f);
        sOL.size = new ParticleSystem.MinMaxCurve(1f, c);

        AlphaLife(_voidMist, CVoidMist,
            new[] { 0f, 0.2f, 0.8f, 1f },
            new[] { 0f, 0.8f, 0.6f, 0f });

        var n = _voidMist.noise;
        n.enabled = true;
        n.strength = 1.2f;
        n.frequency = 0.08f;
        n.scrollSpeed = 0.04f;
        n.octaveCount = 3;
        n.quality = ParticleSystemNoiseQuality.High;

        var r = Rend(_voidMist);
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 1;
        r.sortingLayerName = "Default";
        SetCircleMat(r, new Color(0.25f, 0.20f, 0.35f), true);

        _voidMist.Play();
    }

    // ═══════════════════════════════════════════
    //  RUNTIME
    // ═══════════════════════════════════════════

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

        if (!_gustActive && _gustTimer >= _nextGust)
            TriggerGust(gustStrength);
        if (!_gustActive && spd >= dashSpeedThreshold)
            TriggerGust(gustStrength * 1.8f);

        _gustCur = Mathf.Lerp(_gustCur, _gustTarget,
                              Time.deltaTime * (_gustActive ? 8f : 3f));

        if (_gustActive && _gustTimer >= _nextGust + gustDuration)
        {
            _gustActive = false;
            _gustTarget = 0f;
            _nextGust = _gustTimer + Random.Range(gustIntervalMin, gustIntervalMax);
        }
    }

    void DriveVelocity()
    {
        float spd = playerRb ? Mathf.Abs(playerRb.linearVelocity.x) : 0f;
        float wind = (baseWindSpeed + spd * playerSpeedInfluence + _gustCur) * _alpha;

        SetVel(_streaksVel, -wind * 1.3f, -wind * 2.2f, -0.8f, 0.8f);
        SetVel(_firefliesVel, -wind * 0.1f, -wind * 0.3f, -0.06f, 0.06f);
        SetVel(_motesVel, -wind * 0.1f, -wind * 0.35f, -0.08f, 0.08f);
    }

    void SetAllAlpha(float a)
    {
        SetPS(_coldStreaks, CColdStreak, AStreak * a);
        SetPS(_dreamFireflies, CFirefly, AFirefly * a);
        SetPS(_iceMotes, CIceMote, AMote * a);
        SetPS(_voidMist, CVoidMist, AVoid * a);
    }

    void SetPS(ParticleSystem ps, Color c, float a)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = A(c, a);
    }

    // ═══════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════

    public void FadeIn(float dur = 1.0f) { _fadeTarget = 1f; _fadeDur = dur; _fading = true; }
    public void FadeOut(float dur = 1.5f) { _fadeTarget = 0f; _fadeDur = dur; _fading = true; }

    public void TriggerGust(float strength)
    {
        _gustActive = true;
        _gustTarget = strength;
        _gustTimer = _nextGust;
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

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

    static void SetVel(ParticleSystem.VelocityOverLifetimeModule vel,
                       float xMin, float xMax, float yMin, float yMax)
    {
        var zero = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.x = zero; vel.y = zero; vel.z = zero;
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
            new GradientColorKey[]
            {
                new GradientColorKey(col, 0f),
                new GradientColorKey(col, 1f)
            },
            System.Array.ConvertAll(t, i =>
                new GradientAlphaKey(a[System.Array.IndexOf(t, i)], i))
        );
        mod.color = new ParticleSystem.MinMaxGradient(g);
    }

    Texture2D CreateSoftCircleTexture(int size = 64)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalized = dist / radius;
                float alpha = Mathf.Clamp01(1f - normalized);
                alpha = alpha * alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    void SetCircleMat(ParticleSystemRenderer r, Color tint, bool additive)
    {
        Shader sh = additive
            ? (Shader.Find("Particles/Additive")
            ?? Shader.Find("Legacy Shaders/Particles/Additive")
            ?? Shader.Find("Sprites/Default"))
            : (Shader.Find("Particles/Alpha Blended")
            ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
            ?? Shader.Find("Sprites/Default"));

        if (sh == null) { Debug.LogWarning("[DreamWindVFX] Shader not found: " + r.name); return; }

        var mat = new Material(sh);
        mat.color = new Color(tint.r, tint.g, tint.b, additive ? 0.9f : 0.7f);
        mat.mainTexture = _softCircleTex;
        r.material = mat;
    }
}