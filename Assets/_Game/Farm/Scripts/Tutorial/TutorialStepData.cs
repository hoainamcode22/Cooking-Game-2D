using UnityEngine;

public enum TutorialWaitAction
{
    WaitForClick,              // Đợi player bấm popup / bấm Next
    WaitForHarvest,            // NotifyHarvest()
    WaitForPlant,              // NotifyPlant()
    WaitForCook,               // NotifyCook()
    WaitForDelivery,           // NotifyDelivery() — giao hàng cho nhà dân
    WaitForBuyItem,            // NotifyBuyItem()  — mua vật phẩm trong shop
    WaitForBuyAnimal,          // NotifyBuyAnimal() — mua gia súc
    WaitForTrainLoad,          // NotifyTrainLoad() — giao hàng cho tàu hoả
    Auto,                      // Tự chuyển sau khi hiện text xong
    WaitForAllPlotsPlanted,         // NotifyAllPlotsPlanted() — tất cả ô lúa tutorial đã trồng
    WaitForAllPlotsHarvested,       // NotifyAllPlotsHarvested() — tất cả ô lúa tutorial đã thu hoạch
    WaitForAllFlowerPlotsPlanted,   // NotifyAllFlowerPlotsPlanted() — tất cả chậu hoa đã trồng
    WaitForAllFlowerPlotsHarvested, // NotifyAllFlowerPlotsHarvested() — tất cả chậu hoa đã thu hoạch
    WaitForOpenCropProcess,         // NotifyOpenCropProcess() — player mở CropProcessPopup
    WaitForSpeedUp,                 // NotifySpeedUp() — player dùng gem speed-up
    WaitForSickleShown,             // NotifySickleShown() — liềm tray đã hiện
    WaitForSeedPanel,
    WaitForOpenShop,            // ShopManager.OpenShop() — tutorial L2: mở shop
    WaitForCloseShop,           // ShopManager.CloseShop() — tutorial L2: đóng shop
    WaitForOpenPen,             // PenMiniPanelUI.OpenPanel() — tutorial L2: mở chuồng
    WaitForFeed,                // PenMiniPanelUI.TryFeed() OK — đã cho ăn
    WaitForPenSpeedUp,          // PenMiniPanelUI.TrySpeedUpGem() OK — dùng gem hoàn tất chuồng
    WaitForPenHarvest,          // PenMiniPanelUI.TryHarvest() OK — đã thu hoạch chuồng
}

[CreateAssetMenu(fileName = "TutorialStep_00", menuName = "FarmGame/Tutorial/Tutorial Step")]
public class TutorialStepData : ScriptableObject
{
    [Header("NPC Dialog")]
    [TextArea(2, 5)]
    public string npcText = "Chào mừng đến với nông trại!";

    [Tooltip("Sprite chân dung NPC ")]
    public Sprite npcPortrait;

    [Tooltip("Tốc độ gõ từng ký tự (giây/ký tự)")]
    [Range(0.01f, 0.15f)]
    public float typingSpeed = 0.04f;

    [Header("Target & Highlight")]
    [Tooltip("ID khớp với component TutorialTarget. Để trống = không highlight.")]
    public string targetID = "";

    [Tooltip("Circle = khoanh tròn; Rect = bám sát shape nút")]
    public bool useCircleHole = true;

    [Tooltip("Padding thêm quanh target (pixel)")]
    public float holePaddingPx = 16f;

    [Header("Wait Condition")]
    public TutorialWaitAction waitAction = TutorialWaitAction.WaitForClick;

    [Header("Guide Board")]
    [Tooltip("Hiện popup bảng hướng dẫn 4 bước thay cho NPC text bình thường")]
    public bool showGuideBoard = false;

    [Header("Hand Pointer")]
    public bool showHandPointer = true;

    [Tooltip("Offset bàn tay so với tâm target (pixel)")]
    public Vector2 handOffset = new Vector2(40f, -30f);

    [Header("Drag Hint Animation")]
    [Tooltip("Target ID bàn tay kéo ĐẾN (bước kéo-thả). Để trống = không có drag animation.")]
    public string dragToTargetId = "";

    // [THEM 2026-09-21] CAP YEU CAU CUA BUOC — de tutorial biet nguoi choi dang cap may.
    [Header("Cấp yêu cầu")]
    [Tooltip("Cấp người chơi mà bước này dạy. 0 = không đặt tay → suy từ tên asset " +
             "(L1L2_xx → 1, L2_xx → 2, L3_xx → 3...). Chuỗi có cấp THẤP HƠN cấp hiện tại " +
             "của người chơi sẽ được bỏ qua lúc bắt đầu tutorial.")]
    [SerializeField] private int capYeuCau = 0;

    /// <summary>Giá trị đặt tay trong Inspector (0 = không đặt). Chỉ khi &gt; 0 thì bước mới bị CHẶN
    /// lúc chạy nếu cấp hiện tại còn thấp hơn (giữ nguyên hành vi chuỗi mặc định cho người chơi mới).</summary>
    public int CapYeuCauTuongMinh => Mathf.Max(0, capYeuCau);

    /// <summary>Cấp hiệu lực: giá trị đặt tay, hoặc suy từ tên asset khi = 0. Trả 0 nếu không suy được.</summary>
    public int CapYeuCau => capYeuCau > 0 ? capYeuCau : SuyCapTuTen(name);

    /// <summary>Suy cấp từ tên asset / thư mục: "L1L2_05_..." → 1, "L2_03_..." → 2, "L1_L2" → 1, "L3_..." → 3.
    /// Lấy số ĐẦU TIÊN ngay sau chữ L ở đầu tên. Không khớp → 0 (không giới hạn).</summary>
    public static int SuyCapTuTen(string ten)
    {
        if (string.IsNullOrEmpty(ten) || ten.Length < 2) return 0;
        if (ten[0] != 'L' && ten[0] != 'l') return 0;

        int gia = 0;
        int i = 1;
        while (i < ten.Length && ten[i] >= '0' && ten[i] <= '9')
        {
            gia = gia * 10 + (ten[i] - '0');
            i++;
            if (gia > 999) break;
        }
        return i > 1 ? gia : 0;
    }
}
