using UnityEngine;

/// <summary>
/// BỘ Ô ART CHO HỆ THỐNG XÂY DỰNG
/// ═══════════════════════════════
/// Tạo asset: chuột phải ▸ Create ▸ FarmGame ▸ Construction Art Kit
/// Rồi kéo vào ô "Art Kit" của ConstructionManager trong scene.
///
/// CÁCH DÙNG: mỗi field dưới đây là MỘT Ô TRỐNG. Để trống thì hệ thống vẽ
/// hình thủ tục bằng code, **tô đúng màu nhận dạng ghi trong tooltip** — nhìn
/// màu là biết ô nào. Thả sprite của bạn vào là nó thay ngay, không cần sửa code.
///
/// Bật <see cref="showSlotLabels"/> để hiện tên ô đè lên từng mảnh trong Scene.
/// </summary>
[CreateAssetMenu(fileName = "ConstructionArtKit", menuName = "FarmGame/Construction Art Kit")]
public class ConstructionArtKit : ScriptableObject
{
    // ════════════════════════════════════════════════════════════════════
    // BẢNG MÀU NHẬN DẠNG — mỗi ô một màu riêng, không trùng nhau
    // ════════════════════════════════════════════════════════════════════
    public static readonly Color C_Ground   = new Color(0.55f, 0.38f, 0.22f, 0.75f); // NÂU ĐẤT
    public static readonly Color C_Post     = new Color(0.85f, 0.45f, 0.15f, 1f);    // CAM
    public static readonly Color C_Rail     = new Color(0.95f, 0.75f, 0.20f, 1f);    // VÀNG
    public static readonly Color C_Brace    = new Color(0.60f, 0.85f, 0.30f, 1f);    // XANH LÁ MẠ
    public static readonly Color C_Board    = new Color(0.25f, 0.75f, 0.70f, 1f);    // XANH NGỌC
    public static readonly Color C_Worker   = new Color(0.30f, 0.55f, 0.95f, 1f);    // XANH DƯƠNG
    public static readonly Color C_NamePlate= new Color(0.55f, 0.35f, 0.85f, 0.85f); // TÍM
    public static readonly Color C_TimerBar = new Color(0.15f, 0.15f, 0.18f, 0.85f); // ĐEN XÁM
    public static readonly Color C_Clock    = new Color(0.95f, 0.95f, 0.95f, 1f);    // TRẮNG
    public static readonly Color C_RushBtn  = new Color(0.30f, 0.80f, 0.20f, 1f);    // XANH LÁ ĐẬM
    public static readonly Color C_CoinIcon = new Color(1.00f, 0.80f, 0.15f, 1f);    // VÀNG KIM
    public static readonly Color C_GemIcon  = new Color(0.45f, 0.85f, 0.95f, 1f);    // XANH KIM CƯƠNG
    public static readonly Color C_PriceBar = new Color(0.10f, 0.10f, 0.12f, 0.88f); // ĐEN
    public static readonly Color C_GiftBox  = new Color(0.95f, 0.95f, 0.92f, 1f);    // TRẮNG NGÀ
    public static readonly Color C_Ribbon   = new Color(0.90f, 0.25f, 0.45f, 1f);    // HỒNG ĐẬM
    public static readonly Color C_Balloon  = new Color(0.95f, 0.30f, 0.30f, 1f);    // ĐỎ
    public static readonly Color C_HardHat  = new Color(1.00f, 0.85f, 0.10f, 1f);    // VÀNG MŨ

    // ════════════════════════════════════════════════════════════════════
    [Header("◆ CHẾ ĐỘ DỰNG NỀN")]
    [Tooltip("BẬT để hiện tên từng ô đè lên mảnh tương ứng trong Scene.\n" +
             "Nhìn nhãn + màu là biết chỗ nào cần thả art. NHỚ TẮT trước khi build.")]
    public bool showSlotLabels = false;

    [Tooltip("BẬT để tô màu nhận dạng cả khi ĐÃ có sprite — dùng lúc căn chỉnh vị trí.")]
    public bool forcePlaceholderColors = false;

    // ════════════════════════════════════════════════════════════════════
    [Header("◆ CÔNG TRƯỜNG — mặt đất & giàn giáo")]

    [Tooltip("NÂU ĐẤT — thảm đất lộ ra dưới chân công trường. Phủ đúng N×M ô.")]
    public Sprite groundPatch;

    [Tooltip("CAM — cọc gỗ dựng đứng của giàn giáo. Sprite dọc, pivot giữa.")]
    public Sprite scaffoldPost;

    [Tooltip("VÀNG — thanh gỗ ngang nối các cọc. Sprite ngang, pivot giữa.")]
    public Sprite scaffoldRail;

    [Tooltip("XANH LÁ MẠ — thanh chống chéo hai bên giàn giáo.")]
    public Sprite scaffoldBrace;

    [Tooltip("XANH NGỌC — tấm ván dựa nghiêng vào giàn giáo.")]
    public Sprite leaningBoard;

    [Tooltip("XANH DƯƠNG — công nhân. Nên là sprite có animation riêng (Animator) " +
             "thay vì sprite tĩnh, xem mục Prefab công nhân bên dưới.")]
    public Sprite worker;

    [Tooltip("Thay cả công nhân bằng PREFAB (có Animator, hiệu ứng búa...). " +
             "Nếu gán thì bỏ qua ô 'Worker' phía trên.")]
    public GameObject workerPrefab;

    [Tooltip("Hạt bụi/khói bay lên. Để trống = dùng hạt tròn mờ vẽ bằng code.")]
    public Sprite dustParticle;

    // ════════════════════════════════════════════════════════════════════
    [Header("◆ UI NỔI TRÊN ĐẦU CÔNG TRƯỜNG")]

    [Tooltip("TÍM — nền sau TÊN công trình. Để trống = chỉ có chữ, không nền. " +
             "Nên dùng sprite 9-slice.")]
    public Sprite namePlateBg;

    [Tooltip("ĐEN XÁM — nền thanh đếm ngược. Sprite 9-slice bo góc.")]
    public Sprite timerBarBg;

    [Tooltip("TRẮNG — icon đồng hồ bên trái thời gian.")]
    public Sprite clockIcon;

    [Tooltip("XANH LÁ ĐẬM — nền nút tăng tốc. Sprite 9-slice bo góc.")]
    public Sprite rushButtonBg;

    [Tooltip("VÀNG KIM — icon xu trên nút tăng tốc (khi trừ bằng vàng).")]
    public Sprite coinIcon;

    [Tooltip("XANH KIM CƯƠNG — icon kim cương (khi trừ bằng gem).")]
    public Sprite gemIcon;

    // ════════════════════════════════════════════════════════════════════
    [Header("◆ THANH GIÁ LÚC ĐẶT (\"MUA VỚI GIÁ ...\")")]

    [Tooltip("ĐEN — nền thanh giá phía trên 3 nút ✕ ↻ ✓.")]
    public Sprite priceBarBg;

    // ════════════════════════════════════════════════════════════════════
    [Header("◆ HIỆU ỨNG HOÀN THÀNH")]

    [Tooltip("TRẮNG NGÀ — mặt hộp quà bọc công trình lúc khánh thành.")]
    public Sprite giftBoxSide;

    [Tooltip("HỒNG ĐẬM — dải ruy băng quấn quanh hộp.")]
    public Sprite ribbon;

    [Tooltip("HỒNG ĐẬM — hoa hồng ruy băng gắn 3 mặt hộp.")]
    public Sprite rosette;

    [Tooltip("ĐỎ — bóng bay. Màu sẽ được đổi ngẫu nhiên đỏ/vàng/xanh lúc chạy.")]
    public Sprite balloon;

    [Tooltip("VÀNG MŨ — icon mũ bảo hộ + tick xanh bật lên khi xây xong.")]
    public Sprite hardHatDone;

    // ════════════════════════════════════════════════════════════════════
    // API
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Định danh từng ô — dùng cho nhãn debug và tra màu.</summary>
    public enum Slot
    {
        GroundPatch, ScaffoldPost, ScaffoldRail, ScaffoldBrace, LeaningBoard,
        Worker, DustParticle,
        NamePlateBg, TimerBarBg, ClockIcon, RushButtonBg, CoinIcon, GemIcon,
        PriceBarBg,
        GiftBoxSide, Ribbon, Rosette, Balloon, HardHatDone
    }

    /// <summary>Sprite của một ô. Trả null nếu chưa gán → nơi gọi tự vẽ thủ tục.</summary>
    public Sprite GetSprite(Slot slot) => slot switch
    {
        Slot.GroundPatch   => groundPatch,
        Slot.ScaffoldPost  => scaffoldPost,
        Slot.ScaffoldRail  => scaffoldRail,
        Slot.ScaffoldBrace => scaffoldBrace,
        Slot.LeaningBoard  => leaningBoard,
        Slot.Worker        => worker,
        Slot.DustParticle  => dustParticle,
        Slot.NamePlateBg   => namePlateBg,
        Slot.TimerBarBg    => timerBarBg,
        Slot.ClockIcon     => clockIcon,
        Slot.RushButtonBg  => rushButtonBg,
        Slot.CoinIcon      => coinIcon,
        Slot.GemIcon       => gemIcon,
        Slot.PriceBarBg    => priceBarBg,
        Slot.GiftBoxSide   => giftBoxSide,
        Slot.Ribbon        => ribbon,
        Slot.Rosette       => rosette,
        Slot.Balloon       => balloon,
        Slot.HardHatDone   => hardHatDone,
        _                  => null
    };

    /// <summary>Màu nhận dạng của một ô.</summary>
    public static Color ColorOf(Slot slot) => slot switch
    {
        Slot.GroundPatch   => C_Ground,
        Slot.ScaffoldPost  => C_Post,
        Slot.ScaffoldRail  => C_Rail,
        Slot.ScaffoldBrace => C_Brace,
        Slot.LeaningBoard  => C_Board,
        Slot.Worker        => C_Worker,
        Slot.DustParticle  => C_Ground,
        Slot.NamePlateBg   => C_NamePlate,
        Slot.TimerBarBg    => C_TimerBar,
        Slot.ClockIcon     => C_Clock,
        Slot.RushButtonBg  => C_RushBtn,
        Slot.CoinIcon      => C_CoinIcon,
        Slot.GemIcon       => C_GemIcon,
        Slot.PriceBarBg    => C_PriceBar,
        Slot.GiftBoxSide   => C_GiftBox,
        Slot.Ribbon        => C_Ribbon,
        Slot.Rosette       => C_Ribbon,
        Slot.Balloon       => C_Balloon,
        Slot.HardHatDone   => C_HardHat,
        _                  => Color.magenta
    };

    /// <summary>Tên hiển thị tiếng Việt của ô — dùng cho nhãn debug.</summary>
    public static string LabelOf(Slot slot) => slot switch
    {
        Slot.GroundPatch   => "Thảm đất",
        Slot.ScaffoldPost  => "Cọc giàn giáo",
        Slot.ScaffoldRail  => "Thanh ngang",
        Slot.ScaffoldBrace => "Thanh chống",
        Slot.LeaningBoard  => "Ván dựa",
        Slot.Worker        => "Công nhân",
        Slot.DustParticle  => "Hạt bụi",
        Slot.NamePlateBg   => "Nền tên",
        Slot.TimerBarBg    => "Nền đồng hồ",
        Slot.ClockIcon     => "Icon đồng hồ",
        Slot.RushButtonBg  => "Nền nút rush",
        Slot.CoinIcon      => "Icon xu",
        Slot.GemIcon       => "Icon kim cương",
        Slot.PriceBarBg    => "Nền thanh giá",
        Slot.GiftBoxSide   => "Mặt hộp quà",
        Slot.Ribbon        => "Ruy băng",
        Slot.Rosette       => "Hoa ruy băng",
        Slot.Balloon       => "Bóng bay",
        Slot.HardHatDone   => "Mũ bảo hộ",
        _                  => slot.ToString()
    };

    /// <summary>
    /// Lấy sprite + màu cho một ô, xử lý sẵn mọi trường hợp.
    /// Nơi gọi chỉ cần: `kit.Resolve(slot, fallbackSprite, out var spr, out var col)`.
    /// </summary>
    /// <param name="slot">Ô cần lấy.</param>
    /// <param name="proceduralFallback">Hình vẽ bằng code, dùng khi ô còn trống.</param>
    /// <param name="sprite">Sprite kết quả.</param>
    /// <param name="color">Màu tint kết quả.</param>
    /// <returns>true nếu đang dùng ART THẬT của bạn, false nếu đang là placeholder.</returns>
    public bool Resolve(Slot slot, Sprite proceduralFallback, out Sprite sprite, out Color color)
    {
        Sprite mine = GetSprite(slot);

        if (mine != null)
        {
            sprite = mine;
            // Có art thật → để trắng (không tint) trừ khi đang bật chế độ căn chỉnh
            color  = forcePlaceholderColors ? ColorOf(slot) : Color.white;
            return true;
        }

        sprite = proceduralFallback;
        color  = ColorOf(slot);
        return false;
    }

    /// <summary>
    /// Bản tĩnh — dùng được cả khi kit chưa gán (kit == null).
    /// Không có kit thì mọi thứ đều là placeholder có màu.
    /// </summary>
    public static bool ResolveSafe(ConstructionArtKit kit, Slot slot, Sprite fallback,
                                   out Sprite sprite, out Color color)
    {
        if (kit != null) return kit.Resolve(slot, fallback, out sprite, out color);

        sprite = fallback;
        color  = ColorOf(slot);
        return false;
    }

    /// <summary>Có cần hiện nhãn tên ô không? An toàn với kit null.</summary>
    public static bool WantLabels(ConstructionArtKit kit) => kit != null && kit.showSlotLabels;

#if UNITY_EDITOR
    /// <summary>Đếm số ô đã gán / tổng số ô — hiện trong Inspector cho dễ theo dõi.</summary>
    public string ProgressText()
    {
        int done = 0, total = 0;
        foreach (Slot s in System.Enum.GetValues(typeof(Slot)))
        {
            total++;
            if (GetSprite(s) != null) done++;
        }
        if (workerPrefab != null && worker == null) done++;   // prefab thay cho sprite
        return $"Đã gán {done}/{total} ô art";
    }
#endif
}
