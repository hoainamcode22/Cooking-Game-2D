using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// HINT GÓC MÀN HÌNH khi quầy hàng bán được / hết hạn (Vòng 16) — dùng chung cho MỌI mặt hàng của quầy, không riêng cá.
    /// Nghe PlayerStallManager.OnListingSold / OnListingExpired → xếp hàng đợi, mỗi hint trượt vào từ mép PHẢI-DƯỚI,
    /// giữ ~2.7 s rồi trượt ra. 1 dòng, cao 56 px, nền UIStandardSprites.RowDark, icon hàng 40 px + icon vàng.
    /// Nội dung: "{TênNPC} đã mua {sl} {TênHàng} của bạn" + [icon vàng] "+{vàng}"; hết hạn: "{TênHàng} ×{sl} hết hạn, đã trả về kho".
    /// Tên NPC bốc ổn định theo listingId từ OrderNameBank.CustomerIds (bank chỉ có MÃ khách, bảng danh xưng ở đây).
    /// GÓC PHẢI-DƯỚI vì: WarehouseGainToastUI neo TRÊN-GIỮA (0.5,1)+(150,-130); FishingInviteHintUI neo PHẢI-TRÊN (-24,-140);
    /// HUD farm chiếm trái-trên (hồ sơ/nhiệm vụ/edit), phải-trên (tiền), trái-dưới (tab nav). Phải-dưới chỉ có nút edit mobile
    /// ở (-40,220) → hint đặt dưới nó ở y=150.
    /// KHÔNG dùng coroutine: object "StallSaleHint" luôn active nhưng con "Root" bật/tắt; mọi chuyển động chạy trong Update (unscaled).
    /// [DefaultExecutionOrder(100)]: OnEnable chạy SAU Awake của PlayerStallManager (Instance đã có) và TRƯỚC Start của nó
    /// (nhịp bắt kịp offline bắn OnListingSold) → không bỏ lỡ hint lúc vào game.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class StallSaleHintUI : MonoBehaviour
    {
        private struct HintItem
        {
            public string text;
            public Sprite icon;
            public int gold;      // <= 0 → ẩn icon vàng + số
        }

        private enum Phase { Idle, SlideIn, Hold, SlideOut }

        private const string RootName = "Root";
        private const int MaxQueued = 8;
        private static readonly Vector2 HintSize = new Vector2(640f, 56f);
        private const float IconSize = 40f;
        private const float GoldIconSize = 28f;

        [Header("Vị trí (anchor phải-dưới) — Sếp kéo trong Inspector")]
        [SerializeField] private Vector2 shownPos = new Vector2(-24f, 150f);

        [Header("Nhịp (giây, unscaled)")]
        [SerializeField] private float slideInSeconds = 0.25f;
        [SerializeField] private float holdSeconds = 2.7f;
        [SerializeField] private float slideOutSeconds = 0.2f;

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private RectTransform root;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtMessage;
        [SerializeField] private Image imgGold;
        [SerializeField] private TextMeshProUGUI txtGold;

        private readonly Queue<HintItem> _queue = new Queue<HintItem>();
        private PlayerStallManager _stall;
        private float _retrySubscribeAt;
        private Phase _phase = Phase.Idle;
        private float _phaseStart;
        private CanvasGroup _cg;

        /// <summary>Danh xưng khách theo mã OrderNameBank.CustomerIds (heo, cun, meo, tho, gau, cuu, bo, vit, ga, soc, nai, chuot).</summary>
        private static readonly Dictionary<string, string> CustomerTitleVi = new Dictionary<string, string>
        {
            { "heo", "Bác Heo" }, { "cun", "Cậu Cún" }, { "meo", "Chị Mèo" }, { "tho", "Cô Thỏ" },
            { "gau", "Bác Gấu" }, { "cuu", "Cô Cừu" }, { "bo", "Bác Bò" }, { "vit", "Cô Vịt" },
            { "ga", "Chị Gà" }, { "soc", "Bé Sóc" }, { "nai", "Anh Nai" }, { "chuot", "Bé Chuột" },
        };

        private void Awake()
        {
            BuildIfEmpty();
            if (root != null) { root.gameObject.SetActive(false); }
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // ── Đăng ký sự kiện ─────────────────────────────────────────────────

        private void TrySubscribe()
        {
            PlayerStallManager cur = PlayerStallManager.Instance;
            if (cur == null) { _retrySubscribeAt = Time.unscaledTime + 0.5f; return; }
            if (ReferenceEquals(cur, _stall)) { return; }
            Unsubscribe();
            _stall = cur;
            _stall.OnListingSold += OnSold;
            _stall.OnListingExpired += OnExpired;
        }

        private void Unsubscribe()
        {
            if (_stall == null) { return; }
            _stall.OnListingSold -= OnSold;
            _stall.OnListingExpired -= OnExpired;
            _stall = null;
        }

        private void OnSold(PlayerListing l, int gold)
        {
            if (l == null) { return; }
            string ten = ItemName(l.itemId);
            string npc = CustomerName(l.listingId);
            Enqueue(new HintItem
            {
                text = Loc.TF("{0} đã mua {1} {2} của bạn", npc, FishingUiKit.Num(Mathf.Max(1, l.quantity)), ten),
                icon = ItemIcon(l.itemId),
                gold = gold,
            });
        }

        private void OnExpired(PlayerListing l)
        {
            if (l == null) { return; }
            Enqueue(new HintItem
            {
                // [Reviewer L8] refundPending = giỏ/kho đầy, quầy CHƯA hoàn được (B8: sẽ thử lại) → không nói dối "đã trả về kho".
                text = l.refundPending
                    ? Loc.TF("{0} ×{1} hết hạn, sẽ hoàn về kho khi có chỗ", ItemName(l.itemId), FishingUiKit.Num(Mathf.Max(1, l.quantity)))
                    : Loc.TF("{0} ×{1} hết hạn, đã trả về kho", ItemName(l.itemId), FishingUiKit.Num(Mathf.Max(1, l.quantity))),
                icon = ItemIcon(l.itemId),
                gold = 0,
            });
        }

        private void Enqueue(HintItem item)
        {
            if (root == null) { return; }
            // Bắt kịp offline có thể bắn hàng chục sự kiện một lúc — giữ 8 cái đầu, còn lại bỏ (đã có FX vàng + lịch sử quầy).
            if (_queue.Count >= MaxQueued) { return; }
            _queue.Enqueue(item);
        }

        // ── Tra tên / icon ──────────────────────────────────────────────────

        private static string ItemName(string itemId)
        {
            StallItemCatalog catalog = StallItemCatalog.Instance;
            string ten = catalog != null ? catalog.GetDisplayName(itemId) : null;
            if (string.IsNullOrEmpty(ten) || ten == itemId) { ten = MarketPriceTable.GetDisplayName(itemId); }
            return string.IsNullOrEmpty(ten) ? itemId : ten;
        }

        private static Sprite ItemIcon(string itemId)
        {
            StallItemCatalog catalog = StallItemCatalog.Instance;
            return catalog != null ? catalog.GetIcon(itemId) : null;
        }

        /// <summary>Cùng listingId luôn ra cùng khách (hash FNV-1a ổn định, không dùng string.GetHashCode).</summary>
        private static string CustomerName(string listingId)
        {
            string[] ids = OrderNameBank.CustomerIds;
            if (ids == null || ids.Length == 0) { return Loc.T("Khách"); }
            uint h = 2166136261u;
            string s = listingId ?? string.Empty;
            for (int i = 0; i < s.Length; i++) { h = (h ^ s[i]) * 16777619u; }
            string ma = ids[(int)(h % (uint)ids.Length)];
            string title;
            if (CustomerTitleVi.TryGetValue(ma, out title)) { return Loc.T(title); }
            return Loc.T("Khách") + " " + ma;
        }

        // ── Chuyển động (Update, không coroutine) ────────────────────────────

        private void Update()
        {
            if (_stall == null && Time.unscaledTime >= _retrySubscribeAt) { TrySubscribe(); }
            if (root == null) { return; }

            float now = Time.unscaledTime;
            switch (_phase)
            {
                case Phase.Idle:
                    if (_queue.Count > 0) { BeginShow(_queue.Dequeue(), now); }
                    break;

                case Phase.SlideIn:
                {
                    float k = Ease(now, slideInSeconds);
                    root.anchoredPosition = Vector2.LerpUnclamped(HiddenPos(), shownPos, k);
                    if (_cg != null) { _cg.alpha = k; }
                    if (k >= 1f) { _phase = Phase.Hold; _phaseStart = now; }
                    break;
                }

                case Phase.Hold:
                    // Còn hint chờ thì rút ngắn thời gian giữ để hàng đợi không dồn quá lâu.
                    float hold = _queue.Count > 0 ? Mathf.Min(holdSeconds, 1.6f) : holdSeconds;
                    if (now - _phaseStart >= hold) { _phase = Phase.SlideOut; _phaseStart = now; }
                    break;

                case Phase.SlideOut:
                {
                    float k = Ease(now, slideOutSeconds);
                    root.anchoredPosition = Vector2.LerpUnclamped(shownPos, HiddenPos(), k);
                    if (_cg != null) { _cg.alpha = 1f - k; }
                    if (k >= 1f) { root.gameObject.SetActive(false); _phase = Phase.Idle; }
                    break;
                }
            }
        }

        private float Ease(float now, float duration)
        {
            if (duration <= 0.0001f) { return 1f; }
            float t = Mathf.Clamp01((now - _phaseStart) / duration);
            return 1f - (1f - t) * (1f - t);   // ease-out
        }

        private Vector2 HiddenPos()
        {
            return shownPos + new Vector2(HintSize.x + 40f, 0f);   // trượt ra ngoài mép phải
        }

        private void BeginShow(HintItem item, float now)
        {
            if (txtMessage != null) { txtMessage.text = item.text ?? string.Empty; }
            if (imgIcon != null)
            {
                FishingUiKit.SetIcon(imgIcon, item.icon, new Color(1f, 1f, 1f, 0.25f));
                imgIcon.enabled = item.icon != null;
            }
            bool coVang = item.gold > 0;
            if (imgGold != null) { imgGold.enabled = coVang && imgGold.sprite != null; }
            if (txtGold != null)
            {
                txtGold.gameObject.SetActive(coVang);
                txtGold.text = coVang ? "+" + item.gold.ToString(CultureInfo.InvariantCulture) : string.Empty;
            }

            root.anchoredPosition = HiddenPos();
            if (_cg != null) { _cg.alpha = 0f; }
            FishingUiKit.ActivateUpToCanvas(root);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            _phase = Phase.SlideIn;
            _phaseStart = now;
        }

        // ── Dựng (tool + runtime, idempotent) ───────────────────────────────

        /// <summary>Dựng con nếu thiếu; không huỷ con có sẵn; chỉ gán field trống.</summary>
        public void BuildIfEmpty()
        {
            var selfRt = transform as RectTransform;
            if (selfRt != null && selfRt.anchorMin == selfRt.anchorMax) { FishingUiKit.Stretch(selfRt); }

            bool created;
            RectTransform r = FishingUiKit.Child(transform, RootName, HintSize, Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(r, new Vector2(1f, 0f), shownPos); r.sizeDelta = HintSize; }
            if (root == null) { root = r; }

            _cg = FishingUiKit.GetOrAdd<CanvasGroup>(root.gameObject);
            _cg.blocksRaycasts = false;
            _cg.interactable = false;

            // Nền: hàng lõm tối chuẩn (RowDark), không tự vẽ khung mới.
            Image bg = FishingUiKit.Panel(root, "Img_Bg", HintSize, UIStandardSprites.RowDark, Vector2.zero, (Color)new Color32(60, 50, 40, 230));
            FishingUiKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            Image icon = FishingUiKit.Icon(root, "Img_Icon", null, new Vector2(IconSize, IconSize));
            FishingUiKit.AnchorLeft(icon.rectTransform, 10f);
            if (imgIcon == null) { imgIcon = icon; }

            // Vàng ở mép phải: số trước (neo phải, 90 px), icon vàng bên trái số.
            TextMeshProUGUI gold = FishingUiKit.Label(root, "Txt_Gold", string.Empty, 24f, TextAlignmentOptions.Right, new Vector2(90f, 40f), Vector2.zero, (Color)new Color32(255, 214, 90, 255), true);
            FishingUiKit.AnchorRight(gold.rectTransform, 12f);
            gold.textWrappingMode = TextWrappingModes.NoWrap;
            gold.overflowMode = TextOverflowModes.Overflow;
            if (txtGold == null) { txtGold = gold; }

            Image goldIcon = FishingUiKit.Icon(root, "Img_Gold", UIStandardSprites.IconGold, new Vector2(GoldIconSize, GoldIconSize), Vector2.zero, new Color(1f, 0.84f, 0.3f, 1f));
            FishingUiKit.AnchorRight(goldIcon.rectTransform, 12f + 90f + 4f);
            if (imgGold == null) { imgGold = goldIcon; }

            // Chữ: từ sau icon (10 + 40 + 10) tới trước icon vàng (12 + 90 + 4 + 28 + 8).
            float left = 10f + IconSize + 10f;
            float right = 12f + 90f + 4f + GoldIconSize + 8f;
            TextMeshProUGUI msg = FishingUiKit.Label(root, "Txt_Msg", string.Empty, 24f, TextAlignmentOptions.Left, new Vector2(HintSize.x - left - right, 44f), Vector2.zero, FishingUiKit.TextLight, false);
            FishingUiKit.AnchorLeft(msg.rectTransform, left);
            msg.textWrappingMode = TextWrappingModes.NoWrap;
            msg.overflowMode = TextOverflowModes.Ellipsis;
            if (txtMessage == null) { txtMessage = msg; }
        }
    }
}
