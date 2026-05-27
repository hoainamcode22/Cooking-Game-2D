using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Popup "Painel" hiển thị tiến trình cây đang lớn.
/// Gắn script này lên GameObject Painel — con trực tiếp của ô đất (World Space Canvas).
/// Mỗi ô đất có một instance riêng; không dùng Singleton.
/// </summary>
public class CropProcessPopupUI : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private TMP_Text txtCropName;
    [SerializeField] private TMP_Text txtTimeRemaining;

    [Header("Progress Bar")]
    [SerializeField] private Image progressFill;

    [Header("Speed Up")]
    [SerializeField] private Button btnSpeedUp;
    [SerializeField] private TMP_Text txtGemCost;
    [SerializeField] private int speedUpGemCost = 1;

    public bool IsOpen => gameObject.activeSelf;

    private PlotController currentPlot;
    private bool popupInputLockHeld;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        bool startOpen = gameObject.activeSelf;

        if (btnSpeedUp != null)
            btnSpeedUp.onClick.AddListener(OnSpeedUpClicked);

        if (!startOpen) gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (currentPlot != null)
        {
            if (currentPlot.IsGrowing)
            {
                RefreshDisplay();
            }
            else
            {
                // Cây đã chín tự nhiên trong lúc popup đang mở → đóng popup
                ClosePopup();
                return;
            }
        }

        // Click ra ngoài popup → đóng
        if (Input.GetMouseButtonDown(0) && !IsPointerOverPopupUI(Input.mousePosition))
            ClosePopup();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Bật popup và bind dữ liệu từ ô đất đang growing.</summary>
    public void OpenForPlot(PlotController plot)
    {
        if (plot == null || !plot.IsGrowing) return;

        currentPlot = plot;

        if (txtGemCost != null)
            txtGemCost.text = speedUpGemCost.ToString();

        RefreshDisplay();
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        gameObject.SetActive(false);
        currentPlot = null;
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    /// <summary>
    /// Gán hàm này vào btn_RutNang_TGCay trên Prefab UI (OnClick → CropProcessPopupUI.OnGemClick).
    /// </summary>
    public void OnGemClick()
    {
        if (currentPlot == null)
        {
            Debug.LogError("[CropProcessPopup] OnGemClick: currentPlot là NULL — OpenForPlot chưa được gọi hoặc popup bị mở sai.");
            return;
        }

        if (!currentPlot.IsGrowing)
        {
            Debug.LogWarning("[CropProcessPopup] OnGemClick: ô đất không đang Growing, bỏ qua.");
            ClosePopup();
            return;
        }

        if (FarmEconomyManager.Instance == null)
        {
            Debug.LogError("[CropProcessPopup] OnGemClick: FarmEconomyManager.Instance NULL.");
            return;
        }

        if (FarmEconomyManager.Instance.Gems < 1)
        {
            FarmUIManager.Instance?.ShowHint("Không đủ kim cương để tăng tốc.");
            return;
        }

        // InstantGrow tự trừ gem + ép trạng thái Ready
        currentPlot.InstantGrow();
        ReleasePopupInputBlock();
        gameObject.SetActive(false);
        currentPlot = null;
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void RefreshDisplay()
    {
        if (currentPlot == null) return;

        if (txtCropName != null)
            txtCropName.text = currentPlot.CurrentCrop != null
                ? currentPlot.CurrentCrop.displayName
                : "Đang lớn...";

        if (txtTimeRemaining != null)
            txtTimeRemaining.text = currentPlot.GetRemainingTimeText();

        if (progressFill != null)
            progressFill.fillAmount = currentPlot.GetGrowProgress01();
    }

    private void OnSpeedUpClicked()
    {
        if (currentPlot == null || !currentPlot.IsGrowing) return;

        if (FarmEconomyManager.Instance == null)
        {
            Debug.LogWarning("[CropProcessPopup] FarmEconomyManager NULL");
            return;
        }

        if (!FarmEconomyManager.Instance.SpendGems(speedUpGemCost))
        {
            FarmUIManager.Instance?.ShowHint("Không đủ kim cương để tăng tốc.");
            return;
        }

        currentPlot.CompleteInstantly();
        ClosePopup();
    }

    // ── UI Raycast ────────────────────────────────────────────────────────────

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, true);

        if (popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        popupInputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, false);

        if (!popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        popupInputLockHeld = false;
    }

    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }
}
