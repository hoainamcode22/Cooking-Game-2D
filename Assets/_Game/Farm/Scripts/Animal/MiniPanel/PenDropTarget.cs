using UnityEngine;

/// <summary>
/// Gắn vào cùng GameObject với BoxCollider2D của chuồng.
/// DraggableFeedItem và PenBasketDragItem gọi hàm này khi phát hiện drop trúng collider.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PenDropTarget : MonoBehaviour
{
    [SerializeField] private PenMiniPanelUI miniPanel;

    private void Start()
    {
        if (miniPanel == null)
            Debug.LogError("[PenDropTarget] miniPanel chưa được gán!");
    }

    public PenMiniPanelUI.PenState CurrentState =>
        miniPanel != null ? miniPanel.CurrentState : PenMiniPanelUI.PenState.Idle;

    /// <summary>Gọi từ DraggableFeedItem khi drop thức ăn vào chuồng.</summary>
    public bool ReceiveFoodDrop(string foodItemId)
    {
        if (miniPanel == null) return false;
        bool ok = miniPanel.TryFeed(foodItemId);
        if (ok) Debug.Log($"[PenDropTarget] Feed '{foodItemId}' vào {gameObject.name} OK");
        else    Debug.Log($"[PenDropTarget] Feed '{foodItemId}' bị từ chối (state={CurrentState})");
        return ok;
    }

    /// <summary>Gọi từ PenBasketDragItem khi drop rổ vào chuồng.</summary>
    public bool ReceiveBasketDrop()
    {
        if (miniPanel == null) return false;
        bool ok = miniPanel.TryHarvest();
        if (ok) Debug.Log($"[PenDropTarget] Harvest {gameObject.name} OK");
        else    Debug.Log($"[PenDropTarget] Harvest bị từ chối (state={CurrentState})");
        return ok;
    }
}

