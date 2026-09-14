using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// QUẦY CÁ ở FARM (dưới Canvas_FishingPopup). Component nằm trên "FishCounterPopup" luôn active; con "Root" bật/tắt.
    /// Tab BÁN CÁ: giỏ cá → vàng (RewardFlyFX.GoiYDiemXuatPhat rồi AddGold, không gọi Fly thêm) + EXP = vàng/expPerGoldDivisor.
    /// Tab CẦN CÂU: 4 card RodData, mua qua FishingGearState.TryBuy (tiền đã tự phát tiếng ở FarmEconomyManager).
    /// KHÔNG đụng ShopManager. FarmInputLock Register/Unregister cân (_inputLockHeld). AnyOpen cho PopupManager.
    /// Thoát: X · chạm nền mờ · Escape/Back qua FishingPopupStack (chỉ khi ở đỉnh). Root bị tắt từ ngoài → Update tự dọn cờ/lock (MarkClosed).
    /// </summary>
    public class FishCounterPopupUI : MonoBehaviour
    {
        public static FishCounterPopupUI Instance { get; private set; }
        public static bool AnyOpen
        {
            get
            {
                if (Instance == null)
                    Instance = FindFirstObjectByType<FishCounterPopupUI>(FindObjectsInactive.Include);
                return Instance != null && Instance.IsOpen;
            }
        }

        private static readonly Vector2 FrameSize = new Vector2(1160f, 780f);
        private const float RowHeight = 108f;
        private const float RodRowHeight = 150f;
        private const float ToastSeconds = 2.2f;

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private RectTransform root;
        [SerializeField] private Button btnDim;
        [SerializeField] private RectTransform frame;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnTabSell;
        [SerializeField] private Button btnTabRods;
        [SerializeField] private RectTransform pageSell;
        [SerializeField] private RectTransform pageRods;
        [SerializeField] private TextMeshProUGUI txtBasketInfo;
        [SerializeField] private RectTransform sellContent;
        [SerializeField] private Button btnSellAll;
        [SerializeField] private TextMeshProUGUI txtEquipped;
        [SerializeField] private RectTransform rodContent;
        [SerializeField] private RectTransform toastRoot;
        [SerializeField] private TextMeshProUGUI txtToast;

        private bool _inputLockHeld;
        private bool _wired;
        private Coroutine _toastRoutine;
        private readonly List<RectTransform> _sellRows = new List<RectTransform>();
        private readonly List<RectTransform> _rodRows = new List<RectTransform>();

        public bool IsOpen { get { return root != null && root.gameObject.activeInHierarchy; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning(FishingIds.LogTag + " FishCounterPopupUI trùng tại '" + name + "' — tự ẩn, không huỷ."); gameObject.SetActive(false); return; }
            Instance = this;
            BuildIfEmpty();
            Wire();
            if (root != null) { root.gameObject.SetActive(false); }
        }

        private void OnEnable()
        {
            FishBasket.Instance.OnChanged += RefreshSell;
            FishingGearState.Instance.OnChanged += RefreshRods;
        }

        private void OnDisable()
        {
            FishBasket.Instance.OnChanged -= RefreshSell;
            FishingGearState.Instance.OnChanged -= RefreshRods;
            MarkClosed();
        }

        private void OnDestroy() { FishingPopupStack.Remove(this); if (Instance == this) { Instance = null; } }

        private void Update()
        {
            if (!IsOpen)
            {
                // Root bị tắt trực tiếp từ ngoài (không qua ClosePopup) → dọn lock như đã đóng.
                if (_inputLockHeld) { MarkClosed(); }
                return;
            }
            // Escape / Back Android: chỉ popup ở đỉnh FishingPopupStack xử lý, 1 lần mỗi frame.
            if (FishingPopupStack.ConsumeEscape(this)) { ClosePopup(); }
        }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            if (btnDim != null) { btnDim.onClick.AddListener(ClosePopup); }
            if (btnClose != null) { btnClose.onClick.AddListener(ClosePopup); }
            if (btnTabSell != null) { btnTabSell.onClick.AddListener(() => ShowTab(0)); }
            if (btnTabRods != null) { btnTabRods.onClick.AddListener(() => ShowTab(1)); }
            if (btnSellAll != null) { btnSellAll.onClick.AddListener(SellAll); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MỞ / ĐÓNG
        // ─────────────────────────────────────────────────────────────────────

        public static void Open()
        {
            FishCounterPopupUI inst = Instance;
            if (inst == null) { inst = FindFirstObjectByType<FishCounterPopupUI>(FindObjectsInactive.Include); Instance = inst; }
            if (inst == null) { Debug.Log(FishingIds.LogTag + " Chưa có FishCounterPopupUI trong scene farm — chạy Tools/Farm Game/Hồ Câu/5. Gắn vào SCN_Farm."); return; }
            inst.OpenInternal();
        }

        private void OpenInternal()
        {
            if (!FishingDatabase.IsEnabled) { Toast(Loc.T("Quầy Cá chưa mở")); return; }
            if (IsOpen) { return; }
            BuildIfEmpty();
            Wire();
            FishingUiKit.ActivateUpToCanvas(root);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            AcquireLock();
            FishingPopupStack.Push(this);
            ShowTab(FishingGearState.Instance.HasUsableRod || FishBasket.Instance.TotalCount > 0 ? 0 : 1);
            if (frame != null) { JuicyPulseFX.Play(frame, 1.06f, 0.22f); }
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        public void ClosePopup()
        {
            if (!IsOpen) { return; }
            root.gameObject.SetActive(false);
            MarkClosed();
        }

        /// <summary>Dọn MỌI trạng thái "đang mở": cờ AnyOpen (PopupManager), FarmInputLock, stack Escape. An toàn gọi nhiều lần.</summary>
        private void MarkClosed()
        {
            ReleaseLock();
            FishingPopupStack.Remove(this);
        }

        private void AcquireLock()
        {
            if (root != null) { FarmInputLock.SetPopupRaycastBlock(root.gameObject, true); }
            if (!_inputLockHeld) { FarmInputLock.RegisterPopupOpen(); _inputLockHeld = true; }
        }

        private void ReleaseLock()
        {
            if (root != null) { FarmInputLock.SetPopupRaycastBlock(root.gameObject, false); }
            if (_inputLockHeld) { FarmInputLock.RegisterPopupClose(); _inputLockHeld = false; }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DỰNG (idempotent, không huỷ con)
        // ─────────────────────────────────────────────────────────────────────

        public void BuildIfEmpty()
        {
            var selfRt = transform as RectTransform;
            if (selfRt != null && selfRt.anchorMin == selfRt.anchorMax) { FishingUiKit.Stretch(selfRt); }
            bool created;

            RectTransform r = FishingUiKit.Child(transform, "Root", Vector2.zero, Vector2.zero, out created);
            if (created) { FishingUiKit.Stretch(r); }
            if (root == null) { root = r; }

            Button dim = FishingUiKit.DimBackground(root, "Img_Dim");
            if (btnDim == null) { btnDim = dim; }

            RectTransform fr = FishingUiKit.Child(root, "Frame", FrameSize);
            if (frame == null) { frame = fr; }
            FishingUiKit.Panel(frame, "Img_Frame", FrameSize, UIStandardSprites.FrameWood, Vector2.zero, FishingUiKit.FrameFallback);
            FishingUiKit.Stretch(FishingUiKit.FindChild(frame, "Img_Frame"));
            FishingUiKit.Panel(frame, "Img_Paper", FrameSize - new Vector2(40f, 40f), UIStandardSprites.PanelPaper);
            FishingUiKit.Stretch(FishingUiKit.FindChild(frame, "Img_Paper"), 20f);

            float top = FrameSize.y * 0.5f;
            Image ribbon = FishingUiKit.Panel(frame, "Img_Ribbon", new Vector2(440f, 96f), UIStandardSprites.Ribbon, new Vector2(0f, top - 6f), (Color)new Color32(220, 150, 50, 255));
            ribbon.raycastTarget = false;
            FishingUiKit.Label(ribbon.transform, "Txt_Title", Loc.T("QUẦY CÁ"), 42f, TextAlignmentOptions.Center, new Vector2(400f, 70f), new Vector2(0f, 4f), FishingUiKit.TextLight, true);

            Button close = FishingUiKit.CloseButton(frame, null, new Vector2(4f, 4f));
            if (btnClose == null) { btnClose = close; }

            // Hai tab
            Button tSell = FishingUiKit.Button(frame, "Tab_Sell", Loc.T("BÁN CÁ"), new Vector2(300f, 92f), UIStandardSprites.BtnYellow3D, null, new Vector2(-170f, top - 120f), 30f);
            if (btnTabSell == null) { btnTabSell = tSell; }
            Button tRods = FishingUiKit.Button(frame, "Tab_Rods", Loc.T("CẦN CÂU"), new Vector2(300f, 92f), UIStandardSprites.BtnYellow3D, null, new Vector2(170f, top - 120f), 30f);
            if (btnTabRods == null) { btnTabRods = tRods; }

            // Trang BÁN CÁ
            RectTransform ps = FishingUiKit.Child(frame, "Page_Sell", FrameSize - new Vector2(60f, 220f), new Vector2(0f, -70f));
            if (pageSell == null) { pageSell = ps; }
            TextMeshProUGUI info = FishingUiKit.Label(pageSell, "Txt_BasketInfo", string.Empty, 28f, TextAlignmentOptions.Center, new Vector2(900f, 40f), new Vector2(0f, 250f), FishingUiKit.TextMuted, true);
            if (txtBasketInfo == null) { txtBasketInfo = info; }
            RectTransform c;
            FishingUiKit.ScrollList(pageSell, "SellList", new Vector2(960f, 400f), out c, new Vector2(0f, 10f));
            if (sellContent == null) { sellContent = c; }
            Button sellAll = FishingUiKit.Button(pageSell, "Btn_SellAll", Loc.T("Bán tất cả"), new Vector2(320f, 96f), UIStandardSprites.BtnGreen3D, null, new Vector2(0f, -250f), 32f);
            if (btnSellAll == null) { btnSellAll = sellAll; }

            // Trang CẦN CÂU
            RectTransform pr = FishingUiKit.Child(frame, "Page_Rods", FrameSize - new Vector2(60f, 220f), new Vector2(0f, -70f));
            if (pageRods == null) { pageRods = pr; }
            TextMeshProUGUI eq = FishingUiKit.Label(pageRods, "Txt_Equipped", string.Empty, 28f, TextAlignmentOptions.Center, new Vector2(900f, 40f), new Vector2(0f, 250f), FishingUiKit.TextMuted, true);
            if (txtEquipped == null) { txtEquipped = eq; }
            FishingUiKit.ScrollList(pageRods, "RodList", new Vector2(960f, 500f), out c, new Vector2(0f, -40f));
            if (rodContent == null) { rodContent = c; }

            // Toast ngoài Root
            RectTransform toast = FishingUiKit.Child(transform, "Toast_Msg", new Vector2(760f, 76f), new Vector2(0f, 260f), out created);
            if (toastRoot == null) { toastRoot = toast; }
            Image tbg = FishingUiKit.Panel(toast, "Img_Bg", new Vector2(760f, 76f), UIStandardSprites.RowDark, Vector2.zero, new Color(0f, 0f, 0f, 0.65f));
            tbg.raycastTarget = false;
            FishingUiKit.Stretch(tbg.rectTransform);
            TextMeshProUGUI tt = FishingUiKit.Label(toast, "Txt", string.Empty, 28f, TextAlignmentOptions.Center, new Vector2(720f, 60f), Vector2.zero, FishingUiKit.TextLight, true);
            if (txtToast == null) { txtToast = tt; }
            if (created) { toast.gameObject.SetActive(false); }

            pageRods.gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TAB
        // ─────────────────────────────────────────────────────────────────────

        private void ShowTab(int index)
        {
            bool sell = index == 0;
            if (pageSell != null) { pageSell.gameObject.SetActive(sell); }
            if (pageRods != null) { pageRods.gameObject.SetActive(!sell); }
            // Tab đang chọn sáng, tab kia mờ (không tô màu đục lên art).
            Image a = btnTabSell != null ? btnTabSell.targetGraphic as Image : null;
            Image b = btnTabRods != null ? btnTabRods.targetGraphic as Image : null;
            if (a != null) { a.color = sell ? Color.white : FishingUiKit.DisabledTint; }
            if (b != null) { b.color = sell ? FishingUiKit.DisabledTint : Color.white; }
            if (sell) { RefreshSell(); } else { RefreshRods(); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BÁN CÁ
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshSell()
        {
            if (!IsOpen || sellContent == null) { return; }
            FishBasket basket = FishBasket.Instance;
            FishingDatabase db = FishingDatabase.Instance;
            IReadOnlyList<FishStack> items = basket.Items;
            int count = 0;
            int total = 0;
            for (int i = 0; i < items.Count; i++)
            {
                FishStack st = items[i];
                if (st == null || st.amount <= 0) { continue; }
                FishData fish = FishingCatchResolver.FindFishLoose(db, st.fishId);
                int unit = fish != null ? fish.sellPrice : 0;
                total += unit * st.amount;
                RectTransform row = EnsureRow(_sellRows, sellContent, count, RowHeight, BuildSellRow);
                BindSellRow(row, st, fish, unit);
                count++;
            }
            for (int i = count; i < _sellRows.Count; i++) { if (_sellRows[i] != null) { _sellRows[i].gameObject.SetActive(false); } }
            if (txtBasketInfo != null)
            {
                txtBasketInfo.text = count == 0
                    ? Loc.T("Giỏ cá trống — ra hồ câu rồi quay lại")
                    : Loc.TF("Giỏ {0}/{1} loại · Tổng {2} vàng", FishingUiKit.Num(basket.UsedSlots), FishingUiKit.Num(basket.SlotCapacity), FishingUiKit.Num(total));
            }
            FishingUiKit.SetEnabled(btnSellAll, total > 0);
        }

        private void BuildSellRow(RectTransform row)
        {
            Image icon = FishingUiKit.Icon(row, "Img_Icon", null, new Vector2(80f, 80f));
            FishingUiKit.AnchorLeft(icon.rectTransform, 16f);
            TextMeshProUGUI nm = FishingUiKit.Label(row, "Txt_Name", string.Empty, 30f, TextAlignmentOptions.Left, new Vector2(340f, 44f), Vector2.zero, FishingUiKit.TextDark, true);
            FishingUiKit.AnchorLeft(nm.rectTransform, 112f, 14f);
            TextMeshProUGUI price = FishingUiKit.Label(row, "Txt_Price", string.Empty, 26f, TextAlignmentOptions.Left, new Vector2(340f, 36f), Vector2.zero, FishingUiKit.TextMuted, false);
            FishingUiKit.AnchorLeft(price.rectTransform, 112f, -22f);
            TextMeshProUGUI qty = FishingUiKit.Label(row, "Txt_Qty", string.Empty, 30f, TextAlignmentOptions.Right, new Vector2(120f, 44f), Vector2.zero, FishingUiKit.TextDark, true);
            FishingUiKit.AnchorRight(qty.rectTransform, 230f);
            Button sell = FishingUiKit.Button(row, "Btn_Sell", Loc.T("Bán"), new Vector2(190f, 92f), UIStandardSprites.BtnGreen3D, null, Vector2.zero, 28f);
            FishingUiKit.AnchorRight((RectTransform)sell.transform, 14f);
        }

        private void BindSellRow(RectTransform row, FishStack st, FishData fish, int unit)
        {
            RectTransform t = FishingUiKit.FindChild(row, "Img_Icon");
            if (t != null) { FishingUiKit.SetIcon(t.GetComponent<Image>(), fish != null ? fish.icon : null, fish != null ? FishingUiKit.RarityColor(fish.rarity) : Color.gray); }
            t = FishingUiKit.FindChild(row, "Txt_Name");
            if (t != null) { t.GetComponent<TextMeshProUGUI>().text = fish != null ? fish.displayName : st.fishId; }
            t = FishingUiKit.FindChild(row, "Txt_Price");
            if (t != null) { t.GetComponent<TextMeshProUGUI>().text = Loc.TF("{0} vàng/con · {1} vàng", FishingUiKit.Num(unit), FishingUiKit.Num(unit * st.amount)); }
            t = FishingUiKit.FindChild(row, "Txt_Qty");
            if (t != null) { t.GetComponent<TextMeshProUGUI>().text = "x" + FishingUiKit.Num(st.amount); }
            t = FishingUiKit.FindChild(row, "Btn_Sell");
            if (t != null)
            {
                Button b = t.GetComponent<Button>();
                string id = st.fishId;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => SellOne(id, b));
                FishingUiKit.SetEnabled(b, unit > 0);
            }
        }

        private void SellOne(string fishId, Component origin)
        {
            FishBasket basket = FishBasket.Instance;
            int amount = basket.Count(fishId);
            if (amount <= 0) { return; }
            FishData fish = FishingCatchResolver.FindFishLoose(FishingDatabase.Instance, fishId);
            int gold = (fish != null ? fish.sellPrice : 0) * amount;
            if (gold <= 0) { Toast(Loc.T("Loài này chưa có giá bán")); return; }
            if (!basket.Remove(fishId, amount)) { return; }
            Payout(gold, origin);
        }

        private void SellAll()
        {
            FishBasket basket = FishBasket.Instance;
            int gold = FishingCatchResolver.SellValue(basket.Items, FishingDatabase.Instance);
            if (gold <= 0) { Toast(Loc.T("Không có gì để bán")); return; }
            basket.ClearAll();
            Payout(gold, btnSellAll);
        }

        /// <summary>Trả tiền: cộng vàng (FX bay tự bắn theo event) + EXP theo giá trị, không nhân đôi.</summary>
        private void Payout(int gold, Component origin)
        {
            if (FarmEconomyManager.Instance == null) { Debug.LogError(FishingIds.LogTag + " FarmEconomyManager null — không trả được vàng bán cá."); return; }
            RewardFlyFX.GoiYDiemXuatPhat(FishingUiKit.ScreenPointOf(origin));
            FarmEconomyManager.Instance.AddGold(gold);
            int exp = FishingCatchResolver.ExpForSale(gold, FishingDatabase.ConfigOrDefault.expPerGoldDivisor);
            if (exp > 0 && PlayerProgressManager.Instance != null) { PlayerProgressManager.Instance.AddExp(exp); }
            Toast(Loc.TF("+{0} vàng · +{1} EXP", FishingUiKit.Num(gold), FishingUiKit.Num(exp)));
            RefreshSell();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CẦN CÂU
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshRods()
        {
            if (!IsOpen || rodContent == null) { return; }
            FishingDatabase db = FishingDatabase.Instance;
            FishingGearState gear = FishingGearState.Instance;
            int level = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            int count = 0;
            if (db != null)
            {
                for (int i = 0; i < db.rods.Count; i++)
                {
                    RodData rod = db.rods[i];
                    if (rod == null) { continue; }
                    RectTransform row = EnsureRow(_rodRows, rodContent, count, RodRowHeight, BuildRodRow);
                    BindRodRow(row, rod, gear, level);
                    count++;
                }
            }
            for (int i = count; i < _rodRows.Count; i++) { if (_rodRows[i] != null) { _rodRows[i].gameObject.SetActive(false); } }
            if (txtEquipped != null)
            {
                txtEquipped.text = gear.EquippedRod != null
                    ? Loc.TF("Đang cầm: {0} · độ bền {1}/{2}", gear.EquippedRod.itemName, FishingUiKit.Num(gear.EquippedDurabilityLeft), FishingUiKit.Num(gear.EquippedRod.durabilityCasts))
                    : Loc.T("Chưa có cần câu — mua một cây để ra hồ");
            }
        }

        private void BuildRodRow(RectTransform row)
        {
            Image icon = FishingUiKit.Icon(row, "Img_Icon", null, new Vector2(110f, 110f));
            FishingUiKit.AnchorLeft(icon.rectTransform, 16f);
            TextMeshProUGUI nm = FishingUiKit.Label(row, "Txt_Name", string.Empty, 32f, TextAlignmentOptions.Left, new Vector2(420f, 44f), Vector2.zero, FishingUiKit.TextDark, true);
            FishingUiKit.AnchorLeft(nm.rectTransform, 142f, 40f);
            TextMeshProUGUI stat = FishingUiKit.Label(row, "Txt_Stat", string.Empty, 24f, TextAlignmentOptions.Left, new Vector2(460f, 70f), Vector2.zero, FishingUiKit.TextMuted, false);
            FishingUiKit.AnchorLeft(stat.rectTransform, 142f, -22f);
            Image coin = FishingUiKit.Icon(row, "Img_Coin", UIStandardSprites.IconGold, new Vector2(44f, 44f));
            FishingUiKit.AnchorRight(coin.rectTransform, 350f, 34f);
            TextMeshProUGUI price = FishingUiKit.Label(row, "Txt_Cost", string.Empty, 30f, TextAlignmentOptions.Right, new Vector2(150f, 44f), Vector2.zero, FishingUiKit.TextDark, true);
            FishingUiKit.AnchorRight(price.rectTransform, 400f, 34f);
            TextMeshProUGUI lock_ = FishingUiKit.Label(row, "Txt_Lock", string.Empty, 24f, TextAlignmentOptions.Right, new Vector2(230f, 36f), Vector2.zero, FishingUiKit.TextMuted, false);
            FishingUiKit.AnchorRight(lock_.rectTransform, 350f, -30f);
            Button buy = FishingUiKit.Button(row, "Btn_Buy", Loc.T("Mua"), new Vector2(200f, 96f), UIStandardSprites.BtnGreen3D, null, Vector2.zero, 30f);
            FishingUiKit.AnchorRight((RectTransform)buy.transform, 14f);
        }

        private void BindRodRow(RectTransform row, RodData rod, FishingGearState gear, int level)
        {
            RectTransform t = FishingUiKit.FindChild(row, "Img_Icon");
            if (t != null) { FishingUiKit.SetIcon(t.GetComponent<Image>(), rod.itemIcon, new Color(0.6f, 0.45f, 0.3f, 1f)); }
            t = FishingUiKit.FindChild(row, "Txt_Name");
            if (t != null) { t.GetComponent<TextMeshProUGUI>().text = rod.itemName + "  " + FishingUiKit.Stars(rod.tier); }
            t = FishingUiKit.FindChild(row, "Txt_Stat");
            if (t != null)
            {
                int pct = Mathf.RoundToInt(rod.catchChanceBonus * 100f);
                t.GetComponent<TextMeshProUGUI>().text = Loc.TF("Độ bền {0} lần quăng · +{1}% bắt · cắn nhanh x{2}", FishingUiKit.Num(rod.durabilityCasts), FishingUiKit.Num(pct), (1f / Mathf.Max(0.2f, rod.biteWaitMultiplier)).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            }
            t = FishingUiKit.FindChild(row, "Img_Coin");
            if (t != null) { FishingUiKit.SetIcon(t.GetComponent<Image>(), rod.IsGemRod ? UIStandardSprites.IconGem : UIStandardSprites.IconGold, rod.IsGemRod ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.85f, 0.2f)); }
            t = FishingUiKit.FindChild(row, "Txt_Cost");
            if (t != null) { t.GetComponent<TextMeshProUGUI>().text = FishingUiKit.Num(rod.IsGemRod ? rod.diamondPrice : rod.goldPrice); }
            bool locked = level < rod.unlockLevel;
            t = FishingUiKit.FindChild(row, "Txt_Lock");
            if (t != null)
            {
                bool owned = gear.EquippedRod != null && gear.EquippedRod == rod;
                t.GetComponent<TextMeshProUGUI>().text = locked ? Loc.TF("Mở ở cấp {0}", FishingUiKit.Num(rod.unlockLevel)) : (owned ? Loc.T("Đang cầm") : string.Empty);
            }
            t = FishingUiKit.FindChild(row, "Btn_Buy");
            if (t != null)
            {
                Button b = t.GetComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => Buy(rod));
                FishingUiKit.SetEnabled(b, !locked);
            }
        }

        private void Buy(RodData rod)
        {
            string reason;
            if (!FishingGearState.Instance.TryBuy(rod, out reason)) { Toast(reason); return; }
            Toast(Loc.TF("Đã mua {0}", rod.itemName));
            if (frame != null) { JuicyPulseFX.Play(frame, 1.03f, 0.18f); }
            RefreshRods();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HÀNG (pool theo index, ẩn hàng dư)
        // ─────────────────────────────────────────────────────────────────────

        private static RectTransform EnsureRow(List<RectTransform> pool, RectTransform content, int index, float height, System.Action<RectTransform> build)
        {
            while (pool.Count <= index) { pool.Add(null); }
            RectTransform row = pool[index];
            if (row == null)
            {
                row = FishingUiKit.Row(content, "Row_" + index, height, UIStandardSprites.CardInner);
                build(row);
                pool[index] = row;
            }
            row.gameObject.SetActive(true);
            row.SetSiblingIndex(index);
            return row;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TOAST
        // ─────────────────────────────────────────────────────────────────────

        private void Toast(string vi)
        {
            if (toastRoot == null || txtToast == null) { Debug.Log(FishingIds.LogTag + " " + vi); return; }
            FishingUiKit.ActivateUpToCanvas(toastRoot);
            txtToast.text = vi ?? string.Empty;
            toastRoot.gameObject.SetActive(true);
            toastRoot.SetAsLastSibling();
            if (_toastRoutine != null) { StopCoroutine(_toastRoutine); }
            if (gameObject.activeInHierarchy) { _toastRoutine = StartCoroutine(HideToast()); }
        }

        private IEnumerator HideToast()
        {
            yield return new WaitForSecondsRealtime(ToastSeconds);
            _toastRoutine = null;
            if (toastRoot != null) { toastRoot.gameObject.SetActive(false); }
        }
    }
}
