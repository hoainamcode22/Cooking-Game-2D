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
}
