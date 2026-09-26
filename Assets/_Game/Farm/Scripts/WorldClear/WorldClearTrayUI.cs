// ============================================================================
//  WorldClearTrayUI — KHAY DUNG CU (Rìu / Kéo / Búa) hien khi cham cay / bui / da (2026-09-25)
//  Nam trong Canvas_HUD ten "WorldClearTray" (Tools > Farm Game > Dung Cu > 2 dung san vao Hierarchy
//  de Sep keo chinh tay; khong co thi luc chay tu dung y het).
//  - Icon dung cu KEO duoc (WorldClearToolDrag) -> tha vao vat de bat dau.
//  - Het dung cu: icon mo, nut "BUY TOOLS" mo Shop tab Cong cu.
//  - Vat dang lam: hien gio con lai + nut kim cuong "Finish now".
//  Nen kem bo goc (SeedPanelSkin) giong khay hat giong.
// ============================================================================
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldClearTrayUI : MonoBehaviour
{
    public static bool DangKeo { get; set; }

    [Header("Tham chieu (tool / code tu gan)")]
    public Image nen;
    public Image nenIcon;
    public Image icon;
    public TextMeshProUGUI txtSoLuong;
    public TextMeshProUGUI txtTen;
    public TextMeshProUGUI txtHuongDan;
    public Button btnShop;
    public TextMeshProUGUI txtShop;
    public Button btnKimCuong;
    public TextMeshProUGUI txtKimCuong;
    public Button btnDong;
    public CanvasGroup nhom;

    private WorldClearable _vat;
    private bool _dangHien;
    private Coroutine _anim;
    private string _cuSo, _cuHd, _cuGem;

    public WorldClearable Vat => _vat;
    public WorldClearKind Kind => _vat != null ? _vat.kind : WorldClearKind.Riu;
    public bool CoTheKeo => _dangHien && _vat != null && !_vat.DangLam && !_vat.DaXong
                            && WorldClearManager.Instance != null && WorldClearManager.Instance.SoDungCu(_vat.kind) > 0;

    // =====================================================================
    //  Chu (vi -> Loc.T ra tieng Anh)
    // =====================================================================
    public static string TenVat(WorldClearKind k)
    {
        switch (k) { case WorldClearKind.Keo: return "Bụi cây"; case WorldClearKind.Bua: return "Tảng đá"; default: return "Cây gỗ"; }
    }
    public static string TenDungCu(WorldClearKind k)
    {
        switch (k) { case WorldClearKind.Keo: return "Kéo"; case WorldClearKind.Bua: return "Búa"; default: return "Rìu"; }
    }
    public static string ChuKeo(WorldClearKind k)
    {
        switch (k) { case WorldClearKind.Keo: return "Kéo kéo vào bụi cây để cắt"; case WorldClearKind.Bua: return "Kéo búa vào tảng đá để đập"; default: return "Kéo rìu vào cây để chặt"; }
    }
    public static string ChuHet(WorldClearKind k)
    {
        switch (k) { case WorldClearKind.Keo: return "Hết kéo! Mua thêm ở Cửa hàng"; case WorldClearKind.Bua: return "Hết búa! Mua thêm ở Cửa hàng"; default: return "Hết rìu! Mua thêm ở Cửa hàng"; }
    }
    public static string ChuDangLam(WorldClearKind k)
    {
        switch (k) { case WorldClearKind.Keo: return "Đang cắt... {0}"; case WorldClearKind.Bua: return "Đang đập... {0}"; default: return "Đang chặt... {0}"; }
    }

    // =====================================================================
    //  Tim / dung
    // =====================================================================
    public static WorldClearTrayUI TimHoacDung()
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<WorldClearTrayUI>())
            if (t != null && t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded) { t.ApSkin(); t.NoiNut(); return t; }

        Transform cha = null;
        var hud = GameObject.Find("Canvas_HUD");
        if (hud != null && hud.GetComponent<Canvas>() != null) cha = hud.transform;
        if (cha == null)
        {
            var go = new GameObject("Canvas_WorldClear", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 150;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.matchWidthOrHeight = 0.5f;
            cha = go.transform;
        }
        var tray = Dung(cha);
        tray.NoiNut();
        return tray;
    }

    /// <summary>Dung khay duoi 'cha' (Canvas). Dung chung cho luc chay va tool Editor.</summary>
    public static WorldClearTrayUI Dung(Transform cha)
    {
        var root = TaoRt("WorldClearTray", cha);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 36f);
        root.sizeDelta = new Vector2(660f, 180f);
        var tray = root.gameObject.AddComponent<WorldClearTrayUI>();
        tray.nhom = root.gameObject.AddComponent<CanvasGroup>();
        tray.nen = root.gameObject.AddComponent<Image>();

        // O dung cu ben trai
        var o = TaoRt("O_DungCu", root);
        o.anchorMin = o.anchorMax = new Vector2(0f, 0.5f);
        o.pivot = new Vector2(0.5f, 0.5f);
        o.anchoredPosition = new Vector2(98f, 0f);
        o.sizeDelta = new Vector2(136f, 136f);
        tray.nenIcon = o.gameObject.AddComponent<Image>();
        tray.nenIcon.raycastTarget = false;

        var ic = TaoRt("Icon", o);
        ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
        ic.sizeDelta = new Vector2(112f, 112f);
        tray.icon = ic.gameObject.AddComponent<Image>();
        tray.icon.preserveAspect = true;
        tray.icon.raycastTarget = true;
        ic.gameObject.AddComponent<WorldClearToolDrag>().tray = tray;

        tray.txtSoLuong = TaoChu("Txt_SoLuong", o, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(6f, -4f), new Vector2(90f, 40f), 32f, TextAlignmentOptions.BottomRight, FontStyles.Bold);

        // Chu ben phai
        tray.txtTen = TaoChu("Txt_Ten", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(186f, -20f), new Vector2(420f, 44f), 34f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        tray.txtHuongDan = TaoChu("Txt_HuongDan", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(186f, -68f), new Vector2(430f, 44f), 25f, TextAlignmentOptions.TopLeft, FontStyles.Normal);

        tray.btnShop = TaoNut("Btn_Shop", root, out tray.txtShop);
        tray.btnKimCuong = TaoNut("Btn_KimCuong", root, out tray.txtKimCuong);

        // Nut dong
        var d = TaoRt("Btn_Dong", root);
        d.anchorMin = d.anchorMax = new Vector2(1f, 1f);
        d.pivot = new Vector2(0.5f, 0.5f);
        d.anchoredPosition = new Vector2(-14f, -14f);
        d.sizeDelta = new Vector2(48f, 48f);
        var dImg = d.gameObject.AddComponent<Image>();
        tray.btnDong = d.gameObject.AddComponent<Button>();
        tray.btnDong.targetGraphic = dImg;
        var chuX = TaoChu("X", d, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 26f, TextAlignmentOptions.Center, FontStyles.Bold);
        chuX.text = "X";
        chuX.color = Color.white;

        tray.ApSkin();
        root.gameObject.SetActive(false);
        return tray;
    }

    private static RectTransform TaoRt(string ten, Transform cha)
    {
        var go = new GameObject(ten, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(cha, false);
        return rt;
    }

    private static TextMeshProUGUI TaoChu(string ten, Transform cha, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, float co, TextAlignmentOptions canh, FontStyles kieu)
    {
        var rt = TaoRt(ten, cha);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.pivot = aMin == aMax ? aMin : new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = co;
        t.fontStyle = kieu;
        t.alignment = canh;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        t.color = SeedPanelSkin.MauChuTen;
        return t;
    }

    private static Button TaoNut(string ten, Transform cha, out TextMeshProUGUI chu)
    {
        var rt = TaoRt(ten, cha);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-22f, 18f);
        rt.sizeDelta = new Vector2(230f, 58f);
        var img = rt.gameObject.AddComponent<Image>();
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        chu = TaoChu("Txt", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 24f, TextAlignmentOptions.Center, FontStyles.Bold);
        chu.color = Color.white;
        return b;
    }

    /// <summary>Sprite ve bang code khong luu duoc trong scene -> gan lai moi lan vao game.</summary>
    public void ApSkin()
    {
        if (nen != null) SeedPanelSkin.ApDungKhung(nen);
        var bo = WorldClearFX.BoGocTrang(64, 22, 100f);
        if (nenIcon != null) { nenIcon.sprite = bo; nenIcon.type = Image.Type.Sliced; nenIcon.color = new Color(1f, 0.90f, 0.72f, 1f); }
        MauNut(btnShop, new Color(0.34f, 0.70f, 0.25f, 1f), bo);
        MauNut(btnKimCuong, new Color(0.22f, 0.55f, 0.92f, 1f), bo);
        MauNut(btnDong, new Color(0.80f, 0.40f, 0.28f, 1f), bo);
        DungSkinGame();
    }

    // [2026-09-25] Dung DUNG art cua game: nut dong = Btn_Close cua popup, nut kim cuong = card Btn_SpeedUp + Icon_Gem.
    private static Sprite _spDong, _spTangToc, _spGem;
    private static bool _daTimSkin;

    private static void TimSkinGame()
    {
        if (_daTimSkin && _spDong != null && _spTangToc != null && _spGem != null) return;
        _daTimSkin = true;
        foreach (var im in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (im == null || im.sprite == null || !im.gameObject.scene.IsValid()) continue;
            string n = im.gameObject.name;
            if (_spDong == null && n == "Btn_Close") _spDong = im.sprite;
            else if (_spTangToc == null && (n == "Btn_SpeedUp" || n == "Btn_gem")) _spTangToc = im.sprite;
            else if (_spGem == null && n == "Icon_Gem") _spGem = im.sprite;
        }
    }

    private void DungSkinGame()
    {
        TimSkinGame();
        if (btnDong != null && _spDong != null)
        {
            var img = btnDong.targetGraphic as Image;
            // [2026-09-26] Giong HET nut dong cac popup: btn_red_small kieu Sliced, vuong 60x60 (thanh nut tron), chu X trang dam 26
            if (img != null) { img.sprite = _spDong; img.type = Image.Type.Sliced; img.preserveAspect = false; img.pixelsPerUnitMultiplier = 1f; img.color = Color.white; }
            var rt = (RectTransform)btnDong.transform; rt.sizeDelta = new Vector2(60f, 60f); rt.anchoredPosition = new Vector2(-8f, -8f);
            var x = btnDong.transform.Find("X");
            if (x != null)
            {
                x.gameObject.SetActive(true);
                var tx = x.GetComponent<TMP_Text>();
                if (tx != null) { tx.text = "X"; tx.color = Color.white; tx.fontSize = 26f; tx.fontStyle = FontStyles.Bold; tx.alignment = TextAlignmentOptions.Center; }
            }
        }
        if (btnKimCuong != null)
        {
            var img = btnKimCuong.targetGraphic as Image;
            if (img != null && _spTangToc != null) { img.sprite = _spTangToc; img.type = Image.Type.Sliced; img.color = Color.white; }
            var rt = (RectTransform)btnKimCuong.transform; rt.sizeDelta = new Vector2(250f, 66f);
            var gt = btnKimCuong.transform.Find("Icon_Gem") as RectTransform;
            if (gt == null && _spGem != null)
            {
                var go = new GameObject("Icon_Gem", typeof(RectTransform), typeof(Image));
                gt = (RectTransform)go.transform; gt.SetParent(btnKimCuong.transform, false);
                gt.anchorMin = gt.anchorMax = new Vector2(1f, 0.5f); gt.pivot = new Vector2(1f, 0.5f);
                gt.anchoredPosition = new Vector2(-14f, 2f); gt.sizeDelta = new Vector2(44f, 44f);
                var gi = go.GetComponent<Image>(); gi.sprite = _spGem; gi.preserveAspect = true; gi.raycastTarget = false;
            }
            if (txtKimCuong != null)
            {
                var tr = txtKimCuong.rectTransform;
                tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(12f, 0f); tr.offsetMax = new Vector2(gt != null ? -62f : -12f, 0f);
                txtKimCuong.alignment = TextAlignmentOptions.Center;
                txtKimCuong.color = Color.white;
            }
        }
    }

    private static void MauNut(Button b, Color c, Sprite sp)
    {
        if (b == null) return;
        var img = b.targetGraphic as Image;
        if (img == null) img = b.GetComponent<Image>();
        if (img == null) return;
        img.sprite = sp; img.type = Image.Type.Sliced; img.color = c;
    }

    private bool _daNoi;
    public void NoiNut()
    {
        if (_daNoi) return;
        _daNoi = true;
        if (btnDong != null) btnDong.onClick.AddListener(An);
        if (btnShop != null) btnShop.onClick.AddListener(MoShop);
        if (btnKimCuong != null) btnKimCuong.onClick.AddListener(BamKimCuong);
    }

    private void Awake() { ApSkin(); NoiNut(); }

    // =====================================================================
    //  Hien / an
    // =====================================================================
    public void HienChon(WorldClearable c)
    {
        _vat = c;
        if (FarmUIManager.Instance != null) FarmUIManager.Instance.HideAllPopups();
        MoRa();
        LamMoi(true);
    }

    public void HienDangLam(WorldClearable c) => HienChon(c);

    public void KhiVatXong(WorldClearable c) { if (_vat == c) An(); }

    private void MoRa()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (!_dangHien)
        {
            _dangHien = true;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(Pop(true));
        }
    }

    public void An()
    {
        if (!_dangHien) return;
        _dangHien = false;
        _vat = null;
        if (!gameObject.activeInHierarchy) return;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(Pop(false));
    }

    private IEnumerator Pop(bool mo)
    {
        var rt = (RectTransform)transform;
        float t = 0f, T = mo ? 0.22f : 0.14f;
        while (t < T)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / T);
            float s = mo ? Mathf.LerpUnclamped(0.86f, 1f, BackOut(u)) : Mathf.Lerp(1f, 0.9f, u);
            rt.localScale = new Vector3(s, s, 1f);
            if (nhom != null) nhom.alpha = mo ? Mathf.Clamp01(u * 1.6f) : 1f - u;
            yield return null;
        }
        rt.localScale = Vector3.one;
        if (nhom != null) nhom.alpha = mo ? 1f : 0f;
        if (!mo) gameObject.SetActive(false);
        _anim = null;
    }

    private static float BackOut(float t) { float c = 1.7f; t -= 1f; return 1f + (c + 1f) * t * t * t + c * t * t; }

    private void Update()
    {
        if (!_dangHien) return;
        bool dong = _vat == null || _vat.DaXong
                    || (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
                    || FarmInputLock.IsPopupOpen || FarmInputLock.IsSeedPopupOpen || FarmInputLock.IsCookingMode
                    || EditModeManager.IsEditMode;
        if (dong && !DangKeo) { An(); return; }
        LamMoi(false);
    }

    private void LamMoi(bool ep)
    {
        var mgr = WorldClearManager.Instance;
        if (_vat == null || mgr == null) return;
        var k = _vat.kind;
        if (ep)
        {
            if (icon != null) icon.sprite = mgr.Cfg.IconCua(k);
            if (txtTen != null) txtTen.text = Loc.T(TenVat(k)) + "  ·  " + Loc.T(TenDungCu(k));
            if (txtShop != null) txtShop.text = Loc.T("MUA DỤNG CỤ");
            _cuSo = _cuHd = _cuGem = null;
        }

        int so = mgr.SoDungCu(k);
        string sSo = "x" + so;
        if (sSo != _cuSo && txtSoLuong != null) { txtSoLuong.text = sSo; txtSoLuong.color = so > 0 ? SeedPanelSkin.MauSoCon : SeedPanelSkin.MauSoHet; _cuSo = sSo; }

        bool lam = _vat.DangLam;
        string hd;
        if (lam)
        {
            int s = _vat.GiayConLai;
            string gio = s >= 60 ? (s / 60) + ":" + (s % 60).ToString("00") : s + "s";
            hd = Loc.TF(ChuDangLam(k), gio);
        }
        else hd = Loc.T(so > 0 ? ChuKeo(k) : ChuHet(k));
        if (hd != _cuHd && txtHuongDan != null) { txtHuongDan.text = hd; _cuHd = hd; }

        if (btnKimCuong != null) btnKimCuong.gameObject.SetActive(lam);
        if (btnShop != null) btnShop.gameObject.SetActive(!lam && so <= 0);
        if (lam && txtKimCuong != null)
        {
            string g = Loc.TF("Xong ngay: {0}", mgr.GiaKimCuong(_vat).ToString());
            if (g != _cuGem) { txtKimCuong.text = g; _cuGem = g; }
        }
        if (icon != null)
        {
            var c = icon.color;
            c.a = lam ? 0.55f : (so > 0 ? 1f : 0.45f);
            if (!DangKeo) icon.color = c;
        }
    }

    private void MoShop()
    {
        An();
        var sm = ShopManager.Instance;
        if (sm == null) return;
        sm.OpenShop();
        sm.ShowTab(3);                                      // tab thu 4: CONG CU
    }

    private void BamKimCuong()
    {
        var mgr = WorldClearManager.Instance;
        if (mgr == null || _vat == null) return;
        if (!mgr.ThuXongNgay(_vat))
        {
            LockedHintFX.Show(Loc.T("Không đủ kim cương"));
            RungLoi();
        }
    }

    public void RungLoi()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(Lac());
    }

    private IEnumerator Lac()
    {
        var rt = icon != null ? icon.rectTransform : (RectTransform)transform;
        Vector2 p0 = rt.anchoredPosition;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = p0 + new Vector2(Mathf.Sin(t * 60f) * 10f * (1f - t / 0.3f), 0f);
            yield return null;
        }
        rt.anchoredPosition = p0;
    }
}
