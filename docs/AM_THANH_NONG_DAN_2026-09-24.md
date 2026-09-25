# Am thanh + Ong nong dan (2026-09-24)
Backup: _Backup_AmThanh_Farmer_20260924_165411 (10 script + 3 .meta sheet farmer)

## Am thanh (Assets/Resources/Audio/*.ogg, mono, tong ~141KB; nguon 2.6MB)
| File | Luc phat | Ghi chu |
|---|---|---|
| sfx_combo_tap | moi lan bam NAU x1 x2... | pitch cao dan theo combo, nut NAU tat tieng bam mac dinh |
| sfx_fire_flicker | di kem combo tap | cooldown 0.22s |
| sfx_fire_burst | lua bung (het combo) + lua lo DANG CHAY (nho, 3-5s/lan khi dang nau) | khong loop |
| sfx_perfect | chu HOAN HAO | duck nhac nen 0.7s |
| sfx_cook_fail | sai cong thuc | |
| sfx_pot_drop | nguyen lieu roi vao noi | cooldown 0.09s -> 2-3 tieng lach tach |
| cooking_sizzle | bat dau nau (3s, am luong 0.5) | |
| sfx_dish_whoosh / sfx_plate_clink | mon bay ra / cham dia | |
| success | nau xong (chi keu 1 lan) | |
| sfx_card_pick | keo the (card_pick) + the bay ve (card_return, pitch tram) | chung 1 file |
| do hiem (vang/kim cuong/gia vi) | tieng vang ting ting (sfx_coin) | |
| sfx_bush_rustle | cham bui co | |
| sfx_item_bounce | icon thu hoach nay len (item_bounce) + vao kho (warehouse_collect, pitch cao) | chung 1 file |
| sfx_locked | o dat chua mo = tieng nut | |
| sfx_pickup_lift | nhac cong trinh trong Edit mode | |
| sfx_tourist_happy | giao mon cho khach | cooldown 1.2s |
Chinh tong am luong SFX moi: AudioManager > heSoSfxMoi. Tieng moi truong: PlaySfxGan() chi phat khi ortho < orthoGanDePhat (620).
Thu muc "Assets/ÂM THANH GAME" nam ngoai Resources -> khong vao build, xoa duoc (can Sep dong y).

## Ong nong dan
1. Tools > Farm Game > Nong Dan > 1 (cat 3 sheet, tao Resources/FarmerConfig.asset, backup .meta _Backup_NongDan_*)
2. (tuy chon) > 2 tao Farmer_Crew trong Hierarchy de chinh tay -> Ctrl+S. Khong tao thi luc Play tu sinh.
3. > 3 kiem tra. > 4 hoan tac cat sprite.
Luat: 1 ong / 4 o ruong dang trong (nhom o gan nhau). Stage 1-3: tuoi / cuoc. Stage 4-5 + chin: di qua di lai. Chau hoa: 1 ong chi tuoi, dung canh chau.
Tham so (FarmerConfig): chieuCaoNhinThay 120, tocDoDi 55, walkFps 6, vet mo 0.28s / song 0.6s / alpha 0.26.
