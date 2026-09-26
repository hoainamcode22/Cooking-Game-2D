# Prompt cho doi ve: CHO (Market) nhieu lop, co animation nhu may xay (2026-09-25)

## Nhan xet cho hien tai (chợ.png, 409x610)
- Cung phong cach ve tay mem nhu may xay moi, nhung MAI GO QUA NHAT (be sang, it bong) nen nhin "phai mau" canh may xay go dam, am.
- Bong do tren nen dat gan nhu khong co -> cho nhu "noi" tren map. May xay moi co bong + be da day dan.
- Chi co 1 hinh tinh -> khong co gi chuyen dong. Can tach lop nhu may xay.

## Quy tac (giong may xay)
1. Ve cho MOT lan (file goc). Moi lop xuat tu cung file goc, CUNG canvas, CUNG goc toa do -> xep chong khop tung pixel.
2. BASE = cho day du nhung BO bo phan chuyen dong. Cho bo phan do che thi ve kin phan phia sau.
3. Frame bo phan: CHI co bo phan do, con lai trong suot. Khong ve lai mai / cot / bong.
4. Khong ve quang sang den, khong ve khoi vao frame (engine lam).
5. Loop lien: frame cuoi noi muot ve frame dau.
6. Isometric 2:1, anh sang tren-trai, bang mau go AM + DAM giong may xay (feedmill_base.png lam mau tham chieu).

## Kich thuoc
- Canvas moi lop: 820 x 820 px (gap doi cho cu). Chan cho (goc duoi cung cua be) nam o GIUA mep duoi canvas, cach mep duoi ~16 px.
- Strip frame ghep ngang, moi o dung 820x820.

## File can giao (Assets/Art/Buildings/Market/)
| File | Noi dung | Frame |
|---|---|---|
| market_base.png | Quay cho day du: mai go, 4 cot, ke hang day trai cay rau cu, bao gao, binh mat ong, chau hoa nho, be go + bong do tren dat. KHONG co 2 den long, KHONG co co duoi ca tren noc, KHONG co bang gia treo | 1 |
| market_flag.png | Day co duoi ca nho nhieu mau vat ngang truoc mai, bay pho phat trong gio | 6 |
| market_sign.png | Bang gia go nho treo 2 day thung o goc mai, dung dua nhe trai-phai | 6 |
| market_lantern.png | 2 den long treo duoi mai (than den, chua sang) | 1 |
| market_lantern_glow.png | Quang sang vang mem cua 1 den, chi quang sang, 160x160 | 1 |
| market_produce_01..06.png | 6 mon le de code cho "nay": tao do, cam, ca rot, bap cai, chuoi, ot. Moi mon 96x96, co bong nho duoi chan | 1 moi mon |
| market_keeper_idle.png (tuy chon) | Co ban hang dung sau quay, go vai tay / lau quay, 256x320 moi o | 8 |

## Prompt tieng Anh (AI) - BASE
"Isometric 2:1 cozy farm game market stall, hand-painted stylized mobile game art, warm saturated wood with deep soft shadows (match reference image of a wooden feed mill), wooden shingle roof, four posts, shelves full of fruits and vegetables in crates, rice sacks, honey jar, small flower pot, wooden base platform with soft ground shadow. Static only: NO hanging lanterns, NO bunting flags, NO hanging price sign, NO light glow, NO smoke. Transparent background, centered, full stall visible."

## Prompt - BO PHAN (inpaint tren CHINH anh base, khong tao anh moi)
"Only the [colorful triangle bunting flags across the front of the roof / small wooden price sign hanging on two ropes / two unlit hanging lanterns], isolated on a fully transparent background, exact same position, scale, perspective and lighting as in the base image, animation frame N of 6, seamless loop, nothing else."

## Prompt - MON LE
"Single [red apple / orange / carrot / cabbage / banana / chili] for a cozy isometric mobile farm game, hand-painted, soft top-left light, tiny soft shadow under it, transparent background, 96x96."

## Phia code (Tech Lead lam khi co hinh, giong may xay)
- Base dung yen, co + bang gia chay frame, den nhap nhay nhe, quang sang manh hon ban dem.
- Moi vai giay 1 mon le "nay" len khoi ke roi roi lai cho cu (nhu hang vua duoc bay ra); khi nguoi choi ban duoc hang: 2-3 mon nay lien tiep.
- Co ban hang (neu co) dung idle, thinh thoang go tay.
