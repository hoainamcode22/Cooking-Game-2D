# PATCH đề xuất — TrainManager.cs (chờ user duyệt, KHÔNG tự áp)

## Vì sao cần patch (đã thử hết cách không-sửa-file trước)

`TrainManager` là **hệ duy nhất trong toàn dự án chưa được lưu** (đã grep `PlayerPrefs`
toàn bộ Assets — 0 kết quả trong Train/). Mất mát thật khi thoát game:

- Người chơi nạp 3/4 toa (kho ĐÃ bị trừ hàng qua `TrainInventoryAdapter.RemoveItem`) → thoát
  → vào lại tàu về chuyến mới, **hàng đã trừ bốc hơi**.
- Thưởng đang chờ thu (`RewardReadyToCollect`) → thoát → **mất nguyên chuyến tàu**.

Public API hiện có **không đủ** để SaveAdapters làm việc từ bên ngoài:

| Cần | Có sẵn? |
|---|---|
| Đọc `State` | ✅ `public TrainState State { get; private set; }` |
| Đọc `SlotData` | ✅ `public TrainWagonSlotData[] SlotData { get; private set; }` |
| Đọc chỉ số chuyến `_tripIndex` (quyết định preset hàng/thưởng) | ❌ private, không getter |
| Đặt lại state machine + sinh đúng chuyến + refresh UI | ❌ `GenerateNewTrip / ApplyRewardsToSlots / ChangeState / RefreshAll…` đều private |

→ Patch **CHỈ THÊM** 1 DTO + 2 method public vào cuối class. Không đổi chữ ký nào đang có,
không sửa một dòng logic cũ nào. Chưa ai gọi 2 method mới thì hành vi game **y hệt trước patch**.

`SaveAdapters.TrainAdapter` dò 2 method này bằng **reflection**: gói save biên dịch và chạy được
cả TRƯỚC và SAU khi patch được duyệt — duyệt patch xong là hệ tàu tự động được lưu/phục hồi,
không phải sửa thêm file nào của gói save.

## Diff chính xác

**File:** `Assets/_Game/Farm/Scripts/Train/TrainManager.cs`
**Vị trí chèn:** cuối class — SAU dấu `}` đóng method `GetRewardPreset(int index)`
(hiện là **dòng 679** của file, ngay TRƯỚC dấu `}` đóng class ở **dòng 680**).

Chèn nguyên khối sau (thụt lề 4 space, cùng cấp với các method khác trong class):

```csharp
    // ═══════════════════════════════════════════════════════════════════
    //  M0-2 SAVE — PHẦN THÊM MỚI (patch). Không method cũ nào bị đổi.
    //  SaveAdapters.TrainAdapter (gói SaveSystem) gọi 2 hàm này qua reflection.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Ảnh chụp một chuyến tàu — đủ để dựng lại sau khi thoát game.</summary>
    [System.Serializable]
    public class TrainTripSnapshot
    {
        public int    tripIndex = -1;      // -1 = snapshot rỗng, bỏ qua
        public int    state;               // (int)TrainState lúc chụp
        public int[]  cargoCurrent;        // currentAmount từng toa (pha nạp hàng)
        public bool[] rewardCollected;     // isCollected từng toa (pha thưởng)
    }

    /// <summary>Chụp trạng thái chuyến hiện tại. Trả null nếu tàu chưa init.</summary>
    public TrainTripSnapshot CaptureTripSnapshot()
    {
        if (SlotData == null) return null;

        var snap = new TrainTripSnapshot
        {
            tripIndex       = _tripIndex,
            state           = (int)State,
            cargoCurrent    = new int[SlotData.Length],
            rewardCollected = new bool[SlotData.Length]
        };

        for (int i = 0; i < SlotData.Length; i++)
        {
            if (SlotData[i] == null) continue;
            snap.cargoCurrent[i]    = SlotData[i].currentAmount;
            snap.rewardCollected[i] = SlotData[i].isCollected;
        }
        return snap;
    }

    /// <summary>
    /// Dựng lại chuyến tàu từ snapshot. Gọi khi tàu ĐÃ init xong và đang đứng ga
    /// (State == WaitingForLoad — SaveBootstrap tự đợi đúng thời điểm này).
    /// Snapshot null / rỗng / gọi sai thời điểm → bỏ qua an toàn, tàu chạy chuyến mới.
    /// </summary>
    public void RestoreTripSnapshot(TrainTripSnapshot snap)
    {
        if (snap == null || snap.tripIndex < 0) return;

        if (State != TrainState.WaitingForLoad || SlotData == null)
        {
            Debug.LogWarning($"[Save] Train: RestoreTripSnapshot gọi lúc State={State} — bỏ qua " +
                             "(chỉ phục hồi khi tàu đang đứng ga chờ nạp).");
            return;
        }

        var savedState = (TrainState)snap.state;

        // ── Pha THƯỞNG dở dang: hàng đã gửi đi, thưởng chưa thu hết ─────────
        // (ShipDeparting tính là pha thưởng: hàng đã nạp đủ, OnShippingReachedTunnel
        //  kiểu gì cũng áp thưởng — thoát đúng khúc đó không được nuốt thưởng của người chơi.)
        bool phaThuong = savedState == TrainState.ShipDeparting
                      || savedState == TrainState.Processing
                      || savedState == TrainState.RewardArriving
                      || savedState == TrainState.RewardReadyToCollect;

        if (phaThuong)
        {
            bool conThuongChuaThu = false;
            if (snap.rewardCollected != null)
                for (int i = 0; i < snap.rewardCollected.Length; i++)
                    if (!snap.rewardCollected[i]) { conThuongChuaThu = true; break; }

            if (!conThuongChuaThu)
            {
                // Thu sạch rồi mà chưa kịp sang chuyến mới → mở thẳng chuyến kế.
                _tripIndex = snap.tripIndex + 1;
                GenerateNewTrip();
                RefreshAllShippingSlots();
                Debug.Log($"[Save] Train: chuyến #{snap.tripIndex} đã xong — mở chuyến #{_tripIndex}.");
                return;
            }

            _tripIndex = snap.tripIndex;
            GenerateNewTrip();

            // Mô phỏng lại đúng các bước của OnShippingReachedTunnel():
            shippingPathFollower?.HideTrain();
            HideAllShippingSlots();
            if (shippingPathFollower != null && pointHiddenShip != null && pointStationShip != null)
            {
                Vector3 hiddenPos  = pointHiddenShip.position;
                Vector3 stationPos = pointStationShip.position;
                shippingPathFollower.SnapToPosition(hiddenPos, (hiddenPos - stationPos).normalized);
            }

            ApplyRewardsToSlots();
            if (snap.rewardCollected != null)
                for (int i = 0; i < SlotData.Length && i < snap.rewardCollected.Length; i++)
                    if (SlotData[i] != null) SlotData[i].isCollected = snap.rewardCollected[i];

            ChangeState(TrainState.RewardArriving);
            RefreshAllRewardSlots();
            DisableAllRewardSlotInteractions();
            ShowRewardAtTunnelThenMoveToStation(OnRewardArrivedAtStation);

            Debug.Log($"[Save] Train: phục hồi chuyến #{_tripIndex} ở pha thưởng.");
            return;
        }

        // ── Pha NẠP HÀNG (WaitingForLoad) hoặc vừa đóng chuyến (RewardDeparting) ──
        if (savedState == TrainState.RewardDeparting)
        {
            _tripIndex = snap.tripIndex + 1;   // thưởng thu xong, tàu đang rời ga → chuyến kế
            GenerateNewTrip();
            RefreshAllShippingSlots();
            Debug.Log($"[Save] Train: chuyến #{snap.tripIndex} đã đóng — mở chuyến #{_tripIndex}.");
            return;
        }

        _tripIndex = snap.tripIndex;
        GenerateNewTrip();

        if (snap.cargoCurrent != null)
            for (int i = 0; i < SlotData.Length && i < snap.cargoCurrent.Length; i++)
                if (SlotData[i] != null)
                    SlotData[i].currentAmount =
                        Mathf.Clamp(snap.cargoCurrent[i], 0, SlotData[i].requiredAmount);

        RefreshAllShippingSlots();
        Debug.Log($"[Save] Train: phục hồi chuyến #{_tripIndex} ở pha nạp hàng.");

        // Save rơi đúng khoảnh khắc đã nạp đủ 4 toa nhưng tàu chưa kịp lăn bánh →
        // cho lăn bánh nốt (tàu đang đứng ở ga nên trình tự y hệt lần nạp toa cuối).
        CheckAllLoaded();
    }
```

## Tính an toàn của patch

- **Không đổi chữ ký public nào đang dùng** — chỉ thêm 1 nested class + 2 method mới.
- **Default an toàn:** không ai gọi → không chạy; snapshot null/rỗng → return sớm;
  gọi sai thời điểm → log warning và bỏ qua (tàu chạy chuyến mới như hành vi cũ).
- Chỉ dùng lại các method private CÓ SẴN (`GenerateNewTrip`, `ApplyRewardsToSlots`,
  `ChangeState`, `RefreshAllShippingSlots`, `RefreshAllRewardSlots`,
  `DisableAllRewardSlotInteractions`, `HideAllShippingSlots`,
  `ShowRewardAtTunnelThenMoveToStation`, `OnRewardArrivedAtStation`, `CheckAllLoaded`)
  — không viết lại logic nào.
- Đã compile-sanity bằng mcs với stub UnityEngine + đúng file TrainManager.cs thật
  (trước và sau khi chèn khối trên) — xem báo cáo tích hợp.

## Trạng thái khi CHƯA duyệt patch

Gói SaveSystem vẫn hoạt động đầy đủ cho mọi hệ khác. Riêng tàu: `save.json` chỉ chứa
bản chụp đọc-được (`train.hasData = true, restorable = false`) và log nhắc đúng một lần:

```
[Save] Train: TrainManager chưa có CaptureTripSnapshot — chỉ chụp để đọc, KHÔNG phục hồi được. Duyệt TrainManager.PATCH.md để bật.
```
