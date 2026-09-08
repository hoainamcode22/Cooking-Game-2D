using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Gắn trên Tab_Fishing (Button) trong Canvas_HUD/BottomLeft_Nav_Group của SCN_Farm (tool FishingFarmHookSetupTool tạo).
    /// Ẩn khi hệ tắt hoặc chưa đủ cấp; đủ cấp thì hiện (nghe OnLevelChanged). onClick → FishingEntryPopupUI.Open().
    /// Không đụng TownshipHUDController.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class FishingHudTabButton : MonoBehaviour
    {
        private Button _button;
        private bool _wired;
        private bool _subscribed;

        private void Awake()
        {
            _button = GetComponent<Button>();
            Wire();
        }

        private void OnEnable()
        {
            if (!_subscribed && PlayerProgressManager.Instance != null) { PlayerProgressManager.Instance.OnLevelChanged += OnLevelChanged; _subscribed = true; }
            RefreshVisible();
        }

        private void OnDisable()
        {
            if (_subscribed && PlayerProgressManager.Instance != null) { PlayerProgressManager.Instance.OnLevelChanged -= OnLevelChanged; }
            _subscribed = false;
        }

        private void Start()
        {
            // Thứ tự Awake không xác định: OnEnable có thể chạy trước khi PlayerProgressManager tự tạo → đăng ký lại ở Start.
            if (!_subscribed && PlayerProgressManager.Instance != null) { PlayerProgressManager.Instance.OnLevelChanged += OnLevelChanged; _subscribed = true; }
            RefreshVisible();
        }

        private void Wire()
        {
            if (_wired || _button == null) { return; }
            _wired = true;
            _button.onClick.AddListener(OnClick);
        }

        private void OnLevelChanged(int level) { RefreshVisible(); }

        /// <summary>Ẩn/hiện bằng CanvasGroup + interactable để không phá layout của nhóm tab (SetActive sẽ làm HorizontalLayout dồn lại).</summary>
        private void RefreshVisible()
        {
            bool show = FishingDatabase.IsEnabled;
            if (show)
            {
                int level = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
                show = level >= FishingDatabase.ConfigOrDefault.unlockLevel;
            }
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) { cg = gameObject.AddComponent<CanvasGroup>(); }
            cg.alpha = show ? 1f : 0f;
            cg.interactable = show;
            cg.blocksRaycasts = show;
            if (_button != null) { _button.interactable = show; }
        }

        private void OnClick()
        {
            if (!FishingDatabase.IsEnabled) { return; }
            if (AudioManager.Instance != null) { AudioManager.Instance.PlayUIClick(); }
            FishingEntryPopupUI.Open();
        }
    }
}
