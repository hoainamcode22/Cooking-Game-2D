# PROMPT GỬI GPT (agent-sprite-forge) — BỘ ASSET SCENE BẾP "KITCHEN COOK FLOW" (Sprint K2)
> ĐÍNH KÈM & TUÂN THỦ TUYỆT ĐỐI: `production/ART_RULES_STUDIO.md`
> (KHÔNG text/số trên asset · KHÔNG nền/bóng · meta Single · frame cùng canvas · style bớt AI-look).
> Style chuẩn: theo bộ Export_Train_UI_Package (kem #F5E9D0, nâu gỗ #5B3417, cam #E8A33D,
> outline nâu đậm cartoon dày, khối 3D mềm, dễ thương cho phụ nữ & trẻ em).
> Tham chiếu bố cục: mockup "Kitchen Cook Flow" của Sếp (đội đã có). Vẽ LẠI theo tay game-art,
> tránh gradient/blur kiểu AI, ưu tiên mảng màu phẳng + outline.

Xuất vào: `Assets/Export_Kitchen_UI_Package/Sprites/`

## A. NỀN & KHÔNG GIAN BẾP
1. `kitchen_wall_tile.png` (512x512, tile ngang được) — tường gỗ kem sọc dọc nhẹ.
2. `kitchen_floor_diamond_tile.png` (512x512, tileable) — sàn caro thoi nâu 2 tông.
3. `kitchen_shelf_props.png` — kệ treo: 2 chảo đồng + xiên que + hành tỏi treo (1 cụm trang trí).
4. `plant_pot.png` · `sack_flour.png` · `cat_sleeping.png` (mèo vàng nằm ngủ cuộn tròn) — 3 file rời.

## B. LÒ NƯỚNG ĐẤT (trọng tâm scene — 3 trạng thái + lửa)
5. `oven_body.png` (~512x512) — lò đất nung vòm cam-nâu, miệng lò tối, đế gạch.
6. `oven_fire_01..04.png` — CHỈ ngọn lửa + than hồng trong miệng lò (4 frame loop, canvas nhỏ riêng
   đặt vừa miệng lò, không vẽ lại thân lò).
7. `oven_glow.png` — quầng sáng cam mờ alpha (đặt sau lửa).

## C. TRẠM CHẾ BIẾN
8. `prep_table.png` — bàn gỗ + thớt + dao (không món ăn).
9. `plating_table.png` — bàn gỗ + dĩa sứ trắng trống.
10. `warehouse_hatch.png` — hộp cửa gỗ "VÀO KHO" (cửa trượt, KHÔNG chữ — biển trống).
11. `chalkboard_menu.png` — bảng đen viền gỗ TRỐNG (chữ món do game render).

## D. MÈO THẦN TÀI (linh vật — animation)
12. `maneki_idle_01..04.png` — mèo trắng tam thể đeo yếm đỏ, 1 tay vẫy lên xuống (4 frame loop,
    thân đứng yên cùng vị trí, chỉ tay + tai nhúc nhích), KHÔNG chữ trên yếm/đồng xu.

## E. UI KHUNG & WIDGET (9-slice ghi rõ border)
13. `panel_board_wood.png` (9-slice 36px) — khung panel công thức nâu đậm.
14. `panel_paper_cream.png` (9-slice 24px) — giấy kem lót trong panel.
15. `card_ingredient.png` (9-slice 20px) — thẻ nguyên liệu bo góc kem viền nâu.
16. `card_selected_glow.png` (9-slice 20px) — viền chọn xanh lá phát sáng (overlay).
17. `card_locked.png` (9-slice 20px) + `icon_lock.png` — thẻ khoá xám + ổ khoá.
18. `taste_bar_track.png` (9-slice 10px) + `taste_bar_fill.png` + `taste_marker.png` (vạch đỏ 6x28).
19. `btn_big_green.png` / `btn_big_gray.png` / `btn_red_small.png` (9-slice 16px) — nút NẤU/chờ/Bỏ hết.
20. `tab_pill_on.png` / `tab_pill_off.png` (9-slice 14px) · `chip_taste.png` (pill nhỏ 9-slice)
    · `ribbon_header_orange.png` (9-slice 28/14) — ruy-băng tiêu đề panel/đơn khách.

KHÔNG cần vẽ: icon 21 nguyên liệu/gia vị + icon món ăn (game có sẵn trong data asset).
Nghiệm thu: ghép thử 1 màn theo mockup — mọi khung co giãn 9-slice không méo góc.
