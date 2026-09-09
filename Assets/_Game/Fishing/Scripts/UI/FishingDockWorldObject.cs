using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FarmGame.Fishing
{
    /// <summary>
    /// BẾN HỒ CÂU ngoài map farm — bấm vào mở FishingEntryPopupUI (chọn nhân vật → chọn phòng → vào scene câu).
    /// Khác FishCounterWorldObject: quầy cá là chỗ BÁN CÁ / MUA CẦN, bến là LỐI VÀO scene câu.
    /// Chép đúng khuôn FishCounterWorldObject/StallWorldObject: New Input System + Collider2D.OverlapPoint
    /// (KHÔNG OnMouseDown: mobile nhiều camera không thấy chạm, và bỏ qua mọi chốt chặn).
    /// Tool FishingFarmHookSetupTool tạo object + BoxCollider2D + component này; Sếp kéo tới chỗ muốn và thay sprite.
    /// </summary>
    public class FishingDockWorldObject : MonoBehaviour
    {
        [Header("Liên kết (tự tìm nếu trống)")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Collider2D targetCollider;

        [Header("Điều kiện mở")]
        [Tooltip("Cấp tối thiểu. 0 = lấy theo FishingConfig.unlockLevel.")]
        public int requiredLevel = 0;

        [Tooltip("Tên các Canvas popup — bấm trúng chúng thì KHÔNG tính là bấm vào bến.")]
        [SerializeField] private string[] popupCanvasNames = { "Canvas_Popup", "Canvas_MarketPopup", "Canvas_StallPopup", FishingIds.FarmPopupCanvasName, "Canvas_TouristBoatPopup" };

        private static readonly List<RaycastResult> RaycastBuffer = new List<RaycastResult>();

        private void Awake()
        {
            if (mainCamera == null) { mainCamera = Camera.main; }
            if (targetCollider == null) { targetCollider = GetComponent<Collider2D>(); }
            if (targetCollider == null) { Debug.LogWarning(FishingIds.LogTag + " FishingDockWorldObject '" + name + "' thiếu Collider2D — không bấm được."); }
        }

        private void Update()
        {
            Vector2 screenPos;
            if (TryGetPointerScreenPosition(out screenPos)) { TryOpen(screenPos); }
        }

        private static bool TryGetPointerScreenPosition(out Vector2 screenPos)
        {
            screenPos = default(Vector2);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { screenPos = Mouse.current.position.ReadValue(); return true; }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) { screenPos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            return false;
        }

        private void TryOpen(Vector2 screenPos)
        {
            if (!FishingDatabase.IsEnabled) { return; }
            if (FarmInputLock.BlockWorldInteraction) { return; }
            // Minigame nấu ăn nạp chồng lên scene farm — lúc đó click thuộc về minigame.
            if (SceneManager.GetSceneByName("SampleScene").isLoaded) { return; }
            if (EditModeManager.IsEditMode) { return; }
            if (FarmInputLock.BlockMapPan) { return; }
            if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) { return; }
            if (FishCounterPopupUI.AnyOpen || FishingEntryPopupUI.AnyOpen) { return; }
            if (mainCamera == null) { mainCamera = Camera.main; }
            if (mainCamera == null || targetCollider == null) { return; }
            if (IsPointerOverPopupUI(screenPos)) { return; }

            Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
            if (!targetCollider.OverlapPoint(new Vector2(world3.x, world3.y))) { return; }

            int need = requiredLevel > 0 ? requiredLevel : FishingDatabase.ConfigOrDefault.unlockLevel;
            if (GetPlayerLevel() < need)
            {
                Debug.Log(FishingIds.LogTag + " Bến Hồ Câu mở ở cấp " + need + ".");
                if (FarmUIManager.Instance != null) { FarmUIManager.Instance.ShowHint(Loc.TF("Bến Hồ Câu mở ở cấp {0}", need)); }
                return;
            }
            FishingEntryPopupUI.Open();
        }

        private static int GetPlayerLevel()
        {
            if (PlayerProgressManager.Instance != null) { return PlayerProgressManager.Instance.Level; }
            if (FarmLevelManager.Instance != null) { return FarmLevelManager.Instance.CurrentLevel; }
            return 1;
        }

        private bool IsPointerOverPopupUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) { return false; }
            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            RaycastBuffer.Clear();
            EventSystem.current.RaycastAll(eventData, RaycastBuffer);
            for (int i = 0; i < RaycastBuffer.Count; i++)
            {
                Canvas parentCanvas = RaycastBuffer[i].gameObject.GetComponentInParent<Canvas>();
                if (parentCanvas == null) { continue; }
                for (int n = 0; n < popupCanvasNames.Length; n++) { if (parentCanvas.name == popupCanvasNames[n]) { return true; } }
            }
            return false;
        }
    }
}
