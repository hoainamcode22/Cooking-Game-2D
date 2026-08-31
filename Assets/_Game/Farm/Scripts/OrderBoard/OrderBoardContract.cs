using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HỢP ĐỒNG GIỮA DEV-A (dữ liệu/logic) VÀ DEV-B (giao diện) — mục 8 file
/// `production\TEAM_BANG_DON_HANG.md`.
///
/// VÌ SAO PHẢI CÓ FILE NÀY thay vì hẹn miệng tên hàm rồi mỗi người tự viết:
/// hai dev làm song song trên cùng một project KHÔNG có `.asmdef`. Nếu phần giao diện
/// gọi thẳng `OrderBoardManager.Instance.XXX()` mà file đó chưa tồn tại thì **toàn bộ
/// project không biên dịch được** — DEV-A cũng đứng luôn, không test được gì. Ngược lại
/// nếu DEV-A tự đặt tên hàm khác thì tới lúc ráp lại phải sửa hàng trăm dòng UI.
///
/// Cách giải: DEV-B sở hữu file hợp đồng này. Trong đó có sẵn:
///   • hai kiểu dữ liệu mô tả một đơn hàng;
///   • một lớp trừu tượng <see cref="OrderBoardManagerBase"/> khai báo ĐÚNG 4 hàm mà
///     giao diện cần, kèm singleton + sự kiện đổi bảng.
///
/// DEV-A chỉ việc viết `public class OrderBoardManager : OrderBoardManagerBase` rồi
/// override 4 hàm. Trong lúc chờ, `Instance` trả `null` và popup hiện lưới rỗng —
/// biên dịch vẫn sạch, không ai chặn ai.
/// </summary>
public static class OrderBoardContractDoc
{
    // Lớp rỗng cố ý: chỉ để chú thích tổng quan có chỗ bám. Không dùng ở đâu khác.
}

// ─────────────────────────────────────────────────────────────────────────────
//  1 · MỘT DÒNG YÊU CẦU TRONG ĐƠN
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Một món trong đơn: cần bao nhiêu, đang có bao nhiêu.
///
/// <see cref="ownedAmount"/> do GIAO DIỆN tự nạp lại mỗi lần vẽ bằng
/// <see cref="OrderBoardManagerBase.GetOwnedAmount"/>, DEV-A KHÔNG cần đồng bộ.
/// Lý do: kho thay đổi liên tục (thu hoạch, nấu ăn, mua bán). Nếu bắt bên logic phải
/// cập nhật lại từng đơn mỗi lần kho đổi thì chỉ cần quên một đường là con số
/// `có/cần` trên popup nói dối — mà đó lại đúng là con số người chơi dựa vào để quyết
/// định có giao hay không.
/// </summary>
[Serializable]
public class OrderBoardRequirementView
{
    [Tooltip("Khớp với itemId trong MarketPriceTable / kho.")]
    public string itemId;

    [Tooltip("Tên hiển thị. Bỏ trống thì giao diện tự tra MarketPriceTable.")]
    public string displayName;

    [Tooltip("Số lượng đơn hàng yêu cầu — vế PHẢI của `có/cần`.")]
    public int needAmount;

    [Tooltip("Số lượng đang có trong kho — vế TRÁI của `có/cần`. Giao diện tự nạp.")]
    public int ownedAmount;

    /// <summary>Đủ hàng cho riêng món này chưa.</summary>
    public bool IsEnough => ownedAmount >= needAmount;

    /// <summary>Tên để hiện lên ô yêu cầu, có đường lui khi DEV-A bỏ trống.</summary>
    public string ResolveDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;

        string fromTable = MarketPriceTable.GetDisplayName(itemId);
        return string.IsNullOrEmpty(fromTable) ? itemId : fromTable;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  2 · MỘT ĐƠN HÀNG
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Một đơn hàng như GIAO DIỆN nhìn thấy. Cố ý là "view" chứ không phải model gốc của
/// DEV-A: bên logic muốn thêm bao nhiêu trường nội bộ (bậc khó, hạt giống ngẫu nhiên,
/// dấu thời gian lưu…) cũng được, miễn đổ ra được mấy trường dưới đây.
/// </summary>
[Serializable]
public class OrderBoardOrderView
{
    [Tooltip("Khoá duy nhất. Giao diện chỉ cầm chuỗi này khi gọi giao/bỏ đơn.")]
    public string orderId;

    [Tooltip("Tên đơn — kho tên 300+ của DEV-A (mục 5.2 file TEAM).")]
    public string title;

    [Tooltip("Mã khách hàng. Chờ art; tạm thời giao diện tô màu theo mã để mỗi khách một sắc.")]
    public string customerAvatarId;

    [Tooltip("Vàng thưởng — hiện trên phiếu (B5) và ô thưởng cột phải (B6).")]
    public int rewardGold;

    [Tooltip("EXP thưởng — ĐÃ là con số cuối cùng, giao diện KHÔNG nhân thêm.")]
    public int rewardExp;

    [Tooltip("Tối đa 6 món — lưới yêu cầu cột phải là 3x2.")]
    public List<OrderBoardRequirementView> requirements = new List<OrderBoardRequirementView>();

    /// <summary>
    /// Nạp lại `ownedAmount` cho mọi món từ kho.
    ///
    /// Gọi ngay trước khi vẽ. Tách riêng thành hàm chứ không nhét vào getter vì
    /// mỗi lần vẽ lưới 3x3 là 9 đơn × tối đa 4 món = 36 lượt tra kho; gom vào một
    /// chỗ thì sau này muốn cache cũng chỉ sửa đúng đây.
    /// </summary>
    public void RefreshOwnedAmounts()
    {
        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        if (requirements == null) return;

        for (int i = 0; i < requirements.Count; i++)
        {
            OrderBoardRequirementView r = requirements[i];
            if (r == null) continue;
            r.ownedAmount = board != null ? board.GetOwnedAmount(r.itemId) : 0;
        }
    }

    /// <summary>
    /// Đủ hàng cho TOÀN BỘ đơn chưa — quyết định phiếu xanh hay trắng ngà (B4)
    /// và phiếu ghim ngoài map xanh hay trắng (B2).
    ///
    /// Hỏi thẳng kho chứ không đọc `ownedAmount` đã lưu: hàm này còn được phiếu ghim
    /// ngoài map gọi lúc popup đang đóng, khi đó chưa ai nạp `ownedAmount` cả.
    /// </summary>
    public bool CanDeliverNow()
    {
        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        if (board == null) return false;
        if (requirements == null || requirements.Count == 0) return false;

        for (int i = 0; i < requirements.Count; i++)
        {
            OrderBoardRequirementView r = requirements[i];
            if (r == null) continue;
            if (board.GetOwnedAmount(r.itemId) < r.needAmount) return false;
        }

        return true;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  3 · BỐN HÀM DEV-B CẦN TỪ DEV-A
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Lớp cha của `OrderBoardManager` (DEV-A viết).
///
/// Singleton + sự kiện được hiện thực sẵn ở đây để DEV-A không phải chép lại đoạn
/// `Instance = this` quen thuộc — và quan trọng hơn: để giao diện chỉ phụ thuộc vào
/// ĐÚNG một cái tên duy nhất, dù bên logic có đổi tên lớp con thành gì.
/// </summary>
public abstract class OrderBoardManagerBase : MonoBehaviour
{
    /// <summary>Số ô trên lưới phiếu (3x3) — mục 5.4 file TEAM: luôn giữ đủ 9 đơn.</summary>
    public const int SlotCount = 9;

    /// <summary>Số ô trên lưới yêu cầu cột phải (3x2).</summary>
    public const int MaxRequirementSlots = 6;

    public static OrderBoardManagerBase Instance { get; private set; }

    /// <summary>
    /// DEV-A bắn sự kiện này SAU MỖI lần bảng đổi (sinh đơn, giao, bỏ).
    /// Popup và phiếu ghim ngoài map đều nghe — nhờ vậy hai chỗ không bao giờ
    /// hiện hai con số khác nhau về cùng một bảng.
    /// </summary>
    public event Action OnBoardChanged;

    protected virtual void Awake()
    {
        // Có sẵn một cái khác rồi thì tự huỷ COMPONENT (không huỷ GameObject): object
        // chứa nó có thể còn mang thứ khác của scene, huỷ cả object là mất oan.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[BảngĐơn] Có hơn một OrderBoardManager trong scene — giữ cái đầu tiên.");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>DEV-A gọi sau khi đổi nội dung bảng.</summary>
    protected void RaiseBoardChanged() => OnBoardChanged?.Invoke();

    // ── 4 HÀM BẮT BUỘC ───────────────────────────────────────────────────────

    /// <summary>(1) Danh sách đơn đang treo. Thứ tự trả về = thứ tự ô trên lưới 3x3.</summary>
    public abstract IReadOnlyList<OrderBoardOrderView> GetOrders();

    /// <summary>(2) Số lượng một món đang có trong kho — vế trái của `có/cần` (B7).</summary>
    public abstract int GetOwnedAmount(string itemId);

    /// <summary>
    /// (3) Giao đơn: trừ kho NGUYÊN TỬ, cộng vàng/EXP, bắn hook nhiệm vụ.
    /// Trả false kèm <paramref name="failReason"/> để giao diện hiện thông báo
    /// thay vì im lặng — người chơi bấm mà không thấy gì xảy ra là lỗi tệ nhất.
    /// </summary>
    public abstract bool TryDeliverOrder(string orderId, out string failReason);

    /// <summary>(4) Bỏ đơn (nút thùng rác) rồi sinh đơn mới lấp chỗ.</summary>
    public abstract bool DiscardOrder(string orderId);
}

// ─────────────────────────────────────────────────────────────────────────────
//  4 · TRA ICON VẬT PHẨM
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tìm icon cho một `itemId`.
///
/// Dùng lại <c>StallItemCatalog</c> đã có sẵn (nó quét CropData + InventoryItemData lúc
/// build scene) thay vì viết bộ tra thứ hai: hai bảng icon song song là con đường ngắn
/// nhất tới cảnh cùng một củ cà rốt hiện hai hình khác nhau ở hai màn hình.
/// Không có catalog trong scene thì trả null — ô yêu cầu tự lùi về khối màu phẳng,
/// vẫn đọc được tên và con số `có/cần`.
/// </summary>
public static class OrderBoardIconResolver
{
    public static Sprite GetIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        StallItemCatalog catalog = StallItemCatalog.Instance;
        return catalog != null ? catalog.GetIcon(itemId) : null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  AVATAR KHÁCH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bảng ảnh khách hàng, do <see cref="OrderBoardPopupUI"/> nạp vào lúc bật popup.
    ///
    /// Để ở đây thay vì để riêng trong popup vì tờ phiếu ngoài lưới sau này cũng có thể
    /// muốn vẽ mặt khách — hai nơi tra chung một bảng thì không bao giờ lệch. Bảng rỗng
    /// (chưa ai gắn art) không phải là lỗi: chỗ gọi tự lùi về <see cref="TintFromId"/>.
    /// </summary>
    private static Dictionary<string, Sprite> _avatarTheoMa;

    /// <summary>Popup gọi lúc Awake. Truyền null hoặc mảng rỗng = xoá bảng, quay về tô màu.</summary>
    public static void DangKyAvatar(IEnumerable<KeyValuePair<string, Sprite>> bang)
    {
        if (bang == null) { _avatarTheoMa = null; return; }

        Dictionary<string, Sprite> moi = null;
        foreach (var cap in bang)
        {
            // Bỏ qua ô trống: người dùng gắn 5/12 con thì 5 con đó có ảnh, 7 con còn lại
            // vẫn tô màu — không bắt phải vẽ đủ bộ mới được dùng con nào.
            if (string.IsNullOrEmpty(cap.Key) || cap.Value == null) continue;
            (moi ??= new Dictionary<string, Sprite>())[cap.Key] = cap.Value;
        }

        _avatarTheoMa = moi;
    }

    /// <summary>Ảnh khách theo mã, chưa gắn art thì null.</summary>
    /// <summary>Ảnh khách theo mã, tự động nạp từ Resources/Avatars nếu chưa gán tay.</summary>
    public static Sprite GetAvatar(string customerId)
    {
        if (!string.IsNullOrEmpty(customerId) && _avatarTheoMa != null && _avatarTheoMa.TryGetValue(customerId, out Sprite s) && s != null)
            return s;

        if (string.IsNullOrEmpty(customerId)) return null;

        // Fallback tự động nạp từ bộ 8 avatar trong Resources/Avatars
        int idx = System.Array.IndexOf(OrderNameBank.CustomerIds, customerId);
        if (idx < 0) idx = Mathf.Abs(customerId.GetHashCode()) % 8;
        else idx = idx % 8;

        return Resources.Load<Sprite>($"Avatars/avatar_npc_{idx}");
    }

    /// <summary>
    /// Màu nhận dạng suy ra từ chuỗi — dùng cho ô art chưa có ảnh (avatar khách, icon
    /// vật phẩm thiếu icon). Cùng một id luôn ra cùng một màu, nên nhìn vẫn phân biệt
    /// được khách này với khách kia dù chưa ai vẽ gì.
    /// </summary>
    public static Color TintFromId(string id, float saturation = 0.45f, float value = 0.85f)
    {
        if (string.IsNullOrEmpty(id)) return new Color(0.62f, 0.62f, 0.62f, 1f);

        // Hash ổn định tự viết, KHÔNG dùng string.GetHashCode(): giá trị của nó không
        // được bảo đảm giống nhau giữa các phiên chạy/nền tảng, màu avatar sẽ nhảy lung tung.
        unchecked
        {
            uint h = 2166136261u;
            for (int i = 0; i < id.Length; i++)
            {
                h ^= id[i];
                h *= 16777619u;
            }

            float hue = (h % 1000u) / 1000f;
            return Color.HSVToRGB(hue, saturation, value);
        }
    }
}
