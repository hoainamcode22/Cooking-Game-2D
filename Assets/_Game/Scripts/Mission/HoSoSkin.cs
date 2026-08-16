using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HỒ SƠ — theo render `HoSoAvatar_A.html`. Khung gỗ + ảnh avatar là ART có sẵn
/// (đẹp, giữ nguyên); vỏ chỉ đưa phần thông tin về ngôn ngữ chung: ruy băng
/// "HỒ SƠ" · giấy kem panel phải · ô tên thẻ kem · thanh EXP ruột xanh · dải
/// thống kê thẻ kem chữ nâu · X đỏ. Nhãn "Cấp 1 - 50" đã sửa tận gốc trong
/// AvatarProfilePopupUI (chuỗi ghi cứng, mỗi lần mở lại đè — sửa ở vỏ là vô ích).
///
/// Tham chiếu lấy thẳng từ [SerializeField] của AvatarProfilePopupUI (SkinVi.Lay)
/// — không dò tên. Chỉ TÔ / THÊM lớp trang trí, logic nguyên vẹn.
/// </summary>
[DisallowMultipleComponent]
public class HoSoSkin : MonoBehaviour
{
    [Tooltip("Bỏ tick để về vỏ cũ.")]
    public bool batAo = false;

    private AvatarProfilePopupUI _popup;
    private GameObject _root;
    private bool _daAp;

    private void Awake() => _popup = GetComponent<AvatarProfilePopupUI>();

    private void Update()
    {
        if (!batAo || !Application.isPlaying || _popup == null || _daAp) return;

        if (_root == null)
        {
            _root = SkinVi.Lay<GameObject>(_popup, "popupRoot");
            if (_root == null) return;
        }
        if (!_root.activeInHierarchy) return;

        _daAp = true;
        MacAo();
    }

    private void MacAo()
    {
        Transform goc = _root.transform;

        // Ruy băng "HỒ SƠ" giữa mép trên tấm ván (ván = Image to nhất, thường là
        // art gỗ có sẵn — KHÔNG tô đè art, chỉ gắn ruy băng lên trên).
        var van = SkinVi.TimVanGo(goc);
        Transform chaRuyBang = van != null ? van.transform : goc;
        SkinKit.LamRuyBang(chaRuyBang, "HỒ SƠ", new Vector2(0.5f, 1f),
                           new Vector2(0f, 14f), new Vector2(430f, 98f));

        SkinVi.NutDong(SkinVi.Lay<Button>(_popup, "btnClose"));

        // Panel giấy bên phải = Image tổ tiên gần nhất của chữ "Cấp độ".
        var txtLevel = SkinVi.Lay<TMP_Text>(_popup, "txtLevel");
        if (txtLevel != null)
        {
            Image giay = null;
            for (var p = txtLevel.transform.parent; p != null && p != goc.parent; p = p.parent)
            {
                giay = p.GetComponent<Image>();
                if (giay != null) break;
            }
            if (giay != null && (van == null || giay != van))
                SkinKit.MacAoGiay(giay, 22f);

            txtLevel.color = TaskPopupDesign.ChuTieuDe;
            txtLevel.fontStyle = FontStyles.Bold;
        }
        // Chữ "Cấp độ 1 - 30": nâu rõ — TenBinhThuong nhạt quá, chìm trên giấy (ảnh 13/08).
        var txtRange = SkinVi.Lay<TMP_Text>(_popup, "txtLevelRange");
        if (txtRange != null) txtRange.color = TaskPopupDesign.Hex("#8d7550");

        // Ô tên: thẻ kem lõm, chữ nâu + nhãn "Tên nông trại" phía trên như mock.
        var oTen = SkinVi.Lay<TMP_InputField>(_popup, "inputPlayerName");
        var oTenImg = oTen != null ? oTen.GetComponent<Image>() : null;
        if (oTenImg != null)
        {
            SkinKit.MacAoThe(oTenImg, 16f);
            oTenImg.color = TaskPopupDesign.Hex("#fdf3da");
            if (oTen.textComponent != null)
                oTen.textComponent.color = TaskPopupDesign.ChuTieuDe;

            // Nhãn làm CON của chính ô tên, neo mép trên — toạ độ tương đối với ô
            // nên không thể bay sai chỗ (bản trước tính theo cha của ô, trượt lên
            // tận ruy băng vì neo của scene khác giả định).
            var oTenRt = (RectTransform)oTen.transform;
            if (oTenRt.Find("Skin_NhanTen") == null)
            {
                var go = new GameObject("Skin_NhanTen", typeof(RectTransform));
                var nrt = (RectTransform)go.transform;
                nrt.SetParent(oTenRt, false);
                nrt.anchorMin = new Vector2(0f, 1f);
                nrt.anchorMax = new Vector2(1f, 1f);
                nrt.pivot = new Vector2(0f, 0f);
                nrt.anchoredPosition = new Vector2(10f, 4f);
                nrt.sizeDelta = new Vector2(-20f, 26f);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = "Tên nông trại";
                tmp.fontSize = 22f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.BottomLeft;
                tmp.color = TaskPopupDesign.Hex("#8d7550");
                tmp.raycastTarget = false;
            }
        }

        // Thanh EXP: máng tan nhạt + ruột XANH LÁ thiết kế + số trắng.
        var ruot = SkinVi.Lay<Image>(_popup, "expFill");
        if (ruot != null)
        {
            ruot.color = TaskPopupDesign.TdRuotDuoi;
            var mang = ruot.transform.parent != null
                ? ruot.transform.parent.GetComponent<Image>() : null;
            if (mang != null)
            {
                mang.sprite = SkinKit.BoGoc(20f);
                mang.type = Image.Type.Sliced;
                mang.color = TaskPopupDesign.Hex("#e8d3ab");
            }
        }
        var soExp = SkinVi.Lay<TMP_Text>(_popup, "txtExpValue");
        if (soExp != null) soExp.color = Color.white;

        // Dải thống kê (Kho Cấp x · Điểm nấu ăn y): thẻ kem, chữ nâu.
        MacKhoi(SkinVi.Lay<GameObject>(_popup, "legacyStatsRoot"));

        // Panel thẻ hồ sơ do script game tự sinh nhưng LUÔN ghi chữ rỗng (nó nhường
        // hiển thị cho dải legacy) — tấm thẻ trống nổi đè lên dải thật trong ảnh
        // 13/08. Tắt phần NHÌN THẤY, không đụng SetActive (logic đang quản).
        var theRong = SkinVi.Lay<GameObject>(_popup, "profileCardsRoot");
        if (theRong != null)
            foreach (var anh in theRong.GetComponentsInChildren<Image>(true))
                anh.enabled = false;

        // Bảng chọn avatar (bật khi bấm ảnh): nền xám tối cũ → giấy kem.
        var chon = SkinVi.Lay<GameObject>(_popup, "avatarChoicesRoot");
        var chonImg = chon != null ? chon.GetComponent<Image>() : null;
        if (chonImg != null) SkinKit.MacAoGiay(chonImg, 18f);

        // Font tròn của mock cho toàn popup (không có font vỏ thì đây là no-op).
        SkinKit.ApFont(_root.transform);
    }

    private static void MacKhoi(GameObject khoi)
    {
        if (khoi == null) return;
        var img = khoi.GetComponent<Image>();
        if (img != null) SkinKit.MacAoThe(img, 16f);
        foreach (var the in khoi.GetComponentsInChildren<Image>(true))
            if (the != img && !the.transform.name.StartsWith("Skin_") && the.sprite == null)
                SkinKit.MacAoThe(the, 14f);
        foreach (var tmp in khoi.GetComponentsInChildren<TMP_Text>(true))
            if (!tmp.transform.parent.name.StartsWith("Skin_"))
                tmp.color = TaskPopupDesign.TenBinhThuong;
    }
}
