using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Panel bạn bè trong scene câu (con "Panel_Friends" của Canvas_FishingHUD): khung dài cuộn dọc, mỗi bạn 1 FriendCardUI.
    /// "Mời" → FishingNetHub.Friends.InviteToRoom(id, Room.CurrentRoomId). "Tới" → đóng panel, FishingSession.SelectRoom(roomId);
    /// vòng 1 đang trong scene câu chỉ toast "Sẽ đổi phòng ở vòng online". Refresh theo OnFriendsChanged.
    /// </summary>
    public class FriendsPanelUI : MonoBehaviour
    {
        public static readonly Vector2 PanelSize = new Vector2(720f, 860f);

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private Image imgFrame;
        [SerializeField] private Image imgPaper;
        [SerializeField] private TextMeshProUGUI txtHeader;
        [SerializeField] private Button btnClose;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [SerializeField] private TextMeshProUGUI txtEmpty;

        private readonly List<FriendCardUI> _cards = new List<FriendCardUI>();
        private bool _wired;

        private void Awake() { BuildIfEmpty(); Wire(); }

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
            IFriendService fs = FishingNetHub.Friends;
            if (fs != null) { fs.OnFriendsChanged -= Refresh; fs.OnFriendsChanged += Refresh; }
            Refresh();
        }

        private void OnDisable()
        {
            IFriendService fs = FishingNetHub.Friends;
            if (fs != null) { fs.OnFriendsChanged -= Refresh; }
        }

        public void Close()
        {
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
            gameObject.SetActive(false);
        }

        /// <summary>Dựng con nếu thiếu; không huỷ con có sẵn.</summary>
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

            TextMeshProUGUI header = FishingUiKit.Label(transform, "Txt_Header", Loc.T("Bạn bè"), 36f, TextAlignmentOptions.Center, new Vector2(PanelSize.x - 160f, 60f), new Vector2(0f, PanelSize.y * 0.5f - 60f), FishingUiKit.TextDark, true);
            if (txtHeader == null) { txtHeader = header; }

            Button close = FishingUiKit.CloseButton(transform, null, new Vector2(4f, 4f));
            if (btnClose == null) { btnClose = close; }

            RectTransform c;
            ScrollRect sr = FishingUiKit.ScrollList(transform, "List", new Vector2(PanelSize.x - 60f, PanelSize.y - 170f), out c, new Vector2(0f, -20f));
            if (scroll == null) { scroll = sr; }
            if (content == null) { content = c; }

            TextMeshProUGUI empty = FishingUiKit.Label(transform, "Txt_Empty", Loc.T("Chưa có bạn — chạm vào người chơi khác để kết bạn"), 28f, TextAlignmentOptions.Center, new Vector2(PanelSize.x - 120f, 90f), new Vector2(0f, -20f), FishingUiKit.TextMuted);
            if (txtEmpty == null) { txtEmpty = empty; }
        }

        /// <summary>Vẽ lại danh sách bạn (tái dùng Card_i).</summary>
        public void Refresh()
        {
            if (!gameObject.activeInHierarchy) { return; }
            IFriendService fs = FishingNetHub.Friends;
            IRoomService room = FishingNetHub.Room;
            string myRoom = room != null ? room.CurrentRoomId : null;
            int count = 0;
            if (fs != null && fs.Friends != null && content != null)
            {
                IReadOnlyList<FriendEntry> list = fs.Friends;
                for (int i = 0; i < list.Count; i++)
                {
                    FriendEntry e = list[i];
                    if (e == null) { continue; }
                    FriendCardUI card = EnsureCard(count);
                    card.Bind(e, myRoom, OnInvite, OnGoto);
                    count++;
                }
                if (txtHeader != null) { txtHeader.text = Loc.TF("Bạn bè ({0})", FishingUiKit.Num(count)); }
            }
            for (int i = count; i < _cards.Count; i++) { if (_cards[i] != null) { _cards[i].gameObject.SetActive(false); } }
            if (txtEmpty != null) { txtEmpty.gameObject.SetActive(count == 0); }
        }

        private FriendCardUI EnsureCard(int index)
        {
            while (_cards.Count <= index) { _cards.Add(null); }
            FriendCardUI card = _cards[index];
            if (card == null)
            {
                RectTransform rt = FishingUiKit.Child(content, "Card_" + index, new Vector2(0f, FriendCardUI.Height));
                card = FishingUiKit.GetOrAdd<FriendCardUI>(rt.gameObject);
                card.BuildIfEmpty();
                _cards[index] = card;
            }
            card.gameObject.SetActive(true);
            card.transform.SetSiblingIndex(index);
            return card;
        }

        private void OnInvite(FriendEntry e)
        {
            IRoomService room = FishingNetHub.Room;
            IFriendService fs = FishingNetHub.Friends;
            if (e == null || fs == null) { return; }
            string myRoom = room != null ? room.CurrentRoomId : null;
            if (string.IsNullOrEmpty(myRoom)) { Toast(Loc.T("Bạn chưa ở trong phòng nào")); return; }
            bool ok = fs.InviteToRoom(e.playerId, myRoom);
            Toast(ok ? Loc.TF("Đã mời {0} vào phòng", e.displayName) : Loc.T("Không mời được lúc này"));
            if (ok && AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
        }

        private void OnGoto(FriendEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.roomId)) { return; }
            gameObject.SetActive(false);
            FishingSession.SelectRoom(e.roomId);
            if (FishingSession.IsInFishingScene)
            {
                // Vòng 1 offline: chưa đổi phòng nóng trong scene câu.
                Toast(Loc.T("Sẽ đổi phòng ở vòng online"));
                Debug.Log(FishingIds.LogTag + " Tới phòng bạn " + e.roomId + " — vòng 1 chỉ ghi nhớ, chưa đổi phòng nóng.");
            }
        }

        private static void Toast(string vi)
        {
            if (FishingHudUI.Instance != null) { FishingHudUI.Instance.ShowToast(vi); }
        }
    }
}
