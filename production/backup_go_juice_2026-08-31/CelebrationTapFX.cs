using System.Collections;
using UnityEngine;

/// <summary>
/// [JUICE PACK T1 — 2026-08-31] Pháo hoa + confetti khi USER CHẠM công trình vừa xây xong.
/// Tham chiếu video Township: chạm → bung confetti to, rõ, NỔI TRÊN công trình.
///
/// Cách dùng (1 dòng, gọi từ bất kỳ đâu):
///     CelebrationTapFX.Play(worldPos, 1f);
///
/// Đặc điểm:
///  • Sorting layer "Foreground" + order 500 → LUÔN cao hơn công trình (ObjectsFront).
///  • Chạy bằng unscaled time — popup mở (timeScale=0) vẫn nổ bình thường.
///  • Sprite: ưu tiên art đội vẽ tại Resources/FX/Celebrate/ (confetti_01..06, spark_star,
///    spark_dot) — CHƯA có art thì tự vẽ mảnh màu runtime, vẫn xem được ngay.
///  • Thuần cộng thêm: không đụng ConstructionCompleteFX đang chạy.
/// </summary>
public class CelebrationTapFX : MonoBehaviour
{
    private const string SortLayer   = "Foreground"; // tồn tại sẵn trong project (5 layer đã kiểm)
    private const int    BaseOrder   = 500;
    private const float  LifeTime    = 2.0f;

    // ── Núm chỉnh "to, rõ" theo lệnh Sếp (mặc định đã to hơn FX cũ ~1.8×) ──
    public static float  GlobalScale   = 1.8f;
    public static int    ConfettiCount = 46;
    public static int    SparkCount    = 14;

    private static Sprite[] _artConfetti;   // cache art đội vẽ (nếu có)
    private static Sprite   _artSpark;
    private static Sprite   _fallback;      // ô vuông trắng vẽ runtime
    private static bool     _artLoaded;

    private static readonly Color[] Palette =
    {
        new Color32(255, 92, 92, 255), new Color32(255, 200, 60, 255),
        new Color32(120, 205, 255, 255), new Color32(140, 220, 90, 255),
        new Color32(220, 120, 255, 255), new Color32(255, 150, 40, 255),
    };

    /// <summary>Nổ pháo hoa/confetti tại vị trí world. scale=1 chuẩn, công trình to truyền 1.3-1.6.</summary>
    public static void Play(Vector3 worldPos, float scale = 1f)
    {
        LoadArtOnce();
        var go = new GameObject("CelebrationTapFX");
        go.transform.position = worldPos;
        var fx = go.AddComponent<CelebrationTapFX>();
        fx.StartCoroutine(fx.Routine(Mathf.Max(0.3f, scale) * GlobalScale));
        Destroy(go, LifeTime + 0.5f);
    }

    private static void LoadArtOnce()
    {
        if (_artLoaded) return;
        _artLoaded = true;
        var list = new System.Collections.Generic.List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            var s = Resources.Load<Sprite>($"FX/Celebrate/confetti_{i:00}");
            if (s != null) list.Add(s);
        }
        _artConfetti = list.Count > 0 ? list.ToArray() : null;
        _artSpark = Resources.Load<Sprite>("FX/Celebrate/spark_star");
    }

    private static Sprite Fallback()
    {
        if (_fallback != null) return _fallback;
        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[64];
        for (int i = 0; i < 64; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px); tex.Apply();
        _fallback = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        return _fallback;
    }

    private IEnumerator Routine(float scale)
    {
        // ── Đợt 1: vòng SPARK toả tròn (pháo hoa) ──
        for (int i = 0; i < SparkCount; i++)
            SpawnPiece(spark: true, i, SparkCount, scale);
        // ── Đợt 2 (trễ 1 nhịp ngắn): mưa CONFETTI ──
        yield return WaitUnscaled(0.08f);
        for (int i = 0; i < ConfettiCount; i++)
            SpawnPiece(spark: false, i, ConfettiCount, scale);
    }

    private void SpawnPiece(bool spark, int index, int total, float scale)
    {
        var go = new GameObject(spark ? "Spark" : "Confetti");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();

        if (spark) sr.sprite = _artSpark != null ? _artSpark : Fallback();
        else       sr.sprite = _artConfetti != null
                                ? _artConfetti[Random.Range(0, _artConfetti.Length)]
                                : Fallback();
        if (sr.sprite == _fallback)
            sr.color = Palette[Random.Range(0, Palette.Length)];

        sr.sortingLayerName = SortLayer;
        sr.sortingOrder = BaseOrder + (spark ? 10 : 0) + index % 10;

        float ang = (360f / total) * index + Random.Range(-14f, 14f);
        Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
        float speed = spark ? Random.Range(260f, 380f) : Random.Range(120f, 260f);
        float size  = (spark ? Random.Range(28f, 40f) : Random.Range(16f, 30f)) * scale;
        go.transform.localScale = Vector3.one * size;

        StartCoroutine(PieceRoutine(go.transform, sr, dir * speed * scale, spark));
    }

    private IEnumerator PieceRoutine(Transform t, SpriteRenderer sr, Vector2 vel, bool spark)
    {
        float life = spark ? Random.Range(0.45f, 0.7f) : Random.Range(1.1f, 1.7f);
        float g    = spark ? 60f : 420f;                    // confetti rơi nặng hơn
        float spin = Random.Range(-540f, 540f);
        float e = 0f;
        Vector3 baseScale = t.localScale;
        while (e < life && t != null)
        {
            float dt = Time.unscaledDeltaTime;
            e += dt;
            vel += Vector2.down * g * dt;
            if (!spark) vel.x *= (1f - 1.6f * dt);          // confetti chao nghiêng chậm dần
            t.position += (Vector3)(vel * dt * 0.01f);
            t.Rotate(0f, 0f, spin * dt);
            float k = e / life;
            if (k > 0.55f)
            {
                var c = sr.color; c.a = 1f - (k - 0.55f) / 0.45f; sr.color = c;
                t.localScale = baseScale * (1f - 0.25f * (k - 0.55f) / 0.45f);
            }
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }

    private static IEnumerator WaitUnscaled(float s)
    { float e = 0f; while (e < s) { e += Time.unscaledDeltaTime; yield return null; } }
}
