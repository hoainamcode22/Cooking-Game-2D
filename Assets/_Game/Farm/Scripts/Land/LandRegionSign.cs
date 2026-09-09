using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bien bao dat o giua khu dat CHUA MUA — kieu "CAP 40 MO" / "12 450 vang" trong Township.
///
/// Gan len prefab bien bao. Prefab can co:
///   • SpriteRenderer (hinh tam bien)
///   • Collider2D (de bam duoc)
///   • (tuy chon) TextMesh hoac TMP_Text de hien chu — keo vao 'label'.
///
/// Bam vao bien:
///   • Neu du dieu kien  -> ban onRequestBuy (UI mo popup xac nhan) ; neu khong
///     gan UI thi mua thang luon (buyDirectlyIfNoPopup = true).
///   • Neu chua du       -> ban onBlocked kem ly do de UI hien toast.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LandRegionSign : MonoBehaviour
{
    [Header("Hien thi")]
    [Tooltip("TextMesh (3D Text) hien noi dung. Co the de trong.")]
    public TextMesh label;
    [Tooltip("SpriteRenderer cua tam bien — dung de doi mau khi du/khong du dieu kien.")]
    public SpriteRenderer board;
    public Color colorReady   = Color.white;
    public Color colorBlocked = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Hanh vi")]
    [Tooltip("Bat = bam la mua ngay khi khong co UI popup nao lang nghe.")]
    public bool buyDirectlyIfNoPopup = true;

    [Header("Su kien (noi vao UI popup mua dat)")]
    public UnityEvent<LandRegionData> onRequestBuy;
    public UnityEvent<string>         onBlocked;

    private LandRegionData _region;
    private LandExpansionManager _manager;

    /// <summary>Manager goi khi sinh bien.</summary>
    public void Bind(LandRegionData region, LandExpansionManager manager)
    {
        _region = region;
        _manager = manager;
        Refresh();
    }

    private void OnEnable()  { LandExpansionManager.OnRegionUnlocked += HandleUnlocked; }
    private void OnDisable() { LandExpansionManager.OnRegionUnlocked -= HandleUnlocked; }

    private void HandleUnlocked(LandRegionData r) { if (r == _region) Destroy(gameObject); }

    /// <summary>Cap nhat chu + mau theo dieu kien hien tai.</summary>
    public void Refresh()
    {
        if (_region == null || _manager == null) return;
        bool ok = _manager.CanBuy(_region, out string reason);

        if (label != null)
        {
            if (!ok && !string.IsNullOrEmpty(reason) && reason.StartsWith("Mở ở cấp"))
                label.text = reason.ToUpperInvariant();                  // "MO O CAP 40"
            else if (_region.goldPrice > 0)
                label.text = $"{_region.displayName}\n{_region.goldPrice:n0} vang";
            else if (_region.gemPrice > 0)
                label.text = $"{_region.displayName}\n{_region.gemPrice} kim cuong";
            else
                label.text = _region.displayName;
        }
        if (board != null) board.color = ok ? colorReady : colorBlocked;
    }

    private void OnMouseUpAsButton()
    {
        if (_region == null || _manager == null) return;

        // Dang o Edit Mode / dang dat cong trinh thi khong xu ly
        if (EditModeManager.IsEditMode) return;
        if (PlacementManager.IsPlacingNewObject) return;

        if (!_manager.CanBuy(_region, out string reason))
        {
            onBlocked?.Invoke(reason);
            Refresh();
            return;
        }

        if (onRequestBuy != null && onRequestBuy.GetPersistentEventCount() > 0)
        {
            onRequestBuy.Invoke(_region);   // UI se goi TryBuy sau khi nguoi choi xac nhan
            return;
        }

        if (buyDirectlyIfNoPopup) _manager.TryBuy(_region);
    }
}
