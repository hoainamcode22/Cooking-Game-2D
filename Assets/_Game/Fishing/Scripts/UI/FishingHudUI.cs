using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// HUD scene câu, gắn trên Canvas_FishingHUD (tool tạo canvas Overlay 1920x1080 match 0.5; đây chỉ dựng con).
    /// Trái-trên: avatar + tên + Lv (khối "TopLeft_Profile" — vẫn dựng nhưng chỉ HIỆN khi cfg.hudShowProfileBlock, mặc định tắt vì tên đã có
    /// PlayerNameTag trên đầu) · trái-dưới: Joystick (VirtualJoystickUI) · phải-dưới: QUĂNG / THU · cạnh phải: cột tab
    /// Giỏ · Bạn bè · Chat · Riêng tư · Về farm · giữa-trên: vòng cửa sổ cắn + nhãn pha. Panel con mở loại trừ nhau.
    /// Nghe FishingController.Local (OnPhaseChanged/OnCatch/OnEscape/OnNothing/OnRodBroken/OnBlocked/OnPerfectCast), kiểm CanCast mỗi 0.2 s.
    /// Nút QUĂNG là GIỮ-THẢ (EventTrigger PointerDown/Up/Exit): giữ → CastPowerMeterUI.Begin(), thả → Cast(power). Editor: phím Space.
    /// Phím Escape / Back: HUD KHÔNG xử lý — từng panel/popup tự hỏi FishingPopupStack (chỉ đỉnh stack đóng). Space bỏ qua khi đang gõ chat.
    /// </summary>
    public class FishingHudUI : MonoBehaviour
    {
        public static FishingHudUI Instance { get; private set; }

        private const float BigButton = 150f;
        private const float TabSize = 96f;
        private const float TabGap = 14f;
        // Zoom (Dev F): 2 nút nhỏ 64 px, cột phải, PHÍA TRÊN nhóm tab (RightBar_Tabs cao 550, tâm y = 60 → mép trên ≈ 335).
        private const float ZoomButtonSize = 64f;
        private const float ZoomGroupY = 430f;
        private const float PollInterval = 0.2f;
        private const float ToastSeconds = 2.2f;

        [Header("Góc trái-trên (ẩn/hiện theo cfg.hudShowProfileBlock)")]
        [SerializeField] private RectTransform profileRoot;
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
        [SerializeField] private CastPowerMeterUI castMeter;

        [Header("Cột tab phải")]
        [SerializeField] private Button tabBasket;
        [SerializeField] private Button tabFriends;
        [SerializeField] private Button tabChat;
        [SerializeField] private Button tabPrivate;
        [SerializeField] private Image imgPrivateOn;
        [SerializeField] private Button btnHome;

        [Header("Zoom camera (Dev F)")]
        [SerializeField] private RectTransform zoomGroup;
        [SerializeField] private Button btnZoomIn;
        [SerializeField] private Button btnZoomOut;

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
        private FishingCameraFollow _camFollow;
        private bool _wired;
        private bool _privateOn;
        private float _nextPoll;
        private Coroutine _toastRoutine;
        // Đang giữ QUĂNG bằng phím Space (Editor) — chỉ thả theo phím khi chính phím bắt đầu.
        private bool _keyboardHold;

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
            PollKeyboardCast();
        }

        /// <summary>Bàn phím test (Editor/PC): giữ Space = giữ QUĂNG, thả Space = thả. Bỏ qua khi đang gõ ô chat.</summary>
        private void PollKeyboardCast()
        {
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) { return; }
            if (kb.spaceKey.wasPressedThisFrame && !IsTypingInInputField())
            {
                _keyboardHold = true;
                OnCastPress();
            }
            if (kb.spaceKey.wasReleasedThisFrame && _keyboardHold)
            {
                _keyboardHold = false;
                OnCastRelease();
            }
        }

        private static bool IsTypingInInputField()
        {
            EventSystem es = EventSystem.current;
            GameObject sel = es != null ? es.currentSelectedGameObject : null;
            return sel != null && sel.GetComponent<TMP_InputField>() != null;
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
                _ctrl.OnPerfectCast += OnPerfectCast;
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
                _ctrl.OnPerfectCast -= OnPerfectCast;
                _ctrl = null;
            }
            if (_remotes != null) { _remotes.OnRemoteTapped -= OnRemoteTapped; _remotes = null; }
            if (castMeter != null && castMeter.IsCharging) { castMeter.Cancel(); }
        }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            WireCastHold();
            if (btnReel != null) { btnReel.onClick.AddListener(OnReelClick); }
            if (tabBasket != null) { tabBasket.onClick.AddListener(() => TogglePanel(basketPanel != null ? basketPanel.gameObject : null)); }
            if (tabFriends != null) { tabFriends.onClick.AddListener(() => TogglePanel(friendsPanel != null ? friendsPanel.gameObject : null)); }
            if (tabChat != null) { tabChat.onClick.AddListener(OnChatTab); }
            if (tabPrivate != null) { tabPrivate.onClick.AddListener(OnPrivateTab); }
            if (btnHome != null) { btnHome.onClick.AddListener(OnHomeClick); }
            if (btnZoomIn != null) { btnZoomIn.onClick.AddListener(OnZoomInClick); }
            if (btnZoomOut != null) { btnZoomOut.onClick.AddListener(OnZoomOutClick); }
        }

        /// <summary>
        /// Btn_Cast giữ-thả qua EventTrigger (không dùng onClick nữa để không quăng 2 lần):
        /// PointerDown → Begin meter · PointerUp / PointerExit (đang giữ) → End meter + Cast(power). Meter tự thả sau 4 s → OnAutoEnd.
        /// </summary>
        private void WireCastHold()
        {
            if (btnCast == null) { return; }
            EventTrigger trigger = FishingUiKit.GetOrAdd<EventTrigger>(btnCast.gameObject);
            if (trigger == null) { btnCast.onClick.AddListener(OnCastClick); return; }
            AddTrigger(trigger, EventTriggerType.PointerDown, OnCastPointerDown);
            AddTrigger(trigger, EventTriggerType.PointerUp, OnCastPointerUp);
            AddTrigger(trigger, EventTriggerType.PointerExit, OnCastPointerExit);
            if (castMeter != null) { castMeter.OnAutoEnd += OnMeterAutoEnd; }
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
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
            if (profileRoot == null) { profileRoot = profile; }
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
            // Sếp: "khung avatar mình nghĩ xoá đi" → chỉ ẨN theo cờ (object giữ nguyên để bật lại được, không Destroy).
            ApplyProfileVisibility();

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

            // ── Dưới-giữa: bảng đo lực quăng (ẩn, hiện khi giữ QUĂNG), cách đáy 250 px để không đè QUĂNG/THU ──
            RectTransform meterRt = FishingUiKit.Child(root, CastPowerMeterUI.ObjectName, CastPowerMeterUI.CardSize, Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(meterRt, new Vector2(0.5f, 0f), new Vector2(0f, 250f)); }
            CastPowerMeterUI meter = FishingUiKit.GetOrAdd<CastPowerMeterUI>(meterRt.gameObject);
            if (castMeter == null) { castMeter = meter; }
            meter.BuildIfEmpty();
            if (created) { meterRt.gameObject.SetActive(false); }

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

            // ── Cạnh phải, TRÊN nhóm tab: 2 nút zoom nhỏ (không đè RightBar_Tabs) ──
            RectTransform zoomBar = FishingUiKit.Child(root, "RightBar_Zoom", new Vector2(ZoomButtonSize + 20f, ZoomButtonSize * 2f + 30f), Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(zoomBar, new Vector2(1f, 0.5f), new Vector2(-16f, ZoomGroupY)); }
            if (zoomGroup == null) { zoomGroup = zoomBar; }
            Button zin = ZoomButton(zoomBar, "Btn_ZoomIn", "+", new Vector2(0f, ZoomButtonSize * 0.5f + 6f));
            if (btnZoomIn == null) { btnZoomIn = zin; }
            Button zout = ZoomButton(zoomBar, "Btn_ZoomOut", "−", new Vector2(0f, -ZoomButtonSize * 0.5f - 6f));
            if (btnZoomOut == null) { btnZoomOut = zout; }
            FishingConfig zoomCfg = FishingDatabase.ConfigOrDefault;
            bool zoomOn = zoomCfg == null || zoomCfg.zoomButtonsEnabled;
            if (zoomBar.gameObject.activeSelf != zoomOn) { zoomBar.gameObject.SetActive(zoomOn); }

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

        /// <summary>
        /// Nút zoom vuông 64 px. Không dùng FishingUiKit.Button vì hàm đó ép tối thiểu 92 px (MinButtonSize) — ở đây cần nút nhỏ.
        /// </summary>
        private Button ZoomButton(Transform parent, string name, string glyph, Vector2 pos)
        {
            bool created;
            RectTransform rt = FishingUiKit.Child(parent, name, new Vector2(ZoomButtonSize, ZoomButtonSize), pos, out created);
            Image img = FishingUiKit.GetOrAdd<Image>(rt.gameObject);
            if (created || img.sprite == null) { FishingUiKit.SetSlicedOrSimple(img, UIStandardSprites.BtnPaper, FishingUiKit.PanelFallback); }
            img.raycastTarget = true;
            Button btn = FishingUiKit.GetOrAdd<Button>(rt.gameObject);
            if (btn.targetGraphic == null) { btn.targetGraphic = img; }
            TextMeshProUGUI tmp = FishingUiKit.Label(rt, "Txt", glyph, 42f, TextAlignmentOptions.Center, new Vector2(ZoomButtonSize, ZoomButtonSize), Vector2.zero, FishingUiKit.TextDark, true);
            if (created) { tmp.overflowMode = TextOverflowModes.Overflow; }
            // Font Baloo2 dựng atlas tĩnh có thể thiếu dấu trừ "−" (U+2212) → rơi về gạch ngang thường để không ra ô vuông.
            if (!string.IsNullOrEmpty(glyph) && tmp.font != null && !tmp.font.HasCharacter(glyph[0])) { tmp.text = "-"; }
            return btn;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ZOOM CAMERA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>FishingCameraFollow của scene (tìm 1 lần, có thể null nếu Sếp gỡ script khỏi Main Camera).</summary>
        private FishingCameraFollow CameraFollow()
        {
            if (_camFollow == null) { _camFollow = FindFirstObjectByType<FishingCameraFollow>(); }
            return _camFollow;
        }

        private void OnZoomInClick()
        {
            FishingCameraFollow cf = CameraFollow();
            if (cf == null) { ShowToast(Loc.T("Main Camera chưa có FishingCameraFollow")); return; }
            cf.ZoomIn();
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        private void OnZoomOutClick()
        {
            FishingCameraFollow cf = CameraFollow();
            if (cf == null) { ShowToast(Loc.T("Main Camera chưa có FishingCameraFollow")); return; }
            cf.ZoomOut();
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HỒ SƠ
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Bật/tắt khối TopLeft_Profile theo cfg.hudShowProfileBlock (mặc định TẮT). Gọi ở BuildIfEmpty (tool + runtime) và RefreshProfile.
        /// Các field con (imgAvatar/txtPlayerName/txtPlayerLevel) vẫn được gán → ghi text lên object inactive không NRE.
        /// </summary>
        private void ApplyProfileVisibility()
        {
            if (profileRoot == null) { return; }
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            bool show = cfg != null && cfg.hudShowProfileBlock;
            if (profileRoot.gameObject.activeSelf != show) { profileRoot.gameObject.SetActive(show); }
        }

        private void RefreshProfile()
        {
            ApplyProfileVisibility();
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
            // Rời Idle mà vẫn đang giữ (vd. quăng bằng cách khác) → thu bảng đo, không quăng thêm.
            if (phase != FishingPhase.Idle && castMeter != null && castMeter.IsCharging) { castMeter.Cancel(); _keyboardHold = false; }
            RefreshButtons();
        }

        private void OnPerfectCast()
        {
            // FX thị giác "HOÀN HẢO!" đã do CastPowerMeterUI.End() bắn; ở đây chỉ đổi chữ toast.
            ShowToast(Loc.T("Quăng hoàn hảo! Cá hiếm dễ cắn hơn"));
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

        /// <summary>Fallback khi không gắn được EventTrigger: bấm = quăng theo cách cũ (không thanh đo).</summary>
        private void OnCastClick()
        {
            if (_ctrl == null) { ShowToast(Loc.T("Chưa sẵn sàng")); return; }
            _ctrl.Cast();
        }

        private void OnCastPointerDown(BaseEventData data) { OnCastPress(); }

        private void OnCastPointerUp(BaseEventData data) { OnCastRelease(); }

        /// <summary>Kéo ngón ra khỏi nút khi đang giữ = thả (tránh kẹt meter khi PointerUp rơi ngoài nút).</summary>
        private void OnCastPointerExit(BaseEventData data) { if (castMeter != null && castMeter.IsCharging && !_keyboardHold) { OnCastRelease(); } }
        // [Lead vòng 13] Ghi chú: trượt ngón ra khỏi nút = thả (quăng theo lực lúc đó). Muốn đổi thành HUỶ thì thay OnCastRelease() bằng castMeter.Cancel().

        /// <summary>Bắt đầu giữ QUĂNG: đủ điều kiện → mở bảng đo; không → gọi Cast() cũ để controller bắn OnBlocked kèm lý do.</summary>
        private void OnCastPress()
        {
            if (_ctrl == null) { ShowToast(Loc.T("Chưa sẵn sàng")); return; }
            if (castMeter != null && castMeter.IsCharging) { return; }
            // Đang trong lượt câu (Casting..Result): nút đã mờ, bỏ qua im lặng (EventTrigger vẫn nhận chạm dù Button.interactable=false).
            if (_ctrl.Phase != FishingPhase.Idle) { return; }
            if (!_ctrl.CanCast) { _keyboardHold = false; _ctrl.Cast(); return; }
            if (castMeter == null) { _keyboardHold = false; _ctrl.Cast(); return; }
            castMeter.Begin();
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        /// <summary>Thả QUĂNG: lấy lực từ bảng đo rồi quăng. Không đang giữ → bỏ qua (PointerUp + PointerExit có thể cùng bắn).</summary>
        private void OnCastRelease()
        {
            if (castMeter == null || !castMeter.IsCharging) { return; }
            float power = castMeter.End();
            if (_ctrl == null) { return; }
            _ctrl.Cast(power);
        }

        private void OnMeterAutoEnd(float power)
        {
            _keyboardHold = false;
            if (_ctrl == null) { return; }
            _ctrl.Cast(power);
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
            if (toastRoot == null || txtToast == null || !isActiveAndEnabled) { Debug.Log(FishingIds.LogTag + " Toast: " + vi); return; }
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
