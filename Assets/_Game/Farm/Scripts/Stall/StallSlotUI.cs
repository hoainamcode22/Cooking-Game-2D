using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MỘT Ô TRÊN LƯỚI QUẦY — biết vẽ đủ BỐN trạng thái (B3).
///
/// Bốn nhánh trạng thái được dựng sẵn thành bốn nhánh con trong prefab rồi bật/tắt,
/// KHÔNG dựng bằng code lúc chạy. Nhờ vậy chủ dự án mở prefab ra là kéo art vào được
/// từng trạng thái mà không phải đọc một dòng C# nào — đúng bài học rút ra từ
/// `UnifiedTaskPopupUI` (1433 dòng dựng UI bằng code, hardcode 200 toạ độ).
/// </summary>
public class StallSlotUI : MonoBehaviour
{
    [Header("Bốn nhánh trạng thái (bật/tắt, không dựng bằng code)")]
    [SerializeField] private GameObject stateEmptyRoot;
    [SerializeField] private GameObject stateSellingRoot;
    [SerializeField] private GameObject stateUnlockableRoot;
    [SerializeField] private GameObject stateLockedRoot;

    [Header("Trạng thái TRỐNG")]
    [SerializeField] private Button   buttonSell;
    [SerializeField] private TMP_Text textEmptyLabel;

    [Header("Trạng thái ĐANG BÁN")]
    [SerializeField] private Image    imageItemIcon;
    [SerializeField] private TMP_Text textQuantity;
    [SerializeField] private TMP_Text textPrice;
    [SerializeField] private TMP_Text textRemainTime;
    [SerializeField] private GameObject loaBadge;
    [SerializeField] private Button   buttonCancel;

    [Header("Trạng thái KHOÁ — MỞ ĐƯỢC")]
    [SerializeField] private Button   buttonUnlock;
    [SerializeField] private TMP_Text textUnlockCost;
    [SerializeField] private TMP_Text textUnlockLabel;

    [Header("Chỗ chờ art")]
    [Tooltip("Ảnh nền phẳng của ô — chủ dự án thay bằng art sau.")]
    [SerializeField] private Image imageArtSlotBackground;

    private StallPopupUI _owner;
    private int          _slotIndex = -1;

    public int SlotIndex => _slotIndex;

    /// <summary>Gắn ô vào popup. Gọi một lần sau khi Instantiate prefab.</summary>
    public void Bind(StallPopupUI owner, int slotIndex)
    {
        _owner     = owner;
        _slotIndex = slotIndex;

        // RemoveAllListeners trước: prefab có thể đã lưu sẵn listener từ Inspector, và
        // ô được dùng lại giữa các lần mở popup — không dọn thì mỗi lần mở lại chồng
        // thêm một listener và một cú bấm sẽ mở panel chọn vật phẩm nhiều lần.
        if (buttonSell != null)
        {
            buttonSell.onClick.RemoveAllListeners();
            buttonSell.onClick.AddListener(OnClickSell);
        }

        if (buttonUnlock != null)
        {
            buttonUnlock.onClick.RemoveAllListeners();
            buttonUnlock.onClick.AddListener(OnClickUnlock);
        }

        if (buttonCancel != null)
        {
            buttonCancel.onClick.RemoveAllListeners();
            buttonCancel.onClick.AddListener(OnClickCancel);
        }

        Refresh();
    }

    private void OnClickSell()   { if (_owner != null) _owner.OnSlotRequestSell(_slotIndex); }
    private void OnClickUnlock() { if (_owner != null) _owner.OnSlotRequestUnlock(_slotIndex); }
    private void OnClickCancel() { if (_owner != null) _owner.OnSlotRequestCancel(_slotIndex); }

    /// <summary>Vẽ lại ô theo trạng thái hiện tại của quầy.</summary>
    public void Refresh()
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null || _slotIndex < 0)
        {
            ApplyState(StallSlotState.Locked);
            return;
        }

        StallSlotState state = stall.GetSlotState(_slotIndex);
        ApplyState(state);

        switch (state)
        {
            case StallSlotState.Empty:
                if (textEmptyLabel != null) textEmptyLabel.text = Loc.T("Bán vật phẩm");
                break;

            case StallSlotState.Selling:
                FillSelling(stall.GetListingAtSlot(_slotIndex));
                break;

            case StallSlotState.Unlockable:
                if (textUnlockLabel != null) textUnlockLabel.text = stall.DuCapMoO(_slotIndex) ? Loc.T("Thêm ô") : Loc.TF("Cần cấp {0}", stall.GetSlotRequiredLevel(_slotIndex));
                if (textUnlockCost  != null) textUnlockCost.text  = stall.GetSlotUnlockGoldCost(_slotIndex).ToString("N0");
                break;

            case StallSlotState.Locked:
                // Ô trơn — cố ý không có chữ, để người chơi đọc lưới là thấy ngay
                // "còn xa mới tới lượt", khác hẳn ô đang mời mở khoá ngay bên cạnh.
                break;
        }
    }

    private void FillSelling(PlayerListing listing)
    {
        if (listing == null) return;

        StallItemCatalog catalog = StallItemCatalog.Instance;

        if (imageItemIcon != null)
        {
            Sprite icon = catalog != null ? catalog.GetIcon(listing.itemId) : null;
            imageItemIcon.sprite = icon;
            // Icon null thì giữ ô màu phẳng thay vì hiện hình trắng vô nghĩa —
            // vẫn đọc được số lượng và giá bên dưới.
            imageItemIcon.enabled = icon != null;
        }

        if (textQuantity != null) textQuantity.text = "x" + listing.quantity;
        if (textPrice    != null) textPrice.text    = listing.TotalPrice.ToString("N0");
        if (loaBadge     != null) loaBadge.SetActive(listing.hasLoa);

        if (textRemainTime != null)
            textRemainTime.text = FormatRemaining(listing.RemainingSeconds(System.DateTime.UtcNow));
    }

    private void ApplyState(StallSlotState state)
    {
        SetActiveSafe(stateEmptyRoot,      state == StallSlotState.Empty);
        SetActiveSafe(stateSellingRoot,    state == StallSlotState.Selling);
        SetActiveSafe(stateUnlockableRoot, state == StallSlotState.Unlockable);
        SetActiveSafe(stateLockedRoot,     state == StallSlotState.Locked);

        if (imageArtSlotBackground != null)
        {
            // Ô chưa tới lượt chìm hẳn xuống nền; ô dùng được thì nổi lên.
            // [2026-09-23] BO tint tim toi: sprite goc la the go bo tron dep; tint bien no thanh
            // o vuong tim den, chu nau tren do khong doc duoc. O chua toi luot chi mo di.
            imageArtSlotBackground.color = state == StallSlotState.Locked
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;
        }
        // [2026-09-23 FIX VO CHU] KHONG dung outlineWidth/outlineColor: goi 2 thuoc tinh do tao
        // ra material rieng gan voi atlas cua font CU; ngay sau do SkinKit.ApFont doi font =>
        // material tro sai atlas => chu vo thanh vun trang. Chi doi mau + dam (khong dung material).
        var sang = new Color(1f, 0.97f, 0.88f, 1f);
        foreach (var t in new[] { textEmptyLabel, textUnlockLabel })
        {
            if (t == null) continue;
            t.color = sang; t.fontStyle |= TMPro.FontStyles.Bold;
        }
        // [2026-09-25] Sep: "text sang len". Gia / so luong / gio con lai tren the cam truoc day nau nhat -> kem sang, dam.
        foreach (var t in new[] { textPrice, textQuantity })
        {
            if (t == null) continue;
            t.color = sang; t.fontStyle |= TMPro.FontStyles.Bold;
        }
        if (textRemainTime != null) { textRemainTime.color = new Color(1f, 0.93f, 0.78f, 1f); textRemainTime.fontStyle |= TMPro.FontStyles.Bold; }
        CanGiuaIcon();
    }

    private bool _daCanIcon;
    /// <summary>[2026-09-25] Icon vat pham nam CHINH GIUA the (truoc lech len tren). Giu nguyen kich thuoc.</summary>
    private void CanGiuaIcon()
    {
        if (_daCanIcon || imageItemIcon == null) return;
        _daCanIcon = true;
        var rt = imageItemIcon.rectTransform;
        var cha = rt.parent as RectTransform;
        Vector2 kt = rt.rect.size;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = kt;
        rt.anchoredPosition = new Vector2(0f, cha != null ? cha.rect.height * 0.04f : 8f);   // nhich len chut, chua cho dong gia o duoi
        imageItemIcon.preserveAspect = true;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }

    /// <summary>"2g 05p" / "38p" / "45 giây" — đủ chính xác cho thứ tính bằng giờ.</summary>
    public static string FormatRemaining(double seconds)
    {
        if (seconds <= 0) return Loc.T("Hết hạn");

        int total = Mathf.CeilToInt((float)seconds);
        int hours = total / 3600;
        int mins  = (total % 3600) / 60;

        // $"..." ghép thẳng KHÔNG BAO GIỜ khớp khoá trong bảng dịch (khoá là chuỗi MẪU
        // còn nguyên {0}), nên hai dòng này trước đây luôn hiện "2g 05p" kể cả khi game
        // đang chạy tiếng Anh. Loc.TF tra MẪU trước rồi mới Format.
        if (hours > 0) return Loc.TF("{0}g {1:00}p", hours, mins);
        if (mins  > 0) return Loc.TF("{0}p", mins);
        return Loc.TF("{0} giây", total);
    }
}
