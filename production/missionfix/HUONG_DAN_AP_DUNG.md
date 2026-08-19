# Hướng dẫn áp dụng — Fix 12 mission "chết" trỏ vào `pho_beef`

**Lý do:** 12 mission `proc_c_*` trong `Main_L1_L10` có `targetItemId = pho_beef` — dish id này không tồn tại (dish thật là `pho_bo_tai`), nên tiến độ không bao giờ tăng. Gói fix gồm 2 file:

| File | Đích trong project | Vai trò |
|---|---|---|
| `FixPhoBeefMissions.cs` | `Assets/_Game/Farm/Editor/FixPhoBeefMissions.cs` | Sửa 12 asset đang nằm trên đĩa |
| `MissionSetupTool.PATCHED.cs` | Đổi tên thành `MissionSetupTool.cs`, **ghi đè** `Assets/_Game/Farm/Editor/MissionSetupTool.cs` | Sửa tận gốc tool sinh mission — chạy lại "Setup Missions L1-L30" sau này không tái sinh bug |

## Bước 1 — Copy file

1. Copy `FixPhoBeefMissions.cs` vào `Assets/_Game/Farm/Editor/`.
   - **Bắt buộc nằm trong folder `Editor/`** (file dùng `UnityEditor`). File đã bọc thêm `#if UNITY_EDITOR` nên lỡ đặt sai chỗ cũng không vỡ build, nhưng đúng chỗ là `Editor/`.
2. Copy `MissionSetupTool.PATCHED.cs` vào `Assets/_Game/Farm/Editor/` và **đổi tên thành `MissionSetupTool.cs`** (đè lên file cũ). KHÔNG giữ cả 2 bản — class trùng tên sẽ báo lỗi compile.
3. Quay lại Unity, chờ compile xong, không có lỗi ở Console.

## Bước 2 — Dry-Run (chỉ xem, không ghi)

Menu: **Tools → Farm Game → Missions → Fix Pho Beef Missions (Dry-Run)**

Console sẽ in 12 dòng dạng:

```
[DryRun] proc_c_4_1: 'pho_beef' → 'com_chien_trung' | Nấu 3 món pho_beef → Nấu 3 món Cơm chiên trứng
...
[DryRun] Tổng kết: sẽ-sửa 12, bỏ-qua 0, missing 0 (trên 12 mission trong bảng duyệt). KHÔNG có gì được ghi.
```

Dry-run không ghi gì vào asset — chạy thoải mái.

## Bước 3 — APPLY

Menu: **Tools → Farm Game → Missions → Fix Pho Beef Missions (APPLY)**

Console phải in: `[Apply] Tổng kết: đã sửa 12, skip 0, missing 0 ...` và dòng `✅ FIX HOÀN TẤT`.

- Tool idempotent: chạy APPLY lần 2 sẽ ra `đã sửa 0, skip 12` — vô hại.
- Chỉ đổi `missionName` / `targetItemId` / `targetAmount`; requiredLevel, reward, eventType, icon giữ nguyên.

## Bước 4 — Verify

1. Mở 1 asset, ví dụ `Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_15_2.asset` trong Inspector:
   - `Mission Name` = `Nấu 8 món Phở bò tái`
   - `Target Item Id` = `pho_bo_tai`
   - `Target Amount` = `8`, `Required Level` = `15`, reward không đổi.
2. Xem log Console đủ 12 dòng `[Apply]` + `✅ FIX HOÀN TẤT`.
3. (Tuỳ chọn) Chạy **Tools → Farm Game → Test → Check Missions** — phải PASS như trước.

## Rollback

- **Ngay trong Editor:** tool có `Undo.RecordObject` → bấm **Ctrl+Z** (Edit → Undo "Fix pho_beef mission") ngay sau khi APPLY. Lưu ý: Undo chỉ hoàn tác giá trị trong bộ nhớ; nếu đã Save/đóng Editor thì dùng git.
- **Bằng git:**
  ```bash
  git checkout -- "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_4_1.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_6_3.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_8_5.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_15_2.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_17_4.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_19_6.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_21_8.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_23_10.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_24_1.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_26_3.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_28_5.asset" \
    "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10/Mission_proc_c_30_7.asset"
  ```
  (rollback tool: `git checkout -- Assets/_Game/Farm/Editor/MissionSetupTool.cs` và xoá `FixPhoBeefMissions.cs`)

## Ghi chú kỹ thuật

- 12 mission này KHÔNG phải dòng `MissionDef` tĩnh — chúng được **sinh tự động** trong `MissionSetupTool.SetupMissions()` từ mảng `dishes` chứa `"pho_beef"`. Bản PATCHED sửa: (1) `"pho_beef"` → `"pho_bo_tai"` trong mảng, (2) thêm bảng `PROC_COOK_OVERRIDES` áp đúng 12 mission theo bảng duyệt (3 mission cấp thấp L4/L6/L8 đổi sang món cấp thấp + tên hiển thị tiếng Việt chuẩn). `targetAmount` do công thức `1 + lvl/2` sinh ra vốn đã khớp bảng duyệt nên không cần override.
- Còn 1 asset legacy `Data_Ewa/Mission_pho_beef.asset` (format cũ, không nằm trong database nào) — ngoài phạm vi đợt này, đợi Tech Lead quyết.
- `proc_c_2_3` / `proc_c_3_2` không đụng tới (theo spec — để đợt sau). Thực tế 2 id này cũng không nằm trong nhóm pho_beef.
