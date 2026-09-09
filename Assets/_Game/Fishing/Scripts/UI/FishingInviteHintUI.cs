using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Thẻ nhỏ góc phải-trên FARM: "A đang mời bạn vào câu cá" + nút "Tới" (→ FishingEntryPopupUI.OpenForInvite) + X.
    /// Nghe FishingNetHub.Friends.OnInviteReceived (đăng ký OnEnable/huỷ OnDisable). Tự ẩn sau HideAfterSeconds.
    /// Component nằm trên "FishingInviteHint" (dưới Canvas_FishingPopup) luôn active; con "Root" bật/tắt.
    /// Là THẺ BÁO không chặn thao tác (không modal): không có nền mờ, KHÔNG tham gia FishingPopupStack (Escape phải dành cho popup thật đang mở);
    /// đóng bằng X, "Tới" hoặc tự ẩn.
    /// </summary>
    public class FishingInviteHintUI : MonoBehaviour
    {
        private const float HideAfterSeconds = 20f;
        private static readonly Vector2 CardSize = new Vector2(560f, 120f);

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI txtMessage;
        [SerializeField] private Button btnGo;
        [SerializeField] private Button btnClose;

        private InviteInfo _current;
        private Coroutine _hideRoutine;
        private bool _wired;

        private void Awake()
        {
            BuildIfEmpty();
            Wire();
            if (root != null) { root.gameObject.SetActive(false); }
        }

        private void OnEnable()
        {
            IFriendService friends = FishingNetHub.Friends;
            if (friends != null) { friends.OnInviteReceived += OnInvite; }
        }

        private void OnDisable()
        {
            IFriendService friends = FishingNetHub.Friends;
            if (friends != null) { friends.OnInviteReceived -= OnInvite; }
        }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            if (btnGo != null) { btnGo.onClick.AddListener(Go); }
            if (btnClose != null) { btnClose.onClick.AddListener(Hide); }
        }

        public void BuildIfEmpty()
        {
            var selfRt = transform as RectTransform;
            if (selfRt != null && selfRt.anchorMin == selfRt.anchorMax) { FishingUiKit.Stretch(selfRt); }
            bool created;
            RectTransform r = FishingUiKit.Child(transform, "Root", CardSize, Vector2.zero, out created);
            if (created) { FishingUiKit.SetAnchor(r, new Vector2(1f, 1f), new Vector2(-24f, -140f)); r.sizeDelta = CardSize; }
            if (root == null) { root = r; }

            Image bg = FishingUiKit.Panel(root, "Img_Bg", CardSize, UIStandardSprites.CardOuter, Vector2.zero, FishingUiKit.PanelFallback);
            FishingUiKit.Stretch(bg.rectTransform);
            Image inner = FishingUiKit.Panel(root, "Img_Inner", CardSize, UIStandardSprites.CardInner);
            FishingUiKit.Stretch(inner.rectTransform, 12f);
            inner.raycastTarget = false;

            TextMeshProUGUI msg = FishingUiKit.Label(root, "Txt_Msg", string.Empty, 26f, TextAlignmentOptions.Left, new Vector2(330f, 90f), Vector2.zero, FishingUiKit.TextDark, true);
            FishingUiKit.AnchorLeft(msg.rectTransform, 22f);
            if (txtMessage == null) { txtMessage = msg; }

            Button go = FishingUiKit.Button(root, "Btn_Go", Loc.T("Tới"), new Vector2(130f, 92f), UIStandardSprites.BtnGreen3D, null, Vector2.zero, 30f);
            FishingUiKit.AnchorRight((RectTransform)go.transform, 70f);
            if (btnGo == null) { btnGo = go; }

            Button close = FishingUiKit.CloseButton(root, null, new Vector2(8f, 8f));
            if (btnClose == null) { btnClose = close; }
        }

        private void OnInvite(InviteInfo invite)
        {
            if (invite == null || root == null) { return; }
            // Đang ở scene câu thì không hiện thẻ farm (HUD scene câu lo).
            if (FishingSession.IsInFishingScene) { return; }
            _current = invite;
            if (txtMessage != null) { txtMessage.text = Loc.TF("{0} đang mời bạn vào câu cá", string.IsNullOrEmpty(invite.fromName) ? invite.fromId : invite.fromName); }
            FishingUiKit.ActivateUpToCanvas(root);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            JuicyPulseFX.Play(root, 1.08f, 0.25f);
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayBubblePop(); }
            if (_hideRoutine != null) { StopCoroutine(_hideRoutine); }
            if (gameObject.activeInHierarchy) { _hideRoutine = StartCoroutine(AutoHide()); }
        }

        private IEnumerator AutoHide()
        {
            yield return new WaitForSecondsRealtime(HideAfterSeconds);
            _hideRoutine = null;
            Hide();
        }

        private void Go()
        {
            InviteInfo inv = _current;
            Hide();
            if (inv == null) { return; }
            FishingEntryPopupUI.OpenForInvite(inv);
        }

        private void Hide()
        {
            _current = null;
            if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
            if (root != null) { root.gameObject.SetActive(false); }
        }
    }
}
