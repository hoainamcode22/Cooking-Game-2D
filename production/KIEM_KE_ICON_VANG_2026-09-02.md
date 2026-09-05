# KIỂM KÊ ICON VÀNG — 2026-09-02 (DEV-D · tools-programmer)

Lệnh Sếp: mọi chỗ liên quan VÀNG dùng **1 icon duy nhất = icon HUD**.

**Icon chuẩn:** `Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/vang-removebg-preview.png`
· guid `a1c4be4bd781bd74399a37785962ed71` · spriteFileID `-846414766330871110` (HUD 65×65, preserveAspect=true).

**Cách quét:** script python đọc-only quét toàn bộ `Assets/**/*.unity|*.prefab|*.asset` (bỏ Library/Temp),
bắt mọi reference `{fileID, guid, type:3}` trỏ tới 11 guid icon vàng (10 lệch + 1 chuẩn để thống kê),
dựng map GameObject/Transform để in đường dẫn hierarchy. Kèm grep toàn bộ `*.cs`.

## TÓM TẮT

| Loại | Tổng ref tìm thấy | Đã CHUẨN (giữ) | LỆCH chuẩn | SẼ ĐỔI (tool) | CẦN SẾP QUYẾT |
|---|---|---|---|---|---|
| Scene (.unity) | 16 | 3 | 13 | 12 | 1 (Kitchen UI field) |
| Prefab | 16 | 5 | 11 | 11 | 0 |
| SO/.asset | 558 + 2 lệch | 556 | 2 | 1 | 1 (Tile title map) |
| **Cộng** | **590** | **564** | **26** | **24** | **2** |
| Code .cs | — | — | — | 0 (tool không sửa code) | xem mục CODE |

Icon lệch chuẩn đang dùng (10 file): 2× `Icon_vang.png` (thietke Redesign game/game1), `Anh/vang-removebg-preview.png` (bản sao trùng art, khác guid), `Art/UI/Currency/icon_gold.png`, `Assetsgame/Icon_vang.png`, `Export_Kitchen_UI_Package/Sprites/icon_gold.png`, `maptitle .../Sprite_coin_icon.png`, `UI_OrderBoard/ob_coin.png`, `UI_Stall/stall_icon_coin.png`, `Export_Popups_Chon/ShopPopup/assets/Icon_vang.png` (0 ref — chỉ nằm trong project, không ai dùng).

## A. SCENE — 13 reference lệch chuẩn

### SCN_Farm.unity (11)

| # | Object (hierarchy) | Component.field | Sprite cũ | Quyết định |
|---|---|---|---|---|
| 1 | `Canvas_MarketPopup/Panel_Dim/Popup_Board/Btn_Refresh/Icon_Gold` | Image.m_Sprite | Icon_vang (Redesign game) | **SẼ ĐỔI** — icon giá refresh chợ |
| 2 | `Canvas_MarketPopup/Panel_Dim/Popup_Board/Chip_Gold/Icon_Gold` | Image.m_Sprite | Icon_vang (Redesign game) | **SẼ ĐỔI** — chip số dư vàng chợ |
| 3 | `Canvas_Popup/popup_Menu/Inner_PaperContainer/Header_Bar/Gold_Chip/Img_GoldIcon` | Image.m_Sprite | Icon_vang (game1) | **SẼ ĐỔI** — chip vàng shop |
| 4 | `Canvas_Popup/popup_Menu/ShopItem_Template/Btn_Buy/Img_CurrencyIcon` | Image.m_Sprite | Icon_vang (game1) | **SẼ ĐỔI** — icon giá trên nút mua |
| 5 | `Canvas_Popup/popup_Menu/ShopItem_Template` | ShopItemUI.iconGold (field) | Icon_vang (game1) | **SẼ ĐỔI** — field code gán runtime |
| 6 | `Canvas_Popup/UnifiedTaskPopupRoot` | UnifiedTaskPopupUI.coinIcon (field) | Assetsgame/Icon_vang | **SẼ ĐỔI** — icon thưởng nhiệm vụ |
| 7 | `Canvas_Popup/popup_SKPhucLoi/img_khung_PLPhai/ObjectBtnPhucLoiNap/btn_Nhan_PL/Image` | Image.m_Sprite | Assetsgame/Icon_vang | **SẼ ĐỔI** — Image đồng xu RIÊNG trong nút (đúng luật Lead: nút nạp chỉ đổi icon xu bên trong) |
| 8 | `Canvas_Popup/popup_SKPhucLoi/img_Khung_PLTrai/Scroll View/Viewport/Content/btn_PhucLoiNap/Image` | Image.m_Sprite | Anh/vang-removebg (bản sao) | **SẼ ĐỔI** — như trên |
| 9 | `Canvas_Popup/popup_DiemDanh/PhucLoiDiemDanhTuan/Content_DDPhucLoi/btn_PLDay1/img_AnhVP` | Image.m_Sprite | Assetsgame/Icon_vang | **SẼ ĐỔI** — ảnh quà ngày 1 = vàng |
| 10 | `Canvas_TouristBoatPopup/TouristBoatPopups/DockPurchasePopup/Root/Card/Content/CostRow/CostIcon` | Image.m_Sprite | Kitchen icon_gold | **SẼ ĐỔI** — UI farm, chỉ mượn ảnh Kitchen |
| 11 | `Canvas_TouristBoatPopup/TouristBoatPopups/DockPurchasePopup` | DockPurchasePopupUI.goldIconSprite (field) | Kitchen icon_gold | **SẼ ĐỔI** — như trên |

### SampleScene.unity (2)

| # | Object | Component.field | Sprite cũ | Quyết định |
|---|---|---|---|---|
| 12 | `Canvas/Right_Panel/ScoreResult_Box/Btn_ClaimRewardBG/Img_ClaimRewardCoin` | Image.m_Sprite | Anh/vang-removebg (bản sao) | **SẼ ĐỔI** — cùng art, gom về 1 guid |
| 13 | `Kitchen_UI_v2` | KitchenSceneV2UI.iconGold (field) | Kitchen icon_gold | **CẦN SẾP QUYẾT** — thuộc skin đồng bộ `Export_Kitchen_UI_Package` (Lead đã cảnh báo bộ Kitchen export); tool BỎ QUA mặc định |

Ghi chú: 3 ref chuẩn sẵn trong SCN_Farm (HUD `Icon_Gold`, `Popup_LevelUp_Township/.../Hang_Vang/Icon`, `CoinFlyFX.coinSprite`) — **GIỮ**.

## B. PREFAB — 11 reference lệch chuẩn (tất cả SẼ ĐỔI)

| # | Prefab | Object | Sprite cũ |
|---|---|---|---|
| 1 | `Assets/_Game/Prefab/ui/Market/MarketListingCard_Prefab.prefab` | `Btn_Buy/Icon_Gold` (Image) | Icon_vang (Redesign game) |
| 2-5 | `Assets/_Game/Prefab/ui/OrderBoard/Canvas_OrderBoardPopup.prefab` | `Box_Reward/Row_Gold/IMG_ArtRewardIcon`, `FX_DeliverRoot/Fly_1`, `Fly_3`, `Fly_5` (Image ×4) | ob_coin |
| 6 | `Assets/_Game/Prefab/ui/OrderBoard/PF_OrderTicket.prefab` | `State_Filled/Row_Gold/IMG_ArtRewardIcon` (Image) | ob_coin |
| 7-9 | `Assets/_Game/Prefab/ui/Stall/Canvas_StallPopup.prefab` | `GoldBar/IMG_ArtGoldIcon`, `Row_Price/Value_Box/IMG_ArtCoin`, `Switch_Loa/IMG_ArtLoaCoin` (Image ×3) | stall_icon_coin |
| 10-11 | `Assets/_Game/Prefab/ui/Stall/PF_StallSlot.prefab` | `Btn_Unlock/IMG_ArtCoin`, `Row_Price/IMG_ArtCoin` (Image ×2) | stall_icon_coin |

Lý do đổi cả ob_coin/stall_icon_coin: là icon ĐỒNG XU thuần trong UI (sprite 128×128 do SpriteFactory sinh code), đúng phạm vi "đồng nhất toàn game". Vuông ↔ icon chuẩn gần vuông → không méo; tool vẫn bật preserveAspect.

Prefab chuẩn sẵn (GIỮ): `KhungEwar.prefab`, `KhungHatGiong.prefab` ×2 (CÔNG TRÌNH + Frefab_home).

## C. ScriptableObject / .asset — 2 lệch chuẩn

| # | Asset | Field | Sprite cũ | Quyết định |
|---|---|---|---|---|
| 1 | `Assets/_Game/Resources/RewardIconLibrary.asset` | `goldSprite` | Art/UI/Currency/icon_gold | **SẼ ĐỔI** — thư viện icon thưởng dùng cho RewardFlyFX/FloatingNumber; đổi qua SerializedObject (Lead cho phép) |
| 2 | `Assets/maptitle/AssetsTitl/Tiles/UI/Sprite_coin_icon.asset` | Tile.m_Sprite | Sprite_coin_icon | **CẦN SẾP QUYẾT** — là TILE của tilemap màn title (world-art, không phải UI Image); đổi sang icon HUD có thể lệch style map. Tool BỎ QUA mặc định |

556 ref chuẩn sẵn trong `.asset` (GIỮ): toàn bộ `Mission_*.asset` (Data_Ewa + Achievements) và item data đã trỏ đúng icon chuẩn làm icon thưởng vàng.

## D. CODE (.cs) — tool KHÔNG sửa; liệt kê để Lead xử riêng

### D1. Editor tool build-time (GIỮ — chỉ chạy khi dựng lại UI; nếu Sếp chạy lại các tool này thì icon sẽ lệch trở lại → khi đó chạy lại APPLY của GoldIconUnifyTool)

| File:dòng | Nội dung |
|---|---|
| `Assets/_Game/Editor/KitchenV2SetupTool.cs:168` | gán `skin.iconGold` = Kitchen `icon_gold.png` |
| `Assets/_Game/Farm/Editor/MarketBoardUIBuilder.cs:219,260,457` | LoadSprite `Icon_vang.png` (Redesign game) cho Icon_Gold chợ |
| `Assets/_Game/Farm/Editor/OrderBoardHierarchyBuilderTool.cs:392,513,580` | dùng `ob_coin` cho Row_Gold + Fly FX |
| `Assets/_Game/Farm/Editor/OrderBoardSpriteFactory.cs:77` | SINH sprite `ob_coin` 128×128 bằng code |
| `Assets/_Game/Farm/Editor/StallHierarchyBuilderTool.cs:279,573,627,758,809` | dùng `stall_icon_coin` ×5 |
| `Assets/_Game/Farm/Editor/StallSpriteFactory.cs:62` | SINH sprite `stall_icon_coin` 128×128 |
| `Assets/_Game/Farm/Editor/ShopNewUIBuilder.cs:105` | LoadSprite `Icon_vang.png` (game1) cho shop |
| `Assets/_Game/Farm/Editor/TownshipHUDBuilderTool.cs:193` | HUD builder cũng ưu tiên `Icon_vang.png` trước khi fallback — lệch với HUD thật đang dùng vang-removebg |
| `Assets/_Game/Farm/Editor/TaskPopupSpriteWireTool.cs:32` | wire `coinIcon` = `Assets/Assetsgame/Icon_vang.png` |
| `Assets/_Game/Farm/Editor/SetupUnifiedTaskPopupTool.cs:89` | FindSprite("Sprite_coin_icon", "Icon_vang", ...) |
| `Assets/_Game/Farm/Editor/RewardFxSetupTool.cs:41,94` | RewardIconLibrary.goldSprite mặc định = `Art/UI/Currency/icon_gold.png` |
| `Assets/_Game/Farm/Editor/LevelUpPopupTownshipTool.cs:56-57` | đã trỏ ĐÚNG icon chuẩn (GIỮ) |
| `Assets/_Game/Farm/Editor/TouristBoatUIPopupSetupTool.cs:732,735` | FindSprite theo tên "icon_gold" trước |
| `Assets/_Game/Farm/Editor/DemoL1L10Tool.cs:417,452`, `FarmFxSetupTool.cs:42`, `GeneratePerfectHUD.cs:81`, `UiJuiceMasterTool.cs:14` | gán/tìm icon vàng best-effort (build-time) |

### D2. Runtime code — CẦN SẾP QUYẾT / Lead xử riêng

| File:dòng | Nội dung | Đề xuất |
|---|---|---|
| `Assets/Export_Train_UI_Package/Scripts/TrainDataModel.cs:73` | hardcode path string `".../UnifiedTaskPopup_Redesign/assets/Icon_vang.png"` cho reward "gold" | CẦN SẾP QUYẾT — sửa 1 dòng path sang icon chuẩn (Lead sửa, DEV-D không được đụng .cs cũ) |
| `Assets/Export_Train_UI_Package/Scripts/TrainDataModel.cs:73` | hardcode đường dẫn `Icon_vang` | ⚠ CẦN SẾP QUYẾT — Lead sửa 1 dòng sau khi Sếp duyệt |
| `Assets/_Game/Farm/Scripts/UI/RewardIconLibrary.cs:8,21` | comment gọi `icon_gold.png` là "chính thức" | GIỮ code — sau APPLY field `goldSprite` đã trỏ icon HUD, comment thành cũ (vô hại, dọn sau) |
| `Assets/_Game/Scripts/UI/AvatarProfilePopupUI.cs:758-764` | fallback nạp icon vàng bằng `UnityEditor.AssetDatabase` trong file runtime | ✅ ĐÃ KIỂM (03/09): có bọc `#if UNITY_EDITOR` đúng chuẩn — không gãy build. Popup avatar sẽ vẫn hiện icon cũ trong Editor cho tới khi Sếp duyệt sửa 1 dòng trỏ về icon HUD |
| *(quét bù 2026-09-03: LevelUpRewardIconResolver/RewardFlyFX/FloatingNumber/CoinFlyFX)* | không hardcode đường icon vàng nào — đều đi qua `RewardIconLibrary.goldSprite` | ✅ APPLY đổi 1 chỗ là phủ hết các hệ này |

## E. KHÔNG ĐỤNG (xác nhận an toàn)

`khungvang-removebg-preview.png` (khung), `ribbon_banner_gold.png` (ruy băng), `shop_btn_buy_gold.png` (nền nút mua), `seed_marigold.png` (hạt giống), nền nút `btn_TuiVang_PL`/`btn_GoiQGioVang` — KHÔNG nằm trong 10 guid icon vàng nên tool không bao giờ chạm tới (lọc theo guid, không lọc theo tên).

## F. CÔNG CỤ THI HÀNH

`Assets/_Game/Farm/Editor/GoldIconUnifyTool.cs` — menu `Tools/Farm Game/Đồng nhất icon vàng/`:
1. `★ DRY-RUN (chỉ liệt kê)` → Console + `production/backup_round2_2026-09-02/goldicon_dryrun_report.txt`
2. `★ APPLY (đổi thật — có xác nhận)` → ghi sổ `production/backup_round2_2026-09-02/goldicon_undo.json` TRƯỚC khi đổi (idempotent), bật preserveAspect cho Image bị đổi, SaveOpenScenes + SaveAssets
3. `Hoàn tác (đọc sổ JSON)` → trả từng reference về sprite cũ + preserveAspect cũ

Quy trình Sếp bấm: mở **SCN_Farm** → DRY-RUN → đọc → APPLY → mở **SampleScene** → DRY-RUN → APPLY (sổ hoàn tác dùng chung, không hỏng).
