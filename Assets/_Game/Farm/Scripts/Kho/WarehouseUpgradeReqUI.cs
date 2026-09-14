using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Khung "Nâng cấp Kho" (WarehouseUpgradeReqUI) — Giao diện Popup Nâng cấp kho chuẩn Township:
/// - Nền ván gỗ bo góc cao cấp (FrameWood) lót giấy kem ấm áp (PanelPaper).
/// - Nút ĐÓNG (X) tròn đỏ 3D nổi bật ở góc trên phải.
/// - Hàng thẻ Card nguyên liệu gỗ/giấy bo góc hiển thị rõ số lượng sở hữu/yêu cầu.
/// - Dòng giá vàng có icon đồng xu lúa vàng óng ả chính thức.
/// - Nút bấm hành động 3D nổi bật (BtnGreen3D khi đủ / BtnGray khi thiếu) kèm hiệu ứng nảy Pop-in khi mở.
/// </summary>
public class WarehouseUpgradeReqUI : MonoBehaviour
{
    private const int MAX_O = 6;

    private static readonly Color MAU_NEN_TOI   = new Color(0.04f, 0.08f, 0.03f, 0.72f);
    private static readonly Color MAU_CHU_TIEU_DE = new Color(0.35f, 0.18f, 0.05f, 1f); // Nâu gỗ đậm
    private static readonly Color MAU_CHU       = new Color(0.42f, 0.30f, 0.16f, 1f);
    private static readonly Color MAU_DU        = new Color(0.18f, 0.58f, 0.18f, 1f); // Xanh lá đủ
    private static readonly Color MAU_THIEU     = new Color(0.85f, 0.22f, 0.18f, 1f); // Đỏ thiếu
    private static readonly Color MAU_O_DU      = new Color(0.92f, 0.98f, 0.90f, 1f);
    private static readonly Color MAU_O_THIEU   = new Color(1.00f, 0.93f, 0.91f, 1f);

    private TMP_FontAsset _font;
    private int _capHienTai = -1;

    private RectTransform _bangRoot;
    private Image _frameWood;
    private Image _paperBg;
    private TMP_Text _tieuDe;
    private Image _iconVang;
    private TMP_Text _dongVang;
    private RectTransform _hangO;
    private Button _nutXacNhan;
    private Image _imgNutXacNhan;
    private TMP_Text _chuNutXacNhan;
    private TMP_Text _dongCanhBao;
    private Button _nutDong;

    private readonly List<ONguyenLieu> _oList = new List<ONguyenLieu>();
    private System.Action _khiXacNhan;
    private Coroutine _popAnimCoroutine;

    private class ONguyenLieu
    {
        public GameObject root;
        public Image nenOuter;
        public Image nenInner;
        public Image icon;
        public TMP_Text ten;
        public TMP_Text soLuong;
        public string itemId;
    }

    public static WarehouseUpgradeReqUI LayHoacTao(Transform trongPopup, TMP_FontAsset font)
    {
        if (trongPopup == null) { return null; }
        Canvas canvas = trongPopup.GetComponentInParent<Canvas>();
        Transform cha = canvas != null ? canvas.transform : trongPopup.root;

        Transform cu = cha.Find("KhungNangCapKho");
        if (cu != null) { return cu.GetComponent<WarehouseUpgradeReqUI>(); }

        GameObject go = new GameObject("KhungNangCapKho", typeof(RectTransform));
        go.transform.SetParent(cha, false);
        WarehouseUpgradeReqUI ui = go.AddComponent<WarehouseUpgradeReqUI>();
        ui._font = font;
        ui.Dung();
        go.SetActive(false);
        return ui;
    }

    public void Mo(int capHienTai, System.Action khiXacNhan)
    {
        _capHienTai = capHienTai;
        _khiXacNhan = khiXacNhan;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        VeLai();

        // Hiá»‡u á»©ng náº£y Pop-in má»Ÿ popup má»m máº¡i, sá»‘ng Ä‘á»™ng
        if (_popAnimCoroutine != null) StopCoroutine(_popAnimCoroutine);
        _popAnimCoroutine = StartCoroutine(PopInRoutine());
    }

    public void Dong()
    {
        if (_popAnimCoroutine != null) StopCoroutine(_popAnimCoroutine);
        gameObject.SetActive(false);
    }

    private void OnEnable() { InvokeRepeating(nameof(VeLai), 0.5f, 0.5f); }
    private void OnDisable() { CancelInvoke(nameof(VeLai)); }

    private IEnumerator PopInRoutine()
    {
        if (_bangRoot == null) yield break;
        float dur = 0.24f;
        float elapsed = 0f;
        _bangRoot.localScale = Vector3.one * 0.7f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            // Ease out back náº£y nháº¹
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float p = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            float scale = Mathf.LerpUnclamped(0.7f, 1f, p);
            _bangRoot.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        _bangRoot.localScale = Vector3.one;
        _popAnimCoroutine = null;
    }

    // â”€â”€ Dá»±ng khung UI chuáº©n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private void Dung()
    {
        RectTransform me = (RectTransform)transform;
        PhuKin(me);

        // 1. MÃ n Ä‘en má» che toÃ n bá»™ phÃ­a sau (nuá»‘t click)
        Image nenToi = gameObject.AddComponent<Image>();
        nenToi.color = MAU_NEN_TOI;
        Button chanClick = gameObject.AddComponent<Button>();
        chanClick.transition = Selectable.Transition.None;
        chanClick.onClick.AddListener(Dong);

        // 2. ROOT Báº¢NG POPUP (Rá»™ng 780, Cao 560)
        const float RONG = 780f, CAO = 560f;
        GameObject bangGo = new GameObject("BangRoot", typeof(RectTransform));
        bangGo.transform.SetParent(me, false);
        _bangRoot = (RectTransform)bangGo.transform;
        _bangRoot.anchorMin = _bangRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _bangRoot.pivot = new Vector2(0.5f, 0.5f);
        _bangRoot.anchoredPosition = Vector2.zero;
        _bangRoot.sizeDelta = new Vector2(RONG, CAO);

        Button chanXuyen = bangGo.AddComponent<Button>();
        chanXuyen.transition = Selectable.Transition.None; // KhÃ´ng Ä‘Ã³ng khi click vÃ o báº£ng

        // 3. Khung gá»— ngoÃ i (FrameWood)
        chanXuyen.transition = Selectable.Transition.None; // Không đóng khi click vào bảng

        // 3. Khung gỗ ngoài (FrameWood)
        Sprite sprFrame = UIStandardSprites.FrameWood;
        _frameWood = TaoAnh(_bangRoot, "FrameWood", Color.white, sprFrame);
        PhuKin(_frameWood.rectTransform);
        _frameWood.type = Image.Type.Sliced;
        if (sprFrame == null) _frameWood.color = new Color(0.55f, 0.38f, 0.20f, 1f);

        // 4. Lót giấy kem bên trong (PanelPaper)
        Sprite sprPaper = UIStandardSprites.PanelPaper;
        _paperBg = TaoAnh(_frameWood.transform, "PaperBg", Color.white, sprPaper);
        PhuKin(_paperBg.rectTransform, 26f); // Thụt vào 26px từ viền gỗ
        _paperBg.type = Image.Type.Sliced;
        if (sprPaper == null) _paperBg.color = new Color(0.99f, 0.95f, 0.88f, 1f);

        // 5. Tiêu đề: NÂNG CẤP KHO
        _tieuDe = TaoChu(_paperBg.transform, "TieuDe", Loc.T("Nâng cấp Kho"), 36, MAU_CHU_TIEU_DE, TextAlignmentOptions.Center);
        Neo(_tieuDe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(650f, 48f));
        _tieuDe.fontStyle = FontStyles.Bold;

        // 6. Cụm giá vàng: Icon đồng xu lúa + Text số vàng
        GameObject goldGroup = new GameObject("GoldGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        goldGroup.transform.SetParent(_paperBg.transform, false);
        RectTransform goldRt = (RectTransform)goldGroup.transform;
        Neo(goldRt, new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(400f, 44f));
        HorizontalLayoutGroup hlgGold = goldGroup.GetComponent<HorizontalLayoutGroup>();
        hlgGold.spacing = 10f;
        hlgGold.childAlignment = TextAnchor.MiddleCenter;
        hlgGold.childControlWidth = false;
        hlgGold.childControlHeight = false;

        _iconVang = TaoAnh(goldRt, "IconGold", Color.white, UIStandardSprites.IconGold);
        _iconVang.rectTransform.sizeDelta = new Vector2(38f, 38f);
        _iconVang.preserveAspect = true;

        _dongVang = TaoChu(goldRt, "TxtGold", "1.500", 28, MAU_CHU, TextAlignmentOptions.Left);
        _dongVang.rectTransform.sizeDelta = new Vector2(280f, 40f);
        _dongVang.fontStyle = FontStyles.Bold;

        // 7. Hàng card nguyên liệu (Gỗ, Đá, Đinh...)
        GameObject hang = new GameObject("HangCard", typeof(RectTransform));
        hang.transform.SetParent(_paperBg.transform, false);
        _hangO = (RectTransform)hang.transform;
        Neo(_hangO, new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(690f, 180f));
        HorizontalLayoutGroup hlg = hang.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 18f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < MAX_O; i++) { _oList.Add(TaoCard(_hangO)); }

        // 8. Dòng cảnh báo khi thiếu đồ
        _dongCanhBao = TaoChu(_paperBg.transform, "CanhBao", "", 21, MAU_THIEU, TextAlignmentOptions.Center);
        Neo(_dongCanhBao.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -346f), new Vector2(680f, 34f));
        _dongCanhBao.fontStyle = FontStyles.Bold;

        // 9. NÚT XÁC NHẬN 3D (BtnGreen3D hoặc BtnGray)
        Sprite sprBtn = UIStandardSprites.BtnGreen3D != null ? UIStandardSprites.BtnGreen3D : UIStandardSprites.BtnGreen;
        _imgNutXacNhan = TaoAnh(_paperBg.transform, "NutXacNhan", Color.white, sprBtn);
        Neo(_imgNutXacNhan.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(280f, 68f));
        _imgNutXacNhan.type = Image.Type.Sliced;
        _nutXacNhan = _imgNutXacNhan.gameObject.AddComponent<Button>();
        _nutXacNhan.targetGraphic = _imgNutXacNhan;
        _nutXacNhan.onClick.AddListener(BamXacNhan);

        _chuNutXacNhan = TaoChu(_imgNutXacNhan.rectTransform, "Chu", Loc.T("NÂNG CẤP"), 28, Color.white, TextAlignmentOptions.Center);
        PhuKin(_chuNutXacNhan.rectTransform);
        _chuNutXacNhan.fontStyle = FontStyles.Bold;

        // 10. NÚT ĐÓNG (X) TRÒN ĐỎ 3D NỔI BẬT GÓC TRÊN PHẢI
        Sprite sprClose = UIStandardSprites.Close;
        Image imgDong = TaoAnh(_bangRoot, "BtnClose", Color.white, sprClose);
        Neo(imgDong.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(64f, 64f));
        imgDong.type = Image.Type.Sliced;
        _nutDong = imgDong.gameObject.AddComponent<Button>();
        _nutDong.targetGraphic = imgDong;
        _nutDong.onClick.AddListener(Dong);

        TMP_Text txtX = TaoChu(imgDong.rectTransform, "TxtX", "X", 26, Color.white, TextAlignmentOptions.Center);
        PhuKin(txtX.rectTransform);
        txtX.fontStyle = FontStyles.Bold;
    }

    private ONguyenLieu TaoCard(Transform cha)
    {
        ONguyenLieu o = new ONguyenLieu();

        // Khung Card ngoài bo góc
        Sprite sprOuter = UIStandardSprites.CardOuter;
        Image outer = TaoAnh(cha, "CardOuter", Color.white, sprOuter);
        outer.type = Image.Type.Sliced;
        o.root = outer.gameObject;
        o.nenOuter = outer;

        LayoutElement le = outer.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 118f;
        le.preferredHeight = 175f;

        // Nền giấy kem trong
        Sprite sprInner = UIStandardSprites.CardInner;
        o.nenInner = TaoAnh(outer.rectTransform, "CardInner", MAU_O_THIEU, sprInner);
        PhuKin(o.nenInner.rectTransform, 6f);
        o.nenInner.type = Image.Type.Sliced;

        // Icon nguyên liệu (Gỗ, Đá...)
        o.icon = TaoAnh(o.nenInner.rectTransform, "Icon", Color.white, null);
        Neo(o.icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(76f, 76f));
        o.icon.preserveAspect = true;

        // Tên nguyên liệu
        o.ten = TaoChu(o.nenInner.rectTransform, "Ten", "", 19, MAU_CHU, TextAlignmentOptions.Center);
        Neo(o.ten.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(106f, 26f));
        o.ten.fontStyle = FontStyles.Bold;

        // Số lượng hiện có / yêu cầu
        o.soLuong = TaoChu(o.nenInner.rectTransform, "SoLuong", "", 22, MAU_THIEU, TextAlignmentOptions.Center);
        Neo(o.soLuong.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(106f, 30f));
        o.soLuong.fontStyle = FontStyles.Bold;

        return o;
    }

    // ── Cập nhật nội dung hiển thị ─────────────────────────────────────────
    public void VeLai()
    {
        if (_bangRoot == null || _capHienTai < 0) return;

        if (!WarehouseUpgradeCostTable.CanUpgradeFurther(_capHienTai))
        {
            _tieuDe.text = Loc.T("Kho đã ở cấp tối đa");
            _dongVang.text = "";
            _dongCanhBao.text = "";
            foreach (ONguyenLieu o in _oList) { o.root.SetActive(false); }
            _nutXacNhan.gameObject.SetActive(false);
            return;
        }

        WarehouseUpgradeCostTable.Entry e = WarehouseUpgradeCostTable.CostFor(_capHienTai);
        _tieuDe.text = $"{Loc.T("Nâng cấp Kho")}  ·  {Loc.T("Cấp")} {_capHienTai} → {_capHienTai + 1}";

        FarmEconomyManager eco = FarmEconomyManager.Instance;
        int vangDangCo = eco != null ? eco.Gold : 0;
        bool duVang = e.gold <= 0 || vangDangCo >= e.gold;
        _dongVang.text = $"{Loc.T("Vàng")}: <color=#{(duVang ? "2E7D32" : "C62828")}>{vangDangCo:n0}</color> / {e.gold:n0}";

        bool duHet = duVang;
        int n = e.materials != null ? e.materials.Count : 0;
        for (int i = 0; i < _oList.Count; i++)
        {
            ONguyenLieu o = _oList[i];
            if (i >= n)
            {
                o.root.SetActive(false);
                continue;
            }
            o.root.SetActive(true);
            BuildMaterialCost c = e.materials[i];
            o.itemId = c.itemId;

            int dangCo = BuildMaterials.Owned(c.itemId);
            bool du = dangCo >= c.amount;
            duHet &= du;

            Sprite icon = BuildMaterials.IconOf(c.itemId);
            o.icon.sprite = icon;
            o.icon.enabled = true;
            o.icon.color = icon != null ? Color.white : new Color(0.78f, 0.68f, 0.52f, 1f);

            o.ten.text = Loc.T(BuildMaterials.DisplayNameOf(c.itemId));
            o.soLuong.text = du ? $"{dangCo}/{c.amount}  ✓" : $"{dangCo}/{c.amount}";
            o.soLuong.color = du ? MAU_DU : MAU_THIEU;
            o.nenInner.color = du ? MAU_O_DU : MAU_O_THIEU;
        }

        _nutXacNhan.gameObject.SetActive(true);
        _nutXacNhan.interactable = duHet;

        Sprite sprBtnOn = UIStandardSprites.BtnGreen3D != null ? UIStandardSprites.BtnGreen3D : UIStandardSprites.BtnGreen;
        Sprite sprBtnOff = UIStandardSprites.BtnGray != null ? UIStandardSprites.BtnGray : UIStandardSprites.BtnGreen;

        if (_imgNutXacNhan != null)
        {
            _imgNutXacNhan.sprite = duHet ? sprBtnOn : sprBtnOff;
            _imgNutXacNhan.color = duHet ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        _chuNutXacNhan.text = duHet ? Loc.T("NÂNG CẤP") : Loc.T("CHƯA ĐỦ");
        _dongCanhBao.text = duHet ? "" : Loc.T("Còn thiếu đồ — gom thêm từ tàu lửa rồi quay lại nhé!");
    }

    private void BamXacNhan()
    {
        if (_khiXacNhan != null) { _khiXacNhan.Invoke(); }
        VeLai();
    }

    // â”€â”€ Tiá»‡n Ã­ch Ä‘á»‹nh vá»‹ RectTransform â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private static void PhuKin(RectTransform rt, float noiRong = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(noiRong, noiRong);
        rt.offsetMax = new Vector2(-noiRong, -noiRong);
    }

    private static void Neo(RectTransform rt, Vector2 neo, Vector2 viTri, Vector2 co)
    {
        rt.anchorMin = rt.anchorMax = neo;
        rt.pivot = new Vector2(0.5f, neo.y >= 0.999f ? 1f : (neo.y <= 0.001f ? 0f : 0.5f));
        rt.anchoredPosition = viTri;
        rt.sizeDelta = co;
    }

    private static Image TaoAnh(Transform cha, string ten, Color mau, Sprite sprite)
    {
        GameObject go = new GameObject(ten, typeof(RectTransform));
        go.transform.SetParent(cha, false);
        Image img = go.AddComponent<Image>();
        img.color = mau;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    private TMP_Text TaoChu(Transform cha, string ten, string noiDung, float co, Color mau,
                            TextAlignmentOptions canLe)
    {
        GameObject go = new GameObject(ten, typeof(RectTransform));
        go.transform.SetParent(cha, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) { t.font = _font; }
        t.text = noiDung;
        t.fontSize = co;
        t.color = mau;
        t.alignment = canLe;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}