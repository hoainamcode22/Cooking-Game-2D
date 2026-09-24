// ============================================================================
//  LockedHintFX — cham vao thu CHUA MO KHOA -> dong chu ngan hien len roi mo dan (2026-09-24)
//    Vd: "Cooking unlocks at Level 4"  (tieng Viet: "Nấu ăn mở ở cấp 4")
//  Hien ngay tren cho ngon tay cham, nay nhe, troi len ~60px, mo dan trong ~1.8s.
//  Tu tao 1 Canvas rieng (thu tu 450: tren popup, duoi man chuyen canh), dung lai 1 o chu duy
//  nhat -> cham lien tuc khong sinh rac. Khong can keo tha gi.
//    LockedHintFX.ChanTheoCap("Nấu ăn", 4)  -> true neu chua du cap (va da hien chu)
//    LockedHintFX.Show("...")                 -> hien 1 dong bat ky
// ============================================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LockedHintFX : MonoBehaviour
{
    private static LockedHintFX _inst;
    private static Sprite _pill;

    private RectTransform _goc, _vien;
    private TMP_Text _txt;
    private CanvasGroup _cg;
    private float _t = -1f;
    private Vector2 _p0;
    private const float THOI_GIAN = 1.8f;

    /// <summary>Cap hien tai cua nguoi choi.</summary>
    public static int CapHienTai
    {
        get
        {
            if (PlayerProgressManager.Instance != null) return PlayerProgressManager.Instance.Level;
            if (FarmLevelManager.Instance != null) return FarmLevelManager.Instance.CurrentLevel;
            return 1;
        }
    }

    /// <summary>Chua du cap -> hien "X unlocks at Level N" va tra true (nguoi goi dung lai).</summary>
    public static bool ChanTheoCap(string tenTinhNangVi, int capCan, Vector2? viTriManHinh = null)
    {
        if (capCan <= 0 || CapHienTai >= capCan) return false;
        Show(Loc.TF("{0} mở ở cấp {1}", Loc.T(tenTinhNangVi), "<color=#FFD54A>" + capCan + "</color>"), viTriManHinh);
        return true;
    }

    public static void Show(string text, Vector2? viTriManHinh = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (_inst == null)
        {
            var go = new GameObject("[LockedHintFX]");
            _inst = go.AddComponent<LockedHintFX>();
            _inst.Dung();
        }
        _inst.Hien(text, viTriManHinh ?? ConTro());
    }

    private void Dung()
    {
        var cv = gameObject.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 450;
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        _goc = (RectTransform)transform;

        var v = new GameObject("Hint_Pill", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _vien = (RectTransform)v.transform;
        _vien.SetParent(_goc, false);
        _vien.anchorMin = _vien.anchorMax = new Vector2(0.5f, 0.5f);
        var img = v.GetComponent<Image>();
        img.sprite = VienBoGoc(); img.type = Image.Type.Sliced;
        img.color = new Color(0.14f, 0.09f, 0.05f, 0.86f);
        img.raycastTarget = false;
        _cg = v.GetComponent<CanvasGroup>();
        _cg.blocksRaycasts = false; _cg.interactable = false;

        var t = new GameObject("Txt_Hint", typeof(RectTransform));
        var trt = (RectTransform)t.transform; trt.SetParent(_vien, false);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(26f, 6f); trt.offsetMax = new Vector2(-26f, -6f);
        _txt = t.AddComponent<TextMeshProUGUI>();
        var f = TimFont(); if (f != null) _txt.font = f;
        _txt.fontSize = 32f; _txt.fontStyle = FontStyles.Bold;
        _txt.color = Color.white;
        _txt.alignment = TextAlignmentOptions.Center;
        _txt.textWrappingMode = TextWrappingModes.NoWrap;
        _txt.richText = true;
        _txt.raycastTarget = false;
        v.SetActive(false);
    }

    private void Hien(string text, Vector2 manHinh)
    {
        _txt.text = text;
        Vector2 pref = _txt.GetPreferredValues(text, 2000f, 60f);
        _vien.sizeDelta = new Vector2(Mathf.Clamp(pref.x + 60f, 220f, 1100f), 64f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_goc, manHinh, null, out Vector2 local);
        Rect r = _goc.rect;
        float nuaW = _vien.sizeDelta.x * 0.5f + 16f;
        local.x = Mathf.Clamp(local.x, r.xMin + nuaW, r.xMax - nuaW);
        local.y = Mathf.Clamp(local.y + 70f, r.yMin + 60f, r.yMax - 140f);
        _p0 = local;
        _t = 0f;
        _vien.gameObject.SetActive(true);
        AudioManager.Instance?.PlayUIClick();
    }

    private void Update()
    {
        if (_t < 0f) return;
        _t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_t / THOI_GIAN);
        float s = k < 0.1f ? Mathf.Lerp(0.8f, 1.06f, k / 0.1f) : (k < 0.18f ? Mathf.Lerp(1.06f, 1f, (k - 0.1f) / 0.08f) : 1f);
        _vien.localScale = new Vector3(s, s, 1f);
        _vien.anchoredPosition = _p0 + new Vector2(0f, 60f * (1f - (1f - k) * (1f - k)));
        _cg.alpha = k < 0.08f ? k / 0.08f : (k < 0.62f ? 1f : 1f - (k - 0.62f) / 0.38f);
        if (k >= 1f) { _t = -1f; _vien.gameObject.SetActive(false); }
    }

    private static Vector2 ConTro()
    {
#if ENABLE_INPUT_SYSTEM
        var p = UnityEngine.InputSystem.Pointer.current;
        if (p != null) return p.position.ReadValue();
#else
        return Input.mousePosition;
#endif
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private static TMP_FontAsset TimFont()
    {
        foreach (var t in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            if (t != null && t.font != null && t.gameObject.activeInHierarchy) return t.font;
        return TMP_Settings.defaultFontAsset;
    }

    /// <summary>Vien bo tron 9-slice ve bang code (48px, bo goc 22).</summary>
    private static Sprite VienBoGoc()
    {
        if (_pill != null) return _pill;
        const int N = 48; const float R = 22f;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, R, N - R), cy = Mathf.Clamp(y + 0.5f, R, N - R);
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                float a = Mathf.Clamp01(R - d + 0.5f);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px); tex.Apply(false, false);
        _pill = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(23f, 23f, 23f, 23f));
        _pill.name = "LockedHint_Pill";
        return _pill;
    }
}
