using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// HUD scene câu, gắn trên Canvas_FishingHUD (tool tạo canvas Overlay 1920x1080 match 0.5; đây chỉ dựng con).
    /// Trái-trên: avatar + tên + Lv · trái-dưới: Joystick (VirtualJoystickUI) · phải-dưới: QUĂNG / THU · cạnh phải: cột tab
    /// Giỏ · Bạn bè · Chat · Riêng tư · Về farm · giữa-trên: vòng cửa sổ cắn + nhãn pha. Panel con mở loại trừ nhau.
    /// Nghe FishingController.Local (OnPhaseChanged/OnCatch/OnEscape/OnNothing/OnRodBroken/OnBlocked), kiểm CanCast mỗi 0.2 s.
    /// </summary>
    public class FishingHudUI : MonoBehaviour
    {
        public static FishingHudUI Instance { get; private set; }

        private const float BigButton = 150f;
        private const float TabSize = 96f;
        private const float TabGap = 14f;
        private const float PollInterval = 0.2f;
        private const float ToastSeconds = 2.2f;

        [Header("Góc trái-trên")]
        [SerializeField] private Image imgAvatarFrame;
        [SerializeField] private Image imgAvatar;
        [SerializeField] private TextMeshProUGUI txtPlayerName;
        [SerializeField] private TextMeshProUGUI txtPlayerLevel;

        [Header("Joystick (Dev A)")]
        [SerializeField] private VirtualJoystickUI joystick;

        [Header("Nút hành động")]
        [SerializeField] private Button btnCast;
        [SerializeField] private Button btnReel;
        [SerializeField] private TextMeshProUGUI txtBlockReason;

        [Header("Cột tab phải")]
        [SerializeField] private Button tabBasket;
        [SerializeField] private Button tabFriends;
        [SerializeField] private Button tabChat;
        [SerializeField] private Button tabPrivate;
        [SerializeField] private Image imgPrivateOn;
        [SerializeField] private Button btnHome;

        [Header("Giữa-trên")]
        [SerializeField] private Image imgBiteRing;
        [SerializeField] private TextMeshProUGUI txtPhase;

        [Header("Toast + panel con")]
        [SerializeField] private RectTransform toastRoot;
        [SerializeField] private TextMeshProUGUI txtToast;
        [SerializeField] private FishBasketPanelUI basketPanel;
        [SerializeField] private FriendsPanelUI friendsPanel;
        [SerializeField] private ChatPanelUI chatPanel;
        [SerializeField] private PlayerInfoPopupUI playerInfoPopup;
        [SerializeField] private FishingResultToastUI resultToast;

        private FishingController _ctrl;
        private RemotePlayersManager _remotes;
        private bool _wired;
        private bool _privateOn;
        private float _nextPoll;
        private Coroutine _toastRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        // ─────────────────────────────────────────────────────────────────────
        //  VÒNG ĐỜI
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning(FishingIds.LogTag + " FishingHudUI trùng tại '" + name + "' — tự ẩn, không huỷ."); gameObject.SetActive(false); return; }
            Instance = this;
            BuildIfEmpty();
            Wire();
        }

        private void OnDestroy() { if (Instance == this) { Instance = null; } }

        private void Start()
        {
            RefreshProfile();
            TrySubscribe();
            if (chatPanel != null) { chatPanel.EnsureListening(); }
            _privateOn = FishingNetHub.Friends != null && FishingNetHub.Friends.PrivateMode;
            RefreshPrivateVisual();
            RefreshButtons();
            Transform ringRoot = BiteRingRoot();
            if (ringRoot != null) { ringRoot.gameObject.SetActive(false); }
        }

        /// <summary>Gốc để ẩn/hiện vòng cắn: nền "Img_BiteRingBg" nếu vòng nằm trong đó, không thì chính vòng.</summary>
        private Transform BiteRingRoot()
        {
            if (imgBiteRing == null) { return null; }
            Transform p = imgBiteRing.transform.parent;
            return (p != null && p.name == "Img_BiteRingBg") ? p : imgBiteRing.transform;
        }

        private void OnEnable() { TrySubscribe(); }

        private void OnDisable() { Unsubscribe(); }

        private void Update()
        {
            if (_ctrl == null || _remotes == null) { if (Time.unscaledTime >= _nextPoll) { TrySubscribe(); } }
            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + PollInterval;
                RefreshButtons();
            }
            if (_ctrl != null && _ctrl.Phase == FishingPhase.Bite && imgBiteRing != null)
            {
                imgBiteRing.fillAmount = Mathf.Clamp01(_ctrl.BiteWindowRemaining01);
            }
        }

        private void TrySubscribe()
        {
            if (_ctrl == null && FishingController.Local != null)
            {
                _ctrl = FishingController.Local;
                _ctrl.OnPhaseChanged += OnPhaseChanged;
                _ctrl.OnCatch += OnCatch;
                _ctrl.OnEscape += OnEscape;
                _ctrl.OnNothing += OnNothing;
                _ctrl.OnRodBroken += OnRodBroken;
                _ctrl.OnBlocked += OnBlocked;
                OnPhaseChanged(_ctrl.Phase);
            }
            if (_remotes == null && RemotePlayersManager.Instance != null)
            {
                _remotes = RemotePlayersManager.Instance;
                _remotes.OnRemoteTapped += OnRemoteTapped;
            }
        }

        private void Unsubscribe()
        {
            if (_ctrl != null)
            {
                _ctrl.OnPhaseChanged -= OnPhaseChanged;
                _ctrl.OnCatch -= OnCatch;
                _ctrl.OnEscape -= OnEscape;
                _ctrl.OnNothing -= OnNothing;
                _ctrl.OnRodBroken -= OnRodBroken;
                _ctrl.OnBlocked -= OnBlocked;
                _ctrl = null;
            }
            if (_remotes != null) { _remotes.OnRemoteTapped -= OnRemoteTapped; _remotes = null; }
        }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            if (btnCast != null) { btnCast.onClick.AddListener(OnCastClick); }
            if (btnReel != null) { btnReel.onClick.AddListener(OnReelClick); }
            if (tabBasket != null) { tabBasket.onClick.AddListener(() => TogglePanel(basketPanel != null ? basketPanel.gameObject : null)); }
            if (tabFriends != null) { tabFriends.onClick.AddListener(() => TogglePanel(friendsPanel != null ? friendsPanel.gameObject : null)); }
            if (tabChat != null) { tabChat.onClick.AddListener(OnChatTab); }
            if (tabPrivate != null) { tabPrivate.onClick.AddListener(OnPrivateTab); }
            if (btnHome != null) { btnHome.onClick.AddListener(OnHomeClick); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DỰNG (tool lúc Editor + runtime fallback), find-or-create theo tên, không huỷ con
        // ─────────────────────────────────────────────────────────────────────

        public void BuildIfEmpty()
        {
            Transform root = transform;
            bool created;

            // ── Góc trái-trên: hồ sơ ──
            RectTransform profile = FishingUiKit.Child(root, "TopLeft_Profile", new Vector2(460f, 130f), Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(profile, new Vector2(0f, 1f), new Vector2(24f, -24f)); }
            Image avFrame = FishingUiKit.Icon(profile, "Img_AvatarFrame", UIStandardSprites.AvatarBase, new Vector2(116f, 116f), new Vector2(66f, -65f), (Color)new Color32(120, 90, 60, 255));
            if (avFrame.sprite == null) { avFrame.sprite = FishingUiKit.Circle(); }
            if (imgAvatarFrame == null) { imgAvatarFrame = avFrame; }
            Image av = FishingUiKit.Icon(avFrame.transform, "Img_Avatar", null, new Vector2(94f, 94f), Vector2.zero, new Color(0f, 0f, 0f, 0f));
            if (imgAvatar == null) { imgAvatar = av; }
            TextMeshProUGUI nm = FishingUiKit.Label(profile, "Txt_Name", string.Empty, 30f, TextAlignmentOptions.Left, new Vector2(320f, 40f), new Vector2(290f, -45f), FishingUiKit.TextLight, true);
            if (created) { FishingUiKit.AddShadow(nm, new Color(0f, 0f, 0f, 0.6f), new Vector2(1f, -2f)); }
            if (txtPlayerName == null) { txtPlayerName = nm; }
            TextMeshProUGUI lv = FishingUiKit.Label(profile, "Txt_Level", string.Empty, 26f, TextAlignmentOptions.Left, new Vector2(320f, 34f), new Vector2(290f, -85f), (Color)new Color32(255, 225, 120, 255), true);
            if (created) { FishingUiKit.AddShadow(lv, new Color(0f, 0f, 0f, 0.6f), new Vector2(1f, -2f)); }
            if (txtPlayerLevel == null) { txtPlayerLevel = lv; }

            // ── Trái-dưới: joystick (Dev A dựng nền + núm) ──
            RectTransform joy = FishingUiKit.Child(root, "Joystick", new Vector2(320f, 320f), Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(joy, new Vector2(0f, 0f), new Vector2(60f, 60f)); }
            VirtualJoystickUI vj = FishingUiKit.GetOrAdd<VirtualJoystickUI>(joy.gameObject);
            if (joystick == null) { joystick = vj; }
            if (vj != null) { vj.BuildIfEmpty(); }

            // ── Phải-dưới: QUĂNG / THU ──
            RectTransform actions = FishingUiKit.Child(root, "BottomRight_Actions", new Vector2(200f, 420f), Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(actions, new Vector2(1f, 0f), new Vector2(-150f, 60f)); }
            Button cast = FishingUiKit.Button(actions, "Btn_Cast", Loc.T("QUĂNG"), new Vector2(BigButton, BigButton), UIStandardSprites.BtnGreen3D, null, new Vector2(0f, 215f), 34f);
            if (btnCast == null) { btnCast = cast; }
            Button reel = FishingUiKit.Button(actions, "Btn_Reel", Loc.T("THU"), new Vector2(BigButton, BigButton), UIStandardSprites.BtnYellow3D, null, new Vector2(0f, 45f), 34f, null, (Color)new Color32(230, 180, 60, 255));
            if (btnReel == null) { btnReel = reel; }
            TextMeshProUGUI reason = FishingUiKit.Label(actions, "Txt_BlockReason", string.Empty, 20f, TextAlignmentOptions.Center, new Vector2(300f, 60f), new Vector2(0f, -60f), FishingUiKit.TextLight);
            if (created) { FishingUiKit.AddShadow(reason, new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, -1f)); }
            if (txtBlockReason == null) { txtBlockReason = reason; }

            // ── Cạnh phải giữa: cột tab ──
            RectTransform bar = FishingUiKit.Child(root, "RightBar_Tabs", new Vector2(TabSize + 20f, (TabSize + TabGap) * 5f), Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(bar, new Vector2(1f, 0.5f), new Vector2(-16f, 60f)); }
            float step = TabSize + TabGap;
            Button tb = Tab(bar, "Tab_Basket", Loc.T("Giỏ"), new Vector2(0f, step * 2f));
            if (tabBasket == null) { tabBasket = tb; }
            Button tf = Tab(bar, "Tab_Friends", Loc.T("Bạn bè"), new Vector2(0f, step * 1f));
            if (tabFriends == null) { tabFriends = tf; }
            Button tc = Tab(bar, "Tab_Chat", Loc.T("Chat"), new Vector2(0f, 0f));
            if (tabChat == null) { tabChat = tc; }
            Button tp = Tab(bar, "Tab_Private", Loc.T("Riêng tư"), new Vector2(0f, -step * 1f));
            if (tabPrivate == null) { tabPrivate = tp; }
            Image on = FishingUiKit.Icon(tp.transform, "Img_On", UIStandardSprites.CheckBadge, new Vector2(34f, 34f), new Vector2(TabSize * 0.5f - 12f, TabSize * 0.5f - 12f), FishingUiKit.OnlineGreen);
            if (imgPrivateOn == null) { imgPrivateOn = on; }
            Button home = Tab(bar, "Btn_Home", Loc.T("Về farm"), new Vector2(0f, -step * 2f));
            if (btnHome == null) { btnHome = home; }

            // ── Giữa-trên: vòng cửa sổ cắn + nhãn pha ──
            RectTransform biteRoot = FishingUiKit.Child(root, "TopCenter_Bite", new Vector2(700f, 160f), Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(biteRoot, new Vector2(0.5f, 1f), new Vector2(0f, -30f)); }
            Image ringBg = FishingUiKit.Icon(biteRoot, "Img_BiteRingBg", FishingUiKit.Circle(), new Vector2(110f, 110f), new Vector2(0f, -20f), new Color(0f, 0f, 0f, 0.35f));
            if (ringBg.sprite != null) { ringBg.color = new Color(0f, 0f, 0f, 0.35f); }
            Image ring = FishingUiKit.Icon(ringBg.transform, "Img_BiteRing", FishingUiKit.Circle(), new Vector2(98f, 98f), Vector2.zero, (Color)new Color32(255, 200, 60, 255));
            if (imgBiteRing == null) { imgBiteRing = ring; }
            imgBiteRing.type = Image.Type.Filled;
            imgBiteRing.fillMethod = Image.FillMethod.Radial360;
            imgBiteRing.fillOrigin = (int)Image.Origin360.Top;
            imgBiteRing.fillClockwise = false;
            if (imgBiteRing.sprite != null) { imgBiteRing.color = new Color32(255, 200, 60, 255); }
            TextMeshProUGUI ph = FishingUiKit.Label(biteRoot, "Txt_Phase", string.Empty, 38f, TextAlignmentOptions.Center, new Vector2(700f, 60f), new Vector2(0f, -110f), FishingUiKit.TextLight, true);
            if (created) { FishingUiKit.AddShadow(ph, new Color(0f, 0f, 0f, 0.7f), new Vector2(2f, -2f)); }
            if (txtPhase == null) { txtPhase = ph; }

            // ── Toast nhỏ ──
            RectTransform toast = FishingUiKit.Child(root, "Toast_Msg", new Vector2(760f, 76f), new Vector2(0f, 300f), out created);
            if (toastRoot == null) { toastRoot = toast; }
            Image toastBg = FishingUiKit.Panel(toast, "Img_Bg", new Vector2(760f, 76f), UIStandardSprites.RowDark, Vector2.zero, new Color(0f, 0f, 0f, 0.6f));
            toastBg.raycastTarget = false;
            FishingUiKit.Stretch(toastBg.rectTransform);
            TextMeshProUGUI tt = FishingUiKit.Label(toast, "Txt", string.Empty, 28f, TextAlignmentOptions.Center, new Vector2(720f, 60f), Vector2.zero, FishingUiKit.TextLight, true);
            if (txtToast == null) { txtToast = tt; }
            if (created) { toast.gameObject.SetActive(false); }

            // ── Panel con (ẩn) ──
            RectTransform pb = FishingUiKit.Child(root, "Panel_Basket", FishBasketPanelUI.PanelSize, Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(pb, new Vector2(1f, 0.5f), new Vector2(-150f, 0f)); }
            FishBasketPanelUI bp = FishingUiKit.GetOrAdd<FishBasketPanelUI>(pb.gameObject);
            if (basketPanel == null) { basketPanel = bp; }
            bp.BuildIfEmpty();
            if (created) { pb.gameObject.SetActive(false); }

            RectTransform pf = FishingUiKit.Child(root, "Panel_Friends", FriendsPanelUI.PanelSize, Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(pf, new Vector2(1f, 0.5f), new Vector2(-150f, 0f)); }
            FriendsPanelUI fp = FishingUiKit.GetOrAdd<FriendsPanelUI>(pf.gameObject);
            if (friendsPanel == null) { friendsPanel = fp; }
            fp.BuildIfEmpty();
            if (created) { pf.gameObject.SetActive(false); }

            RectTransform pc = FishingUiKit.Child(root, "Panel_Chat", ChatPanelUI.PanelSize, Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(pc, new Vector2(0.5f, 0f), new Vector2(0f, 30f)); }
            ChatPanelUI cp = FishingUiKit.GetOrAdd<ChatPanelUI>(pc.gameObject);
            if (chatPanel == null) { chatPanel = cp; }
            cp.BuildIfEmpty();
            if (created) { pc.gameObject.SetActive(false); }

            RectTransform popup = FishingUiKit.Child(root, "Popup_PlayerInfo", Vector2.zero, Vector2.zero, out created);
            if (created) { FishingUiKit.Stretch(popup); }
            PlayerInfoPopupUI pi = FishingUiKit.GetOrAdd<PlayerInfoPopupUI>(popup.gameObject);
            if (playerInfoPopup == null) { playerInfoPopup = pi; }
            pi.BuildIfEmpty();
            if (created) { popup.gameObject.SetActive(false); }

            RectTransform res = FishingUiKit.Child(root, "Toast_Result", new Vector2(560f, 200f), new Vector2(0f, 140f), out created);
            FishingResultToastUI rt = FishingUiKit.GetOrAdd<FishingResultToastUI>(res.gameObject);
            if (resultToast == null) { resultToast = rt; }
            rt.BuildIfEmpty();
            if (created) { res.gameObject.SetActive(false); }
        }

        /// <summary>Tab vuông 96 px: nền BtnPaper + con "Icon" (đội vẽ gắn) + nhãn nhỏ dưới.</summary>
        private Button Tab(Transform parent, string name, string label, Vector2 pos)
        {
            Button b = FishingUiKit.Button(parent, name, null, new Vector2(TabSize, TabSize), UIStandardSprites.BtnPaper, null, pos);
            FishingUiKit.Icon(b.transform, "Icon", null, new Vector2(54f, 54f), new Vector2(0f, 10f), new Color(1f, 1f, 1f, 0.25f));
            TextMeshProUGUI t = FishingUiKit.Label(b.transform, "Txt_Label", label, 18f, TextAlignmentOptions.Center, new Vector2(TabSize + 10f, 24f), new Vector2(0f, -32f), FishingUiKit.TextDark, true);
            t.overflowMode = TextOverflowModes.Overflow;
            return b;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HỒ SƠ
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshProfile()
        {
            string name = PlayerPrefs.GetString("PLAYER_PROFILE_NAME", Loc.T("Nông Dân Vui Vẻ"));
            int avatarIndex = PlayerPrefs.GetInt("PLAYER_PROFILE_AVATAR_INDEX", 0);
            int level = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            if (txtPlayerName != null) { txtPlayerName.text = name; }
            if (txtPlayerLevel != null) { txtPlayerLevel.text = "Lv " + FishingUiKit.Num(level); }
            if (imgAvatar != null)
            {
                Sprite s = AvatarProfilePopupUI.Instance != null ? AvatarProfilePopupUI.Instance.GetCurrentSelectedAvatar() : null;
                if (s == null) { s = FishingUiKit.AvatarSprite(avatarIndex); }
                imgAvatar.gameObject.SetActive(s != null);
                if (s != null) { FishingUiKit.SetIcon(imgAvatar, s, Color.white); }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TRẠNG THÁI NÚT / PHA
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshButtons()
        {
            if (_ctrl == null)
            {
                FishingUiKit.SetEnabled(btnCast, false);
                FishingUiKit.SetEnabled(btnReel, false);
                if (txtBlockReason != null) { txtBlockReason.text = FishingDatabase.IsEnabled ? Loc.T("Đang chuẩn bị...") : Loc.T("Hệ Hồ Câu chưa bật"); }
                return;
            }
            bool canCast = _ctrl.CanCast;
            FishingPhase p = _ctrl.Phase;
            FishingUiKit.SetEnabled(btnCast, canCast);
            FishingUiKit.SetEnabled(btnReel, p == FishingPhase.Waiting || p == FishingPhase.Bite);
            if (txtBlockReason != null) { txtBlockReason.text = (!canCast && p == FishingPhase.Idle) ? (_ctrl.BlockReasonVi ?? string.Empty) : string.Empty; }
        }

        private void OnPhaseChanged(FishingPhase phase)
        {
            Transform ringRoot = BiteRingRoot();
            if (ringRoot != null)
            {
                ringRoot.gameObject.SetActive(phase == FishingPhase.Bite);
                if (phase == FishingPhase.Bite) { imgBiteRing.fillAmount = 1f; JuicyPulseFX.Play(ringRoot, 1.3f, 0.3f); }
            }
            if (txtPhase != null) { txtPhase.text = PhaseText(phase); }
            if (phase == FishingPhase.Bite && btnReel != null) { JuicyPulseFX.Play(btnReel.transform, 1.25f, 0.3f); }
            RefreshButtons();
        }

        private static string PhaseText(FishingPhase p)
        {
            switch (p)
            {
                case FishingPhase.Casting: return Loc.T("Quăng...");
                case FishingPhase.Waiting: return Loc.T("Chờ cá...");
                case FishingPhase.Bite: return Loc.T("CÁ CẮN! THU NGAY");
                case FishingPhase.Reeling: return Loc.T("Đang thu dây...");
                default: return string.Empty;
            }
        }

        private void OnCatch(FishData fish, float kg)
        {
            FishingResultToastUI.Show(fish, kg);
            if (AudioManager.Instance != null) { AudioManager.Instance.PlaySuccess(); }
        }

        private void OnEscape() { FishingResultToastUI.ShowText(Loc.T("Cá thoát mất rồi!")); }

        private void OnNothing() { FishingResultToastUI.ShowText(Loc.T("Thu dây... không có gì")); }

        private void OnRodBroken()
        {
            ShowToast(Loc.T("Cần đã hỏng — về farm mua cần mới ở Quầy Cá"));
            RefreshButtons();
        }

        private void OnBlocked(string reasonVi)
        {
            ShowToast(string.IsNullOrEmpty(reasonVi) ? Loc.T("Chưa quăng được") : reasonVi);
            if (btnCast != null) { JuicyPulseFX.Play(btnCast.transform, 1.1f, 0.2f); }
        }

        private void OnCastClick()
        {
            if (_ctrl == null) { ShowToast(Loc.T("Chưa sẵn sàng")); return; }
            _ctrl.Cast();
        }

        private void OnReelClick()
        {
            if (_ctrl == null) { return; }
            _ctrl.Reel();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TAB / PANEL
        // ─────────────────────────────────────────────────────────────────────

        private void TogglePanel(GameObject target)
        {
            if (target == null) { return; }
            bool open = !target.activeSelf;
            CloseAllPanels();
            if (open) { target.SetActive(true); target.transform.SetAsLastSibling(); JuicyPulseFX.Play(target.transform, 1.05f, 0.2f); }
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        /// <summary>Đóng cả 3 panel con (loại trừ nhau).</summary>
        public void CloseAllPanels()
        {
            if (basketPanel != null) { basketPanel.gameObject.SetActive(false); }
            if (friendsPanel != null) { friendsPanel.gameObject.SetActive(false); }
            if (chatPanel != null) { chatPanel.gameObject.SetActive(false); }
        }

        private void OnChatTab()
        {
            if (chatPanel == null) { return; }
            bool open = !chatPanel.gameObject.activeSelf;
            TogglePanel(chatPanel.gameObject);
            if (open) { chatPanel.FocusInput(); }
        }

        private void OnPrivateTab()
        {
            _privateOn = !_privateOn;
            if (RemotePlayersManager.Instance != null) { RemotePlayersManager.Instance.SetPrivateMode(_privateOn); }
            if (FishingNetHub.Friends != null) { FishingNetHub.Friends.PrivateMode = _privateOn; }
            RefreshPrivateVisual();
            ShowToast(_privateOn ? Loc.T("Riêng tư: người lạ không thấy tin nhắn của bạn") : Loc.T("Đã tắt riêng tư"));
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        private void RefreshPrivateVisual()
        {
            if (imgPrivateOn != null) { imgPrivateOn.gameObject.SetActive(_privateOn); }
        }

        private void OnHomeClick()
        {
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
            CloseAllPanels();
            FishingSession.ReturnToFarm();
        }

        private void OnRemoteTapped(RemotePlayerView view)
        {
            if (view == null) { return; }
            CloseAllPanels();
            PlayerInfoPopupUI.Open(view);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TOAST
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Thanh thông báo nhỏ, tự ẩn. Gọi từ mọi UI trong scene câu.</summary>
        public void ShowToast(string vi)
        {
            if (toastRoot == null || txtToast == null) { Debug.Log(FishingIds.LogTag + " Toast: " + vi); return; }
            txtToast.text = vi ?? string.Empty;
            toastRoot.gameObject.SetActive(true);
            toastRoot.SetAsLastSibling();
            JuicyPulseFX.Play(toastRoot, 1.08f, 0.2f);
            if (_toastRoutine != null) { StopCoroutine(_toastRoutine); }
            _toastRoutine = StartCoroutine(HideToast());
        }

        private IEnumerator HideToast()
        {
            yield return new WaitForSecondsRealtime(ToastSeconds);
            _toastRoutine = null;
            if (toastRoot != null) { toastRoot.gameObject.SetActive(false); }
        }
    }
}
