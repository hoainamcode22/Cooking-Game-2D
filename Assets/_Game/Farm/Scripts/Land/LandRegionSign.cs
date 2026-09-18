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

    /// <summary>Tien to tieng Viet goc cua ly do "chua du cap" do LandExpansionManager.CanBuy sinh ra.</summary>
    private const string TIEN_TO_MO_O_CAP = "Mở ở cấp";

    /// <summary>
    /// [i18n] True khi <paramref name="reason"/> la ly do "khoa theo cap". Khop ca ban tieng Viet
    /// goc lan ban dich hien hanh (Loc.T) — giong cach TutorialManager.NhanLaBoQuaBuocNay lam —
    /// de bien bao khong hong khi chuoi ly do bi dich sang tieng Anh.
    /// </summary>
    private static bool LaLyDoKhoaTheoCap(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return false;
        reason = reason.Trim();

        if (reason.StartsWith(TIEN_TO_MO_O_CAP, System.StringComparison.Ordinal)) return true;

        string daDich = Loc.T(TIEN_TO_MO_O_CAP);
        if (!string.IsNullOrEmpty(daDich) && daDich != TIEN_TO_MO_O_CAP
            && reason.StartsWith(daDich.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    /// <summary>Cap nhat chu + mau theo dieu kien hien tai.</summary>
    public void Refresh()
    {
        if (_region == null || _manager == null) return;
        bool ok = _manager.CanBuy(_region, out string reason);

        if (label != null)
        {
            // [FIX QA i18n] `reason` co the da duoc dich sang tieng Anh => so tien to CA HAI ngon ngu.
            if (!ok && LaLyDoKhoaTheoCap(reason))
                label.text = Loc.TF("MỞ Ở CẤP {0}", _region.unlockLevel);   // trước: "MO O CAP 40" (mất dấu)
            else if (_region.goldPrice > 0)
                label.text = Loc.TF("{0}\n{1} vàng", Loc.T(_region.displayName), $"{_region.goldPrice:n0}");
            else if (_region.gemPrice > 0)
                label.text = Loc.TF("{0}\n{1} kim cương", Loc.T(_region.displayName), _region.gemPrice);
            else
                label.text = Loc.T(_region.displayName);
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
