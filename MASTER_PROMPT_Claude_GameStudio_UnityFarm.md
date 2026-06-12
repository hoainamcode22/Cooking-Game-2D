# MASTER PROMPT — Claude Game Studio Agents cho Unity Farming/Cooking Game

## Bối cảnh dự án

Project Unity hiện tại:

```text
E:\Game2\Cooking-Game-2D
```

Lưu ý: một số ghi chú cũ có nhắc `E:\game1\Cooking-Game-2D`. Nếu gặp đường dẫn đó, hãy hiểu là đường dẫn cũ. Ưu tiên làm việc trên project root hiện tại:

```text
E:\Game2\Cooking-Game-2D
```

Game là **Unity 2D farming/cooking casual**, phong cách **isometric diamond 45°**, hướng tới **phụ nữ và trẻ em**. Mục tiêu là game phải dễ hiểu, dễ chơi, có cảm giác “build liên tục”, nhiều animation/VFX nhẹ nhàng, màu sắc thân thiện, cozy, gần kiểu Hay Day/Township.

Các scene/module quan trọng dự kiến:
- Farm chính: `SCN_Farm`
- Cooking: `SampleScene`
- Home/menu: `SCN_Home`
- Village order data: `Assets/_Game/Farm/data/Village_data`
- Shop/building data: `Assets/_Game/Farm/CÔNG TRÌNH`
- VFX/firework assets: `Assets/Lana Studio`
- Thành tựu/reward popup hiện có: `popup_Ewar`

## Mục tiêu tổng

Tôi muốn dùng bộ agent/skill trong Claude-Code-Game-Studios như một đội game studio để:

1. Rà soát project hiện tại thật kỹ.
2. Gom các task bị lặp thành roadmap rõ ràng.
3. Chia cho đúng nhóm agent phụ trách: economy, design, Unity dev, UI/UX, VFX/audio, QA/risk, producer.
4. Thiết kế lại progression Level 1 → Level 30.
5. Thiết kế lại economy vàng/kim cương/shop/village orders.
6. Thiết kế lại tutorial Level 1 → Level 5.
7. Thiết kế hệ thống nhiệm vụ, nhiệm vụ ngày, thành tựu.
8. Thiết kế level-up popup + unlock reward.
9. Cải thiện bubble đơn hàng, VFX, màu popup, thời tiết.
10. Sau khi tôi duyệt từng phần, mới viết tool/script/editor tool để setup hierarchy/prefab/data trong Unity.

## Nguyên tắc làm việc bắt buộc

### Tuyệt đối an toàn

Không được tự ý sửa/xóa logic quan trọng nếu tôi chưa duyệt.

Không tự ý sửa trực tiếp các file sau nếu chưa hỏi:
- `.unity`
- `.prefab`
- `.asset`
- `ScriptableObject`
- data chính đang dùng
- scene YAML
- prefab YAML

Không xóa logic cũ, đặc biệt:
- logic ô đất/plot
- logic kéo hạt giống
- logic thu hoạch
- logic village order hiện tại
- logic shop hiện tại
- logic cooking/inventory
- logic level/EXP hiện tại

Nếu cần thay đổi logic lớn, phải dừng lại hỏi tôi duyệt.

### Quy trình bắt buộc

Làm theo flow:

```text
SCAN → REPORT → PROPOSE → USER APPROVES → IMPLEMENT TOOL/SCRIPTS → QA → REPORT
```

Ở vòng đầu tiên chỉ được:
- scan project
- đọc code/data
- liệt kê file liên quan
- phát hiện hệ thống hiện có
- đề xuất kế hoạch
- đề xuất data/progression
- đề xuất tool sẽ tạo

Chưa được sửa code, chưa tạo file, chưa edit scene nếu tôi chưa gõ rõ: **OK LÀM PHẦN NÀY**.

### Mục tiêu build tool

Sau khi tôi duyệt, ưu tiên tạo các **Unity Editor Tool / Setup Tool** để tôi chỉ cần chạy trong Unity là tool tự setup hierarchy, prefab reference, popup, data hoặc helper object.

Tool nên đặt menu kiểu:

```text
Tools/Farm Game/...
```

Ví dụ:
- `Tools/Farm Game/Setup Mission System`
- `Tools/Farm Game/Setup Village Orders`
- `Tools/Farm Game/Setup Level Up Popup`
- `Tools/Farm Game/Setup Shop Locks`
- `Tools/Farm Game/Setup Bubble VFX`
- `Tools/Farm Game/Clean Old Farm Tools`
```

Tool phải có report rõ ràng:
- tạo object nào
- gắn script nào
- cần kéo sprite/asset nào thủ công
- có backup không
- có thể undo không

## Vai trò agent / team cần dùng

Hãy dùng các agent/skill phù hợp trong Claude-Code-Game-Studios. Nếu có orchestration/team command thì hãy chia việc cho các vai trò sau:

### 1. Producer / Project Manager
Nhiệm vụ:
- gom toàn bộ task thành roadmap
- loại bỏ task trùng
- chia milestone
- xác định task nào làm trước/sau
- giữ scope không bị loạn

### 2. Economy Designer
Nhiệm vụ:
- thiết kế economy Level 1 → Level 30
- tính vàng, kim cương, reward, shop price, order payout
- đảm bảo người chơi mua rẻ → giao đơn có lời
- tính người chơi tiết kiệm, phung phí, muốn nhanh
- thiết kế progression unlock vật phẩm/công trình

### 3. Game Designer / Progression Designer
Nhiệm vụ:
- thiết kế Level 1 → Level 30
- Level 1 → 5 cực kỳ quan trọng, phải dễ hiểu
- Level 5 mở Cooking
- thiết kế unlock seed/flower/animal/building/order/cooking
- thiết kế popup lên cấp và quà

### 4. Unity Developer
Nhiệm vụ:
- scan code hiện tại
- tìm manager/script/data đang dùng
- chỉ sau khi tôi duyệt mới viết script/tool
- ưu tiên Unity Editor Tool để setup tự động hierarchy

### 5. UI/UX Designer
Nhiệm vụ:
- thiết kế mission book/task popup
- level-up popup
- shop lock state
- village bubble order
- popup kho/chợ/nhiệm vụ/thành tựu
- màu chủ đạo xanh + nâu, cozy, sạch, global

### 6. VFX / Animation / Audio Designer
Nhiệm vụ:
- bubble animation
- level-up firework
- popup animation
- weather/time tuning
- sound/vfx suggestions
- kiểm tra `Assets/Lana Studio` có firework nào dùng được

### 7. QA / Risk Manager
Nhiệm vụ:
- bảo vệ logic cũ
- liệt kê rủi ro trước khi sửa
- yêu cầu backup
- yêu cầu tôi duyệt trước khi thay đổi logic quan trọng
- kiểm thử sau khi tool chạy

---

# TASK GROUP A — Audit project trước khi làm

## A1. Scan hệ thống hiện có

Hãy scan project và trả report:

1. Hệ thống level/EXP hiện tại nằm ở file nào?
2. Hệ thống vàng/kim cương nằm ở file nào?
3. Hệ thống shop nằm ở file nào?
4. Hệ thống village order nằm ở file nào?
5. Data đơn hàng hiện nằm ở đâu?
6. Hệ thống cooking hiện nằm ở đâu?
7. Hệ thống achievement/reward popup `popup_Ewar` hiện hoạt động chưa?
8. Có hệ thống mission/quest chưa?
9. Có mission UI/hierarchy/tool chưa?
10. Popup level-up hiện có chưa, hay chỉ có text level-up?
11. Firework/VFX hiện có asset nào dùng được?
12. Bubble order đang tạo bằng script nào?
13. Weather/time system đang ở file nào?
14. Những tool cũ nào đang nằm trong project?

Kết quả cần trả:
- danh sách file liên quan
- hệ thống nào đã có
- hệ thống nào thiếu
- hệ thống nào đang lỗi/nguy hiểm
- phần nào có thể làm bằng tool
- phần nào phải setup thủ công trong Unity Inspector

Chưa sửa file.

---

# TASK GROUP B — Economy + Progression Level 1 → 30

## B1. Thiết kế progression Level 1 → Level 30

Cần thiết kế bảng mở khóa từ Level 1 đến Level 30 gồm:

- seed/crop mở khóa
- flower mở khóa
- animal mở khóa
- building mở khóa
- shop item mở khóa
- cooking mở khóa
- village order difficulty
- reward khi lên cấp
- vàng/kim cương thưởng
- popup level-up hiển thị item nào

Yêu cầu:
- Level 1 → 4 chỉ farm/chăn nuôi đơn giản, chưa ép cooking.
- Level 5 mở Cooking.
- Level đầu phải vui, dễ, không bị thiếu tiền.
- Cứ lên level nên tặng hạt giống/hoa/item mới để người chơi thấy tiến triển.
- Thiết kế cho người chơi có cảm giác build liên tục.

Trả về dạng bảng:

```text
Level | Unlock | Gift | New Shop Items | New Orders | Notes
```

## B2. Thiết kế economy vàng/kim cương

Cần phân tích:
- người chơi tiết kiệm
- người chơi phung phí
- người chơi muốn nhanh
- lượng vàng đầu game
- lượng kim cương đầu game
- giá shop
- giá nguyên liệu
- payout đơn hàng
- EXP từ hành động
- lợi nhuận khi mua nguyên liệu rồi giao đơn
- lợi nhuận khi tự farm rồi giao đơn

Nguyên tắc:
- Mua ở shop giá rẻ hơn.
- Giao đơn hàng phải có lời.
- Không để người chơi lỗ khi làm đúng flow.
- Vật phẩm cấp thấp phải rẻ.
- Đơn hàng cấp thấp phải dễ và trả đủ vàng.

Trả về:
- bảng giá đề xuất
- bảng payout đơn hàng
- công thức tính reward
- cảnh báo rủi ro nếu economy hiện tại đang lỗ

Chưa sửa file.

---

# TASK GROUP C — Village Orders / Nhà dân

## C1. Rà soát hệ thống đơn hàng hiện tại

Đọc logic và data tại:

```text
Assets/_Game/Farm/data/Village_data
```

Kiểm tra:
- mỗi house tạo order thế nào
- bubble hiện thế nào
- order refresh thế nào
- reward tính thế nào
- nhà nào đang có order
- có bao nhiêu house mặc định
- hiện tại vì sao order cấp thấp lại có nguyên liệu khó
- item chưa unlock có lọt vào order không
- payout có bị thấp/lỗ không

Chưa sửa file.

## C2. Thiết kế lại nhà dân và order

Mong muốn mới:

- Đầu game chỉ có 4 hoặc 5 nhà dân order.
- Không hiện bubble tràn lan 12 nhà ngay đầu game.
- Nhà cấp thấp cho order cực dễ.
- Nhà càng cao cấp / giá càng cao thì order payout càng cao.
- User có thể mua thêm nhà từ shop.
- Map có bao nhiêu nhà thì thuật toán tạo order theo số nhà đó.
- Order phải dựa trên level và item đã unlock.
- Order xong thì order tiếp theo update.

## C3. Data đơn hàng Level 1 → Level 30

Thiết kế lại data order:

### Level 1 → 4
Chỉ dùng nguyên liệu farm/chăn nuôi cực dễ. Ví dụ:
- x1 lúa
- x3 lúa
- x10 lúa
- x5 ngô
- x3 bắp cải
- x2 cà chua
- trứng/gà nếu đã unlock

### Level 5
Mở cooking. Bắt đầu có đơn món ăn đơn giản.

Cooking Level 5 nên có 10 món đầu tiên dễ nấu, xoay quanh:
- lúa
- ngô
- bắp cải
- cà chua
- thịt gà
- trứng
- thịt heo
- thịt bò

Có thể tự chế món đơn giản, không cần theo công thức thật quá nghiêm ngặt. Ưu tiên dễ farm, dễ hiểu, dễ giao đơn.

Trả về:
- bảng order Level 1 → 30
- bảng order riêng Level 1 → 10 cực dễ
- công thức reward
- data nào cần chỉnh trong project
- tool nào nên tạo để import data

Chưa sửa file.

---

# TASK GROUP D — Shop Unlock / Building Price / Item Lock

## D1. Rà soát shop hiện tại

Kiểm tra:
- item nào đang mở sẵn
- item nào chưa khóa
- item nào giá quá cao
- building/công trình data ở đâu
- seed/animal/building mở theo level chưa
- shop có lock visual chưa

Đường dẫn cần xem:

```text
Assets/_Game/Farm/CÔNG TRÌNH
```

## D2. Thiết kế lock visual cho shop

Mong muốn:
- Ngoài vật phẩm Level 1, tất cả item còn lại bị khóa.
- Item bị khóa có nền xám/đen.
- Chính giữa có icon ổ khóa.
- Có thể hiển thị text: `Mở ở cấp X`.
- Khi lên cấp đủ, item mở khóa.
- Từ Level 1 → 30 mở dần item.

Cần kiểm tra xem đã có UI lock chưa. Nếu chưa có, đề xuất tool/script setup.

## D3. Bảng giá shop mới

Thiết kế:
- giá hạt giống
- giá nguyên liệu
- giá công trình
- giá nhà dân
- giá chuồng gà/heo/bò
- giá mở rộng farm nếu có

Nguyên tắc:
- mua rẻ, giao đơn có lời
- nhà dân càng đắt, order càng lời
- level thấp không nghèo quá nhanh
- level cao vẫn có mục tiêu cày vàng

Chưa sửa file.

---

# TASK GROUP E — Tutorial Level 1 → Level 5

## E1. Rà soát tutorial hiện có

Kiểm tra:
- tutorial Level 1 hiện có chưa
- popup hướng dẫn có chưa
- camera zoom vào ô đất có chưa
- popup chợ/kho có đang mở sẵn khi Play Mode không
- vì sao chợ/kho mở sẵn
- có hand pointer/spotlight chưa
- có flow trồng lúa/hoa chưa

Yêu cầu sửa sau khi tôi duyệt:
- Play Mode không được tự mở popup chợ/kho.
- Tutorial phải zoom camera vào ô đất.
- Có popup hướng dẫn từng bước.
- Có hand pointer/hint.
- Level 1 hướng dẫn trồng lúa và hoa.
- Lên Level 2 hiện popup lên cấp, tặng hạt giống mới/mở shop item.

## E2. Kịch bản Level 1 → 5

Mong muốn:
- Level 1: trồng lúa/hoa, thu hoạch, giao order dễ.
- Level 2: tặng chuồng gà hoặc mở chuồng gà.
- Hướng dẫn cho gà ăn.
- Có thể bấm kim cương để hoàn tất nhanh quá trình cho ăn.
- Thu hoạch thịt gà/trứng.
- Sau đó hint người chơi mua chuồng heo và chuồng bò để đặt sẵn.
- Level 5 mở cooking.

Agent hãy đề xuất kịch bản chi tiết, gồm:
- bước tutorial
- popup text
- camera target
- UI cần mở
- reward từng bước
- điều kiện hoàn thành
- script/data cần có

Chưa sửa file.

---

# TASK GROUP F — Mission / Daily Mission / Achievement

## F1. Kiểm tra mission system hiện có chưa

Hãy rà soát source:
- có `MissionManager` chưa?
- có `QuestManager` chưa?
- có `TaskManager` chưa?
- có data mission chưa?
- có popup nhiệm vụ chưa?
- có hierarchy UI chưa?
- icon sách dưới icon cây búa đã có chưa?
- nơi gắn ảnh/icon ở đâu?
- tool setup hierarchy nhiệm vụ đã có chưa?

Nếu có, liệt kê file/script/prefab/hierarchy.

Nếu chưa có, đề xuất hệ thống.

## F2. Thiết kế Mission Book

Mong muốn:
- Icon quyển sách dưới icon cây búa.
- Popup nhiệm vụ gồm nhiệm vụ chính, nhiệm vụ ngày, tiến độ, phần thưởng, nút nhận quà.
- Nhiệm vụ Level 1 → 30.
- Daily mission mỗi ngày.
- Người chơi hoàn thành thì nhận vàng/kim cương/EXP/item.

## F3. Kiểm tra achievement / popup_Ewar

Hiện tại có thành tựu/reward popup tên `popup_Ewar`.

Cần kiểm tra:
- thành tựu hoạt động chưa?
- hoàn thành có nhận reward không?
- reward có vào inventory/vàng/kim cương không?
- popup có hiển thị đúng không?
- thiếu gì để nối với mission/daily mission?

Chưa sửa file.

---

# TASK GROUP G — Level-Up Popup / Firework / Reward

## G1. Rà soát level-up hiện tại

Kiểm tra:
- sau Level 1 → 2 có popup lên cấp chưa?
- popup có hiển thị item được tặng chưa?
- hiện tại chỉ có text level-up hay đã có popup hoàn chỉnh?
- hệ thống unlock item khi lên cấp có chưa?
- hệ thống reward khi lên cấp có chưa?

## G2. Thiết kế popup level-up

Mong muốn:
- Khung popup đẹp, cozy, xanh/nâu.
- Trên popup có pháo hoa/firework.
- Hiển thị: `Lên cấp 2`.
- Bên dưới có icon vật phẩm được mở khóa.
- Có phần quà tặng nhận ngay.
- Có animation popup bật lên.
- Có nút nhận quà.
- Có âm thanh vui.
- Sau khi nhận quà, item/gold/gem được cộng thật.

Cần kiểm tra VFX tại:

```text
Assets/Lana Studio
```

Nếu có firework dùng được, đề xuất dùng asset nào và cách gắn vào popup/scene.

Chưa sửa file.

---

# TASK GROUP H — VFX / Animation / UI Visual Polish / Weather

## H1. Bubble đơn hàng

Hiện tại bubble đơn hàng trên nhà dân:
- nằm cố định
- nhìn xấu
- không có animation
- không tạo cảm giác vui

Cần đề xuất:
- bubble floating animation
- bounce nhẹ
- sparkle khi có order mới
- pop-in khi order xuất hiện
- shake nhẹ khi gần hết thời gian
- complete animation khi giao hàng
- icon order rõ hơn
- bubble bám theo ngôi nhà đẹp hơn

Sau khi tôi duyệt, tạo tool/script setup.

## H2. Đồng bộ màu popup/UI

Tone mong muốn:
- xanh lá nông trại
- nâu gỗ
- vàng ấm
- trắng kem
- cozy, global, thân thiện

Cần scan popup hiện có:
- popup nào lệch style
- màu nào chưa ổn
- hình nào chưa đồng bộ
- UI nào cần redesign

Chưa sửa file.

## H3. Weather/time system

Mong muốn:
- ban ngày dài hơn, ví dụ scale từ 1 lên 4
- ban đêm ngắn hơn, ví dụ 1 → 2
- mưa thoáng qua hơn
- mưa ít lại vì mưa nhiều khó chịu
- thời tiết tạo cảm giác dễ chịu, không làm phiền gameplay

Cần tìm file weather/time system và đề xuất thông số mới.

Chưa sửa file.

## H4. Sound/VFX ideas

Cần đề xuất:
- âm thanh thu hoạch
- âm thanh trồng cây
- âm thanh popup
- âm thanh level-up
- âm thanh bubble order
- âm thanh cooking
- âm thanh nhận thưởng
- VFX nhỏ cho người chơi có cảm giác thỏa mãn

Chưa sửa file.

---

# TASK GROUP I — Tourist Boat / Ship Event Idea

Đây là ý tưởng tương lai, chưa build ngay. Chỉ phân tích và đề xuất.

Mong muốn:
- Cứ khoảng 30 phút có thông báo tàu khách du lịch sắp tới.
- Tàu cập bến, khách du lịch tới đảo.
- Khách order món ăn/vật phẩm farm.
- Người chơi có thời gian chuẩn bị: trồng trọt/chăn nuôi/nấu ăn.
- Giao đủ thì nhận vàng, EXP, điểm vui vẻ của nông trại.
- Độ vui vẻ cao → tàu tới nhanh hơn, tối đa khoảng 15 phút.
- Độ vui vẻ thấp → tàu tới lâu hơn, có thể 45–60 phút.
- Nếu không giao đủ, khách không vui, điểm vui vẻ giảm.
- Khi người chơi thoát game, timer tàu dừng, không tính offline.
- Có thể có bến tàu, slot thu vé, bán dầu, khu mỏ sau này.

Cần agent đề xuất:
- loop gameplay
- công thức timer
- công thức happiness
- UI cảnh báo
- order difficulty
- cách nối với cooking/village/economy
- rủi ro scope
- nên làm sau milestone nào

Chưa sửa file.

---

# TASK GROUP J — Tool Cleanup

Yêu cầu:
- tìm các tool/editor script cũ ngoài tool cần thiết cho `SCN_Farm`
- chỉ liệt kê trước
- tool nào dư, tool nào nguy hiểm, tool nào còn dùng
- chưa được xóa

Sau khi tôi duyệt mới cleanup.

---

# TASK GROUP K — Output đầu tiên tôi cần nhận

Ở vòng đầu tiên, hãy trả về một báo cáo Markdown gồm:

## 1. Project Audit
- file/script/data đã tìm thấy
- hệ thống đã có
- hệ thống thiếu
- rủi ro

## 2. Task Roadmap đã gom lại
- Milestone 1: Tutorial Level 1–5
- Milestone 2: Economy + Shop + Village Order
- Milestone 3: Mission + Achievement
- Milestone 4: Level-up Popup + Firework
- Milestone 5: VFX/UI polish + Weather
- Milestone 6: Tourist Boat future feature
- Milestone 7: Tool cleanup

## 3. Agent Assignment
Bảng:
```text
Task | Agent/Role | Output | Needs Approval?
```

## 4. Data Proposal
- Level 1 → 30 unlock draft
- Village order draft
- shop price draft
- level-up reward draft
- mission/daily mission draft

## 5. Implementation Plan
Chỉ đề xuất, chưa làm:
- sẽ tạo file nào
- sẽ sửa file nào
- sẽ tạo editor tool nào
- tool menu nằm ở đâu
- cần setup Inspector gì

## 6. Approval Checklist
Tách thành từng phần để tôi duyệt:

```text
[ ] Duyệt Level 1–30 progression
[ ] Duyệt economy/shop price
[ ] Duyệt village orders
[ ] Duyệt tutorial Level 1–5
[ ] Duyệt mission/daily mission
[ ] Duyệt achievement integration
[ ] Duyệt level-up popup
[ ] Duyệt firework/VFX
[ ] Duyệt bubble animation
[ ] Duyệt weather tuning
[ ] Duyệt tourist boat idea
[ ] Duyệt cleanup tool cũ
```

Sau báo cáo này, dừng lại chờ tôi duyệt. Không sửa file.
