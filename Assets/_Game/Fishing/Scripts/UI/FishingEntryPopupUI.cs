using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Popup vào Hồ Câu ở FARM (dưới Canvas_FishingPopup order 410, tool tạo canvas). Component nằm trên object "Popup_FishingEntry"
    /// luôn active; con "Root" (nền mờ + khung) bật/tắt — nhờ vậy coroutine toast chạy được khi popup đóng (mẫu StallPopupUI.popupRoot).
    /// Bước 1: 2 card PlayerF "Cô gái" / PlayerM "Cậu bé" (previewSprite, chọn = SlotSelected) + OK.
    /// Bước 2: danh sách phòng (Room.RequestRoomList): tên, "x/10", nút "Vào" (tắt khi đầy) + Quay lại.
    /// "Vào" → FishingSession.SelectCharacter/SelectRoom/EnterFishingScene. OpenForInvite → nhân vật đã lưu, nhấn mạnh phòng mời, vào ngay nếu còn chỗ.
    /// Guard: FishingDatabase.IsEnabled && level ≥ cfg.unlockLevel. FarmInputLock Register/Unregister cân bằng (_inputLockHeld).
    /// Thoát: X (đóng hẳn ở mọi bước) · chạm nền mờ · Escape/Back qua FishingPopupStack (chỉ khi ở đỉnh) · Step 2 có "Quay lại" về Step 1.
    /// Root bị SetActive(false) từ ngoài → Update tự dọn AnyOpen + lock (MarkClosed) để không kẹt PopupManager.
    /// </summary>
    public class FishingEntryPopupUI : MonoBehaviour
    {
        public static FishingEntryPopupUI Instance { get; private set; }
        /// <summary>Cho PopupManager.IsAnyPopupOpen (YÊU CẦU LIÊN DEV: Lead thêm 1 dòng) — chặn click world khi mở.</summary>
        public static bool AnyOpen
        {
            get
            {
                if (Instance == null)
                    Instance = FindFirstObjectByType<FishingEntryPopupUI>(FindObjectsInactive.Include);
                return Instance != null && Instance.IsOpen;
            }
        }

        private static readonly Vector2 FrameSize = new Vector2(1160f, 780f);
        private static readonly Vector2 CardSize = new Vector2(380f, 520f);
        private const float RoomRowHeight = 104f;
        private const float ToastSeconds = 2.2f;

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private RectTransform root;
        [SerializeField] private Button btnDim;
        [SerializeField] private RectTransform frame;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private Button btnClose;
        [SerializeField] private RectTransform step1;
        [SerializeField] private Image cardF;
        [SerializeField] private Image cardM;
        [SerializeField] private Image previewF;
        [SerializeField] private Image previewM;
        [SerializeField] private Button btnOk;
        [SerializeField] private RectTransform step2;
        [SerializeField] private TextMeshProUGUI txtRoomHint;
        [SerializeField] private RectTransform roomContent;
        [SerializeField] private Button btnBack;
        [SerializeField] private RectTransform toastRoot;
        [SerializeField] private TextMeshProUGUI txtToast;

        private string _selectedCharacterId = FishingIds.CharacterF;
        private string _inviteRoomId;
        private bool _inputLockHeld;
        private bool _wired;
        private bool _entering;
        private int _roomRequestSerial;
        private Coroutine _toastRoutine;
        private readonly List<RectTransform> _roomRows = new List<RectTransform>();

        public bool IsOpen { get { return root != null && root.gameObject.activeInHierarchy; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        // ─────────────────────────────────────────────────────────────────────
        //  VÒNG ĐỜI
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning(FishingIds.LogTag + " FishingEntryPopupUI trùng tại '" + name + "' — tự ẩn, không huỷ."); gameObject.SetActive(false); return; }
            Instance = this;
            BuildIfEmpty();
            Wire();
            if (root != null) { root.gameObject.SetActive(false); }
        }

        private void OnDestroy() { FishingPopupStack.Remove(this); if (Instance == this) { Instance = null; } }

        private void OnDisable() { MarkClosed(); }

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
            if (btnOk != null) { btnOk.onClick.AddListener(GoToStep2); }
            if (btnBack != null) { btnBack.onClick.AddListener(GoToStep1); }
            Button bf = cardF != null ? cardF.GetComponent<Button>() : null;
            Button bm = cardM != null ? cardM.GetComponent<Button>() : null;
            if (bf != null) { bf.onClick.AddListener(() => SelectCharacter(FishingIds.CharacterF)); }
            if (bm != null) { bm.onClick.AddListener(() => SelectCharacter(FishingIds.CharacterM)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MỞ / ĐÓNG (hợp đồng)
        // ─────────────────────────────────────────────────────────────────────

        public static void Open()
        {
            FishingEntryPopupUI inst = Resolve();
            if (inst == null) { return; }
            inst._inviteRoomId = null;
            inst.OpenInternal(false);
        }

        public static void OpenForInvite(InviteInfo invite)
        {
            FishingEntryPopupUI inst = Resolve();
            if (inst == null || invite == null) { return; }
            inst._inviteRoomId = invite.roomId;
            FishingSession.PendingInviteRoomId = invite.roomId;
            inst.OpenInternal(true);
        }

        private static FishingEntryPopupUI Resolve()
        {
            if (Instance != null) { return Instance; }
            var found = FindFirstObjectByType<FishingEntryPopupUI>(FindObjectsInactive.Include);
            if (found != null) { Instance = found; return found; }
            Debug.Log(FishingIds.LogTag + " Chưa có FishingEntryPopupUI trong scene farm — chạy Tools/Farm Game/Hồ Câu/★ SETUP (FishingFarmHookSetupTool).");
            return null;
        }

        private void OpenInternal(bool fromInvite)
        {
            if (!FishingDatabase.IsEnabled) { Toast(Loc.T("Hồ Câu chưa mở")); return; }
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            int level = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            if (level < cfg.unlockLevel) { Toast(Loc.TF("Mở ở cấp {0}", FishingUiKit.Num(cfg.unlockLevel))); return; }
            if (IsOpen) { return; }

            BuildIfEmpty();
            Wire();
            _entering = false;
            _selectedCharacterId = FishingSession.SelectedCharacterId;
            FishingUiKit.ActivateUpToCanvas(root);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            AcquireLock();
            FishingPopupStack.Push(this);
            RefreshCharacterCards();
            if (fromInvite) { GoToStep2(); } else { GoToStep1(); }
            if (frame != null) { JuicyPulseFX.Play(frame, 1.06f, 0.22f); }
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        public void ClosePopup()
        {
            if (!IsOpen) { return; }
            root.gameObject.SetActive(false);
            MarkClosed();
        }

        /// <summary>Dọn MỌI trạng thái "đang mở": cờ AnyOpen (PopupManager), FarmInputLock, stack Escape, request phòng đang chờ. An toàn gọi nhiều lần.</summary>
        private void MarkClosed()
        {
            _roomRequestSerial++;
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
        //  DỰNG
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
            TextMeshProUGUI title = FishingUiKit.Label(ribbon.transform, "Txt_Title", Loc.T("HỒ CÂU"), 42f, TextAlignmentOptions.Center, new Vector2(400f, 70f), new Vector2(0f, 4f), FishingUiKit.TextLight, true);
            if (txtTitle == null) { txtTitle = title; }

            Button close = FishingUiKit.CloseButton(frame, null, new Vector2(4f, 4f));
            if (btnClose == null) { btnClose = close; }

            // ── Bước 1: chọn nhân vật ──
            RectTransform s1 = FishingUiKit.Child(frame, "Step1_Character", FrameSize - new Vector2(60f, 140f), new Vector2(0f, -30f));
            if (step1 == null) { step1 = s1; }
            FishingUiKit.Label(step1, "Txt_Sub", Loc.T("Chọn nhân vật đi câu"), 30f, TextAlignmentOptions.Center, new Vector2(700f, 44f), new Vector2(0f, 285f), FishingUiKit.TextMuted, true);
            Image cf = CharacterCard(step1, "Card_PlayerF", FishingIds.CharacterF, Loc.T("Cô gái"), new Vector2(-230f, 0f));
            if (cardF == null) { cardF = cf; }
            if (previewF == null) { RectTransform pv = FishingUiKit.FindChild(cf.transform, "Img_Preview"); previewF = pv != null ? pv.GetComponent<Image>() : null; }
            Image cm = CharacterCard(step1, "Card_PlayerM", FishingIds.CharacterM, Loc.T("Cậu bé"), new Vector2(230f, 0f));
            if (cardM == null) { cardM = cm; }
            if (previewM == null) { RectTransform pv = FishingUiKit.FindChild(cm.transform, "Img_Preview"); previewM = pv != null ? pv.GetComponent<Image>() : null; }
            Button ok = FishingUiKit.Button(step1, "Btn_OK", Loc.T("OK"), new Vector2(280f, 96f), UIStandardSprites.BtnGreen3D, null, new Vector2(0f, -290f), 34f);
            if (btnOk == null) { btnOk = ok; }

            // ── Bước 2: chọn phòng ──
            RectTransform s2 = FishingUiKit.Child(frame, "Step2_Room", FrameSize - new Vector2(60f, 140f), new Vector2(0f, -30f));
            if (step2 == null) { step2 = s2; }
            TextMeshProUGUI hint = FishingUiKit.Label(step2, "Txt_RoomHint", Loc.T("Chọn phòng"), 30f, TextAlignmentOptions.Center, new Vector2(800f, 44f), new Vector2(0f, 285f), FishingUiKit.TextMuted, true);
            if (txtRoomHint == null) { txtRoomHint = hint; }
            RectTransform c;
            FishingUiKit.ScrollList(step2, "RoomList", new Vector2(900f, 470f), out c, new Vector2(0f, 10f));
            if (roomContent == null) { roomContent = c; }
            Button back = FishingUiKit.Button(step2, "Btn_Back", Loc.T("Quay lại"), new Vector2(260f, 96f), UIStandardSprites.BtnGray, null, new Vector2(0f, -290f), 30f, null, (Color)new Color32(150, 150, 150, 255));
            if (btnBack == null) { btnBack = back; }

            // ── Toast (ngoài Root để hiện được cả khi popup đóng) ──
            RectTransform toast = FishingUiKit.Child(transform, "Toast_Msg", new Vector2(760f, 76f), new Vector2(0f, 260f), out created);
            if (toastRoot == null) { toastRoot = toast; }
            Image tbg = FishingUiKit.Panel(toast, "Img_Bg", new Vector2(760f, 76f), UIStandardSprites.RowDark, Vector2.zero, new Color(0f, 0f, 0f, 0.65f));
            tbg.raycastTarget = false;
            FishingUiKit.Stretch(tbg.rectTransform);
            TextMeshProUGUI tt = FishingUiKit.Label(toast, "Txt", string.Empty, 28f, TextAlignmentOptions.Center, new Vector2(720f, 60f), Vector2.zero, FishingUiKit.TextLight, true);
            if (txtToast == null) { txtToast = tt; }
            if (created) { toast.gameObject.SetActive(false); }

            step2.gameObject.SetActive(false);
        }

        /// <summary>Card nhân vật: nền SlotNormal (Button) + Img_Preview (previewSprite, null → ô màu) + Txt_Name.</summary>
        private Image CharacterCard(Transform parent, string name, string characterId, string fallbackName, Vector2 pos)
        {
            bool created;
            RectTransform rt = FishingUiKit.Child(parent, name, CardSize, pos, out created);
            Image bg = FishingUiKit.GetOrAdd<Image>(rt.gameObject);
            if (created || bg.sprite == null) { FishingUiKit.SetSlicedOrSimple(bg, UIStandardSprites.SlotNormal, new Color(1f, 1f, 1f, 0.5f)); }
            bg.raycastTarget = true;
            Button b = FishingUiKit.GetOrAdd<Button>(rt.gameObject);
            if (b.targetGraphic == null) { b.targetGraphic = bg; }
            b.transition = Selectable.Transition.None;

            FishingDatabase db = FishingDatabase.Instance;
            FishingCharacterDef def = db != null ? db.FindCharacter(characterId) : null;
            Sprite preview = def != null ? def.previewSprite : null;
            Image pv = FishingUiKit.Icon(rt, "Img_Preview", preview, new Vector2(300f, 380f), new Vector2(0f, 30f), new Color(0.85f, 0.8f, 0.7f, 1f));
            if (pv.sprite == null && preview != null) { FishingUiKit.SetIcon(pv, preview, Color.white); }
            FishingUiKit.Label(rt, "Txt_Name", def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : fallbackName, 32f, TextAlignmentOptions.Center, new Vector2(340f, 48f), new Vector2(0f, -210f), FishingUiKit.TextDark, true);
            return bg;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BƯỚC 1
        // ─────────────────────────────────────────────────────────────────────

        private void SelectCharacter(string id)
        {
            _selectedCharacterId = id;
            RefreshCharacterCards();
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        private void RefreshCharacterCards()
        {
            SetCardSelected(cardF, _selectedCharacterId == FishingIds.CharacterF);
            SetCardSelected(cardM, _selectedCharacterId == FishingIds.CharacterM);
        }

        private static void SetCardSelected(Image card, bool on)
        {
            if (card == null) { return; }
            Sprite s = on ? UIStandardSprites.SlotSelected : UIStandardSprites.SlotNormal;
            if (s != null) { FishingUiKit.SetSlicedOrSimple(card, s); }
            else { card.color = on ? new Color(1f, 0.95f, 0.6f, 1f) : new Color(1f, 1f, 1f, 0.5f); }
            if (on) { JuicyPulseFX.Play(card.transform, 1.06f, 0.2f); }
        }

        private void GoToStep1()
        {
            _roomRequestSerial++;
            if (step1 != null) { step1.gameObject.SetActive(true); }
            if (step2 != null) { step2.gameObject.SetActive(false); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BƯỚC 2
        // ─────────────────────────────────────────────────────────────────────

        private void GoToStep2()
        {
            if (step1 != null) { step1.gameObject.SetActive(false); }
            if (step2 != null) { step2.gameObject.SetActive(true); }
            if (txtRoomHint != null) { txtRoomHint.text = string.IsNullOrEmpty(_inviteRoomId) ? Loc.T("Chọn phòng") : Loc.T("Bạn đang mời — vào phòng được đánh dấu"); }
            for (int i = 0; i < _roomRows.Count; i++) { if (_roomRows[i] != null) { _roomRows[i].gameObject.SetActive(false); } }
            IRoomService room = FishingNetHub.Room;
            if (room == null) { Toast(Loc.T("Không kết nối được dịch vụ phòng")); return; }
            int serial = ++_roomRequestSerial;
            room.RequestRoomList(list => { if (serial == _roomRequestSerial && IsOpen) { PopulateRooms(list); } });
        }

        private void PopulateRooms(List<RoomInfo> list)
        {
            int count = 0;
            RoomInfo inviteRoom = null;
            if (list != null && roomContent != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    RoomInfo info = list[i];
                    if (info == null) { continue; }
                    RectTransform row = EnsureRoomRow(count);
                    bool highlight = !string.IsNullOrEmpty(_inviteRoomId) && info.roomId == _inviteRoomId;
                    BindRoomRow(row, info, highlight);
                    if (highlight) { inviteRoom = info; }
                    count++;
                }
            }
            for (int i = count; i < _roomRows.Count; i++) { if (_roomRows[i] != null) { _roomRows[i].gameObject.SetActive(false); } }
            if (count == 0) { Toast(Loc.T("Chưa có phòng nào")); }

            // Lời mời: còn chỗ thì kéo vào ngay.
            if (inviteRoom != null)
            {
                if (!inviteRoom.IsFull) { EnterRoom(inviteRoom.roomId); }
                else { Toast(Loc.T("Phòng của bạn đã đầy, chọn phòng khác")); }
            }
        }

        private RectTransform EnsureRoomRow(int index)
        {
            while (_roomRows.Count <= index) { _roomRows.Add(null); }
            RectTransform row = _roomRows[index];
            if (row == null)
            {
                row = FishingUiKit.Row(roomContent, "Row_" + index, RoomRowHeight, UIStandardSprites.CardInner);
                TextMeshProUGUI nm = FishingUiKit.Label(row, "Txt_RoomName", string.Empty, 30f, TextAlignmentOptions.Left, new Vector2(420f, 44f), Vector2.zero, FishingUiKit.TextDark, true);
                FishingUiKit.AnchorLeft(nm.rectTransform, 24f);
                TextMeshProUGUI cnt = FishingUiKit.Label(row, "Txt_Count", string.Empty, 28f, TextAlignmentOptions.Right, new Vector2(140f, 40f), Vector2.zero, FishingUiKit.TextMuted, true);
                FishingUiKit.AnchorRight(cnt.rectTransform, 220f);
                Button enter = FishingUiKit.Button(row, "Btn_Enter", Loc.T("Vào"), new Vector2(170f, 92f), UIStandardSprites.BtnGreen3D, null, Vector2.zero, 30f);
                FishingUiKit.AnchorRight((RectTransform)enter.transform, 14f);
                Image mark = FishingUiKit.Icon(row, "Img_InviteMark", UIStandardSprites.CheckBadge, new Vector2(40f, 40f), Vector2.zero, FishingUiKit.OnlineGreen);
                FishingUiKit.AnchorRight(mark.rectTransform, 380f);
                _roomRows[index] = row;
            }
            row.gameObject.SetActive(true);
            row.SetSiblingIndex(index);
            return row;
        }

        private void BindRoomRow(RectTransform row, RoomInfo info, bool highlight)
        {
            Image bg = row.GetComponent<Image>();
            if (bg != null)
            {
                Sprite s = highlight ? UIStandardSprites.SlotSelected : UIStandardSprites.CardInner;
                if (s != null) { FishingUiKit.SetSlicedOrSimple(bg, s); }
                else { bg.color = highlight ? new Color(1f, 0.95f, 0.6f, 1f) : new Color(1f, 1f, 1f, 0.35f); }
            }
            RectTransform t = FishingUiKit.FindChild(row, "Txt_RoomName");
            if (t != null) { t.GetComponent<TextMeshProUGUI>().text = string.IsNullOrEmpty(info.displayName) ? info.roomId : info.displayName; }
            t = FishingUiKit.FindChild(row, "Txt_Count");
            if (t != null) { t.GetComponent<TextMeshProUGUI>().text = FishingUiKit.Num(info.playerCount) + "/" + FishingUiKit.Num(info.capacity); }
            t = FishingUiKit.FindChild(row, "Img_InviteMark");
            if (t != null) { t.gameObject.SetActive(highlight); }
            t = FishingUiKit.FindChild(row, "Btn_Enter");
            if (t != null)
            {
                Button enter = t.GetComponent<Button>();
                FishingUiKit.SetEnabled(enter, !info.IsFull);
                FishingUiKit.SetButtonText(enter, info.IsFull ? Loc.T("Đầy") : Loc.T("Vào"));
                string roomId = info.roomId;
                enter.onClick.RemoveAllListeners();
                enter.onClick.AddListener(() => EnterRoom(roomId));
            }
        }

        private void EnterRoom(string roomId)
        {
            if (_entering || string.IsNullOrEmpty(roomId)) { return; }
            _entering = true;
            FishingSession.SelectCharacter(_selectedCharacterId);
            FishingSession.SelectRoom(roomId);
            FishingSession.PendingInviteRoomId = null;
            if (AudioManager.Instance != null) { AudioManager.Instance.PlaySuccess(); }
            ClosePopup();
            if (!FishingSession.EnterFishingScene()) { _entering = false; Toast(Loc.T("Không vào được hồ câu lúc này")); }
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
