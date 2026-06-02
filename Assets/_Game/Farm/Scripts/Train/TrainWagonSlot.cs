using TMPro;
using UnityEngine;

// â”€â”€â”€ Slot mode enum â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

public enum TrainWagonSlotMode
{
    Empty,        // Toa trá»‘ng â€” khÃ´ng hiá»‡n gÃ¬
    CargoRequest, // Chá» náº¡p hÃ ng â€” hiá»‡n icon + currentAmount/requiredAmount
    Reward        // Chá» thu hoáº¡ch â€” hiá»‡n icon + x(amount)
}

// â”€â”€â”€ Runtime slot data â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Runtime data cho 1 toa trong chuyáº¿n hiá»‡n táº¡i.
/// DÃ¹ng chung cho cáº£ cháº¿ Ä‘á»™ náº¡p hÃ ng (CargoRequest) vÃ  cháº¿ Ä‘á»™ thu reward (Reward).
/// </summary>
[System.Serializable]
public class TrainWagonSlotData
{
    [Header("Shared")]
    public string itemId;
    public string displayName;
    public Sprite icon;
    public TrainWagonSlotMode mode;
    public bool isCollected;

    [Header("Cargo Request")]
    public int currentAmount;
    public int requiredAmount;

    [Header("Reward")]
    public int rewardAmount;

    public bool IsCargoComplete =>
        mode == TrainWagonSlotMode.CargoRequest && currentAmount >= requiredAmount;
}


[RequireComponent(typeof(BoxCollider2D))]
public class TrainWagonSlot : MonoBehaviour
{
    [Header("Visual References")]
    [Tooltip("SpriteRenderer world-space hiá»‡n icon váº­t pháº©m")]
    [SerializeField] private SpriteRenderer iconSprite;
    [SerializeField] private TMP_Text       txtLabel;

    [Header("Optional â€” hiá»‡n khi toa trá»‘ng")]
    [SerializeField] private GameObject emptyRoot;

    [Header("Config")]
    [Tooltip("0 = Wagon_01 / WorldSlot_01, 1 = Wagon_02, â€¦")]
    [SerializeField] public int slotIndex = 0;

    // â”€â”€â”€ Runtime â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private TrainWagonSlotData _data;
    private BoxCollider2D      _col;

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        // áº¨n cargo icon máº·c Ä‘á»‹nh â€” chá»‰ hiá»‡n sau khi cháº¥t hÃ ng láº§n Ä‘áº§u
        if (iconSprite != null) iconSprite.enabled = false;
    }

    // â”€â”€â”€ Public API (gá»i tá»« TrainManager) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Refresh visual tá»« slot data má»›i nháº¥t vÃ  báº­t collider.</summary>
    public void Refresh(TrainWagonSlotData data)
    {
        _data = data;
        gameObject.SetActive(true);

        switch (data.mode)
        {
            case TrainWagonSlotMode.Empty:
                ShowEmpty();
                break;

            case TrainWagonSlotMode.CargoRequest:
                ShowCargo(data);
                break;

            case TrainWagonSlotMode.Reward:
                if (data.isCollected) ShowEmpty();
                else ShowReward(data);
                break;
        }
    }

    /// <summary>áº¨n slot hoÃ n toÃ n vÃ  vÃ´ hiá»‡u hoÃ¡ collider (khi tÃ u Ä‘ang cháº¡y).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Chá»‰ vÃ´ hiá»‡u collider (ngÄƒn click), GIá»® NGUYÃŠN visual.
    /// DÃ¹ng khi tÃ u khá»Ÿi hÃ nh â€” cargo image váº«n hiá»ƒn thá»‹ suá»‘t hÃ nh trÃ¬nh.
    /// </summary>
    public void DisableInteraction()
    {
        if (_col != null) _col.enabled = false;
    }

    /// <summary>World-space position cá»§a slot â€” dÃ¹ng lÃ m Ä‘iá»ƒm spawn FX.</summary>
    public Vector3 GetWorldPosition() => transform.position;

    // â”€â”€â”€ Display helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void ShowEmpty()
    {
        SetIcon(null);
        if (iconSprite != null) iconSprite.enabled = false;
        SetLabel("");
        if (emptyRoot != null) emptyRoot.SetActive(true);
        _col.enabled = false; // toa trá»‘ng khÃ´ng thá»ƒ click
    }

    private void ShowCargo(TrainWagonSlotData data)
    {
        bool hasItems = data.currentAmount > 0;

        // emptyRoot: hiá»‡n khi chÆ°a cÃ³ hÃ ng, áº©n khi Ä‘Ã£ cÃ³ Ã­t nháº¥t 1 item
        if (emptyRoot != null) emptyRoot.SetActive(!hasItems);

        // iconSprite: áº©n khi currentAmount == 0, hiá»‡n ngay khi currentAmount >= 1
        if (iconSprite != null)
        {
            if (hasItems && data.icon != null)
            {
                iconSprite.sprite  = data.icon;
                iconSprite.enabled = true;
            }
            else
            {
                iconSprite.enabled = false;
            }
        }

        SetLabel($"{data.currentAmount}/{data.requiredAmount}");

        // Toa Ä‘áº§y â†’ táº¯t collider (khÃ´ng cho click thÃªm)
        _col.enabled = !data.IsCargoComplete;
    }

    private void ShowReward(TrainWagonSlotData data)
    {
        if (emptyRoot != null) emptyRoot.SetActive(false);
        SetIcon(data.icon);
        SetLabel($"x{data.rewardAmount}");

        _col.enabled = true;
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconSprite == null) return;
        if (sprite != null)
        {
            iconSprite.sprite  = sprite;
            iconSprite.enabled = true;
        }
        // KhÃ´ng áº©n icon khi sprite null â€” giá»¯ nguyÃªn sprite cÅ©
    }

    private void SetLabel(string text)
    {
        if (txtLabel != null) txtLabel.text = text;
    }

    // Unity gá»i OnMouseDown khi collider cá»§a chÃ­nh GO nÃ y Ä‘Æ°á»£c click.
    // KhÃ´ng cáº§n tá»± kiá»ƒm tra raycast / OverlapPoint ná»¯a.
    private void OnMouseDown()
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (FarmInputLock.BlockMapPan) return;
        if (TrainManager.Instance == null) return;

        // KhÃ´ng xá»­ lÃ½ khi Ä‘ang cÃ³ popup má»Ÿ
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        TrainManager.Instance.OnWagonSlotClicked(this);
    }
}
