# HƯỚNG DẪN GẮN ART — BẢNG ĐƠN HÀNG

Tool đã dựng xong khung. Tài liệu này liệt kê **đúng 11 ô chờ ảnh**, không thiếu không thừa.
Mọi ô đều tên bắt đầu bằng `IMG_Art...` (trong popup) hoặc `SPR_Art...` (ngoài map) — gõ
`IMG_Art` vào ô Search của Hierarchy là hiện hết.

---

## ⚠ LÀM NGAY TRƯỚC KHI ĐỘNG VÀO ART

### 1. Bấm Ctrl+S — scene CHƯA được lưu

Tôi vừa đọc file `SCN_Farm.unity` trên đĩa: **không có** `OrderBoardSystem`,
`Canvas_OrderBoardPopup`, `OrderBoard_WorldObject`. Ba object đó đang chỉ nằm trong bộ nhớ
Unity. Đóng Unity hoặc mở scene khác lúc này là mất sạch, phải bấm tool lại từ đầu.

### 2. Về 21 lỗi đỏ trong Console

Tôi đã kiểm tra và **không phải lỗi biên dịch**:

| Kiểm tra | Kết quả |
|---|---|
| `Library/ScriptAssemblies/Assembly-CSharp.dll` | build được, có file |
| `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` | build được, có file |
| Log import asset (`Logs/AssetImportWorker*.log`) | không có lỗi thật |

Nếu code lỗi biên dịch thì tool đã không chạy nổi để in ra "Dựng xong". 21 lỗi đó gần như
chắc chắn là **rác còn sót từ các lần chạy trước** trong phiên Unity này.

→ Bấm **Clear** trên Console, rồi bấm Play. Lỗi nào hiện lại mới là lỗi thật — chụp gửi tôi.

### 3. Kéo `OrderBoard_WorldObject` ra khỏi toạ độ (0,0)

Nó đang nằm ở gốc toạ độ, chồng lên bất cứ thứ gì ở đó. Kéo tới chỗ muốn đặt trên bản đồ —
gần `Market` hoặc `CookingGate` là hợp lý vì người chơi hay lui tới khu đó.

### 4. Ô `Board World Anchor: None` — KHÔNG cần gán nữa

Tôi vừa sửa: `OrderBoardWorldObject` tự khai neo cho con cú lúc chạy. Để trống là đúng.
Chỉ gán khi muốn tay cú chỉ vào một điểm cụ thể (đỉnh mái chẳng hạn) thay vì tâm công trình —
lúc đó dùng ô mới `Neo Trỏ Tay` trên chính `OrderBoard_WorldObject`.

---

## PHẦN A · NGOÀI MAP — `OrderBoard_WorldObject`

### A1. Thân công trình (ảnh bảng gỗ) — **1 ô duy nhất**

```
OrderBoard_WorldObject          ← SpriteRenderer nằm NGAY TRÊN GỐC
```

Đúng như đã chốt: cả công trình là **một ảnh**, không tách mái/chóp/khung như video. Gắn thế nào:

1. Chọn `OrderBoard_WorldObject`
2. Trong Inspector, mục **Sprite Renderer**:
   - **Sprite** → kéo ảnh bảng của bạn vào
   - **Color** → đổi từ `#31503B` (xanh rêu tạm) sang **trắng `FFFFFF`**
     ⚠ Quên bước này thì ảnh bị nhuộm xanh, trông như hỏng.
   - **Draw Mode** → đổi `Sliced` sang **`Simple`**
     ⚠ `Sliced` chỉ hợp với ảnh 9-slice. Ảnh vẽ tay để `Sliced` sẽ méo.

**Kích thước ảnh nên vẽ:** khung tạm đang là `3.2 × 2.4` world unit. Sprite trong dự án dùng
Pixels Per Unit mặc định 100 → vẽ khoảng **320 × 240 px** là khớp 1:1. Vẽ to hơn (640×480,
sắc nét hơn) thì đặt Pixels Per Unit = 200 lúc import.

**Sorting** — tool đã tự chép từ `Market`/`CookingGate` nên bảng nằm đúng lớp với các công
trình khác (`Sorting Layer ID 1669604809`, `Order 500`). **Đừng sửa tay**, sửa là bảng chui
xuống dưới đất hoặc đè lên UI.

**Collider** — `BoxCollider2D` đang `3.2 × 2.6`, offset Y `+0.2`. Ảnh bạn vẽ to/nhỏ khác thì
sửa lại cho vừa, không thì vùng bấm lệch so với hình.

### A2. Năm tờ phiếu ghim trên mặt bảng

```
OrderBoard_WorldObject
  └ OrderMarks
      ├ SPR_ArtOrderMark_0   ★
      ├ SPR_ArtOrderMark_1   ★
      ├ SPR_ArtOrderMark_2   ★
      ├ SPR_ArtOrderMark_3   ★
      └ SPR_ArtOrderMark_4   ★
```

Đây là thứ **video không có** nhưng làm bảng ngoài map sống hẳn: phiếu **xanh** = đơn đã đủ
hàng giao được, phiếu **kem** = chưa đủ. Người chơi liếc từ xa là biết có nên chạy tới bảng không.

Gắn: cùng một ảnh tờ giấy nhỏ cho cả 5 ô.

> ⚠ **Ảnh này phải TRẮNG hoặc XÁM** (grayscale). Code đổi màu chúng bằng `SpriteRenderer.color`
> mỗi giây. Vẽ sẵn tờ giấy màu vàng thì lúc tô xanh sẽ ra màu bùn.

Vị trí 5 tờ đã xếp so le sẵn (2 hàng, nghiêng ±4-5°) cho giống giấy ghim vội. Muốn đổi thì kéo
tay trong Scene view, code không ghi đè vị trí.

---

## PHẦN B · POPUP — `Canvas_OrderBoardPopup`

### ⚠ BẮT BUỘC: sửa trong Prefab Mode, không sửa trên scene

`Canvas_OrderBoardPopup` trong Hierarchy là **bản sao của prefab**
`Assets/_Game/Prefab/ui/OrderBoard/Canvas_OrderBoardPopup.prefab`.

Gắn ảnh thẳng trên Hierarchy → Unity chỉ ghi thành *instance override*. Lần sau ai bấm lại tool,
hoặc Revert prefab, là **bay hết công gắn art**.

**Cách đúng:** Project window → double-click file `.prefab` → cửa sổ Prefab Mode mở ra → gắn ảnh
ở đó → Ctrl+S.

### B1. Bốn ô trong popup chính

| Ô | Đường dẫn trong prefab | Kích thước | Video tương ứng |
|---|---|---|---|
| `IMG_ArtPanelBackground` | `Panel_Dim / Popup_Main /` | **1500 × 860** (stretch) | nền gỗ của bảng lớn |
| `IMG_ArtTitleIcon` | `.../ Popup_Main / TitlePill /` | **54 × 54** | icon cuộn giấy cạnh chữ tiêu đề |
| `IMG_ArtCustomerAvatar` | `.../ Col_Detail / Detail_Content / Frame_Avatar /` | **122 × 122** | mặt con vật đặt hàng |
| `IMG_ArtTrashIcon` | `.../ Popup_Main / Btn_Discard /` | **56 × 56** | thùng rác bỏ đơn |

`IMG_ArtRewardIcon` (46×46, hai ô trong `Box_Reward / Row_Exp` và `Row_Gold`) — đây là icon
**EXP** và **vàng**. Dự án đã có sẵn icon vàng, kéo dùng lại cho khớp phần còn lại của game.

> Với mọi ô trên: sau khi kéo Sprite vào, **đặt Color = trắng `FFFFFF`**. Tool tô màu tạm cho
> dễ nhìn lúc chưa có art, giữ nguyên màu đó thì ảnh bị nhuộm.

### B2. Avatar 12 khách hàng — vừa làm xong đường gắn

Trước đây avatar chỉ tô **màu ngẫu nhiên theo mã khách**, không có chỗ gắn ảnh. Tôi vừa thêm.

Chọn `Canvas_OrderBoardPopup` → component **Order Board Popup UI** → mục **Ảnh Khách Hàng**.
Unity sẽ tự điền sẵn **12 dòng có nhãn mã khách**, mỗi dòng một ô Sprite trống:

| # | Mã | Con vật | # | Mã | Con vật |
|---|---|---|---|---|---|
| 1 | `heo` | Heo | 7 | `bo` | Bò |
| 2 | `cun` | Cún | 8 | `vit` | Vịt |
| 3 | `meo` | Mèo | 9 | `ga` | Gà |
| 4 | `tho` | Thỏ | 10 | `soc` | Sóc |
| 5 | `gau` | Gấu | 11 | `nai` | Nai |
| 6 | `cuu` | Cừu | 12 | `chuot` | Chuột |

Chỉ việc kéo ảnh vào cột bên phải. **Không sửa cột mã** — sai một ký tự là avatar im lặng không
hiện, và không có gì báo lỗi.

**Gắn được bao nhiêu dùng bấy nhiêu:** vẽ xong 5 con thì 5 con đó hiện ảnh, 7 con còn lại vẫn
hiện khối màu như cũ. Không phải chờ đủ bộ 12 mới dùng được.

Ảnh nên vẽ **~244 × 244 px** (gấp đôi ô 122 cho nét).

### B3. Tờ phiếu — prefab `PF_OrderTicket`

File riêng: `Assets/_Game/Prefab/ui/OrderBoard/PF_OrderTicket.prefab`. Đây là thứ được nhân bản
9 lần thành lưới 3×3 — **phải sửa trong prefab**, sửa trên scene không có tác dụng vì lưới sinh
lúc chạy.

| Ô | Kích thước | Ghi chú |
|---|---|---|
| `State_Filled / IMG_ArtTicketPaper` | **250 × 210** | tờ giấy đơn hàng, chính là hình chữ nhật trắng trong video |
| `State_Filled / IMG_ArtPin` | **42 × 42** | cái ghim/đinh mũ ở mép trên tờ giấy |
| `State_Filled / Row_Exp / IMG_ArtRewardIcon` | 44 × 44 | icon EXP |
| `State_Filled / Row_Gold / IMG_ArtRewardIcon` | 44 × 44 | icon vàng |

> ⚠ `IMG_ArtTicketPaper` **phải là ảnh trắng/xám**. Code đổi màu tờ giấy theo 4 trạng thái
> (thường / giao được / đang chọn / trống). Vẽ sẵn màu be là 4 trạng thái nhìn như nhau.

### B4. Ô nguyên liệu — prefab `PF_OrderRequireCell`

File riêng: `.../PF_OrderRequireCell.prefab`. Lưới 3×2 bên phải, cũng sinh lúc chạy.

| Ô | Kích thước | Ghi chú |
|---|---|---|
| `State_Filled / IMG_ArtItemIcon` | **68 × 68** | **KHÔNG cần gắn gì** |

Ô này code tự lấy icon từ `StallItemCatalog` — cùng bộ icon cà rốt/ngô/món ăn mà chợ và kho
đang dùng. Gắn ảnh cố định vào đây là **sai**, mọi đơn sẽ hiện chung một hình.

Chỉ đụng vào nếu icon không hiện → nghĩa là scene thiếu `StallItemCatalog`, chạy lại tool dựng chợ.

---

## PHẦN C · CÁC PHẦN KHÔNG CẦN ART

Tool đã sinh sẵn bằng code, chạy được ngay, gắn ảnh vào chỉ đẹp hơn chứ không bắt buộc:

- nút X đỏ lồi ra mép panel · nút **GIAO HÀNG** · nút bỏ đơn
- viền vàng phiếu đang chọn (`Frame_SelectedGlow`)
- dấu tick xanh đơn đủ hàng (`Check_Badge`)
- gạch nét đứt ngăn ô thưởng và lưới yêu cầu (`IMG_DashDivider`)
- 8 làn khói + 6 icon bay + 2 nhãn `+EXP` `+Vàng` khi giao xong (`FX_DeliverRoot`)
- toast báo lỗi (`Message_Toast`)

**Con cú giao hàng** — như bạn dặn, tôi không đụng vào. Vẽ anim xong thì gắn vào
`AnimalGuideController`, hệ thống trỏ tay đã nối sẵn với bảng.

---

## THỨ TỰ LÀM

```
1. Ctrl+S                                    ← quan trọng nhất, scene chưa lưu
2. Clear Console → Play → xem còn lỗi đỏ nào không
3. Kéo OrderBoard_WorldObject tới vị trí trên map
4. Gắn ảnh thân bảng (A1)  → Color trắng, Draw Mode Simple
5. Gắn ảnh tờ phiếu ghim (A2) → ảnh phải grayscale
6. Prefab Mode: Canvas_OrderBoardPopup → 4 ô (B1)
7. Inspector: 12 avatar (B2)
8. Prefab Mode: PF_OrderTicket → giấy + ghim (B3)
9. Ctrl+S lại
```

## KIỂM TRA CUỐI

Bấm Play, rồi:

- [ ] Bấm vào bảng ngoài map → popup mở
- [ ] Lưới trái có 9 tờ phiếu, mỗi tờ tên đơn + EXP + vàng khác nhau
- [ ] Bấm một phiếu → cột phải hiện avatar + tên đơn + lưới nguyên liệu `có/cần`
- [ ] Ít nhất **2 đơn** có dấu tick xanh (giao được ngay) — đây là điều kiện hệ thống tự bảo đảm
- [ ] Bấm **GIAO HÀNG** → khói + số bay lên + vàng/EXP tăng + phiếu mới điền vào chỗ trống
- [ ] Đóng popup → nhìn bảng ngoài map: có tờ phiếu **xanh** ứng với số đơn giao được
- [ ] Vào Edit Mode → phiếu trên bảng biến mất, vào lại thì hiện lại

Mục nào không đạt thì chụp Console gửi tôi.
