using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Popup thông tin người chơi khác (con "Popup_PlayerInfo" của Canvas_FishingHUD, mở khi chạm RemotePlayerView):
    /// avatar, tên, Lv, trạng thái · "Kết bạn"/"Huỷ bạn" · hàng "Kết nối": Bạn bè (xanh) · Chị em (vàng) · Hẹn hò (đỏ) chỉ bật khi đã là bạn
    /// · "Tặng cá" (chọn loại từ giỏ, số lượng 1) · "Tặng gem" (bậc 1/5/10 kẹp cfg.giftGemMin..Max) · "Mời vào phòng". Đóng bằng X hoặc chạm nền mờ.
    /// Escape/Back qua FishingPopupStack (Push OnEnable / Remove OnDisable): sub-panel tặng đang mở → đóng sub-panel trước, không thì đóng popup.
    /// </summary>
    public class PlayerInfoPopupUI : MonoBehaviour
    {
        public static PlayerInfoPopupUI Instance { get; private set; }

        private static readonly Vector2 FrameSize = new Vector2(760f, 880f);
        private static readonly int[] GemTiers = { 1, 5, 10 };

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private Button btnDim;
        [SerializeField] private RectTransform frame;
        [SerializeField] private Button btnClose;
        [SerializeField] private Image imgAvatarFrame;
        [SerializeField] private Image imgAvatar;
        [SerializeField] private TextMeshProUGUI txtName;
        [SerializeField] private TextMeshProUGUI txtLevel;
        [SerializeField] private TextMeshProUGUI txtStatus;
        [SerializeField] private Button btnFriend;
        [SerializeField] private Button btnRelFriend;
        [SerializeField] private Button btnRelSibling;
        [SerializeField] private Button btnRelDating;
        [SerializeField] private Button btnGiftFish;
        [SerializeField] private Button btnGiftGem;
        [SerializeField] private Button btnInvite;
        [SerializeField] private RectTransform subGiftFish;
        [SerializeField] private RectTransform subGiftFishContent;
        [SerializeField] private RectTransform subGiftGem;

        private RemotePlayerView _target;
        private bool _wired;
        private readonly List<RectTransform> _fishRows = new List<RectTransform>();
        private readonly List<Button> _gemButtons = new List<Button>();

        public bool IsOpen { get { return gameObject.activeSelf; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning(FishingIds.LogTag + " PlayerInfoPopupUI trùng tại '" + name + "' — tự ẩn."); gameObject.SetActive(false); return; }
            Instance = this;
            BuildIfEmpty();
            Wire();
        }

        private void OnDestroy() { FishingPopupStack.Remove(this); if (Instance == this) { Instance = null; } }

        private void OnEnable()
        {
            IFriendService fs = FishingNetHub.Friends;
            if (fs != null) { fs.OnFriendsChanged -= Refresh; fs.OnFriendsChanged += Refresh; }
            FishingPopupStack.Push(this);
        }

        private void OnDisable()
        {
            IFriendService fs = FishingNetHub.Friends;
            if (fs != null) { fs.OnFriendsChanged -= Refresh; }
            // Bị tắt từ ngoài (SetActive(false) trực tiếp) cũng dọn như Close(): bỏ mục tiêu, rời stack Escape, ẩn sub-panel.
            _target = null;
            HideSubPanels();
            FishingPopupStack.Remove(this);
        }

        private void Update()
        {
            // Escape / Back Android: chỉ khi ở đỉnh FishingPopupStack, 1 lần mỗi frame. Sub-panel tặng đang mở → đóng nó trước.
            if (!FishingPopupStack.ConsumeEscape(this)) { return; }
            bool subOpen = (subGiftFish != null && subGiftFish.gameObject.activeSelf) || (subGiftGem != null && subGiftGem.gameObject.activeSelf);
            if (subOpen) { HideSubPanels(); return; }
            Close();
        }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            if (btnDim != null) { btnDim.onClick.AddListener(Close); }
            if (btnClose != null) { btnClose.onClick.AddListener(Close); }
            if (btnFriend != null) { btnFriend.onClick.AddListener(OnFriendClick); }
            if (btnRelFriend != null) { btnRelFriend.onClick.AddListener(() => SetRelation(RelationshipKind.Friend)); }
            if (btnRelSibling != null) { btnRelSibling.onClick.AddListener(() => SetRelation(RelationshipKind.Sibling)); }
            if (btnRelDating != null) { btnRelDating.onClick.AddListener(() => SetRelation(RelationshipKind.Dating)); }
            if (btnGiftFish != null) { btnGiftFish.onClick.AddListener(ToggleGiftFish); }
            if (btnGiftGem != null) { btnGiftGem.onClick.AddListener(ToggleGiftGem); }
            if (btnInvite != null) { btnInvite.onClick.AddListener(OnInviteClick); }
            for (int i = 0; i < _gemButtons.Count; i++)
            {
                int amount = GemTiers[Mathf.Min(i, GemTiers.Length - 1)];
                if (_gemButtons[i] != null) { _gemButtons[i].onClick.AddListener(() => GiftGems(amount)); }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MỞ / ĐÓNG (static theo hợp đồng)
        // ─────────────────────────────────────────────────────────────────────

        public static void Open(RemotePlayerView target)
        {
            if (target == null) { return; }
            PlayerInfoPopupUI inst = Resolve();
            if (inst == null) { return; }
            inst._target = target;
            FishingUiKit.ActivateUpToCanvas(inst.transform);
            inst.gameObject.SetActive(true);
            inst.transform.SetAsLastSibling();
            FishingPopupStack.Push(inst);   // đang mở sẵn (chạm người khác) thì OnEnable không chạy → đưa lên đỉnh lại
            inst.HideSubPanels();
            inst.Refresh();
            if (inst.frame != null) { JuicyPulseFX.Play(inst.frame, 1.08f, 0.22f); }
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        public static void Close()
        {
            PlayerInfoPopupUI inst = Instance;
            if (inst == null) { return; }
            inst._target = null;
            inst.gameObject.SetActive(false);
        }

        private static PlayerInfoPopupUI Resolve()
        {
            if (Instance != null) { return Instance; }
            var found = FindFirstObjectByType<PlayerInfoPopupUI>(FindObjectsInactive.Include);
            if (found != null) { Instance = found; return found; }
            FishingHudUI hud = FishingHudUI.Instance;
            if (hud == null) { hud = FindFirstObjectByType<FishingHudUI>(FindObjectsInactive.Include); }
            if (hud == null) { Debug.Log(FishingIds.LogTag + " Không có Canvas_FishingHUD để mở PlayerInfoPopupUI."); return null; }
            RectTransform rt = FishingUiKit.Child(hud.transform, "Popup_PlayerInfo", Vector2.zero);
            FishingUiKit.Stretch(rt);
            var inst = FishingUiKit.GetOrAdd<PlayerInfoPopupUI>(rt.gameObject);
            Instance = inst;
            inst.BuildIfEmpty();
            inst.Wire();
            return inst;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DỰNG
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Dựng con nếu thiếu; không huỷ con có sẵn.</summary>
        public void BuildIfEmpty()
        {
            var rt = transform as RectTransform;
            if (rt != null && rt.anchorMin == rt.anchorMax) { FishingUiKit.Stretch(rt); }

            Button dim = FishingUiKit.DimBackground(transform, "Img_Dim");
            if (btnDim == null) { btnDim = dim; }

            RectTransform fr = FishingUiKit.Child(transform, "Frame", FrameSize);
            if (frame == null) { frame = fr; }
            FishingUiKit.Panel(frame, "Img_Frame", FrameSize, UIStandardSprites.FrameWood, Vector2.zero, FishingUiKit.FrameFallback);
            FishingUiKit.Stretch(FishingUiKit.FindChild(frame, "Img_Frame"), 0f);
            FishingUiKit.Panel(frame, "Img_Paper", FrameSize - new Vector2(40f, 40f), UIStandardSprites.PanelPaper);
            FishingUiKit.Stretch(FishingUiKit.FindChild(frame, "Img_Paper"), 20f);

            Button close = FishingUiKit.CloseButton(frame, null, new Vector2(4f, 4f));
            if (btnClose == null) { btnClose = close; }

            float top = FrameSize.y * 0.5f;
            Image avFrame = FishingUiKit.Icon(frame, "Img_AvatarFrame", UIStandardSprites.AvatarBase, new Vector2(150f, 150f), new Vector2(-230f, top - 130f), (Color)new Color32(120, 90, 60, 255));
            if (avFrame.sprite == null) { avFrame.sprite = FishingUiKit.Circle(); }
            if (imgAvatarFrame == null) { imgAvatarFrame = avFrame; }
            Image av = FishingUiKit.Icon(avFrame.transform, "Img_Avatar", null, new Vector2(122f, 122f), Vector2.zero, new Color(0f, 0f, 0f, 0f));
            if (imgAvatar == null) { imgAvatar = av; }

            TextMeshProUGUI nm = FishingUiKit.Label(frame, "Txt_Name", string.Empty, 40f, TextAlignmentOptions.Left, new Vector2(440f, 56f), new Vector2(90f, top - 95f), FishingUiKit.TextDark, true);
            if (txtName == null) { txtName = nm; }
            TextMeshProUGUI lv = FishingUiKit.Label(frame, "Txt_Level", string.Empty, 28f, TextAlignmentOptions.Left, new Vector2(440f, 40f), new Vector2(90f, top - 145f), FishingUiKit.TextMuted);
            if (txtLevel == null) { txtLevel = lv; }
            TextMeshProUGUI st = FishingUiKit.Label(frame, "Txt_Status", string.Empty, 26f, TextAlignmentOptions.Left, new Vector2(440f, 36f), new Vector2(90f, top - 185f), FishingUiKit.TextMuted);
            if (txtStatus == null) { txtStatus = st; }

            Button friend = FishingUiKit.Button(frame, "Btn_Friend", Loc.T("Kết bạn"), new Vector2(320f, 96f), UIStandardSprites.BtnGreen3D, null, new Vector2(0f, top - 290f), 32f);
            if (btnFriend == null) { btnFriend = friend; }

            FishingUiKit.Label(frame, "Txt_RelationTitle", Loc.T("Kết nối"), 28f, TextAlignmentOptions.Center, new Vector2(400f, 40f), new Vector2(0f, top - 370f), FishingUiKit.TextMuted, true);
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            float relY = top - 440f;
            Button rf = RelButton("Btn_RelFriend", Loc.T("Bạn bè"), new Vector2(-220f, relY), cfg.LineColorFor(RelationshipKind.Friend));
            if (btnRelFriend == null) { btnRelFriend = rf; }
            Button rs = RelButton("Btn_RelSibling", Loc.T("Chị em"), new Vector2(0f, relY), cfg.LineColorFor(RelationshipKind.Sibling));
            if (btnRelSibling == null) { btnRelSibling = rs; }
            Button rd = RelButton("Btn_RelDating", Loc.T("Hẹn hò"), new Vector2(220f, relY), cfg.LineColorFor(RelationshipKind.Dating));
            if (btnRelDating == null) { btnRelDating = rd; }

            float giftY = top - 570f;
            Button gf = FishingUiKit.Button(frame, "Btn_GiftFish", Loc.T("Tặng cá"), new Vector2(300f, 96f), UIStandardSprites.BtnYellow3D, null, new Vector2(-165f, giftY), 30f, null, (Color)new Color32(230, 180, 60, 255));
            if (btnGiftFish == null) { btnGiftFish = gf; }
            Button gg = FishingUiKit.Button(frame, "Btn_GiftGem", Loc.T("Tặng gem"), new Vector2(300f, 96f), UIStandardSprites.BtnGem, null, new Vector2(165f, giftY), 30f, null, (Color)new Color32(70, 140, 230, 255));
            if (btnGiftGem == null) { btnGiftGem = gg; }
            FishingUiKit.Icon(gg.transform, "Img_Gem", UIStandardSprites.IconGem, new Vector2(40f, 40f), new Vector2(-110f, 0f), new Color(0.5f, 0.8f, 1f, 1f));

            Button inv = FishingUiKit.Button(frame, "Btn_Invite", Loc.T("Mời vào phòng"), new Vector2(400f, 96f), UIStandardSprites.BtnGreen3D, null, new Vector2(0f, giftY - 120f), 30f);
            if (btnInvite == null) { btnInvite = inv; }

            // Sub-panel tặng cá: danh sách nhỏ chồng lên giữa frame.
            RectTransform sf = FishingUiKit.Child(frame, "Sub_GiftFish", new Vector2(560f, 520f), new Vector2(0f, -40f));
            if (subGiftFish == null) { subGiftFish = sf; }
            FishingUiKit.Panel(subGiftFish, "Img_Bg", new Vector2(560f, 520f), UIStandardSprites.CardOuter, Vector2.zero, FishingUiKit.PanelFallback);
            FishingUiKit.Stretch(FishingUiKit.FindChild(subGiftFish, "Img_Bg"));
            FishingUiKit.Label(subGiftFish, "Txt_Title", Loc.T("Chọn cá để tặng (1 con)"), 28f, TextAlignmentOptions.Center, new Vector2(500f, 40f), new Vector2(0f, 225f), FishingUiKit.TextDark, true);
            RectTransform c;
            FishingUiKit.ScrollList(subGiftFish, "List", new Vector2(520f, 380f), out c, new Vector2(0f, -10f));
            if (subGiftFishContent == null) { subGiftFishContent = c; }
            FishingUiKit.Label(subGiftFish, "Txt_Empty", Loc.T("Giỏ trống"), 26f, TextAlignmentOptions.Center, new Vector2(400f, 40f), new Vector2(0f, -10f), FishingUiKit.TextMuted);
            Button sfClose = FishingUiKit.CloseButton(subGiftFish, null, new Vector2(10f, 10f));
            sfClose.onClick.RemoveAllListeners(); sfClose.onClick.AddListener(HideSubPanels);

            // Sub-panel tặng gem: 3 bậc.
            RectTransform sg = FishingUiKit.Child(frame, "Sub_GiftGem", new Vector2(560f, 240f), new Vector2(0f, -40f));
            if (subGiftGem == null) { subGiftGem = sg; }
            FishingUiKit.Panel(subGiftGem, "Img_Bg", new Vector2(560f, 240f), UIStandardSprites.CardOuter, Vector2.zero, FishingUiKit.PanelFallback);
            FishingUiKit.Stretch(FishingUiKit.FindChild(subGiftGem, "Img_Bg"));
            FishingUiKit.Label(subGiftGem, "Txt_Title", Loc.T("Tặng bao nhiêu gem?"), 28f, TextAlignmentOptions.Center, new Vector2(500f, 40f), new Vector2(0f, 85f), FishingUiKit.TextDark, true);
            _gemButtons.Clear();
            for (int i = 0; i < GemTiers.Length; i++)
            {
                int amount = Mathf.Clamp(GemTiers[i], cfg.giftGemMin, cfg.giftGemMax);
                Button gb = FishingUiKit.Button(subGiftGem, "Btn_Gem_" + i, FishingUiKit.Num(amount), new Vector2(150f, 96f), UIStandardSprites.BtnGem, null, new Vector2(-170f + i * 170f, -20f), 34f, null, (Color)new Color32(70, 140, 230, 255));
                FishingUiKit.Icon(gb.transform, "Img_Gem", UIStandardSprites.IconGem, new Vector2(34f, 34f), new Vector2(-48f, 0f), new Color(0.5f, 0.8f, 1f, 1f));
                _gemButtons.Add(gb);
            }
            Button sgClose = FishingUiKit.CloseButton(subGiftGem, null, new Vector2(10f, 10f));
            sgClose.onClick.RemoveAllListeners(); sgClose.onClick.AddListener(HideSubPanels);

            subGiftFish.gameObject.SetActive(false);
            subGiftGem.gameObject.SetActive(false);
        }

        /// <summary>Nút kết nối: sprite bo góc vẽ code tint theo màu cfg (không tô đục lên art bake màu) + badge CheckBadge khi đang chọn.</summary>
        private Button RelButton(string name, string text, Vector2 pos, Color tint)
        {
            Button b = FishingUiKit.Button(frame, name, text, new Vector2(200f, 92f), FishingUiKit.Rounded(18f), null, pos, 28f, null, tint);
            Image img = b.targetGraphic as Image;
            if (img != null && img.sprite != null && img.sprite == FishingUiKit.Rounded(18f)) { img.color = tint; }
            Image badge = FishingUiKit.Icon(b.transform, "Img_Sel", UIStandardSprites.CheckBadge, new Vector2(40f, 40f), new Vector2(85f, 34f), FishingUiKit.OnlineGreen);
            badge.gameObject.SetActive(false);
            return b;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HIỂN THỊ
        // ─────────────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (!IsOpen) { return; }
            if (_target == null) { Close(); return; }
            PlayerNetState s = _target.State;
            IFriendService fs = FishingNetHub.Friends;
            string id = _target.PlayerId;
            bool isFriend = fs != null && fs.IsFriend(id);
            RelationshipKind rel = fs != null ? fs.GetRelationship(id) : RelationshipKind.None;

            if (txtName != null) { txtName.text = s != null && !string.IsNullOrEmpty(s.displayName) ? s.displayName : id; }
            if (txtLevel != null) { txtLevel.text = Loc.TF("Cấp {0}", FishingUiKit.Num(s != null ? Mathf.Max(1, s.level) : 1)); }
            if (txtStatus != null) { txtStatus.text = StatusText(s); }
            if (imgAvatar != null)
            {
                Sprite spr = FishingUiKit.AvatarSprite(s != null ? s.avatarIndex : 0);
                imgAvatar.gameObject.SetActive(spr != null);
                if (spr != null) { FishingUiKit.SetIcon(imgAvatar, spr, Color.white); }
            }
            if (btnFriend != null) { FishingUiKit.SetButtonText(btnFriend, isFriend ? Loc.T("Huỷ bạn") : Loc.T("Kết bạn")); }

            SetRelButton(btnRelFriend, isFriend, rel == RelationshipKind.Friend);
            SetRelButton(btnRelSibling, isFriend, rel == RelationshipKind.Sibling);
            SetRelButton(btnRelDating, isFriend, rel == RelationshipKind.Dating);

            if (btnInvite != null)
            {
                IRoomService room = FishingNetHub.Room;
                bool canInvite = room != null && !string.IsNullOrEmpty(room.CurrentRoomId) && isFriend;
                FishingUiKit.SetEnabled(btnInvite, canInvite);
            }
            if (subGiftFish != null && subGiftFish.gameObject.activeSelf) { RefreshGiftFishList(); }
        }

        private static void SetRelButton(Button b, bool enabled, bool selected)
        {
            if (b == null) { return; }
            b.interactable = enabled;
            Image img = b.targetGraphic as Image;
            if (img != null) { Color c = img.color; c.a = enabled ? 1f : 0.4f; img.color = c; }
            RectTransform badge = FishingUiKit.FindChild(b.transform, "Img_Sel");
            if (badge != null) { badge.gameObject.SetActive(selected); }
        }

        private static string StatusText(PlayerNetState s)
        {
            if (s == null) { return string.Empty; }
            if (s.isBot) { return Loc.T("Đang chơi (bot thử)"); }
            switch ((FishingPhase)s.phase)
            {
                case FishingPhase.Casting:
                case FishingPhase.Waiting:
                case FishingPhase.Bite:
                case FishingPhase.Reeling: return Loc.T("Đang câu cá");
                default: return s.moving ? Loc.T("Đang đi dạo") : Loc.T("Đang đứng chơi");
            }
        }

        private void HideSubPanels()
        {
            if (subGiftFish != null) { subGiftFish.gameObject.SetActive(false); }
            if (subGiftGem != null) { subGiftGem.gameObject.SetActive(false); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HÀNH ĐỘNG
        // ─────────────────────────────────────────────────────────────────────

        private void OnFriendClick()
        {
            IFriendService fs = FishingNetHub.Friends;
            if (fs == null || _target == null) { return; }
            string id = _target.PlayerId;
            if (fs.IsFriend(id)) { fs.RemoveFriend(id); Toast(Loc.T("Đã huỷ kết bạn")); }
            else
            {
                PlayerNetState s = _target.State;
                if (s == null) { return; }
                fs.SendFriendRequest(s);
                Toast(Loc.TF("Đã gửi lời mời kết bạn tới {0}", s.displayName));
                if (AudioManager.Instance != null) { AudioManager.Instance.PlaySuccess(); }
            }
            Refresh();
        }

        private void SetRelation(RelationshipKind kind)
        {
            IFriendService fs = FishingNetHub.Friends;
            if (fs == null || _target == null) { return; }
            string id = _target.PlayerId;
            RelationshipKind current = fs.GetRelationship(id);
            RelationshipKind next = current == kind ? RelationshipKind.Friend : kind; // bấm lại = về Bạn bè thường
            bool ok = fs.SetRelationship(id, next);
            if (!ok) { Toast(Loc.T("Phải là bạn trước đã")); }
            else { Toast(Loc.TF("Kết nối: {0}", FriendCardUI.RelationText(next))); if (AudioManager.Instance != null) { AudioManager.Instance.PlayGemSparkle(); } }
            Refresh();
        }

        private void ToggleGiftFish()
        {
            if (subGiftFish == null) { return; }
            bool show = !subGiftFish.gameObject.activeSelf;
            HideSubPanels();
            subGiftFish.gameObject.SetActive(show);
            if (show) { subGiftFish.SetAsLastSibling(); RefreshGiftFishList(); }
        }

        private void ToggleGiftGem()
        {
            if (subGiftGem == null) { return; }
            bool show = !subGiftGem.gameObject.activeSelf;
            HideSubPanels();
            subGiftGem.gameObject.SetActive(show);
            if (show)
            {
                subGiftGem.SetAsLastSibling();
                int have = FarmEconomyManager.Instance != null ? FarmEconomyManager.Instance.Gems : 0;
                FishingConfig cfg = FishingDatabase.ConfigOrDefault;
                for (int i = 0; i < _gemButtons.Count; i++)
                {
                    int amount = Mathf.Clamp(GemTiers[Mathf.Min(i, GemTiers.Length - 1)], cfg.giftGemMin, cfg.giftGemMax);
                    FishingUiKit.SetButtonText(_gemButtons[i], FishingUiKit.Num(amount));
                    FishingUiKit.SetEnabled(_gemButtons[i], have >= amount);
                }
            }
        }

        private void RefreshGiftFishList()
        {
            if (subGiftFishContent == null) { return; }
            FishBasket basket = FishBasket.Instance;
            FishingDatabase db = FishingDatabase.Instance;
            int count = 0;
            if (basket != null)
            {
                IReadOnlyList<FishStack> items = basket.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    FishStack st = items[i];
                    if (st == null || st.amount <= 0) { continue; }
                    RectTransform row = EnsureFishRow(count);
                    FishData fish = FishingCatchResolver.FindFishLoose(db, st.fishId);
                    RectTransform iconRt = FishingUiKit.FindChild(row, "Img_Icon");
                    if (iconRt != null) { FishingUiKit.SetIcon(iconRt.GetComponent<Image>(), fish != null ? fish.icon : null, FishingUiKit.RarityColor(fish != null ? fish.rarity : FishRarity.Common)); }
                    RectTransform nmRt = FishingUiKit.FindChild(row, "Txt_Name");
                    if (nmRt != null) { nmRt.GetComponent<TextMeshProUGUI>().text = (fish != null ? fish.displayName : st.fishId) + "  x" + FishingUiKit.Num(st.amount); }
                    RectTransform btnRt = FishingUiKit.FindChild(row, "Btn_Give");
                    if (btnRt != null)
                    {
                        Button give = btnRt.GetComponent<Button>();
                        string fishId = st.fishId;
                        give.onClick.RemoveAllListeners();
                        give.onClick.AddListener(() => GiftFish(fishId));
                    }
                    count++;
                }
            }
            for (int i = count; i < _fishRows.Count; i++) { if (_fishRows[i] != null) { _fishRows[i].gameObject.SetActive(false); } }
            RectTransform empty = FishingUiKit.FindChild(subGiftFish, "Txt_Empty");
            if (empty != null) { empty.gameObject.SetActive(count == 0); }
        }

        private RectTransform EnsureFishRow(int index)
        {
            while (_fishRows.Count <= index) { _fishRows.Add(null); }
            RectTransform row = _fishRows[index];
            if (row == null)
            {
                row = FishingUiKit.Row(subGiftFishContent, "Row_" + index, 96f, UIStandardSprites.CardInner);
                Image icon = FishingUiKit.Icon(row, "Img_Icon", null, new Vector2(64f, 64f));
                FishingUiKit.AnchorLeft(icon.rectTransform, 12f);
                TextMeshProUGUI nm = FishingUiKit.Label(row, "Txt_Name", string.Empty, 26f, TextAlignmentOptions.Left, new Vector2(260f, 40f), Vector2.zero, FishingUiKit.TextDark, true);
                FishingUiKit.AnchorLeft(nm.rectTransform, 90f);
                Button give = FishingUiKit.Button(row, "Btn_Give", Loc.T("Tặng 1"), new Vector2(130f, 92f), UIStandardSprites.BtnGreen3D, null, Vector2.zero, 26f);
                FishingUiKit.AnchorRight((RectTransform)give.transform, 10f);
                _fishRows[index] = row;
            }
            row.gameObject.SetActive(true);
            row.SetSiblingIndex(index);
            return row;
        }

        private void GiftFish(string fishId)
        {
            IFriendService fs = FishingNetHub.Friends;
            if (fs == null || _target == null) { return; }
            string err;
            bool ok = fs.SendGift(_target.PlayerId, GiftKind.Fish, fishId, 1, out err);
            if (!ok) { Toast(string.IsNullOrEmpty(err) ? Loc.T("Không tặng được") : err); return; }
            Toast(Loc.T("Đã tặng cá!"));
            if (AudioManager.Instance != null) { AudioManager.Instance.PlaySuccess(); }
            RefreshGiftFishList();
        }

        private void GiftGems(int amount)
        {
            IFriendService fs = FishingNetHub.Friends;
            if (fs == null || _target == null) { return; }
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            amount = Mathf.Clamp(amount, cfg.giftGemMin, cfg.giftGemMax);
            string err;
            bool ok = fs.SendGift(_target.PlayerId, GiftKind.Gems, string.Empty, amount, out err);
            if (!ok) { Toast(string.IsNullOrEmpty(err) ? Loc.T("Không tặng được") : err); return; }
            Toast(Loc.TF("Đã tặng {0} gem!", FishingUiKit.Num(amount)));
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayGemSparkle(); }
            HideSubPanels();
        }

        private void OnInviteClick()
        {
            IFriendService fs = FishingNetHub.Friends;
            IRoomService room = FishingNetHub.Room;
            if (fs == null || room == null || _target == null) { return; }
            if (string.IsNullOrEmpty(room.CurrentRoomId)) { Toast(Loc.T("Bạn chưa ở trong phòng nào")); return; }
            bool ok = fs.InviteToRoom(_target.PlayerId, room.CurrentRoomId);
            Toast(ok ? Loc.T("Đã gửi lời mời") : Loc.T("Không mời được lúc này"));
        }

        private static void Toast(string vi)
        {
            if (FishingHudUI.Instance != null) { FishingHudUI.Instance.ShowToast(vi); }
        }
    }
}
