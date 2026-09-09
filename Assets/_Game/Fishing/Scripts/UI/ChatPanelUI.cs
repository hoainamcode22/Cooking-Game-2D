using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Thanh chat dưới giữa scene câu (con "Panel_Chat" của Canvas_FishingHUD): TMP_InputField (mobile tự bật bàn phím khi focus,
    /// characterLimit = cfg.chatMaxLength) + nút Gửi + log 6 dòng gần nhất (tên: nội dung) mờ dần theo tuổi.
    /// Gửi → FishingNetHub.Chat.Send. OnMessage từ chính mình (senderId == Room.LocalPlayerId) → SetLocalBubble + ChatBubbleUI.Show(local).
    /// Tin người khác chỉ vào log (bubble do RemotePlayerView lo). Enter/Submit gửi.
    /// Nghe Chat.OnMessage suốt phiên (EnsureListening do HUD gọi ở Start) để bubble của mình vẫn hiện dù panel đang đóng.
    /// Thoát: X · chạm nền mờ phía sau (Img_Dim con) — ĐANG GÕ thì chạm nền chỉ bỏ focus, không đóng · Escape/Back qua FishingPopupStack:
    /// đang gõ → chỉ bỏ focus + MarkEscapeConsumed (TMP_InputField cũng tự huỷ focus khi Escape; thứ tự Update với EventSystem không chắc
    /// nên nhớ mốc _lastFocusedAt để nhận ra "vừa gõ xong" trong cùng frame/frame kế).
    /// </summary>
    public class ChatPanelUI : MonoBehaviour
    {
        public static readonly Vector2 PanelSize = new Vector2(940f, 340f);
        private const int LogLines = 6;
        private const float LogFadeSeconds = 25f;
        private const float LogMinAlpha = 0.35f;
        // Sau khi bỏ focus, trong khoảng này vẫn coi là "đang gõ" (bắt được cả trường hợp EventSystem đã bỏ focus trước Update của ta).
        private const float TypingGraceSeconds = 0.35f;

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private Button btnDim;
        [SerializeField] private Image imgBg;
        [SerializeField] private RectTransform logRoot;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button btnSend;
        [SerializeField] private Button btnClose;

        private struct LogEntry { public string text; public float time; }
        private readonly List<LogEntry> _log = new List<LogEntry>(LogLines);
        private readonly List<TextMeshProUGUI> _lines = new List<TextMeshProUGUI>(LogLines);
        private bool _wired;
        private bool _listening;
        private float _lastFocusedAt = -10f;

        private void Awake() { BuildIfEmpty(); Wire(); }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            if (btnSend != null) { btnSend.onClick.AddListener(Send); }
            if (btnClose != null) { btnClose.onClick.AddListener(Close); }
            if (btnDim != null) { btnDim.onClick.AddListener(OnDimClick); }
            if (input != null) { input.onSubmit.AddListener(_ => Send()); }
        }

        private void OnEnable()
        {
            BuildIfEmpty();
            Wire();
            EnsureListening();
            if (input != null) { input.characterLimit = FishingDatabase.ConfigOrDefault.chatMaxLength; }
            _lastFocusedAt = -10f;
            FishingPopupStack.Push(this);
            RedrawLog();
        }

        private void OnDisable()
        {
            FishingPopupStack.Remove(this);
        }

        private void OnDestroy()
        {
            FishingPopupStack.Remove(this);
            IChatService chat = FishingNetHub.Chat;
            if (chat != null && _listening) { chat.OnMessage -= OnMessage; }
            _listening = false;
        }

        /// <summary>Đang gõ: ô nhập có focus, hoặc vừa mất focus trong TypingGraceSeconds (EventSystem có thể bỏ focus trước ta).</summary>
        private bool IsTyping()
        {
            if (input == null) { return false; }
            if (input.isFocused) { return true; }
            return Time.unscaledTime - _lastFocusedAt < TypingGraceSeconds;
        }

        /// <summary>Bỏ focus ô nhập và xoá mốc "vừa gõ" để lần Escape/chạm nền kế tiếp đóng panel thật.</summary>
        private void DropFocus()
        {
            if (input != null && input.isFocused) { input.DeactivateInputField(); }
            _lastFocusedAt = -10f;
        }

        /// <summary>Chạm nền mờ: đang gõ → chỉ bỏ focus (bàn phím mobile hạ); không gõ → đóng.</summary>
        private void OnDimClick()
        {
            if (IsTyping()) { DropFocus(); return; }
            Close();
        }

        private void PollEscape()
        {
            if (!FishingPopupStack.IsTop(this) || !FishingPopupStack.EscapePressedThisFrame()) { return; }
            if (IsTyping())
            {
                // Escape khi đang gõ = chỉ thoát ô nhập; đánh dấu đã xử lý để không popup nào khác đóng theo.
                FishingPopupStack.MarkEscapeConsumed();
                DropFocus();
                return;
            }
            if (FishingPopupStack.ConsumeEscape(this)) { Close(); }
        }

        /// <summary>Đăng ký nghe chat đúng 1 lần (HUD gọi ở Start; OnEnable cũng gọi). Huỷ ở OnDestroy.</summary>
        public void EnsureListening()
        {
            if (_listening) { return; }
            IChatService chat = FishingNetHub.Chat;
            if (chat == null) { return; }
            chat.OnMessage -= OnMessage;
            chat.OnMessage += OnMessage;
            _listening = true;
        }

        public void Close()
        {
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
            gameObject.SetActive(false);
        }

        /// <summary>Focus ô nhập (HUD gọi khi mở panel để bàn phím mobile bật ngay).</summary>
        public void FocusInput()
        {
            if (input != null) { input.ActivateInputField(); input.Select(); }
        }

        /// <summary>Dựng con nếu thiếu; không huỷ con có sẵn.</summary>
        public void BuildIfEmpty()
        {
            var rt = transform as RectTransform;
            if (rt != null && rt.sizeDelta == Vector2.zero) { rt.sizeDelta = PanelSize; }
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;

            // Nền mờ nhạt phủ cả màn hình, nằm SAU thanh chat (sibling đầu): chạm ra ngoài = đóng (đang gõ thì chỉ bỏ focus).
            // [Lead vòng 16 — Reviewer L9] Nền mờ chỉ khi cfg.hudPanelTapOutsideCloses: dim 6000 px phủ cả cột tab HUD nên bấm tab khác
            // khi panel đang mở = bấm dim = đóng, phải bấm 2 lần. Mặc định TẮT (panel HUD không modal, đóng bằng X / Escape).
            if (FishingDatabase.ConfigOrDefault.hudPanelTapOutsideCloses)
            {
                Button dim = FishingUiKit.DimBehindPanel(transform, null, FishingUiKit.DimLight);
                if (btnDim == null) { btnDim = dim; }
            }
            else
            {
                Transform dimCu = transform.Find("Img_Dim");
                if (dimCu != null && dimCu.gameObject.activeSelf) { dimCu.gameObject.SetActive(false); }
            }

            Image bg = FishingUiKit.Panel(transform, "Img_Bg", PanelSize, UIStandardSprites.RowDark, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
            if (imgBg == null) { imgBg = bg; }
            FishingUiKit.Stretch(imgBg.rectTransform, 0f);

            Button close = FishingUiKit.CloseButton(transform, null, new Vector2(6f, 6f));
            if (btnClose == null) { btnClose = close; }

            bool logCreated;
            RectTransform lr = FishingUiKit.Child(transform, "Log", new Vector2(PanelSize.x - 40f, PanelSize.y - 130f), new Vector2(0f, 50f), out logCreated);
            if (logRoot == null) { logRoot = lr; }
            VerticalLayoutGroup vlg = FishingUiKit.GetOrAdd<VerticalLayoutGroup>(logRoot.gameObject);
            if (logCreated)
            {
                vlg.childAlignment = TextAnchor.LowerLeft; vlg.spacing = 2f;
                vlg.childControlWidth = true; vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
                vlg.padding = new RectOffset(16, 16, 6, 6);
            }
            _lines.Clear();
            for (int i = 0; i < LogLines; i++)
            {
                TextMeshProUGUI line = FishingUiKit.Label(logRoot, "Line_" + i, string.Empty, 24f, TextAlignmentOptions.Left, new Vector2(0f, 30f), Vector2.zero, FishingUiKit.TextLight);
                LayoutElement le = FishingUiKit.GetOrAdd<LayoutElement>(line.gameObject);
                le.preferredHeight = 30f; le.minHeight = 30f;
                line.textWrappingMode = TextWrappingModes.NoWrap;
                _lines.Add(line);
            }

            TMP_InputField inp = FishingUiKit.InputField(transform, "Input_Chat", new Vector2(PanelSize.x - 40f - 180f, 92f), Loc.T("Nhập tin nhắn..."), cfg.chatMaxLength, new Vector2(-90f, -PanelSize.y * 0.5f + 60f));
            if (input == null) { input = inp; }

            Button send = FishingUiKit.Button(transform, "Btn_Send", Loc.T("Gửi"), new Vector2(160f, 92f), UIStandardSprites.BtnGreen3D, null, new Vector2(PanelSize.x * 0.5f - 20f - 80f, -PanelSize.y * 0.5f + 60f), 30f);
            if (btnSend == null) { btnSend = send; }
        }

        private void Send()
        {
            if (input == null) { return; }
            string text = (input.text ?? string.Empty).Trim();
            if (text.Length == 0) { return; }
            int max = FishingDatabase.ConfigOrDefault.chatMaxLength;
            if (text.Length > max) { text = text.Substring(0, max); }
            IChatService chat = FishingNetHub.Chat;
            if (chat == null) { return; }
            bool ok = chat.Send(text);
            if (!ok)
            {
                if (FishingHudUI.Instance != null) { FishingHudUI.Instance.ShowToast(Loc.T("Chưa vào phòng, không gửi được")); }
                return;
            }
            input.text = string.Empty;
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayBubblePop(); }
            FocusInput();
        }

        private void OnMessage(ChatMessage m)
        {
            if (m == null || string.IsNullOrEmpty(m.text)) { return; }
            string who = string.IsNullOrEmpty(m.senderName) ? m.senderId : m.senderName;
            Push(who + ": " + m.text);

            IRoomService room = FishingNetHub.Room;
            bool mine = room != null && !string.IsNullOrEmpty(room.LocalPlayerId) && m.senderId == room.LocalPlayerId;
            if (mine)
            {
                float secs = FishingDatabase.ConfigOrDefault.bubbleShowSeconds;
                if (RemotePlayersManager.Instance != null) { RemotePlayersManager.Instance.SetLocalBubble(m.text, secs); }
                FishingPlayerController local = FishingPlayerController.Local;
                if (local != null && local.HeadAnchor != null) { ChatBubbleUI.Show(local.HeadAnchor, m.text, secs, true); }
            }
        }

        private void Push(string line)
        {
            _log.Add(new LogEntry { text = line, time = Time.time });
            while (_log.Count > LogLines) { _log.RemoveAt(0); }
            RedrawLog();
        }

        private void Update()
        {
            if (input != null && input.isFocused) { _lastFocusedAt = Time.unscaledTime; }
            PollEscape();
            if (_log.Count == 0 || _lines.Count == 0) { return; }
            // Mờ dần theo tuổi tin (chỉ đổi alpha, rẻ).
            for (int i = 0; i < _lines.Count; i++)
            {
                int li = _log.Count - _lines.Count + i;
                if (li < 0 || _lines[i] == null) { continue; }
                float age = Time.time - _log[li].time;
                float a = Mathf.Lerp(1f, LogMinAlpha, Mathf.Clamp01(age / LogFadeSeconds));
                Color c = _lines[i].color; c.a = a; _lines[i].color = c;
            }
        }

        private void RedrawLog()
        {
            if (_lines.Count == 0) { return; }
            // Dòng cuối = tin mới nhất; dòng trên rỗng nếu chưa đủ.
            for (int i = 0; i < _lines.Count; i++)
            {
                int li = _log.Count - _lines.Count + i;
                if (_lines[i] == null) { continue; }
                _lines[i].text = li >= 0 ? _log[li].text : string.Empty;
                Color c = _lines[i].color; c.a = 1f; _lines[i].color = c;
            }
        }
    }
}
