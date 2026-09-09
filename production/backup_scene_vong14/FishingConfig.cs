using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// MỌI số gameplay của Hồ Câu nằm ở đây (CONTRACT §9: không hardcode gameplay value nơi khác).
    /// FEATURE FLAG: enabled mặc định false trong code; FishingDataSetupTool tạo asset đặt true.
    /// enabled == false ⇒ tab Hồ câu ẩn, quầy cá không mở, game y như trước.
    /// </summary>
    [CreateAssetMenu(fileName = "FishingConfig", menuName = "Farm Game/Fishing/Config")]
    public class FishingConfig : ScriptableObject
    {
        [Header("Bật/tắt")]
        public bool enabled = false;
        [Tooltip("Cấp mở khoá tab Hồ câu ở farm.")]
        [Min(1)] public int unlockLevel = 3;

        [Header("Nhân vật & di chuyển (world chuẩn 1 ô iso = 1 x 0.5 unit)")]
        [Tooltip("Chiều cao nhân vật (unit). 0.6 = tỉ lệ 170/300 của khách du lịch trên farm.")]
        [Min(0.1f)] public float playerWorldHeight = 0.6f;
        [Min(0.1f)] public float moveSpeed = 1.6f;
        [Tooltip("Ngưỡng joystick coi là đang đi.")]
        [Range(0.05f, 0.5f)] public float joystickDeadZone = 0.15f;

        [Header("Camera")]
        [Tooltip("Nửa chiều cao khung nhìn (unit). 6 = thấy ~20x12 unit ≈ 20 ô ngang (tile iso 1x0.5) — 3.2 cũ chỉ thấy ~11 ô nên tile trông quá to.")]
        [Min(0.5f)] public float cameraOrthoSize = 6.0f;
        [Range(0.01f, 0.5f)] public float cameraSmoothTime = 0.12f;

        [Header("Zoom camera (pinch 2 ngón / cuộn chuột / 2 nút +−)")]
        [Tooltip("Zoom vào tối đa (ortho nhỏ nhất).")]
        [Min(1f)] public float cameraZoomMin = 3.5f;
        [Tooltip("Zoom xa tối đa (ortho lớn nhất).")]
        [Min(2f)] public float cameraZoomMax = 11f;
        [Tooltip("Hiện 2 nút + − trên HUD (cột phải, trên nhóm tab).")]
        public bool zoomButtonsEnabled = true;
        [Tooltip("Bước mỗi lần bấm nút, theo tỉ lệ: ortho × (1 + zoomStep) khi zoom xa.")]
        [Range(0.05f, 1f)] public float zoomStep = 0.25f;
        [Tooltip("Bù nhân vật khi zoom xa: 0 = giữ nguyên, 1 = phóng nhân vật cùng tỉ lệ zoom")]
        [Range(0f, 1f)] public float playerScaleZoomCompensation = 0f;

        [Header("Nhịp câu (giây)")]
        [Min(0.1f)] public float castDurationSeconds = 0.8f;
        public Vector2 biteWaitSecondsRange = new Vector2(3f, 9f);
        [Tooltip("Cửa sổ bấm THU sau khi cá cắn. Cần câu có thể cộng thêm (RodData.biteWindowBonusSeconds).")]
        [Min(0.2f)] public float biteWindowSeconds = 1.5f;
        [Min(0.1f)] public float reelDurationSeconds = 0.7f;
        [Min(0.1f)] public float resultShowSeconds = 1.6f;
        [Tooltip("Xác suất nền bắt được khi THU đúng cửa sổ (chưa cộng bonus cần).")]
        [Range(0f, 1f)] public float baseCatchChance = 0.7f;
        [Tooltip("Khoảng cách tối đa từ người chơi tới mép FishingZone để được quăng (unit).")]
        [Min(0.1f)] public float castRangeFromZone = 1.2f;
        [Tooltip("Phao bay xa bao nhiêu unit theo hướng nhìn (dùng khi quăng KHÔNG qua thanh đo).")]
        [Min(0.2f)] public float castDistance = 1.3f;

        [Header("Thanh đo lực quăng (giữ nút QUĂNG rồi thả)")]
        [Tooltip("Xa nhất khi thả ở đầu thanh (unit).")]
        [Min(0.3f)] public float castDistanceMin = 0.9f;
        [Tooltip("Xa nhất khi thả ở cuối thanh (unit).")]
        [Min(0.5f)] public float castDistanceMax = 2.4f;
        [Tooltip("Tốc độ con trượt chạy qua lại (lượt/giây).")]
        [Range(0.3f, 4f)] public float castChargeSpeed = 1.4f;
        [Tooltip("Từ mốc này (0..1) trở lên là vùng HOÀN HẢO.")]
        [Range(0.5f, 0.99f)] public float perfectCastThreshold = 0.88f;
        [Tooltip("Cộng thêm xác suất bắt khi quăng hoàn hảo.")]
        [Range(0f, 0.5f)] public float perfectCatchBonus = 0.15f;
        [Tooltip("Nhân trọng số cá hiếm khi quăng hoàn hảo.")]
        [Range(1f, 4f)] public float perfectRareMultiplier = 1.5f;
        [Tooltip("Ranh giới 3 vùng Gần / Vừa / Xa trên thanh (0..1).")]
        [Range(0.1f, 0.6f)] public float castZoneNearEnd = 0.35f;
        [Range(0.4f, 0.95f)] public float castZoneMidEnd = 0.7f;

        [Header("Đèn theo người chơi")]
        public bool playerLanternEnabled = true;
        [Min(0.2f)] public float playerLanternRadius = 2.4f;
        [Range(0f, 3f)] public float playerLanternIntensity = 0.8f;
        public Color playerLanternColor = new Color(1f, 0.93f, 0.78f, 1f);
        [Tooltip("Rung máy khi cá cắn (mobile).")]
        public bool vibrateOnBite = true;

        [Header("Thưởng")]
        [Tooltip("EXP mỗi lần bắt cá. 0 = chỉ có EXP khi BÁN (chống lạm phát, xem MEMORY touristExpMultiplier).")]
        [Min(0)] public int expPerCatch = 0;
        [Tooltip("EXP khi bán = round(tổng vàng / expPerGoldDivisor). 10 = giống quầy hàng.")]
        [Min(1)] public int expPerGoldDivisor = 10;

        [Header("Giỏ cá")]
        [Tooltip("Số LOẠI cá tối đa trong giỏ (đếm theo loại như kho farm).")]
        [Min(1)] public int basketSlotCapacity = 20;
        [Tooltip("Số con tối đa mỗi loại.")]
        [Min(1)] public int basketMaxPerType = 99;

        [Header("Phòng (offline giả lập, online dùng lại số này)")]
        [Range(1, 20)] public int roomCount = 10;
        [Range(2, 20)] public int roomCapacity = 10;
        [Tooltip("Số bot đi lại/câu/chat trong phòng khi OFFLINE để test bubble, bạn bè, mời. 0 = không bot.")]
        [Range(0, 9)] public int offlineBotCount = 3;
        [Tooltip("OFFLINE: sau bao nhiêu giây ở farm thì giả lập 1 lời mời (0 = tắt).")]
        [Min(0f)] public float offlineSimulateInviteAfterSeconds = 0f;

        [Header("Chat & bubble")]
        [Min(1f)] public float bubbleShowSeconds = 5f;
        [Tooltip("Người khác cách xa hơn khoảng này (unit) thì KHÔNG thấy bubble (trừ khi là bạn).")]
        [Min(0.5f)] public float bubbleVisibleDistance = 4f;
        [Range(1, 200)] public int chatMaxLength = 80;
        [Tooltip("Tần suất đẩy trạng thái của mình lên phòng (lần/giây). Online Firebase nên ≤ 5.")]
        [Range(1f, 20f)] public float netPushRate = 5f;

        [Header("Màu line kết nối")]
        public Color friendLineColor = new Color(0.25f, 0.55f, 1f, 1f);
        public Color siblingLineColor = new Color(1f, 0.85f, 0.2f, 1f);
        public Color datingLineColor = new Color(1f, 0.25f, 0.3f, 1f);
        [Min(0.005f)] public float relationshipLineWidth = 0.03f;

        [Header("Tặng quà")]
        [Min(1)] public int giftGemMin = 1;
        [Min(1)] public int giftGemMax = 20;

        /// <summary>Màu line theo loại kết nối; None = trong suốt.</summary>
        public Color LineColorFor(RelationshipKind kind)
        {
            switch (kind)
            {
                case RelationshipKind.Friend: return friendLineColor;
                case RelationshipKind.Sibling: return siblingLineColor;
                case RelationshipKind.Dating: return datingLineColor;
                default: return Color.clear;
            }
        }
    }
}
