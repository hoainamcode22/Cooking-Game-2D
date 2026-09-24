// ============================================================================
//  KitchenJuiceFX — "DO SUONG TAY" cho bep V3 (2026-09-24)
// ----------------------------------------------------------------------------
//  Tu gan vao Kitchen_UI_v3 luc chay (khong can keo tha). Gom 3 phan:
//   1. THA NGUYEN LIEU VAO NOI: icon mon roi lach tach vao mieng noi (giong hat giong roi
//      xuong ruong) + noi rung rinh + hoi nuoc toe len. Dung cho ca CHAM the va KEO THA the.
//   2. BAM "NAU" LIEN TUC NHOM LUA: moi lan bam hien x1 x2 x3... to giua man, lua trong mieng
//      lo loe len, tia lua bay. Bam du "nhan pham" (ngau nhien) -> lua BUNG + "PERFECT!" ->
//      bat dau nau that (thanh thoi gian cu). Bam cang nhieu cang de roi VANG / KIM CUONG /
//      GIA VI (it thoi, chu yeu cho suong tay). Ngung bam 3.5s -> lua tu bat ("GOOD!").
//   3. BAT KEO THA cho the nguyen lieu/gia vi (KitchenCardDrag, giu 0.16s roi keo).
//   4. Chu dai tran khung -> tu co nho vua khung (TmpVuaKhung), giu co chu Edit mode lam toi da.
//  Toi uu: hat dung pool Image tao 1 lan, 1 vong Update; khong co gi chay khi dung yen.
// ============================================================================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class KitchenJuiceFX : MonoBehaviour
{
    public static KitchenJuiceFX Instance { get; private set; }

    [Header("Combo nhom lua")]
    [Tooltip("So lan bam de lua bung: ngau nhien trong khoang nay (nhan pham).")]
    [SerializeField] private Vector2Int nguongBam = new Vector2Int(4, 14);
    [Tooltip("Ti le 'hen': lua bung chi sau 2-3 lan bam.")]
    [Range(0f, 1f)] [SerializeField] private float tiLeHen = 0.12f;
    [SerializeField] private float choTuDongBat = 3.5f;

    [Header("Roi qua khi bam (chu yeu suong tay)")]
    [Range(0f, 1f)] [SerializeField] private float tiLeVang = 0.06f;
    [Range(0f, 1f)] [SerializeField] private float tiLeKimCuong = 0.012f;
    [Range(0f, 1f)] [SerializeField] private float tiLeGiaVi = 0.035f;
    [Tooltip("Moi lan bam them x% co hoi (bam cang nhieu cang de roi).")]
    [SerializeField] private float tangMoiLanBam = 0.08f;
    [SerializeField] private int toiDaQuaMoiLanNau = 3;
    [SerializeField] private Vector2Int vangRoi = new Vector2Int(3, 8);

    [Header("Tha vao noi")]
    [SerializeField] private int soIconRoi = 5;
    [SerializeField] private float kichThuocIcon = 64f;

    // ── Tham chieu ──
    private RectTransform _lop, _oven, _pot, _btn, _fireGoc, _flash;
    private Image _fire, _fireGlow, _flashImg, _rays;
    private TMP_FontAsset _font;
    private Vector3 _potScale0 = Vector3.one;
    private Quaternion _potRot0 = Quaternion.identity;
    private float _potKick, _potT;
    private CookingChallengeManager _challenge;
    private float _henQuetThe;

    // ── Combo ──
    private enum TrangThai { Nghi, Nhom, Bung }
    private TrangThai _tt = TrangThai.Nghi;
    private int _dem, _nguong, _qua;
    private float _nhiet, _nhietHien, _loe, _lanBamCuoi, _tatLua = -1f;

    // ── Pool hat ──
    private class Hat
    {
        public RectTransform rt; public Image img; public bool song;
        public float tuoi, doi, rot, xoay, kt; public int kieu;
        public Vector2 pos, van, dich, dau; public Color mau;
    }
    private readonly List<Hat> _hat = new List<Hat>();
    private readonly List<TMP_Text> _chu = new List<TMP_Text>();
    private int _chuKe;
    private static Vector3? _diemThaTam;

    // =====================================================================
    //  Tu gan vao Kitchen_UI_v3
    // =====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuKhoiDong()
    {
        SceneManager.sceneLoaded -= KhiLoad;
        SceneManager.sceneLoaded += KhiLoad;
        GanVaoBep();
    }

    private static void KhiLoad(Scene s, LoadSceneMode m) => GanVaoBep();

    private static void GanVaoBep()
    {
        if (Instance != null) return;
        foreach (var ui in FindObjectsByType<KitchenUIv2.KitchenSceneV2UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ui != null && ui.gameObject.name == "Kitchen_UI_v3")
            {
                if (ui.GetComponent<KitchenJuiceFX>() == null) ui.gameObject.AddComponent<KitchenJuiceFX>();
                return;
            }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        if (_tt != TrangThai.Nghi) HuyCombo();
        if (_pot != null) { _pot.localScale = _potScale0; _pot.localRotation = _potRot0; }
        _potKick = 0f;
    }

    private bool Bind()
    {
        if (_lop != null) return true;
        _oven = TimSau(transform, "Oven") as RectTransform;
        _pot  = TimSau(transform, "Pot_Anim") as RectTransform;
        _btn  = TimSau(transform, "Btn_Action") as RectTransform;
        var cha = _oven != null ? _oven.parent : transform;
        if (cha == null) return false;

        var go = new GameObject("Fx_BepSuongTay", typeof(RectTransform));
        go.layer = gameObject.layer;
        _lop = (RectTransform)go.transform;
        _lop.SetParent(cha, false);
        _lop.anchorMin = Vector2.zero; _lop.anchorMax = Vector2.one;
        _lop.offsetMin = _lop.offsetMax = Vector2.zero;
        _lop.SetAsLastSibling();

        if (_pot != null) { _potScale0 = _pot.localScale; _potRot0 = _pot.localRotation; }
        var t = _btn != null ? _btn.GetComponentInChildren<TMP_Text>(true) : null;
        _font = t != null ? t.font : TMP_Settings.defaultFontAsset;
        _challenge = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);

        // Lua combo nam TRONG mieng lo (ngay sau Oven_Fire goc)
        if (_oven != null)
        {
            var goc = _oven.Find("Oven_Fire") as RectTransform;
            var gGo = new GameObject("Fx_ComboGlow", typeof(RectTransform), typeof(Image));
            gGo.layer = gameObject.layer;
            var g = (RectTransform)gGo.transform; g.SetParent(_oven, false);
            _fireGlow = gGo.GetComponent<Image>();
            _fireGlow.sprite = SoftFxSprites.GlowSprite; _fireGlow.raycastTarget = false;
            _fireGlow.color = new Color(1f, 0.55f, 0.15f, 0f);

            var fGo = new GameObject("Fx_ComboFire", typeof(RectTransform), typeof(Image));
            fGo.layer = gameObject.layer;
            _fireGoc = (RectTransform)fGo.transform; _fireGoc.SetParent(_oven, false);
            _fire = fGo.GetComponent<Image>();
            _fire.raycastTarget = false; _fire.preserveAspect = true;
            var imgGoc = goc != null ? goc.GetComponent<Image>() : null;
            _fire.sprite = imgGoc != null && imgGoc.sprite != null ? imgGoc.sprite : SoftFxSprites.GlowSprite;
            if (imgGoc == null || imgGoc.sprite == null) _fire.color = new Color(1f, 0.6f, 0.2f, 0f);
            else _fire.color = new Color(1f, 1f, 1f, 0f);

            foreach (var r in new[] { g, _fireGoc })
            {
                r.anchorMin = r.anchorMax = goc != null ? goc.anchorMin : new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.2f);
                r.anchoredPosition = goc != null ? goc.anchoredPosition + new Vector2(0f, -goc.rect.height * 0.3f) : Vector2.zero;
            }
            _fireGoc.sizeDelta = goc != null ? goc.sizeDelta : new Vector2(100f, 86f);
            g.sizeDelta = _fireGoc.sizeDelta * 2.2f;
            int idx = goc != null ? goc.GetSiblingIndex() + 1 : _oven.childCount;
            g.SetSiblingIndex(Mathf.Min(idx, _oven.childCount - 1));
            _fireGoc.SetSiblingIndex(Mathf.Min(idx + 1, _oven.childCount - 1));
        }

        // Chop sang man hinh khi lua bung
        var flGo = new GameObject("Fx_Flash", typeof(RectTransform), typeof(Image));
        flGo.layer = gameObject.layer;
        _flash = (RectTransform)flGo.transform; _flash.SetParent(_lop, false);
        _flash.anchorMin = new Vector2(-0.2f, -0.2f); _flash.anchorMax = new Vector2(1.2f, 1.2f);
        _flash.offsetMin = _flash.offsetMax = Vector2.zero;
        _flashImg = flGo.GetComponent<Image>();
        _flashImg.raycastTarget = false; _flashImg.color = new Color(1f, 0.9f, 0.6f, 0f);
        _flashImg.enabled = false;

        // Tia nang sau chu PERFECT
        var rGo = new GameObject("Fx_PerfectRays", typeof(RectTransform), typeof(Image));
        rGo.layer = gameObject.layer;
        var rr = (RectTransform)rGo.transform; rr.SetParent(_lop, false);
        rr.sizeDelta = new Vector2(620f, 620f);
        _rays = rGo.GetComponent<Image>();
        _rays.sprite = SoftFxSprites.RaysSprite; _rays.raycastTarget = false;
        _rays.color = new Color(1f, 0.85f, 0.4f, 0f);
        _rays.enabled = false;

        for (int i = 0; i < 40; i++) _hat.Add(TaoHat());
        for (int i = 0; i < 7; i++) _chu.Add(TaoChu());
        return true;
    }

    private Hat TaoHat()
    {
        var go = new GameObject("Fx_Hat", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(_lop, false);
        var img = go.GetComponent<Image>(); img.raycastTarget = false; img.enabled = false;
        return new Hat { rt = rt, img = img };
    }

    private TMP_Text TaoChu()
    {
        // Khung "Fx_Chu" [Bong, Chu]: bong ve TRUOC -> nam duoi chu (khong dung outline de tranh loi font)
        var goc = new GameObject("Fx_Chu", typeof(RectTransform));
        goc.layer = gameObject.layer;
        var grt = (RectTransform)goc.transform; grt.SetParent(_lop, false);
        grt.sizeDelta = new Vector2(760f, 190f);
        var s = TaoTMP("Bong", grt); s.color = new Color(0.35f, 0.12f, 0.02f, 0.75f);
        s.rectTransform.offsetMin = new Vector2(5f, -7f); s.rectTransform.offsetMax = new Vector2(5f, -7f);
        var t = TaoTMP("Chu", grt);
        goc.SetActive(false);
        return t;
    }

    private TMP_Text TaoTMP(string ten, RectTransform cha)
    {
        var go = new GameObject(ten, typeof(RectTransform));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(cha, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    // =====================================================================
    //  1. THA VAO NOI
    // =====================================================================

    /// <summary>Keo tha dung vao noi -> ghi diem tha de icon roi tu dung cho do.</summary>
    public static void DatDiemTha(Vector3 world) => _diemThaTam = world;

    /// <summary>CookingSelectionManager goi khi 1 the vua vao noi (cham hoac keo).</summary>
    public void ThaVaoNoi(Sprite icon, Vector3 tuWorld)
    {
        if (!Bind() || _pot == null || icon == null) return;
        if (_diemThaTam.HasValue) { tuWorld = _diemThaTam.Value; _diemThaTam = null; }

        Vector2 tu = ViTriLop(tuWorld);
        Vector2 mieng = ViTriLop(_pot.TransformPoint(new Vector3(0f, _pot.rect.height * 0.16f, 0f)));
        int n = VfxCauHinh.CheDoNhe ? Mathf.Max(2, soIconRoi / 2) : soIconRoi;
        for (int i = 0; i < n; i++)
        {
            var h = LayHat(); if (h == null) break;
            h.kieu = 1;
            h.img.sprite = icon; h.img.preserveAspect = true;
            h.mau = Color.white;
            h.dau = tu + Random.insideUnitCircle * 20f;
            h.dich = mieng + new Vector2(Random.Range(-38f, 38f), 0f);
            h.pos = h.dau;
            h.tuoi = -i * 0.06f;                      // roi lach tach tung hat
            h.doi = Random.Range(0.55f, 0.7f);
            h.kt = kichThuocIcon * Random.Range(0.75f, 1.05f);
            h.xoay = Random.Range(-360f, 360f);
            BatHat(h);
        }
    }

    private void KhiIconVaoNoi(Vector2 p)
    {
        _potKick = Mathf.Min(1.2f, _potKick + 0.55f);
        for (int k = 0; k < 3; k++)
        {
            var h = LayHat(); if (h == null) return;
            h.kieu = 2;
            h.img.sprite = SoftFxSprites.CircleSprite; h.img.preserveAspect = false;
            h.mau = new Color(1f, 1f, 1f, 0.85f);
            h.pos = p;
            h.van = new Vector2(Random.Range(-90f, 90f), Random.Range(120f, 220f));
            h.tuoi = 0f; h.doi = Random.Range(0.35f, 0.5f);
            h.kt = Random.Range(10f, 18f);
            BatHat(h);
        }
    }

    /// <summary>Con tro co dang tren noi khong (keo tha).</summary>
    public bool TrenNoi(Vector2 screen, Camera cam)
    {
        if (_pot == null) { _pot = TimSau(transform, "Pot_Anim") as RectTransform; if (_pot == null) return false; }
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_pot, screen, cam, out local)) return false;
        Rect r = _pot.rect;
        r.xMin -= 60f; r.xMax += 60f; r.yMin -= 30f; r.yMax += 90f;    // noi rong rai, de tha
        return r.Contains(local);
    }

    /// <summary>Dang keo the tren noi -> noi phong nhe bao hieu.</summary>
    public void BaoHieuNoi(bool tren)
    {
        if (tren) _potKick = Mathf.Max(_potKick, 0.25f);
    }

    // =====================================================================
    //  2. COMBO NHOM LUA
    // =====================================================================

    /// <summary>KitchenSceneV2UI goi khi bam nut NAU. true = combo da xu ly (khong nau ngay).</summary>
    public static bool XuLyNutNau(CookingChallengeManager c)
    {
        var fx = Instance;
        if (fx == null || !fx.isActiveAndEnabled || c == null || c.IsCooking) return false;
        if (!fx.Bind() || fx._oven == null) return false;
        fx._challenge = c;
        fx.Bam();
        return true;
    }

    private void Bam()
    {
        if (_tt == TrangThai.Bung) return;
        if (_tt == TrangThai.Nghi)
        {
            _tt = TrangThai.Nhom;
            _dem = 0; _qua = 0; _nhiet = 0f;
            _nguong = Random.value < tiLeHen ? Random.Range(2, 4) : Random.Range(nguongBam.x, nguongBam.y + 1);
            HienChu(Loc.T("Bấm liên tục để nhóm lửa!"), ViTriTrenLo(170f), 34f, new Color(1f, 0.95f, 0.8f), 1.1f, false);
        }
        _dem++;
        _lanBamCuoi = Time.unscaledTime;
        _nhiet = Mathf.Clamp01(_dem / (float)Mathf.Max(1, _nguong));
        _loe = 1f;

        Color mau = Color.Lerp(new Color(1f, 0.93f, 0.35f), new Color(1f, 0.35f, 0.12f), _nhiet);
        float co = 78f + Mathf.Min(_dem * 4f, 48f);
        HienChu("x" + _dem, ViTriTrenLo(90f) + new Vector2(Random.Range(-70f, 70f), Random.Range(-10f, 20f)), co, mau, 0.65f, true);
        TiaLua(3 + Mathf.RoundToInt(_nhiet * 4f), 1f);
        NhunNut();
        AudioManager.Instance?.PlayButton();
        ThuRoiQua();

        if (_dem >= _nguong) StartCoroutine(CoBung(true));
    }

    private IEnumerator CoBung(bool hoanHao)
    {
        _tt = TrangThai.Bung;
        _nhiet = 1f; _loe = 2f;
        TiaLua(VfxCauHinh.CheDoNhe ? 10 : 20, 1.6f);
        Vector2 p = ViTriTrenLo(150f);
        HienChu(Loc.T(hoanHao ? "HOÀN HẢO!" : "TỐT!"), p, hoanHao ? 128f : 96f,
                hoanHao ? new Color(1f, 0.82f, 0.2f) : new Color(1f, 0.95f, 0.6f), 1.3f, true, true);
        StartCoroutine(CoRays(p));
        StartCoroutine(CoFlash());
        AudioManager.Instance?.PlayCookStart();

        yield return new WaitForSecondsRealtime(0.35f);
        var c = _challenge;
        if (c != null && !c.IsCooking) c.OnClickCookSubmit();
        _tatLua = Time.unscaledTime + 0.9f;       // lua combo tat dan, lua that cua lo tiep quan
        _tt = TrangThai.Nghi;
        _dem = 0;
    }

    private void HuyCombo()
    {
        _tt = TrangThai.Nghi;
        _dem = 0; _nhiet = 0f;
    }

    private void ThuRoiQua()
    {
        if (_dem < 2 || _qua >= toiDaQuaMoiLanNau) return;
        float he = 1f + _dem * tangMoiLanBam;
        float r = Random.value;
        if (r < tiLeKimCuong * he) { RoiKimCuong(); return; }
        r = Random.value;
        if (r < tiLeVang * he) { RoiVang(); return; }
        r = Random.value;
        if (r < tiLeGiaVi * he) RoiGiaVi();
    }

    private void RoiVang()
    {
        var eco = FarmEconomyManager.Instance;
        if (eco == null) return;
        int n = Random.Range(vangRoi.x, vangRoi.y + 1);
        eco.AddGold(n);
        _qua++;
        var lib = RewardIconLibrary.Instance;
        BayQua(lib != null ? lib.goldSprite : null, "+" + n, new Color(1f, 0.85f, 0.25f));
        AudioManager.Instance?.PlayCoinTing();
    }

    private void RoiKimCuong()
    {
        var eco = FarmEconomyManager.Instance;
        if (eco == null) return;
        eco.AddGems(1);
        _qua++;
        var lib = RewardIconLibrary.Instance;
        BayQua(lib != null ? lib.gemSprite : null, "+1", new Color(0.55f, 0.9f, 1f));
        AudioManager.Instance?.PlayGemSparkle();
    }

    private void RoiGiaVi()
    {
        var ktm = KitchenTransferManager.Instance;
        var boot = FindFirstObjectByType<CookingBoot>(FindObjectsInactive.Include);
        if (ktm == null || boot == null || boot.cookingInventoryItems == null) return;
        var ds = new List<InventoryItemData>();
        foreach (var it in boot.cookingInventoryItems)
            if (it != null && it.cookingData != null && it.cookingData.kind == IngredientKind.Seasoning && !string.IsNullOrEmpty(it.itemId))
                ds.Add(it);
        if (ds.Count == 0) return;
        var chon = ds[Random.Range(0, ds.Count)];
        ktm.AddTransferredItem(chon.itemId, 1);
        _qua++;
        BayQua(chon.icon != null ? chon.icon : chon.cookingData.icon, "+1", new Color(1f, 0.95f, 0.8f));
        AudioManager.Instance?.PlayIngredientPop();
    }

    /// <summary>Icon qua bat ra tu mieng lo, vong len roi mo dan + chu "+n".</summary>
    private void BayQua(Sprite icon, string chu, Color mau)
    {
        Vector2 mieng = ViTriTrenLo(0f);
        if (icon != null)
        {
            var h = LayHat();
            if (h != null)
            {
                h.kieu = 3;
                h.img.sprite = icon; h.img.preserveAspect = true;
                h.mau = Color.white;
                h.pos = mieng;
                h.van = new Vector2(Random.Range(-160f, 160f), Random.Range(380f, 480f));
                h.tuoi = 0f; h.doi = 1.3f; h.kt = 78f; h.xoay = Random.Range(-90f, 90f);
                BatHat(h);
            }
        }
        HienChu(chu, mieng + new Vector2(0f, 150f), 52f, mau, 1f, false);
    }

    // =====================================================================
    //  Update
    // =====================================================================

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Bat keo tha cho the moi (moi 1s quet 1 lan, re)
        if (Time.unscaledTime >= _henQuetThe)
        {
            _henQuetThe = Time.unscaledTime + 1f;
            GanKeoThaChoThe();
            TmpVuaKhung.ApDung(transform);   // chu tieng Anh dai tu co vua khung (moi o chi xu ly 1 lan)
        }
        if (_lop == null) return;

        // Ngung bam lau -> lua tu bat; nut bi khoa (bo nguyen lieu) -> huy
        if (_tt == TrangThai.Nhom)
        {
            var b = _btn != null ? _btn.GetComponent<Button>() : null;
            if (b != null && !b.interactable) HuyCombo();
            else if (Time.unscaledTime - _lanBamCuoi > choTuDongBat) StartCoroutine(CoBung(false));
        }

        CapNhatLua(dt);
        CapNhatNoi(dt);
        CapNhatHat(dt);
    }

    private void CapNhatLua(float dt)
    {
        if (_fire == null) return;
        float muc = _tt == TrangThai.Nghi ? 0f : _nhiet;
        if (_tatLua > 0f)
        {
            if (Time.unscaledTime < _tatLua) muc = Mathf.Clamp01((_tatLua - Time.unscaledTime) / 0.9f) * 1.2f;
            else _tatLua = -1f;
        }
        _nhietHien = Mathf.MoveTowards(_nhietHien, muc, dt * 3f);
        _loe = Mathf.MoveTowards(_loe, 0f, dt * 5f);
        bool hien = _nhietHien > 0.01f || _loe > 0.01f;
        if (_fire.enabled != hien) { _fire.enabled = hien; _fireGlow.enabled = hien; }
        if (!hien) return;

        float t = Time.unscaledTime;
        float chop = 0.85f + 0.15f * Mathf.Sin(t * 31f) + Random.Range(-0.05f, 0.05f);
        float s = (0.25f + 0.85f * _nhietHien + 0.35f * _loe) * chop;
        _fireGoc.localScale = new Vector3(s * (0.9f + 0.1f * Mathf.Sin(t * 17f)), s, 1f);
        var c = _fire.color; c.a = Mathf.Clamp01(0.4f + _nhietHien + _loe * 0.5f); _fire.color = c;
        _fireGlow.color = new Color(1f, 0.55f, 0.15f, Mathf.Clamp01(0.25f + 0.45f * _nhietHien + 0.3f * _loe) * chop);
        _fireGlow.rectTransform.localScale = Vector3.one * (0.7f + 0.5f * _nhietHien + 0.3f * _loe);
        if (_tt == TrangThai.Nhom && Random.value < dt * (2f + 10f * _nhietHien)) TiaLua(1, 0.7f);
    }

    private void CapNhatNoi(float dt)
    {
        if (_pot == null || _potKick <= 0f) return;
        _potT += dt;
        _potKick = Mathf.Max(0f, _potKick - dt * 2.2f);
        float k = _potKick;
        float w = Mathf.Sin(_potT * 30f);
        _pot.localScale = new Vector3(_potScale0.x * (1f + 0.07f * w * k), _potScale0.y * (1f - 0.06f * w * k), _potScale0.z);
        _pot.localRotation = _potRot0 * Quaternion.Euler(0f, 0f, 4f * Mathf.Sin(_potT * 21f) * k);
        if (_potKick <= 0f) { _pot.localScale = _potScale0; _pot.localRotation = _potRot0; }
    }

    private void CapNhatHat(float dt)
    {
        for (int i = 0; i < _hat.Count; i++)
        {
            var h = _hat[i];
            if (!h.song) continue;
            h.tuoi += dt;
            if (h.tuoi < 0f) { h.img.enabled = false; continue; }
            if (!h.img.enabled) h.img.enabled = true;
            float k = Mathf.Clamp01(h.tuoi / h.doi);
            switch (h.kieu)
            {
                case 1:   // icon nguyen lieu: bay len tren mieng noi roi roi tom vao
                {
                    Vector2 dinh = new Vector2(Mathf.Lerp(h.dau.x, h.dich.x, 0.7f), Mathf.Max(h.dau.y, h.dich.y) + 150f);
                    Vector2 p;
                    if (k < 0.55f)
                    {
                        float u = k / 0.55f; float e = 1f - (1f - u) * (1f - u);
                        p = Vector2.Lerp(h.dau, dinh, e);
                    }
                    else
                    {
                        float u = (k - 0.55f) / 0.45f;
                        p = Vector2.Lerp(dinh, h.dich, u * u);
                    }
                    h.rt.anchoredPosition = p;
                    float s = k < 0.8f ? 1f : Mathf.Lerp(1f, 0.45f, (k - 0.8f) / 0.2f);
                    h.rt.sizeDelta = new Vector2(h.kt * s, h.kt * s);
                    h.rt.localRotation = Quaternion.Euler(0f, 0f, h.xoay * k);
                    h.img.color = new Color(1f, 1f, 1f, k < 0.85f ? 1f : (1f - k) / 0.15f);
                    if (k >= 1f) { KhiIconVaoNoi(h.dich); TatHat(h); }
                    break;
                }
                case 2:   // hoi nuoc / tia lua: bay toe roi rot nhe
                case 4:
                {
                    h.van += new Vector2(0f, (h.kieu == 4 ? -420f : -520f) * dt);
                    h.pos += h.van * dt;
                    h.rt.anchoredPosition = h.pos;
                    float s = h.kt * (h.kieu == 4 ? (1f - k * 0.6f) : (0.6f + k));
                    h.rt.sizeDelta = new Vector2(s, s);
                    var c = h.mau; c.a *= 1f - k; h.img.color = c;
                    if (k >= 1f) TatHat(h);
                    break;
                }
                case 3:   // qua roi ra tu lo: vong len, nay, mo dan
                {
                    h.van += new Vector2(0f, -700f * dt);
                    h.pos += h.van * dt;
                    h.rt.anchoredPosition = h.pos;
                    float s = k < 0.15f ? Mathf.Lerp(0.3f, 1.2f, k / 0.15f) : Mathf.Lerp(1.2f, 0.9f, (k - 0.15f) / 0.85f);
                    h.rt.sizeDelta = new Vector2(h.kt * s, h.kt * s);
                    h.rt.localRotation = Quaternion.Euler(0f, 0f, h.xoay * k);
                    h.img.color = new Color(1f, 1f, 1f, k < 0.7f ? 1f : (1f - k) / 0.3f);
                    if (k >= 1f) TatHat(h);
                    break;
                }
            }
        }
    }

    // =====================================================================
    //  Hieu ung nho
    // =====================================================================

    private void TiaLua(int n, float manh)
    {
        Vector2 goc = ViTriTrenLo(0f);
        for (int i = 0; i < n; i++)
        {
            var h = LayHat(); if (h == null) return;
            h.kieu = 4;
            h.img.sprite = (i % 2 == 0) ? SoftFxSprites.SparkleSprite : SoftFxSprites.CircleSprite;
            h.img.preserveAspect = false;
            h.mau = Random.value < 0.5f ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(1f, 0.5f, 0.15f, 1f);
            h.pos = goc + new Vector2(Random.Range(-40f, 40f), Random.Range(-10f, 20f));
            float g = Random.Range(40f, 140f) * Mathf.Deg2Rad;
            h.van = new Vector2(Mathf.Cos(g), Mathf.Sin(g)) * Random.Range(220f, 420f) * manh;
            h.tuoi = 0f; h.doi = Random.Range(0.4f, 0.75f);
            h.kt = Random.Range(12f, 26f) * Mathf.Sqrt(manh);
            BatHat(h);
        }
    }

    private void NhunNut()
    {
        if (_btn == null) return;
        StartCoroutine(CoNhun(_btn));
    }

    private IEnumerator CoNhun(RectTransform rt)
    {
        Vector3 s0 = Vector3.one;
        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.16f;
            float s = k < 0.4f ? Mathf.Lerp(1f, 0.88f, k / 0.4f) : Mathf.Lerp(0.88f, 1f, (k - 0.4f) / 0.6f) + 0.06f * Mathf.Sin((k - 0.4f) / 0.6f * Mathf.PI);
            rt.localScale = s0 * s;
            yield return null;
        }
        rt.localScale = s0;
    }

    private void HienChu(string chu, Vector2 viTri, float co, Color mau, float thoiGian, bool nayManh, bool rungLac = false)
    {
        if (_chu.Count == 0) return;
        var t = _chu[_chuKe]; _chuKe = (_chuKe + 1) % _chu.Count;
        StartCoroutine(CoChu(t, chu, viTri, co, mau, thoiGian, nayManh, rungLac));
    }

    private IEnumerator CoChu(TMP_Text t, string chu, Vector2 viTri, float co, Color mau, float thoiGian, bool nayManh, bool rungLac)
    {
        int o = _chu.IndexOf(t);
        int phien = ++_phienChu[o];
        var rt = (RectTransform)t.transform.parent;
        var bong = rt.GetChild(0).GetComponent<TMP_Text>();
        rt.gameObject.SetActive(true);
        t.text = chu; t.fontSize = co; t.color = mau;
        t.enableVertexGradient = true;
        Color sang = Color.Lerp(mau, Color.white, 0.45f);
        t.colorGradient = new VertexGradient(sang, sang, mau, mau);
        bong.text = chu; bong.fontSize = co;
        rt.SetAsLastSibling();
        float xoay = nayManh ? Random.Range(-12f, 12f) : 0f;
        float tt = 0f;
        while (tt < thoiGian)
        {
            if (_phienChu[o] != phien) yield break;   // o chu nay da duoc dung cho chu moi
            tt += Time.unscaledDeltaTime;
            float k = tt / thoiGian;
            float s;
            if (nayManh) s = k < 0.18f ? Mathf.LerpUnclamped(2.4f, 0.9f, k / 0.18f) : (k < 0.3f ? Mathf.Lerp(0.9f, 1.05f, (k - 0.18f) / 0.12f) : 1.05f - 0.05f * (k - 0.3f));
            else s = k < 0.2f ? Mathf.Lerp(0.6f, 1f, k / 0.2f) : 1f;
            if (rungLac && k > 0.15f && k < 0.7f) s *= 1f + 0.04f * Mathf.Sin(tt * 40f);
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, xoay * (1f - k));
            rt.anchoredPosition = viTri + new Vector2(0f, (nayManh ? 60f : 40f) * Mathf.Max(0f, k - 0.3f));
            float a = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
            t.alpha = a;
            bong.alpha = a * 0.75f;
            yield return null;
        }
        rt.gameObject.SetActive(false);
    }

    private int[] _phienChuArr;
    private int[] _phienChu { get { if (_phienChuArr == null || _phienChuArr.Length != _chu.Count) _phienChuArr = new int[_chu.Count]; return _phienChuArr; } }

    private IEnumerator CoRays(Vector2 p)
    {
        if (_rays == null) yield break;
        var rt = _rays.rectTransform;
        rt.anchoredPosition = p;
        rt.SetSiblingIndex(0);
        _rays.enabled = true;
        float t = 0f;
        while (t < 1.4f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 1.4f;
            rt.localRotation = Quaternion.Euler(0f, 0f, -t * 40f);
            float s = Mathf.Lerp(0.4f, 1.2f, Mathf.Min(1f, k * 3f));
            rt.localScale = new Vector3(s, s, 1f);
            _rays.color = new Color(1f, 0.85f, 0.4f, 0.6f * (k < 0.7f ? 1f : (1f - k) / 0.3f));
            yield return null;
        }
        _rays.enabled = false;
    }

    private IEnumerator CoFlash()
    {
        if (_flashImg == null) yield break;
        _flashImg.enabled = true;
        _flash.SetAsFirstSibling();
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            _flashImg.color = new Color(1f, 0.9f, 0.6f, 0.35f * (1f - t / 0.3f));
            yield return null;
        }
        _flashImg.enabled = false;
    }

    // =====================================================================
    //  Keo tha the
    // =====================================================================

    private void GanKeoThaChoThe()
    {
        foreach (var ten in new[] { "Scroll_Grid_Ingredients", "Scroll_Grid_Seasonings" })
        {
            var g = TimSau(transform, ten);
            if (g == null) continue;
            foreach (var the in g.GetComponentsInChildren<SelectableIngredientCard>(true))
                if (the.GetComponent<KitchenCardDrag>() == null) the.gameObject.AddComponent<KitchenCardDrag>();
        }
    }

    // =====================================================================
    //  Tien ich
    // =====================================================================

    private Hat LayHat()
    {
        for (int i = 0; i < _hat.Count; i++) if (!_hat[i].song) return _hat[i];
        return null;   // day pool: bo qua hat nay (khong tao them)
    }

    private void BatHat(Hat h)
    {
        h.song = true;
        h.rt.SetAsLastSibling();
        h.rt.anchoredPosition = h.pos;
        h.rt.sizeDelta = new Vector2(h.kt, h.kt);
        h.rt.localRotation = Quaternion.identity;
        h.img.color = h.mau;
        h.img.enabled = h.tuoi >= 0f;
    }

    private static void TatHat(Hat h)
    {
        h.song = false;
        h.img.enabled = false;
    }

    private Vector2 ViTriLop(Vector3 world)
    {
        Vector3 l = _lop.InverseTransformPoint(world);
        return new Vector2(l.x, l.y);
    }

    /// <summary>Diem ngay tren mieng lo (toa do lop FX), cong them do cao.</summary>
    private Vector2 ViTriTrenLo(float cao)
    {
        if (_oven == null) return Vector2.zero;
        var mieng = _oven.Find("Oven_Fire") as RectTransform;
        Vector3 w = mieng != null ? mieng.position : _oven.position;
        return ViTriLop(w) + new Vector2(0f, cao);
    }

    private static Transform TimSau(Transform cha, string ten)
    {
        if (cha == null) return null;
        for (int i = 0; i < cha.childCount; i++)
        {
            var c = cha.GetChild(i);
            if (c.name == ten) return c;
            var s = TimSau(c, ten);
            if (s != null) return s;
        }
        return null;
    }
}
