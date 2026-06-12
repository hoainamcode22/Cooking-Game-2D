using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup bảng hướng dẫn 4 bước nông trại.
/// Hiện khi TutorialStepData.showGuideBoard = true.
/// Nút "Bắt đầu trồng" → Hide() + TutorialManager.NextStep().
///
/// QUAN TRỌNG — rootPanel thường = chính gameObject này (self-reference).
/// KHÔNG gọi rootPanel.SetActive(false) trong Awake():
///   - Awake() được Unity kích hoạt TRONG lúc Show() gọi rootPanel.SetActive(true)
///   - Nếu Awake SetActive(false) lại → Board bị tắt ngay, không bao giờ hiện
///   - Setup tool đã gọi SetActive(false) khi tạo — Awake không cần làm lại
/// </summary>
public class TutorialGuideBoardUI : MonoBehaviour
{
    [Header("Root Panel (self-reference: chính là Tutorial_GuideBoard)")]
    [SerializeField] private GameObject rootPanel;

    [Header("4 Step Image Slots (gán ảnh minh họa sau)")]
    [Tooltip("[TutorialSetup] Gắn ảnh minh họa bước 1 - Gieo Hạt tại đây")]
    [SerializeField] private Image step1Icon;

    [Tooltip("[TutorialSetup] Gắn ảnh minh họa bước 2 - Tăng Tốc tại đây")]
    [SerializeField] private Image step2Icon;

    [Tooltip("[TutorialSetup] Gắn ảnh minh họa bước 3 - Thu Hoạch tại đây")]
    [SerializeField] private Image step3Icon;

    [Tooltip("[TutorialSetup] Gắn ảnh minh họa bước 4 - Kết Quả tại đây")]
    [SerializeField] private Image step4Icon;

    [Header("Button")]
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        // KHÔNG gọi rootPanel.SetActive(false) ở đây.
        // Gọi SetActive(false) trong Awake sẽ cancel ngay tác dụng của Show()
        // vì Awake() được trigger TRONG lúc Show() đang gọi rootPanel.SetActive(true).
        // SetActive(false) ban đầu được xử lý bởi Setup Tool (rootGo.SetActive(false)).
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    /// <summary>
    /// Hiện board. Gọi được dù gameObject đang inactive
    /// (C# method call không bị block bởi inactive state).
    /// </summary>
    public void Show()
    {
        var target = rootPanel != null ? rootPanel : gameObject;
        target.SetActive(true);

        // Đưa board lên trên cùng trong Canvas — render trên mọi UI khác
        transform.SetAsLastSibling();

        Debug.Log($"[TutorialGuideBoardUI] Show() — rootPanel={(rootPanel != null ? rootPanel.name : "NULL (dùng gameObject)")}");
    }

    /// <summary>Ẩn board.</summary>
    public void Hide()
    {
        var target = rootPanel != null ? rootPanel : gameObject;
        target.SetActive(false);
        Debug.Log("[TutorialGuideBoardUI] Hide()");
    }

    private void OnConfirmClicked()
    {
        Debug.Log("[TutorialGuideBoardUI] Start button clicked — hiding board, advancing tutorial.");
        Hide();
        TutorialManager.Instance?.NextStep();
    }
}
