using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MỘT CÔNG TRƯỜNG ĐANG XÂY.
///
/// Giữ toàn bộ trạng thái của một lượt xây: data, hướng xoay, mốc thời gian, và các
/// hiện vật (giàn giáo + công nhân + khói) cùng UI nổi trên đầu.
///
/// ⏱ ĐỒNG HỒ CHẠY BẰNG MỐC UNIX TUYỆT ĐỐI, KHÔNG BẰNG Time.deltaTime.
/// `StartUnix` + `Duration` là hai con số duy nhất được lưu; thời gian còn lại luôn
/// tính lại từ "bây giờ". Nhờ vậy tắt game 1 phút mở lại thì đã trôi đúng 1 phút, và
/// Time.timeScale = 0 (mở popup) cũng không làm treo tiến độ.
/// Mốc "bây giờ" do <see cref="ConstructionManager.NowUnix"/> cấp — đã chống lùi giờ máy.
///
/// Site KHÔNG tự hoàn thành: ConstructionManager duyệt danh sách mỗi frame và gọi
/// hoàn thành, vì chỉ manager mới biết cách Instantiate + báo lại PlacementManager.
/// </summary>
public class ConstructionSite : MonoBehaviour
{
    public PlaceableItemData Data          { get; private set; }
    public int               RotationSteps { get; private set; }
    public int               PlotId        { get; private set; }
    public long              StartUnix     { get; private set; }
    public float             Duration      { get; private set; }
    public Vector2Int        GridSize      { get; private set; }

    /// <summary>Đã bắt đầu chuỗi hiệu ứng hoàn thành → không nhận rush / không tính lại nữa.</summary>
    public bool IsFinishing { get; private set; }

    /// <summary>
    /// TÂM KHỐI Ô của công trường. Giàn giáo, thảm đất, UI và VFX đều dựng quanh điểm này,
    /// và đây cũng là điểm PlacementManager dùng để giữ / nhả ô lưới.
    /// </summary>
    public Vector3 CenterWorld => transform.position;

    /// <summary>
    /// ĐIỂM NEO — nơi Instantiate prefab thật khi xây xong, và là toạ độ ghi vào save.
    ///
    /// VÌ SAO PHẢI TÁCH KHỎI CenterWorld: art của dự án đặt pivot ở ĐÁY sprite (chuồng bò
    /// cao 447 → lệch 224). Nếu dùng chung một điểm thì hoặc giàn giáo tụt xuống chân nhà
    /// ~2 ô, hoặc công trình xây xong nhảy lên cao 2 ô so với chỗ Ghost vừa hiện.
    /// Quy đổi hai chiều: PlacementManager.AnchorToFootprintCenter / FootprintCenterToAnchor.
    /// </summary>
    public Vector3 AnchorWorld { get; private set; }

    private ConstructionManager             _owner;
    private ConstructionSiteVisuals.Handle  _visuals;
    private ConstructionSiteUI              _ui;
    private BuildingStatusIcon              _statusIcon;

    private readonly List<Vector3> _workerBasePos = new List<Vector3>();
    private float _bobTime;
    private int   _lastShownSecond = int.MinValue;

    // ─────────────────────────────────────────────────────────────────────────

    /// <param name="workerSprite">Ô CŨ trên ConstructionManager. Chỉ dùng khi
    /// <paramref name="artKit"/> chưa có ô "Worker" — xem ConstructionSiteVisuals.Build.</param>
    /// <param name="artKit">Bộ ô art. Được phép null: mọi mảnh sẽ là hình vẽ code tô màu nhận dạng.</param>
    public void Initialize(ConstructionManager owner, PlaceableItemData data, int rotSteps,
                           int plotId, long startUnix, float duration,
                           Sprite workerSprite, string sortingLayer, int baseOrder,
                           ConstructionArtKit artKit = null)
    {
        _owner        = owner;
        Data          = data;
        RotationSteps = rotSteps & 3;
        PlotId        = plotId;
        StartUnix     = startUnix;
        Duration      = Mathf.Max(0.1f, duration);
        GridSize      = PlacementManager.GridSizeOf(data, RotationSteps);

        // Manager đã đặt transform.position = TÂM KHỐI Ô. Suy ngược ra điểm neo ngay tại
        // đây (thay vì thêm tham số) để mọi nơi dùng site đều thấy cùng một cặp số.
        AnchorWorld = PlacementManager.FootprintCenterToAnchor(transform.position, data, RotationSteps);

        float worldW = GridSize.x * PlacementManager.CELL;
        float worldH = GridSize.y * PlacementManager.CELL;

        _visuals = ConstructionSiteVisuals.Build(transform, GridSize, workerSprite,
                                                 sortingLayer, baseOrder, artKit);

        _workerBasePos.Clear();
        foreach (Transform w in _visuals.Workers)
            _workerBasePos.Add(w != null ? w.localPosition : Vector3.zero);

        // UI đẩy lên layer trên cùng + order rất lớn để không bị công trình khác che.
        _ui = ConstructionSiteUI.Build(transform, worldW, worldH,
                                       ConstructionManager.TopSortingLayerName, 30000, artKit);
        _ui.SetBuildingName(!string.IsNullOrEmpty(data.itemName) ? data.itemName : data.itemID);
        _ui.OnRushClicked = HandleRush;

        // ── V9 — ICON MŨ BẢO HỘ NỔI TRÊN ĐẦU ─────────────────────────────────
        // Township giữ icon này SUỐT thời gian xây — đó là ngôn ngữ "chỗ này đang thi công"
        // (§2.B tài liệu phân tích), không phải một hiệu ứng chớp nhoáng lúc bắt đầu.
        //
        // ĐỘ CAO: canvas UI công trường nằm ở `worldH*0.5 + 26` với chiều cao 300 và pivot ở
        // MÉP DƯỚI, nên nó chiếm tới `worldH*0.5 + 326`. Đặt icon ở +416 để chừa một khoảng
        // thở; thấp hơn là icon đè lên nền tên công trình.
        _statusIcon = BuildingStatusIcon.AttachTo(gameObject,
                                                  BuildingStatusIcon.Status.Building,
                                                  worldH * 0.5f + 416f,
                                                  artKit);

        // Dùng "bây giờ" chứ KHÔNG dùng startUnix: công trường khôi phục từ save đã trôi
        // sẵn một khoảng, lấy startUnix sẽ chớp hiện lại thời gian đầy trong 1 frame.
        long now = owner != null ? owner.NowUnix() : startUnix;
        RefreshUI(RemainingSeconds(now));
    }

    // ── Thời gian ────────────────────────────────────────────────────────────

    public float RemainingSeconds(long nowUnix)
        => Mathf.Max(0f, Duration - (nowUnix - StartUnix));

    public float Progress01(long nowUnix)
        => Duration <= 0f ? 1f : Mathf.Clamp01(1f - RemainingSeconds(nowUnix) / Duration);

    /// <summary>Kéo mốc bắt đầu lùi lại đúng bằng `Duration` → còn 0 giây. Dùng cho rush.</summary>
    public void FinishImmediately(long nowUnix)
    {
        StartUnix = nowUnix - Mathf.CeilToInt(Duration);
        _lastShownSecond = int.MinValue;   // ép vẽ lại đồng hồ về 0
    }

    public void MarkFinishing() => IsFinishing = true;

    // ── Cập nhật mỗi frame (manager gọi) ─────────────────────────────────────

    public void Tick(long nowUnix)
    {
        if (_visuals == null) return;

        // Công nhân nhấp nhô — làm bằng sin thay vì Animator để không cần asset nào.
        _bobTime += Time.deltaTime;
        for (int i = 0; i < _visuals.Workers.Count; i++)
        {
            Transform w = _visuals.Workers[i];
            if (w == null || i >= _workerBasePos.Count) continue;

            float phase = _bobTime * 6.2f + i * 1.7f;
            float bob   = Mathf.Abs(Mathf.Sin(phase)) * 7f;
            w.localPosition = _workerBasePos[i] + new Vector3(0f, bob, 0f);
            w.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * 4f);
        }

        if (IsFinishing) return;

        // Chỉ dựng lại text khi con số GIÂY đổi (TMP dựng mesh mỗi lần đổi text —
        // 60 fps × nhiều công trường thì tốn vô ích).
        float remain = RemainingSeconds(nowUnix);
        int   sec    = Mathf.CeilToInt(remain);
        if (sec != _lastShownSecond)
        {
            _lastShownSecond = sec;
            RefreshUI(remain);
        }
    }

    private void RefreshUI(float remaining)
    {
        if (_ui == null || _owner == null) return;

        int  cost       = _owner.GetRushCost(this);
        bool affordable = _owner.CanAfford(cost);
        _ui.SetTimeAndCost(remaining, cost, affordable, _owner.RushUsesGems);
    }

    // ── Hành động ────────────────────────────────────────────────────────────

    private void HandleRush()
    {
        if (_owner == null || IsFinishing) return;
        _owner.TryRush(this);
    }

    public void ShowMessage(string message)
    {
        if (_ui != null) _ui.ShowMessage(message);
    }

    /// <summary>Tắt giàn giáo + UI ngay trước khi chạy hiệu ứng ăn mừng.</summary>
    public void HideConstructionVisuals()
    {
        if (_visuals != null)
        {
            if (_visuals.Dust != null)
                _visuals.Dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_visuals.Root != null)
                _visuals.Root.SetActive(false);
        }

        if (_ui != null) _ui.HideAll();

        // Icon "đang thi công" phải tắt CÙNG LÚC với giàn giáo: để nó bob tiếp trong lúc
        // hộp quà đang mở ra thì người chơi thấy "vẫn đang xây" trong khi đã xây xong.
        if (_statusIcon != null) _statusIcon.SetVisible(false);
    }
}
