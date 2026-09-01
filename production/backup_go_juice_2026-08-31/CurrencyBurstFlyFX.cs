using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [JUICE PACK T2 — 2026-08-31] Vàng/Kim cương BUNG VÒNG TRÒN → khựng 1 nhịp →
/// hút bay về thanh HUD → HUD nhún nảy. Tham chiếu video Township Sếp gửi.
///
/// Cách dùng (worldPos = vị trí nhận thưởng trong map; screen cũng nhận qua overload):
///     CurrencyBurstFlyFX.PlayCoins(worldPos, 12);
///     CurrencyBurstFlyFX.PlayGems(worldPos, 3);
///
/// • Icon: ưu tiên bộ MỚI THỐNG NHẤT của đội vẽ tại Resources/UI/icon_gold_v2 +
///   Resources/UI/icon_gem_v2 (256px) — chưa có thì vẽ đồng xu runtime, chạy được ngay.
/// • HUD target: dò GameObject tên "Vangicon" (vàng — cùng quy ước TouristSmileyFlyFX)
///   và "GemIcon"/"Kimcuongicon" (kim cương); không thấy → bay lên góc phải-trên màn hình.
/// • Canvas riêng sorting 260 → nổi trên mọi UI thường; unscaled time; tự huỷ.
/// • Thuần cộng thêm — KHÔNG thay CoinFlyFX cũ cho tới khi Sếp duyệt chuyển call site.
/// </summary>
public class CurrencyBurstFlyFX : MonoBehaviour
{
    public static string GoldHudName = "Vangicon";
    public static string GemHudName  = "GemIcon";
    public static float  IconSize    = 62f;   // Sếp yêu cầu TĂNG size (cũ ~40)
    public static int    MaxIcons    = 14;    // nhiều hơn thì gộp: vẫn 14 icon, giá trị chia đều

    private static Sprite _gold, _gem, _fallback;
    private static bool _loaded;

    public static void PlayCoins(Vector3 worldPos, int amount) => Play(worldPos, amount, true);
    public static void PlayGems (Vector3 worldPos, int amount) => Play(worldPos, amount, false);

    /// <summary>Bản screen-space: gọi từ UI/popup (toạ độ màn hình, vd Input.mousePosition).</summary>
    public static void PlayCoinsScreen(Vector2 screenPos, int amount) => PlayScreen(screenPos, amount, true);
    public static void PlayGemsScreen (Vector2 screenPos, int amount) => PlayScreen(screenPos, amount, false);

    private static void PlayScreen(Vector2 screenPos, int amount, bool isGold)
    {
        if (amount <= 0) return;
        LoadOnce();
        var go = new GameObject(isGold ? "CoinBurstFX" : "GemBurstFX",
                                typeof(RectTransform), typeof(Canvas));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;
        var fx = go.AddComponent<CurrencyBurstFlyFX>();
        fx.StartCoroutine(fx.Routine(screenPos, Mathf.Min(amount, MaxIcons), isGold));
    }

    private static void Play(Vector3 worldPos, int amount, bool isGold)
    {
        if (amount <= 0) return;
        LoadOnce();

        var go = new GameObject(isGold ? "CoinBurstFX" : "GemBurstFX",
                                typeof(RectTransform), typeof(Canvas));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;

        var fx = go.AddComponent<CurrencyBurstFlyFX>();
        Camera cam = Camera.main;
        Vector2 screen = cam != null ? (Vector2)cam.WorldToScreenPoint(worldPos)
                                     : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        int n = Mathf.Min(amount, MaxIcons);
        fx.StartCoroutine(fx.Routine(screen, n, isGold));
    }

    private static void LoadOnce()
    {
        if (_loaded) return;
        _loaded = true;
        // Đội vẽ bàn giao 2026-08-31 vào Resources/UI/Currency/ — thử đường đó trước,
        // giữ đường cũ làm dự phòng nếu sau này dời file.
        _gold = Resources.Load<Sprite>("UI/Currency/icon_gold_v2");
        if (_gold == null) _gold = Resources.Load<Sprite>("UI/icon_gold_v2");
        _gem  = Resources.Load<Sprite>("UI/Currency/icon_gem_v2");
        if (_gem == null) _gem = Resources.Load<Sprite>("UI/icon_gem_v2");
    }

    private static Sprite Coin()
    {
        if (_fallback != null) return _fallback;
        const int S = 40; int r = S / 2 - 1;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var inner = new Color32(255, 205, 60, 255); var rim = new Color32(200, 140, 20, 255);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(S/2f, S/2f));
            tex.SetPixel(x, y, d > r ? Color.clear : (d > r - 3 ? (Color)rim : (Color)inner));
        }
        tex.Apply();
        _fallback = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 40f);
        return _fallback;
    }

    private IEnumerator Routine(Vector2 screenCenter, int count, bool isGold)
    {
        Sprite spr = isGold ? (_gold != null ? _gold : Coin())
                            : (_gem  != null ? _gem  : Coin());

        RectTransform target = FindHudTarget(isGold, out Vector2 targetScreen);
        var icons = new List<RectTransform>();

        // ── PHA 1: bung VÒNG TRÒN đều quanh tâm (ease-out back nhẹ) ──
        float radius = 96f + count * 4f;
        for (int i = 0; i < count; i++)
        {
            var icon = new GameObject("c", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(transform, false);
            var img = icon.GetComponent<Image>();
            img.sprite = spr; img.raycastTarget = false;
            var rt = (RectTransform)icon.transform;
            rt.sizeDelta = Vector2.one * IconSize;
            rt.position = screenCenter;
            icons.Add(rt);
        }
        float e = 0f; const float burstT = 0.34f;
        while (e < burstT)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / burstT);
            float back = 1f + 1.7f * Mathf.Pow(k - 1f, 3) + 1.7f * Mathf.Pow(k - 1f, 2); // easeOutBack
            for (int i = 0; i < icons.Count; i++)
            {
                float ang = (360f / icons.Count) * i - 90f;
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                icons[i].position = screenCenter + dir * radius * back;
                icons[i].localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k);
            }
            yield return null;
        }

        // ── PHA 2: khựng nhẹ cho user kịp thấy ──
        e = 0f; while (e < 0.22f) { e += Time.unscaledDeltaTime; yield return null; }

        // ── PHA 3: lần lượt hút về HUD (so le 0.03s, bezier cong) ──
        for (int i = 0; i < icons.Count; i++)
        {
            StartCoroutine(FlyOne(icons[i], targetScreen, i * 0.03f,
                                  last: i == icons.Count - 1, target: target));
            yield return null;
        }
        Destroy(gameObject, 1.6f);
    }

    private IEnumerator FlyOne(RectTransform rt, Vector2 dest, float delay, bool last, RectTransform target)
    {
        float e = 0f; while (e < delay) { e += Time.unscaledDeltaTime; yield return null; }
        Vector2 from = rt.position;
        Vector2 ctrl = (from + dest) * 0.5f + new Vector2(0f, 140f);   // vòng cung lên trên
        const float T = 0.5f; e = 0f;
        while (e < T && rt != null)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / T);
            k = k * k * (3f - 2f * k);                                  // smoothstep
            Vector2 a = Vector2.Lerp(from, ctrl, k), b = Vector2.Lerp(ctrl, dest, k);
            rt.position = Vector2.Lerp(a, b, k);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, k);
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
        if (last && target != null) StartCoroutine(BounceHud(target));  // ── PHA 4: HUD nhún ──
    }

    /// <summary>HUD nhún nảy y hệt video — cùng công thức CoDapHud của UnifiedTaskPopupUI.</summary>
    private IEnumerator BounceHud(RectTransform hud)
    {
        Vector3 goc = hud.localScale; const float T = 0.34f; float t = 0f;
        while (t < T && hud != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / T);
            float s = 1f + 0.32f * Mathf.Sin(k * Mathf.PI) - 0.06f * Mathf.Sin(k * Mathf.PI * 2f);
            hud.localScale = goc * s;
            yield return null;
        }
        if (hud != null) hud.localScale = goc;
    }

    private RectTransform FindHudTarget(bool isGold, out Vector2 screenPos)
    {
        string[] names = isGold ? new[] { GoldHudName, "GoldIcon", "icon_vang" }
                                : new[] { GemHudName, "Kimcuongicon", "DiamondIcon", "icon_gem" };
        foreach (string n in names)
        {
            var go = GameObject.Find(n);
            if (go != null && go.transform is RectTransform rt)
            { screenPos = rt.position; return rt; }
        }
        screenPos = new Vector2(Screen.width * (isGold ? 0.62f : 0.78f), Screen.height - 40f);
        return null;
    }
}
