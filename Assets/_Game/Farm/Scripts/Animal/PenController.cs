using UnityEngine;
using UnityEngine.EventSystems;

public class PenController : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    [SerializeField] private int penId = 1;

    [Header("Unlock")]
    [SerializeField] private bool unlockedAtStart = false;
    [SerializeField] private int requiredLevel = 1;
    [SerializeField] private int gemCost = 0;
    [SerializeField] private bool requireAd = false;

    [Header("Refs")]
    [SerializeField] private SpriteRenderer barnSprite;
    [SerializeField] private SpriteRenderer lockSprite;
    [SerializeField] private SpriteRenderer readyIcon;

    public int PenId => penId;
    public bool IsUnlocked { get; private set; }
    public int RequiredLevel => requiredLevel;
    public int GemCost => gemCost;
    public bool RequireAd => requireAd;

    private LivestockPenController livestockPen;

    private void Awake()
    {
        IsUnlocked = unlockedAtStart;
        livestockPen = GetComponent<LivestockPenController>();
        RefreshVisual();

        Debug.Log($"[PenController] Awake OK on {gameObject.name}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[PenController] CLICK on pen {penId}");

        if (!IsUnlocked)
        {
            FarmUIManager.Instance?.ShowHint($"Chuồng {penId} chưa mở. Cần Lv.{requiredLevel}");
            return;
        }

        if (livestockPen != null)
        {
            livestockPen.OpenPopup();
            return;
        }

        FarmUIManager.Instance?.ShowHint($"Đã chọn chuồng {penId}");
    }

    public void SetUnlocked(bool value)
    {
        IsUnlocked = value;
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (barnSprite != null)
            barnSprite.enabled = true;

        if (lockSprite != null)
            lockSprite.enabled = !IsUnlocked;

        if (readyIcon != null)
            readyIcon.enabled = false;
    }
}