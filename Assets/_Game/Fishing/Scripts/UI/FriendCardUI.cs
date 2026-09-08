using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// 1 card bạn bè trong FriendsPanelUI: nền CardInner bo góc, avatar tròn, tên, Lv, chấm online, nhãn quan hệ (màu cfg.LineColorFor),
    /// nút "Mời" (online && (chưa ở phòng nào || khác phòng mình)), nút "Tới" (bạn đang ở phòng khác).
    /// FriendsPanelUI tạo card theo tên Card_i và gọi Bind mỗi lần refresh.
    /// </summary>
    public class FriendCardUI : MonoBehaviour
    {
        public const float Height = 150f;

        [SerializeField] private Image imgBg;
        [SerializeField] private Image imgAvatarFrame;
        [SerializeField] private Image imgAvatar;
        [SerializeField] private Image imgOnline;
        [SerializeField] private TextMeshProUGUI txtName;
        [SerializeField] private TextMeshProUGUI txtLevel;
        [SerializeField] private TextMeshProUGUI txtRelation;
        [SerializeField] private TextMeshProUGUI txtRoom;
        [SerializeField] private Button btnInvite;
        [SerializeField] private Button btnGoto;

        private FriendEntry _entry;
        private Action<FriendEntry> _onInvite;
        private Action<FriendEntry> _onGoto;
        private bool _wired;

        public FriendEntry Entry { get { return _entry; } }

        private void Awake() { BuildIfEmpty(); Wire(); }

        private void Wire()
        {
            if (_wired) { return; }
            _wired = true;
            if (btnInvite != null) { btnInvite.onClick.AddListener(() => { if (_entry != null && _onInvite != null) { _onInvite(_entry); } }); }
            if (btnGoto != null) { btnGoto.onClick.AddListener(() => { if (_entry != null && _onGoto != null) { _onGoto(_entry); } }); }
        }

        /// <summary>Dựng con nếu thiếu (card đặt trong VerticalLayoutGroup → LayoutElement chiều cao cố định).</summary>
        public void BuildIfEmpty()
        {
            LayoutElement le = FishingUiKit.GetOrAdd<LayoutElement>(gameObject);
            le.preferredHeight = Height; le.minHeight = Height;

            Image bg = FishingUiKit.GetOrAdd<Image>(gameObject);
            if (bg.sprite == null) { FishingUiKit.SetSlicedOrSimple(bg, UIStandardSprites.CardInner, new Color(1f, 1f, 1f, 0.35f)); }
            if (imgBg == null) { imgBg = bg; }

            Image frame = FishingUiKit.Icon(transform, "Img_AvatarFrame", UIStandardSprites.AvatarBase, new Vector2(104f, 104f), Vector2.zero, (Color)new Color32(120, 90, 60, 255));
            if (frame.sprite == null) { frame.sprite = FishingUiKit.Circle(); }
            FishingUiKit.AnchorLeft(frame.rectTransform, 16f);
            if (imgAvatarFrame == null) { imgAvatarFrame = frame; }

            Image av = FishingUiKit.Icon(frame.transform, "Img_Avatar", null, new Vector2(84f, 84f), Vector2.zero, new Color(0f, 0f, 0f, 0f));
            if (imgAvatar == null) { imgAvatar = av; }

            Image dot = FishingUiKit.Icon(frame.transform, "Img_Online", FishingUiKit.Circle(), new Vector2(24f, 24f), new Vector2(38f, -38f), FishingUiKit.OfflineGray);
            if (imgOnline == null) { imgOnline = dot; }

            TextMeshProUGUI nm = FishingUiKit.Label(transform, "Txt_Name", string.Empty, 30f, TextAlignmentOptions.Left, new Vector2(260f, 40f), Vector2.zero, FishingUiKit.TextDark, true);
            FishingUiKit.AnchorLeft(nm.rectTransform, 136f, 42f);
            if (txtName == null) { txtName = nm; }

            TextMeshProUGUI lv = FishingUiKit.Label(transform, "Txt_Level", string.Empty, 24f, TextAlignmentOptions.Left, new Vector2(120f, 32f), Vector2.zero, FishingUiKit.TextMuted);
            FishingUiKit.AnchorLeft(lv.rectTransform, 136f, 8f);
            if (txtLevel == null) { txtLevel = lv; }

            TextMeshProUGUI rel = FishingUiKit.Label(transform, "Txt_Relation", string.Empty, 24f, TextAlignmentOptions.Left, new Vector2(160f, 32f), Vector2.zero, FishingUiKit.TextMuted, true);
            FishingUiKit.AnchorLeft(rel.rectTransform, 250f, 8f);
            if (txtRelation == null) { txtRelation = rel; }

            TextMeshProUGUI room = FishingUiKit.Label(transform, "Txt_Room", string.Empty, 22f, TextAlignmentOptions.Left, new Vector2(280f, 30f), Vector2.zero, FishingUiKit.TextMuted);
            FishingUiKit.AnchorLeft(room.rectTransform, 136f, -28f);
            if (txtRoom == null) { txtRoom = room; }

            Button inv = FishingUiKit.Button(transform, "Btn_Invite", Loc.T("Mời"), new Vector2(120f, 92f), UIStandardSprites.BtnGreen3D, null, Vector2.zero, 28f);
            FishingUiKit.AnchorRight((RectTransform)inv.transform, 14f);
            if (btnInvite == null) { btnInvite = inv; }

            Button go = FishingUiKit.Button(transform, "Btn_Goto", Loc.T("Tới"), new Vector2(120f, 92f), UIStandardSprites.BtnYellow3D, null, Vector2.zero, 28f, null, (Color)new Color32(230, 180, 60, 255));
            FishingUiKit.AnchorRight((RectTransform)go.transform, 14f + 120f + 10f);
            if (btnGoto == null) { btnGoto = go; }
        }

        /// <summary>Đổ dữ liệu 1 bạn vào card. myRoomId = phòng mình đang ở (null nếu chưa).</summary>
        public void Bind(FriendEntry e, string myRoomId, Action<FriendEntry> onInvite, Action<FriendEntry> onGoto)
        {
            BuildIfEmpty();
            Wire();
            _entry = e; _onInvite = onInvite; _onGoto = onGoto;
            if (e == null) { return; }
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;

            if (txtName != null) { txtName.text = string.IsNullOrEmpty(e.displayName) ? e.playerId : e.displayName; }
            if (txtLevel != null) { txtLevel.text = "Lv " + FishingUiKit.Num(Mathf.Max(1, e.level)); }
            if (imgOnline != null) { imgOnline.color = e.online ? FishingUiKit.OnlineGreen : FishingUiKit.OfflineGray; }
            if (imgAvatar != null)
            {
                Sprite s = FishingUiKit.AvatarSprite(e.avatarIndex);
                imgAvatar.gameObject.SetActive(s != null);
                if (s != null) { FishingUiKit.SetIcon(imgAvatar, s, Color.white); }
            }
            if (txtRelation != null)
            {
                txtRelation.text = RelationText(e.relation);
                Color c = cfg.LineColorFor(e.relation);
                txtRelation.color = c.a > 0f ? c : FishingUiKit.TextMuted;
            }

            bool inRoom = !string.IsNullOrEmpty(e.roomId);
            bool sameRoom = inRoom && !string.IsNullOrEmpty(myRoomId) && e.roomId == myRoomId;
            if (txtRoom != null)
            {
                if (!e.online) { txtRoom.text = Loc.T("Ngoại tuyến"); }
                else if (sameRoom) { txtRoom.text = Loc.T("Cùng phòng với bạn"); }
                else if (inRoom) { txtRoom.text = Loc.TF("Đang câu ở {0}", e.roomId); }
                else { txtRoom.text = Loc.T("Đang ở farm"); }
            }
            if (btnInvite != null) { FishingUiKit.SetEnabled(btnInvite, e.online && (!inRoom || !sameRoom)); }
            if (btnGoto != null) { btnGoto.gameObject.SetActive(e.online && inRoom && !sameRoom); }
        }

        public static string RelationText(RelationshipKind k)
        {
            switch (k)
            {
                case RelationshipKind.Friend: return Loc.T("Bạn bè");
                case RelationshipKind.Sibling: return Loc.T("Chị em");
                case RelationshipKind.Dating: return Loc.T("Hẹn hò");
                default: return string.Empty;
            }
        }
    }
}
