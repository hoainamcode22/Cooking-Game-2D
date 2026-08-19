# Hướng dẫn dọn ảnh thừa — UnusedAssetAuditTool

**Nguyên tắc: tool KHÔNG BAO GIỜ XOÁ file.** Ảnh nghi thừa chỉ bị MOVE vào
`Assets/_UNUSED_QUARANTINE/` (giữ nguyên cây thư mục, GUID không đổi) và luôn có nút
hoàn tác. Xoá thật hay không là quyết định của bạn, sau khi đã test chán chê.

## Cài đặt

Chép `UnusedAssetAuditTool.cs` vào một folder tên **Editor** bất kỳ, ví dụ
`Assets/_Game/Farm/Editor/`. Menu xuất hiện ở **Tools → Farm Game → Asset Audit**.

## Quy trình khuyên dùng

1. **Commit git trước** — lưới an toàn số 1, làm gì cũng quay lại được.
2. Chạy **`1. Scan Unused Images (Dry-Run)`** — chỉ quét, chưa đụng file nào.
3. Chạy **`4. Open Report CSV`** → mở `Assets/_UNUSED_AUDIT_REPORT.csv` bằng Excel,
   duyệt kỹ **cột `root_folder`**. Nghi phạm chính của project mình: `thietke`
   (file thiết kế export), `maptitle`, `Anh`, `Test nước`, `_Debug_Capture`.
   Thấy folder nào "lạ lạ mà hình như đang dùng" → khoan cách ly, hỏi team trước.
4. Chạy **`2. Quarantine Unused Images (MOVE — có hoàn tác)`** — tool scan lại rồi
   move ảnh thừa vào `Assets/_UNUSED_QUARANTINE/`, có dialog xác nhận số lượng + MB.
5. **Bấm Play test các scene chính** (farm, cooking, title...). Đi đủ các màn hình UI.
6. **Để 1–2 tuần dev tiếp bình thường.** Không ai kêu thiếu ảnh →
7. Lúc đó mới **zip folder `_UNUSED_QUARANTINE` ra NGOÀI project** cất làm backup,
   rồi xoá folder trong Assets. (Tool không tự xoá — bước này bạn làm tay.)

**Nếu thiếu ảnh** (sprite trắng / None / ô vuông hồng): chạy
**`3. Restore ALL From Quarantine`** — mọi ảnh về đúng chỗ cũ theo
`_restore_map.txt`, dòng nào restore xong tự xoá khỏi map.

## Vì sao an toàn

- Move bằng `AssetDatabase.MoveAsset` → **GUID không đổi**, reference nào lỡ sót vẫn
  tự trỏ theo chỗ mới, không vỡ prefab/scene.
- Loại trừ cứng, không bao giờ đánh dấu: `Editor/`, `Resources/`, `StreamingAssets/`,
  `TextMesh Pro/`, `Settings/`, `Plugins/`, `Packages/`.
- `EXTRA_KEEP_FOLDERS` (đầu file .cs) mặc định giữ nguyên **`Assets/_Game`** — art
  gameplay đang được load bằng code nhiều dạng, quét vào dễ dính oan. Khi đã tự tin
  (đã play-test đủ vòng), có thể bỏ dòng này để quét sâu hơn vào `_Game`.

## Giới hạn phải nhớ

Tool tính "đang dùng" bằng dependency từ scene trong Build Settings + Resources +
ProjectSettings + SpriteAtlas. Nó **KHÔNG thấy** ảnh load bằng code:
`Resources.Load` (đã né sẵn vì loại trừ Resources), Addressables, hay load sprite
theo **tên string từ folder thường**. Project có kiểu load đó → bắt buộc test kỹ
sau khi cách ly, đó là lý do có bước "để 1–2 tuần" ở trên.

## Ghi chú thêm

Hai folder `art-backup-original/` và `art-pipeline-45/` nằm **NGOÀI Assets** nên
không ảnh hưởng game và tool cũng không quét tới — muốn gọn repo thì zip cất luôn,
không cần tool.
