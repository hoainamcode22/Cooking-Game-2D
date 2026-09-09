using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ============================================================================
/// POPUP MUA Ô ĐẤT — dựng 100% bằng code, không cần prefab
/// ============================================================================
///
/// VÒNG 16 — làm lại cho đẹp theo yêu cầu:
///   • Chữ TextMeshPro + font "Fonts/Baloo2 SDF" → có dấu, cùng font với các
///     popup khác (KHO VẬT PHẨM, CỬA HÀNG…).
///   • Icon vàng (UIStandardSprites.IconGold) đứng cạnh giá.
///   • Bố cục gọn, hàng card cách đều, KHÔNG có nút X: bấm ra ngoài / Esc là tắt.
///
///   ┌────────────────────────────────────┐
///   │           MỞ RỘNG ĐẤT              │
///   │      Lô 5  ·  104 ô đất            │
///   │  ┌────┐ ┌────┐ ┌────┐ ┌────┐       │
///   │  │ Gỗ │ │ Đá │ │Đinh│ │Kính│       │  xanh = đủ, đỏ = thiếu
///   │  │0/21│ │0/14│ │0/12│ │ 0/7│       │
///   │  └────┘ └────┘ └────┘ └────┘       │
///   │      Cần mở "Lô 4" trước.          │
///   │   (🪙) 13.110          [ MUA ]     │
///   └────────────────────────────────────┘
/// </summary>
public class LandPurchasePopupUI : MonoBehaviour
{
    private static LandPurchasePopupUI _instance;

    private static readonly Color ColTitle  = new Color(0.36f, 0.22f, 0.10f);
    private static readonly Color ColSub    = new Color(0.45f, 0.32f, 0.18f);
    private static readonly Color ColGold   = new Color(0.30f, 0.22f, 0.10f);
    private static readonly Color ColOk     = new Color(0.20f, 0.55f, 0.20f);
    private static readonly Color ColBad    = new Color(0.82f, 0.24f, 0.20f);
    private static readonly Color ColCardOk = new Color(0.90f, 0.97f, 0.88f);
    private static readonly Color ColCardNo = new Color(1.00f, 0.93f, 0.90f);
    private static readonly Color ColBtnOn  = new Color(0.36f, 0.72f, 0.30f);
    private static readonly Color ColBtnOff = new Color(0.62f, 0.62f, 0.62f);

    private LandRegionData _region;
    private LandExpansionManager _manager;
    private TMP_FontAsset _font;

    private TMP_Text _txtTitle, _txtSub, _txtGold, _txtBuy, _txtWarn;
    private Button _btnBuy;
    private Image _imgGoldIcon;
    private Transform _cardRow;

    private class Card { public Image bg; public TMP_Text amount; public string itemId; }
    private readonly List<Card> _cards = new List<Card>();

    // ─────────────────────────────────────────────────────────────────────
    public static void Show(LandRegionData region, LandExpansionManager manager)
    {
        if (region == null || manager == null) return;
        if (_instance == null)
        {
            var go = new GameObject("LandPurchasePopup");
            _instance = go.AddComponent<LandPurchasePopupUI>();
            _instance.Build();
        }
        _instance.Bind(region, manager);
        _instance.gameObject.SetActive(true);
        _instance.transform.SetAsLastSibling();
    }

    public static void Hide()
    {
        if (_instance != null) _instance.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Hide();
    }

    // ─────────────────────────────────────────────────────────────────────
    private void Build()
    {
        _font = Resources.Load<TMP_FontAsset>(LandRegionSignBoard.FontResource);
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // nền mờ toàn màn — bấm ra ngoài là đóng
        var dim = NewImage(transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);
        var dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(Hide);

        // khung gỗ
        var frame = NewImage(transform, "Frame", Color.white, UIStandardSprites.FrameWood);
        frame.type = Image.Type.Sliced;
        var fr = frame.rectTransform;
        fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 0.5f);
        fr.pivot = new Vector2(0.5f, 0.5f);
        fr.anchoredPosition = Vector2.zero;
        fr.sizeDelta = new Vector2(680, 500);
        var block = frame.gameObject.AddComponent<Button>();   // nuốt click, khỏi đóng nhầm
        block.transition = Selectable.Transition.None;

        // giấy bên trong
        var paper = NewImage(frame.transform, "Paper", Color.white, UIStandardSprites.PanelPaper);
        paper.type = Image.Type.Sliced;
        var pr = paper.rectTransform;
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = new Vector2(30, 30); pr.offsetMax = new Vector2(-30, -30);
        if (paper.sprite == null) paper.color = new Color(0.99f, 0.95f, 0.86f);
        if (frame.sprite == null) frame.color = new Color(0.55f, 0.38f, 0.20f);

        _txtTitle = NewText(frame.transform, "Title", "MỞ RỘNG ĐẤT", 40, FontStyles.Bold, ColTitle);
        Anchor(_txtTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(580, 54));

        _txtSub = NewText(frame.transform, "Sub", "", 26, FontStyles.Normal, ColSub);
        Anchor(_txtSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -116), new Vector2(580, 40));

        // hàng card nguyên liệu
        var row = new GameObject("CardRow", typeof(RectTransform));
        row.transform.SetParent(frame.transform, false);
        _cardRow = row.transform;
        Anchor((RectTransform)row.transform, new Vector2(0.5f, 1f), new Vector2(0, -232), new Vector2(600, 170));
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 16; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

        // dòng cảnh báo
        _txtWarn = NewText(frame.transform, "Warn", "", 23, FontStyles.Bold, ColBad);
        Anchor(_txtWarn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -338), new Vector2(600, 34));

        // giá vàng: icon + số
        var goldBox = new GameObject("GoldBox", typeof(RectTransform));
        goldBox.transform.SetParent(frame.transform, false);
        Anchor((RectTransform)goldBox.transform, new Vector2(0.5f, 0f), new Vector2(-150, 78), new Vector2(260, 60));

        _imgGoldIcon = NewImage(goldBox.transform, "Icon", Color.white, UIStandardSprites.IconGold);
        _imgGoldIcon.preserveAspect = true;
        Anchor(_imgGoldIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(52, 52));
        if (_imgGoldIcon.sprite == null) _imgGoldIcon.color = new Color(0.95f, 0.78f, 0.20f);

        _txtGold = NewText(goldBox.transform, "Gold", "", 32, FontStyles.Bold, ColGold);
        _txtGold.alignment = TextAlignmentOptions.MidlineLeft;
        Anchor(_txtGold.rectTransform, new Vector2(0f, 0.5f), new Vector2(170, 0), new Vector2(200, 50));

        // nút MUA
        var buy = NewImage(frame.transform, "BtnBuy", ColBtnOn, UIStandardSprites.BtnGreen);
        buy.type = Image.Type.Sliced;
        Anchor(buy.rectTransform, new Vector2(0.5f, 0f), new Vector2(150, 78), new Vector2(230, 74));
        _btnBuy = buy.gameObject.AddComponent<Button>();
        _btnBuy.targetGraphic = buy;
        _btnBuy.onClick.AddListener(OnBuy);
        _txtBuy = NewText(buy.transform, "Label", "MUA", 32, FontStyles.Bold, Color.white);
        Stretch(_txtBuy.rectTransform);
    }

    // ─────────────────────────────────────────────────────────────────────
    private void Bind(LandRegionData region, LandExpansionManager manager)
    {
        _region = region;
        _manager = manager;

        string lv = region.unlockLevel > 0 ? $"  ·  mở ở cấp {region.unlockLevel}" : "";
        _txtSub.text = $"{region.displayName}  ·  {region.AreaCells} ô đất{lv}";

        if (region.goldPrice > 0)       { _txtGold.text = $"{region.goldPrice:n0}"; _imgGoldIcon.enabled = true; }
        else if (region.gemPrice > 0)   { _txtGold.text = $"{region.gemPrice} kim cương"; _imgGoldIcon.enabled = false; }
        else                            { _txtGold.text = "Miễn phí"; _imgGoldIcon.enabled = false; }

        for (int i = _cardRow.childCount - 1; i >= 0; i--) Destroy(_cardRow.GetChild(i).gameObject);
        _cards.Clear();
        foreach (var c in region.materialCosts)
            if (c.IsValid) MakeCard(c);

        Refresh();
    }

    private void MakeCard(BuildMaterialCost cost)
    {
        var bg = NewImage(_cardRow, $"Card_{cost.itemId}", ColCardNo, UIStandardSprites.PanelPaper);
        bg.type = Image.Type.Sliced;
        var le = bg.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 124; le.preferredHeight = 156;

        var icon = NewImage(bg.transform, "Icon", Color.white, BuildMaterials.IconOf(cost.itemId));
        icon.preserveAspect = true;
        Anchor(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(72, 72));
        if (icon.sprite == null) icon.color = new Color(0.75f, 0.65f, 0.5f);

        var name = NewText(bg.transform, "Name", BuildMaterials.DisplayNameOf(cost.itemId), 20, FontStyles.Normal, ColSub);
        Anchor(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -98), new Vector2(120, 26));

        var amount = NewText(bg.transform, "Amount", "", 24, FontStyles.Bold, ColBad);
        Anchor(amount.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(120, 30));

        _cards.Add(new Card { bg = bg, amount = amount, itemId = cost.itemId });
    }

    public void Refresh()
    {
        if (_region == null) return;

        bool enoughMat = true;
        foreach (var card in _cards)
        {
            int need = 0;
            foreach (var c in _region.materialCosts)
                if (c.itemId == card.itemId) { need = c.amount; break; }
            int own = BuildMaterials.Owned(card.itemId);
            bool ok = own >= need;
            enoughMat &= ok;
            card.amount.text = $"{own}/{need}";
            card.amount.color = ok ? ColOk : ColBad;
            card.bg.color = ok ? ColCardOk : ColCardNo;
        }

        string why = null;
        bool can = false;
        if (_manager != null) can = _manager.CanBuy(_region, out why);
        else why = "Chưa sẵn sàng.";
        if (can && !enoughMat) why = "Thiếu nguyên liệu — gom thêm từ tàu lửa rồi quay lại.";

        bool allow = can && enoughMat;
        _txtWarn.text = allow ? "" : (why ?? "");
        _btnBuy.interactable = allow;
        var img = _btnBuy.GetComponent<Image>();
        if (img != null) img.color = allow ? ColBtnOn : ColBtnOff;
    }

    private void OnEnable()  { InvokeRepeating(nameof(Refresh), 0.4f, 0.6f); }
    private void OnDisable() { CancelInvoke(nameof(Refresh)); }

    // ─────────────────────────────────────────────────────────────────────
    private void OnBuy()
    {
        if (_region == null || _manager == null) return;
        if (!BuildMaterials.HasAll(_region.materialCosts)) { Refresh(); return; }
        if (!_manager.CanBuy(_region, out _)) { Refresh(); return; }
        if (!BuildMaterials.TrySpend(_region.materialCosts)) { Refresh(); return; }

        if (!_manager.TryBuyAndStartClearing(_region))
        {
            var inv = FarmInventoryManager.Instance;
            if (inv != null)
                foreach (var c in _region.materialCosts)
                    if (c.IsValid) inv.AddItem(c.itemId, c.amount);
            Refresh();
            return;
        }
        Hide();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────────────────────────────
    private static Image NewImage(Transform parent, string name, Color c, Sprite s = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c; img.sprite = s;
        return img;
    }

    private TMP_Text NewText(Transform parent, string name, string content, float size,
                             FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = content;
        t.fontSize = size; t.fontStyle = style; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
