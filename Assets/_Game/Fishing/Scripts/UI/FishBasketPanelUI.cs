using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Panel giỏ cá trong scene câu (con "Panel_Basket" của Canvas_FishingHUD). Danh sách cuộn FishBasket.Instance.Items:
    /// icon, tên, số lượng, giá/đơn vị. Header "Giỏ cá x/y loại", nút đóng, ghi chú "Bán ở Quầy Cá tại farm". Refresh theo OnChanged.
    /// Mở/đóng do FishingHudUI (SetActive), loại trừ với panel Bạn bè / Chat.
    /// </summary>
    public class FishBasketPanelUI : MonoBehaviour
    {
        public static readonly Vector2 PanelSize = new Vector2(560f, 820f);
        private const float RowHeight = 100f;

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private Image imgFrame;
        [SerializeField] private Image imgPaper;
        [SerializeField] private TextMeshProUGUI txtHeader;
        [SerializeField] private Button btnClose;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [SerializeField] private TextMeshProUGUI txtEmpty;
        [SerializeField] private TextMeshProUGUI txtNote;

        private readonly List<RectTransform> _rows = new List<RectTransform>();
        private bool _wired;

        private void Awake()
        {
            BuildIfEmpty();
            Wire();
        }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            if (btnClose != null) { btnClose.onClick.AddListener(Close); }
        }

        private void OnEnable()
        {
            BuildIfEmpty();
            Wire();
            FishBasket b = FishBasket.Instance;
            if (b != null) { b.OnChanged -= Refresh; b.OnChanged += Refresh; }
            Refresh();
        }

        private void OnDisable()
        {
            FishBasket b = FishBasket.Instance;
            if (b != null) { b.OnChanged -= Refresh; }
        }

        public void Close()
        {
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
            gameObject.SetActive(false);
        }

        /// <summary>Dựng con nếu thiếu; không huỷ con có sẵn; chỉ gán field trống.</summary>
        public void BuildIfEmpty()
        {
            var rt = transform as RectTransform;
            if (rt != null && rt.sizeDelta == Vector2.zero) { rt.sizeDelta = PanelSize; }

            Image frame = FishingUiKit.Panel(transform, "Img_Frame", PanelSize, UIStandardSprites.FrameWood, Vector2.zero, FishingUiKit.FrameFallback);
            if (imgFrame == null) { imgFrame = frame; }
            FishingUiKit.Stretch(imgFrame.rectTransform, 0f);

            Image paper = FishingUiKit.Panel(transform, "Img_Paper", PanelSize - new Vector2(40f, 40f), UIStandardSprites.PanelPaper);
            if (imgPaper == null) { imgPaper = paper; }
            FishingUiKit.Stretch(imgPaper.rectTransform, 20f);

            TextMeshProUGUI header = FishingUiKit.Label(transform, "Txt_Header", Loc.T("Giỏ cá"), 36f, TextAlignmentOptions.Center, new Vector2(PanelSize.x - 160f, 60f), new Vector2(0f, PanelSize.y * 0.5f - 60f), FishingUiKit.TextDark, true);
            if (txtHeader == null) { txtHeader = header; }

            Button close = FishingUiKit.CloseButton(transform, null, new Vector2(4f, 4f));
            if (btnClose == null) { btnClose = close; }

            RectTransform c;
            ScrollRect sr = FishingUiKit.ScrollList(transform, "List", new Vector2(PanelSize.x - 60f, PanelSize.y - 230f), out c, new Vector2(0f, 10f));
            if (scroll == null) { scroll = sr; }
            if (content == null) { content = c; }

            TextMeshProUGUI empty = FishingUiKit.Label(transform, "Txt_Empty", Loc.T("Giỏ trống — ra mép nước quăng câu nhé!"), 28f, TextAlignmentOptions.Center, new Vector2(PanelSize.x - 100f, 80f), new Vector2(0f, 10f), FishingUiKit.TextMuted);
            if (txtEmpty == null) { txtEmpty = empty; }

            TextMeshProUGUI note = FishingUiKit.Label(transform, "Txt_Note", Loc.T("Bán ở Quầy Cá tại farm"), 24f, TextAlignmentOptions.Center, new Vector2(PanelSize.x - 100f, 40f), new Vector2(0f, -PanelSize.y * 0.5f + 50f), FishingUiKit.TextMuted);
            if (txtNote == null) { txtNote = note; }
        }

        /// <summary>Vẽ lại danh sách theo giỏ (tái dùng hàng theo tên Row_i, không Destroy).</summary>
        public void Refresh()
        {
            if (!gameObject.activeInHierarchy) { return; }
            FishBasket basket = FishBasket.Instance;
            FishingDatabase db = FishingDatabase.Instance;
            int count = 0;
            if (basket != null && content != null)
            {
                IReadOnlyList<FishStack> items = basket.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    FishStack st = items[i];
                    if (st == null || st.amount <= 0) { continue; }
                    RectTransform row = EnsureRow(count);
                    FishData fish = FishingCatchResolver.FindFishLoose(db, st.fishId);
                    BindRow(row, fish, st);
                    count++;
                }
                if (txtHeader != null) { txtHeader.text = Loc.TF("Giỏ cá {0}/{1} loại", basket.UsedSlots, basket.SlotCapacity); }
            }
            for (int i = count; i < _rows.Count; i++) { if (_rows[i] != null) { _rows[i].gameObject.SetActive(false); } }
            if (txtEmpty != null) { txtEmpty.gameObject.SetActive(count == 0); }
            if (scroll != null) { scroll.verticalNormalizedPosition = 1f; }
        }

        private RectTransform EnsureRow(int index)
        {
            while (_rows.Count <= index) { _rows.Add(null); }
            RectTransform row = _rows[index];
            if (row == null)
            {
                row = FishingUiKit.Row(content, "Row_" + index, RowHeight, UIStandardSprites.CardInner);
                Image icon = FishingUiKit.Icon(row, "Img_Icon", null, new Vector2(72f, 72f));
                FishingUiKit.AnchorLeft(icon.rectTransform, 14f);
                TextMeshProUGUI nm = FishingUiKit.Label(row, "Txt_Name", string.Empty, 28f, TextAlignmentOptions.Left, new Vector2(230f, 40f), Vector2.zero, FishingUiKit.TextDark, true);
                FishingUiKit.AnchorLeft(nm.rectTransform, 100f, 16f);
                TextMeshProUGUI price = FishingUiKit.Label(row, "Txt_Price", string.Empty, 22f, TextAlignmentOptions.Left, new Vector2(230f, 32f), Vector2.zero, FishingUiKit.TextMuted);
                FishingUiKit.AnchorLeft(price.rectTransform, 100f, -18f);
                Image gold = FishingUiKit.Icon(row, "Img_Gold", UIStandardSprites.IconGold, new Vector2(26f, 26f), Vector2.zero, (Color)new Color32(240, 200, 60, 255));
                FishingUiKit.AnchorLeft(gold.rectTransform, 100f + 90f, -18f);
                TextMeshProUGUI amt = FishingUiKit.Label(row, "Txt_Amount", string.Empty, 32f, TextAlignmentOptions.Right, new Vector2(120f, 44f), Vector2.zero, FishingUiKit.TextDark, true);
                FishingUiKit.AnchorRight(amt.rectTransform, 20f);
                _rows[index] = row;
            }
            row.gameObject.SetActive(true);
            row.SetSiblingIndex(index);
            return row;
        }

        private void BindRow(RectTransform row, FishData fish, FishStack st)
        {
            Image icon = GetImg(row, "Img_Icon");
            if (icon != null) { FishingUiKit.SetIcon(icon, fish != null ? fish.icon : null, FishingUiKit.RarityColor(fish != null ? fish.rarity : FishRarity.Common)); }
            TextMeshProUGUI nm = GetTxt(row, "Txt_Name");
            if (nm != null) { nm.text = fish != null ? fish.displayName : st.fishId; }
            TextMeshProUGUI price = GetTxt(row, "Txt_Price");
            Image gold = GetImg(row, "Img_Gold");
            int unit = fish != null ? fish.sellPrice : 0;
            if (price != null) { price.text = Loc.TF("{0} /con", FishingUiKit.Num(unit)); price.rectTransform.sizeDelta = new Vector2(80f, 32f); }
            if (gold != null) { gold.gameObject.SetActive(fish != null); }
            TextMeshProUGUI amt = GetTxt(row, "Txt_Amount");
            if (amt != null) { amt.text = "x" + FishingUiKit.Num(st.amount); }
        }

        private static Image GetImg(Transform row, string n) { RectTransform r = FishingUiKit.FindChild(row, n); return r != null ? r.GetComponent<Image>() : null; }
        private static TextMeshProUGUI GetTxt(Transform row, string n) { RectTransform r = FishingUiKit.FindChild(row, n); return r != null ? r.GetComponent<TextMeshProUGUI>() : null; }
    }
}
