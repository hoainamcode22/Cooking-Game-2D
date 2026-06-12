using UnityEngine;

/// <summary>
/// Gáº¯n vÃ o cÃ¹ng GameObject vá»›i BoxCollider2D cá»§a chuá»“ng.
/// DraggableFeedItem vÃ  PenBasketDragItem gá»i hÃ m nÃ y khi phÃ¡t hiá»‡n drop trÃºng collider.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PenDropTarget : MonoBehaviour
{
    [SerializeField] private PenMiniPanelUI miniPanel;
    [SerializeField] private float vfxYOffset = 250f;

    private void Start()
    {
    }

    public PenMiniPanelUI.PenState CurrentState =>
        miniPanel != null ? miniPanel.CurrentState : PenMiniPanelUI.PenState.Idle;

    /// <summary>Gá»i tá»« DraggableFeedItem khi drop thá»©c Äƒn vÃ o chuá»“ng.</summary>
    public bool ReceiveFoodDrop(string foodItemId)
    {
        if (miniPanel == null) return false;
        bool ok = miniPanel.TryFeed(foodItemId, GetVFXWorldPosition());
        return ok;
    }

    /// <summary>Gá»i tá»« PenBasketDragItem khi drop rá»• vÃ o chuá»“ng.</summary>
    public bool ReceiveBasketDrop()
    {
        if (miniPanel == null) return false;
        bool ok = miniPanel.TryHarvest(GetVFXWorldPosition());
        return ok;
    }

    private Vector3 GetVFXWorldPosition()
    {
        return transform.position + Vector3.up * vfxYOffset;
    }
}

