using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiệu ứng world-space tại bến khi MỞ SLOT thành công (BOAT-002 §3.6):
///   • Bảng khóa scale-out (punch nhẹ 1→1.15 rồi thu về 0) + fade toàn bộ
///     SpriteRenderer/TMP con → SetActive(false) khi xong.
///   • 8-12 particle sao/tia vàng tự sinh bằng SpriteRenderer (sprite sao vẽ
///     procedural, cache static — KHÔNG cần asset, không cần ParticleSystem)
///     bay tỏa tròn, xoay, nhỏ dần + mờ dần.
///   • SFX: AudioManager.PlayBuySell (pattern có sẵn — FarmEconomyManager dùng
///     đúng hàm này cho mọi giao dịch mua/bán).
///
/// Cách gọi (TouristBoatUnlockFlow gọi trong handler OnDockUnlocked):
///     DockUnlockCelebrationFX.Phat(viTriBang, bangKhoaRoot);
/// bangKhoaRoot cho phép null (chỉ nổ sao, không có bảng để thu).
///
/// Kích thước tính theo UNIT WORLD của map (hệ tọa độ rất lớn — bảng khóa
/// ~520 unit, xem TouristBoatConfig.lockPanelWidth) nên sao mặc định 40-75 unit.
/// Mọi tween là coroutine tự viết; FX dùng Time.deltaTime (world FX theo nhịp game).
/// </summary>
public class DockUnlockCelebrationFX : MonoBehaviour
{
    // ─── Tuning (const — FX one-shot tạo bằng code, không qua Inspector) ────

    private const int   SoSaoMin        = 8;     // GDD §3.6: 8-12 particle
    private const int   SoSaoMax        = 12;
    private const float SaoSizeMin      = 40f;   // unit world
    private const float SaoSizeMax      = 75f;
    private const float SaoTocDoMin     = 350f;  // unit/giây — tỏa đủ rộng so với bảng ~520
    private const float SaoTocDoMax     = 700f;
    private const float SaoDoiMin       = 0.55f; // giây sống của 1 sao
    private const float SaoDoiMax       = 0.95f;
    private const float SaoTrongLuc     = 420f;  // kéo nhẹ xuống cho có "rơi"
    private const float BangThuSeconds  = 0.45f; // punch + thu bảng khóa
    private const int   SortingOrderSao = 210;   // trên bảng khóa (LockUI sorting ~50)

    // 2 tông vàng ấm — sao xen kẽ cho đỡ đều
    private static readonly Color MauSao1 = new Color(1f, 0.827f, 0.302f); // #FFD34D — vàng HUD
    private static readonly Color MauSao2 = new Color(1f, 0.95f, 0.70f);  // vàng nhạt sáng

    private static Sprite _spriteSao; // cache — vẽ 1 lần cho cả session

    // ─── Runtime của 1 lần nổ ───────────────────────────────────────────────

    private struct Sao
    {
        public Transform      T;
        public SpriteRenderer Sr;
        public Vector2        VanToc;
        public float          XoayDoGiay;
        public float          Doi;      // tuổi thọ
        public float          Tuoi;
        public float          SizeGoc;
    }

    private readonly List<Sao> _saoList = new List<Sao>();
    private Transform _bangKhoa;

    // =========================================================================
    //  API tĩnh — một dòng gọi từ bất kỳ đâu
    // =========================================================================

    /// <summary>
    /// Nổ hiệu ứng mở slot tại worldPos. bangKhoaRoot (cho phép null) = gốc bảng
    /// khóa world-space — được punch-scale rồi thu về 0 + fade, xong SetActive(false).
    /// Object FX tự hủy khi mọi sao tắt.
    /// </summary>
    public static void Phat(Vector3 worldPos, Transform bangKhoaRoot = null)
    {
        var go = new GameObject("DockUnlockCelebrationFX");
        go.transform.position = worldPos;
        var fx = go.AddComponent<DockUnlockCelebrationFX>();
        fx._bangKhoa = bangKhoaRoot;
        fx.StartCoroutine(fx.ChayRoutine(worldPos));
    }

    // =========================================================================
    //  Vòng chạy chính
    // =========================================================================

    private IEnumerator ChayRoutine(Vector3 tam)
    {
        // SFX mua có sẵn — cùng pattern FarmEconomyManager (null-safe)
        AudioManager.Instance?.PlayBuySell();

        SinhSao(tam);

        // Bảng khóa + sao chạy SONG SONG (bảng là coroutine riêng, sao update dưới)
        if (_bangKhoa != null)
            StartCoroutine(ThuBangKhoaRoutine(_bangKhoa));

        // Update sao tới khi con cuối tắt
        bool conSong = true;
        while (conSong)
        {
            conSong = false;
            float dt = Time.deltaTime;

            for (int i = 0; i < _saoList.Count; i++)
            {
                Sao s = _saoList[i];
                if (s.T == null || s.Tuoi >= s.Doi) continue;

                s.Tuoi += dt;
                float p = Mathf.Clamp01(s.Tuoi / s.Doi);

                // Bay tỏa + trọng lực nhẹ
                s.VanToc += Vector2.down * (SaoTrongLuc * dt);
                s.T.position += (Vector3)(s.VanToc * dt);
                s.T.Rotate(0f, 0f, s.XoayDoGiay * dt);

                // Nhỏ dần về cuối đời + fade (giữ rõ nửa đầu, mờ nhanh nửa sau —
                // cùng "gu" floating text của BoatDockSlot)
                float size = s.SizeGoc * Mathf.Lerp(1f, 0.15f, p * p);
                s.T.localScale = new Vector3(size, size, 1f);
                if (s.Sr != null)
                {
                    Color c = s.Sr.color;
                    c.a = 1f - p * p;
                    s.Sr.color = c;
                }

                _saoList[i] = s;
                if (s.Tuoi < s.Doi) conSong = true;
                else s.T.gameObject.SetActive(false);
            }

            yield return null;
        }

        Destroy(gameObject); // sao là con của FX — chết chùm, không rác scene
    }

    /// <summary>Sinh 8-12 sao vàng tỏa tròn quanh tâm (hướng random, lệch lên trên một chút).</summary>
    private void SinhSao(Vector3 tam)
    {
        Sprite sprite = LaySpriteSao();
        int soSao = Random.Range(SoSaoMin, SoSaoMax + 1);

        for (int i = 0; i < soSao; i++)
        {
            var go = new GameObject($"Sao_{i:00}");
            go.transform.SetParent(transform, false);
            go.transform.position = tam;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.color        = (i % 2 == 0) ? MauSao1 : MauSao2;
            sr.sortingOrder = SortingOrderSao;

            // Chia đều vòng tròn + jitter để vừa phủ kín vừa không cứng như compa;
            // thiên lên trên (bias +18°) vì nửa dưới thường bị cầu cảng che.
            float gocDeu   = (360f / soSao) * i;
            float goc      = (gocDeu + Random.Range(-14f, 14f) + 18f) * Mathf.Deg2Rad;
            float tocDo    = Random.Range(SaoTocDoMin, SaoTocDoMax);
            float size     = Random.Range(SaoSizeMin, SaoSizeMax);

            go.transform.localScale = new Vector3(size, size, 1f);

            _saoList.Add(new Sao
            {
                T          = go.transform,
                Sr         = sr,
                VanToc     = new Vector2(Mathf.Cos(goc), Mathf.Sin(goc)) * tocDo,
                XoayDoGiay = Random.Range(-240f, 240f),
                Doi        = Random.Range(SaoDoiMin, SaoDoiMax),
                Tuoi       = 0f,
                SizeGoc    = size,
            });
        }
    }

    /// <summary>
    /// Bảng khóa: punch 1→1.15 (nhịp UnlockFxRoutine của BoatDockSlot) rồi thu về 0
    /// + fade mọi SpriteRenderer/TMP con → SetActive(false), TRẢ lại scale/alpha gốc
    /// (phòng trường hợp object được bật lại — không bị méo/tàng hình vĩnh viễn).
    /// </summary>
    private IEnumerator ThuBangKhoaRoutine(Transform bang)
    {
        Vector3 scaleGoc = bang.localScale;

        SpriteRenderer[] srs  = bang.GetComponentsInChildren<SpriteRenderer>(true);
        TMP_Text[]       tmps = bang.GetComponentsInChildren<TMP_Text>(true);
        var mauSr  = new Color[srs.Length];
        var mauTmp = new Color[tmps.Length];
        for (int i = 0; i < srs.Length; i++)  mauSr[i]  = srs[i] != null ? srs[i].color : Color.white;
        for (int i = 0; i < tmps.Length; i++) mauTmp[i] = tmps[i] != null ? tmps[i].color : Color.white;

        float punchDur = BangThuSeconds * 0.45f;
        float thuDur   = BangThuSeconds * 0.55f;

        // Punch lên 1.15 (sin nửa chu kỳ)
        float t = 0f;
        while (t < punchDur)
        {
            if (bang == null) yield break; // bảng bị destroy giữa chừng — thoát êm
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / punchDur);
            bang.localScale = Vector3.Lerp(scaleGoc, scaleGoc * 1.15f, Mathf.Sin(p * Mathf.PI * 0.5f));
            yield return null;
        }

        // Thu về 0 + fade
        t = 0f;
        while (t < thuDur)
        {
            if (bang == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / thuDur);

            bang.localScale = Vector3.Lerp(scaleGoc * 1.15f, Vector3.zero, p);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                Color c = mauSr[i]; c.a = mauSr[i].a * (1f - p); srs[i].color = c;
            }
            for (int i = 0; i < tmps.Length; i++)
            {
                if (tmps[i] == null) continue;
                Color c = mauTmp[i]; c.a = mauTmp[i].a * (1f - p); tmps[i].color = c;
            }
            yield return null;
        }

        if (bang != null)
        {
            bang.gameObject.SetActive(false);
            bang.localScale = scaleGoc; // trả nguyên trạng cho lần bật lại (nếu có)
            for (int i = 0; i < srs.Length; i++)  if (srs[i] != null)  srs[i].color  = mauSr[i];
            for (int i = 0; i < tmps.Length; i++) if (tmps[i] != null) tmps[i].color = mauTmp[i];
        }
    }

    // =========================================================================
    //  Sprite sao procedural — không cần asset
    // =========================================================================

    /// <summary>
    /// Sprite sao 4 cánh 32x32 vẽ bằng code, trắng (tint màu ở SpriteRenderer),
    /// pixelsPerUnit = 32 → cỡ gốc đúng 1 unit, phóng bằng localScale.
    /// Cache static — cả session vẽ đúng 1 lần.
    /// </summary>
    private static Sprite LaySpriteSao()
    {
        if (_spriteSao != null) return _spriteSao;

        const int N = 32;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name       = "Sao4Canh_Procedural",
            // [QA m-7] HideAndDontSave: texture/sprite tạo bằng code không thuộc scene
            // nào — không đánh dấu thì mỗi lần thoát Play trong Editor để lại 1 bản rác
            // (đồng bộ với TouristRequestBubble của Dev B).
            hideFlags  = HideFlags.HideAndDontSave,
        };

        float tam = (N - 1) * 0.5f;
        var pixels = new Color32[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float dx = (x - tam) / tam; // -1..1
                float dy = (y - tam) / tam;
                float r  = Mathf.Sqrt(dx * dx + dy * dy);

                // Sao 4 cánh: cường độ mạnh dọc theo 2 trục, yếu ở chéo —
                // |dx*dy| nhỏ trên trục, lớn ở chéo. Cộng lõi tròn sáng ở giữa.
                float canh = Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - Mathf.Abs(dx * dy) * 9f);
                float loi  = Mathf.Clamp01(1f - r * 2.4f);
                float a    = Mathf.Clamp01(canh * canh + loi);

                pixels[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true); // makeNoLongerReadable — nhẹ RAM

        _spriteSao = Sprite.Create(tex, new Rect(0, 0, N, N),
                                   new Vector2(0.5f, 0.5f), pixelsPerUnit: N);
        _spriteSao.name      = "Sao4Canh_Procedural";
        _spriteSao.hideFlags = HideFlags.HideAndDontSave;
        return _spriteSao;
    }
}
