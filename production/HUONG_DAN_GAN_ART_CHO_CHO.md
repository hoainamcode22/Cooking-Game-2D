# Hướng dẫn gắn art — Chợ & Quầy Hàng

Danh sách đầy đủ mọi ô chờ art, gắn ở đâu, gắn kiểu gì.

---

## ⚠️ Quy tắc số 1: sửa PREFAB, đừng sửa bản trong scene

`Canvas_StallPopup` trong scene là **prefab instance**. Nếu bạn kéo sprite thẳng vào bản trong Hierarchy thì Unity chỉ ghi "override" cho riêng scene này — chạy lại tool là mất, và prefab gốc vẫn trắng.

**Đúng cách:** double-click file prefab trong Project để vào **Prefab Mode**, sửa trong đó, thoát ra là tự áp dụng.

Đường dẫn prefab:
```
Assets/_Game/Prefab/ui/Stall/Canvas_StallPopup.prefab
Assets/_Game/Prefab/ui/Stall/PF_StallSlot.prefab
Assets/_Game/Prefab/ui/Market/MarketListingCard_Prefab.prefab
Assets/_Game/Prefab/ui/Market/MarketCategoryTab_Prefab.prefab
```

---

## 1. StallSystem — KHÔNG gắn gì cả

Object rỗng chỉ mang script `PlayerStallManager`. Không có hình ảnh nào. **Bỏ qua.**

Chỉ chỉnh số trong Inspector nếu muốn cân bằng: cấp mở ô (3/5/8/12/16/21/27) và giá mở ô (500…24000 vàng).

---

## 2. Stall_WorldObject — công trình ngoài map

| Object con | Component | Gắn gì |
|---|---|---|
| `SPR_ArtStallBody` | SpriteRenderer | **thân quầy** (bàn, khung, chân) |
| `SPR_ArtStallValance` | SpriteRenderer | **mái hiên** vắt ngang trên |
| `SPR_ArtEmptySign` | SpriteRenderer | biển **"chưa bán gì"** khi quầy trống |
| `DisplaySlot_0` … `DisplaySlot_4` | SpriteRenderer | ❌ **ĐỪNG GẮN** |

> **Vì sao đừng gắn DisplaySlot:** năm ô này là chỗ **bày hàng đang bán** lên mặt quầy. `StallCounterDisplay.Refresh()` tự gán icon vật phẩm vào lúc chạy và tự bật/tắt. Gắn tay là bị code ghi đè ngay frame đầu.

**Kéo công trình ra chỗ nào:** hiện nó ở `(0, 0)` — góc gốc bản đồ. Vị trí các công trình khác để tham chiếu:

| Công trình | x | y |
|---|---|---|
| `Market` (chợ) | −1335 | −492 |
| `CookingGate` (bếp) | 2101 | −3150 |

Đặt quầy hàng **gần chợ** là hợp lý nhất — hai thứ cùng chủ đề mua bán. Thử `x ≈ −900, y ≈ −500`.

**Sau khi kéo, kiểm 2 thứ:**
- **Sorting Layer** của 3 SpriteRenderer phải trùng layer các công trình khác, không thì quầy chìm dưới đất hoặc nổi đè lên mọi thứ.
- **BoxCollider2D** phải phủ đúng thân quầy, vì đó là vùng bấm để mở popup.

Kéo **một lần thôi** — tool đã được sửa để lần chạy sau giữ nguyên vị trí bạn đặt.

---

## 3. Canvas_StallPopup — popup quầy hàng (13 ô)

Mở Prefab Mode rồi gắn:

| Đường dẫn trong prefab | Gắn gì |
|---|---|
| `IMG_ArtPanelBackground` | **nền popup** (nên để Sliced 9-slice) |
| `IMG_ArtValance` | **mái hiên** trên đỉnh popup |
| `GoldBar ▸ IMG_ArtGoldIcon` | icon đồng vàng |
| `ProfileBar ▸ IMG_ArtPlayerAvatar` | avatar người chơi |
| `Picker_Root ▸ Picker_Panel ▸ IMG_ArtPickerBackground` | nền panel chọn vật phẩm |
| `… ▸ Col_Categories ▸ Tab_TatCa ▸ IMG_ArtCategoryIcon` | icon **Tất cả** |
| `… ▸ Tab_NongSan ▸ IMG_ArtCategoryIcon` | icon **Nông sản** |
| `… ▸ Tab_Hoa ▸ IMG_ArtCategoryIcon` | icon **Hoa** |
| `… ▸ Tab_HatGiong ▸ IMG_ArtCategoryIcon` | icon **Hạt giống** |
| `… ▸ Tab_CheBien ▸ IMG_ArtCategoryIcon` | icon **Chế biến** |
| `… ▸ Col_Setup ▸ Setup_Content ▸ Row_Price ▸ Value_Box ▸ IMG_ArtCoin` | icon vàng nhỏ |
| `… ▸ Switch_Loa ▸ IMG_ArtSpeakerIcon` | icon **cái loa** |
| `… ▸ Switch_Loa ▸ IMG_ArtLoaCoin` | icon vàng nhỏ (giá bật loa) |

---

## 4. PF_StallSlot — ô quầy (5 ô)

Prefab riêng, phải mở riêng.

| Object | Gắn gì |
|---|---|
| `IMG_ArtSlotBackground` | nền ô (Sliced) |
| `IMG_ArtPlusIcon` | dấu **+** khi ô trống |
| `IMG_ArtLockIcon` | **ổ khoá** khi ô chưa mở |
| `IMG_ArtCoin` | icon vàng (giá mở ô) |
| `IMG_ArtSpeakerIcon` | badge **loa** nhỏ khi đang bật quảng cáo |

---

## 5. MarketListingCard_Prefab — thẻ hàng ở bảng tin (3 ô)

| Object | Gắn gì |
|---|---|
| `Image_Icon` | ❌ **ĐỪNG GẮN** — code tự điền icon vật phẩm |
| `Icon_Gold_ChoArt` | icon đồng vàng |
| `Image_SellerAvatar_ChoArt` | **avatar mặc định** người bán |

---

## 6. Canvas_MarketPopup — bảng tin chợ (3 ô, sửa thẳng trong scene)

Cái này **không phải prefab**, nằm thẳng trong scene nên sửa trực tiếp được.

| Object | Gắn gì |
|---|---|
| `Deco_RibbonTop` | dải trang trí đỉnh popup |
| `Deco_RibbonDots` | hoạ tiết chấm trên dải |
| `Btn_Refresh ▸ Icon_Gold_ChoArt` | icon đồng vàng |
| `Chip_Gold ▸ Icon_Gold_ChoArt` | icon đồng vàng |

Icon danh mục của bảng tin nằm trong `MarketCategoryTab_Prefab.prefab` — mở prefab đó gắn.

---

## 7. Thứ CHẶN, không phải art

Font mặc định `LiberationSans SDF` là **Static, 250 ký tự, fallback rỗng** — không có `ầ ắ đ Đ`. Mọi chữ tiếng Việt ở hai popup sẽ thiếu dấu.

**Cách sửa:** `Window ▸ TextMeshPro ▸ Font Asset Creator`, chọn một font có đủ tiếng Việt (Roboto, Be Vietnam Pro, Nunito), đặt **Atlas Population Mode = Dynamic**, rồi gán làm font mặc định trong `TMP Settings`.

Việc này ảnh hưởng **cả game** chứ không riêng chợ — 229/230 component TMP trong scene đang dùng font thiếu dấu này.

---

## Bảng tick

- [ ] Kéo `Stall_WorldObject` ra chỗ hợp lý, kiểm Sorting Layer + Collider
- [ ] Gắn 3 sprite cho `Stall_WorldObject` (bỏ qua DisplaySlot)
- [ ] Mở `Canvas_StallPopup.prefab`, gắn 13 ô
- [ ] Mở `PF_StallSlot.prefab`, gắn 5 ô
- [ ] Mở `MarketListingCard_Prefab.prefab`, gắn 2 ô (bỏ qua `Image_Icon`)
- [ ] Gắn 4 ô trong `Canvas_MarketPopup` ngay trong scene
- [ ] Mở `MarketCategoryTab_Prefab.prefab`, gắn icon danh mục
- [ ] Đổi font TMP sang font có dấu tiếng Việt
- [ ] Ctrl+S
