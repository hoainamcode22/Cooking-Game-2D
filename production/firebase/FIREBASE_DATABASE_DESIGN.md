# THIẾT KẾ DATABASE FIREBASE — COOKING-FARM-2D

> **Phiên bản:** 1.0 · 2026-08-19 · Backend Architect
> **Phạm vi:** Leaderboard · Kết bạn + Presence · Tham quan làng · Chợ liên server · Catalog tĩnh
> **Stack đã chốt:** Cloud Firestore (database chính) + Realtime Database (chỉ presence) + Firebase Auth + App Check. Unity 6.3, Firebase SDK for Unity.
> **Ràng buộc kinh tế:** Không có tiền thật, không IAP. Toàn bộ vàng/gem kiếm trong game. EXP lên level: `40 + 10n + n²`, max L30.

---

## MỤC LỤC

1. [Kiến trúc tổng quan](#1-kiến-trúc-tổng-quan)
2. [Schema Firestore chi tiết](#2-schema-firestore-chi-tiết)
3. [Schema RTDB — Presence](#3-schema-rtdb--presence)
4. [Phân tích bảo mật](#4-phân-tích-bảo-mật)
5. [Ảnh vật phẩm để đâu](#5-ảnh-vật-phẩm-để-đâu)
6. [Pipeline import catalog](#6-pipeline-import-catalog)
7. [Checklist triển khai](#7-checklist-triển-khai)
8. [Ước tính chi phí free tier](#8-ước-tính-chi-phí)

---

## 1. KIẾN TRÚC TỔNG QUAN

```
┌─────────────────────────────────────────────────────────────────────┐
│  UNITY CLIENT (mobile + Steam)                                       │
│                                                                      │
│  ┌────────────────┐   ┌──────────────────┐   ┌───────────────────┐  │
│  │ SaveSystem     │   │ FirebaseService   │   │ Addressables      │  │
│  │ (JSON local)   │   │ (Auth/Firestore/  │   │ (sprite item —    │  │
│  │ = nguồn sự     │   │  RTDB/Functions)  │   │  ảnh nằm TRONG    │  │
│  │ thật của farm  │   │                   │   │  build, không     │  │
│  │ real-time      │   │                   │   │  lên cloud)       │  │
│  └───────┬────────┘   └────────┬─────────┘   └───────────────────┘  │
│          │  sync snapshot       │                                    │
│          │  (event-based,       │  mọi request đều kèm App Check     │
│          │  không real-time)    │  token + Auth token                │
└──────────┼──────────────────────┼────────────────────────────────────┘
           ▼                      ▼
┌──────────────────────────────────────────────────────────────────────┐
│  FIREBASE                                                             │
│                                                                       │
│  ┌──────────────── Cloud Firestore (database chính) ───────────────┐ │
│  │ users/          hồ sơ + tiền tệ + chỉ số leaderboard             │ │
│  │ usernames/      khoá unique tên hiển thị                         │ │
│  │ farmSnapshots/  ảnh chụp layout farm để tham quan                │ │
│  │ market/         listing chợ liên server                          │ │
│  │ catalog/        data tĩnh vật phẩm (read-only với client)        │ │
│  │ config/         economy config, catalogVersion                   │ │
│  │ transactions/   log giao dịch chợ (chỉ Functions ghi)            │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌── Realtime Database ──┐  ┌── Cloud Functions (Phương án B) ─────┐ │
│  │ /status/{uid}          │  │ purchaseListing / createListing /     │ │
│  │ CHỈ presence           │  │ claimDailyReward / cancelListing      │ │
│  │ (onDisconnect)         │  │ = server authority cho tiền tệ        │ │
│  └────────────────────────┘  └───────────────────────────────────────┘ │
│                                                                       │
│  ┌── Firebase Auth ───────┐  ┌── Firebase Storage ──────────────────┐ │
│  │ Anonymous → link        │  │ CHƯA DÙNG ở MVP. Chỉ dành cho        │ │
│  │ Google/Apple/Steam      │  │ user-generated content sau này       │ │
│  └────────────────────────┘  │ (avatar tự chụp...). KHÔNG chứa       │ │
│                              │ ảnh item catalog.                     │ │
│                              └───────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

### Cái gì nằm đâu — nguyên tắc phân chia

| Dữ liệu | Nơi lưu | Lý do |
|---|---|---|
| State farm chi tiết real-time (cây đang lớn ở giây thứ mấy, con bò đang ăn, vị trí sọt...) | **Local save (JSON — SaveSystem M0-2)** | Thay đổi hàng giây; đẩy lên cloud sẽ nổ quota write ngay. Cloud không cần biết. |
| Hồ sơ người chơi: gold, gems, level, exp, username | Firestore `users/{uid}` | Cần cho leaderboard + chợ + hiển thị khi tham quan. |
| Snapshot farm (layout tĩnh tại một thời điểm) | Firestore `farmSnapshots/{uid}` | Chỉ ghi khi có sự kiện (xây/dời công trình, lên level, mở app...), người khác đọc khi tham quan. |
| Kho đồ phần "được phép bán lên chợ" | Firestore `users/{uid}/inventorySync` | Chỉ sync số lượng item có thể giao dịch, không sync cả kho. |
| Listing chợ | Firestore `market/` | Liên server, mọi người đọc được. |
| Catalog vật phẩm (giá gốc, level unlock) | Firestore `catalog/` | Để Rules/Functions validate giá chợ; client cache local. |
| Bạn bè online/offline | **RTDB `/status/`** | RTDB có `onDisconnect` — Firestore không có. Chỉ dùng RTDB cho đúng việc này. |
| Ảnh item | **Trong game build (Addressables)** | Xem [mục 5](#5-ảnh-vật-phẩm-để-đâu). KHÔNG lên Storage. |

### Cái gì KHÔNG đưa lên cloud (chốt cứng, tránh tranh cãi sau này)

- Timer sinh trưởng cây/vật nuôi, trạng thái máy chế biến từng giây → local.
- Vị trí camera, setting âm thanh, tutorial flags → local.
- Toàn bộ minigame cooking state (flavor vector, điểm từng ván) → local; chỉ đẩy **tổng số món đã nấu** (`stats.dishesCooked`) lên `users/{uid}` cho leaderboard phụ.
- Ảnh sprite item → nằm trong build.

---

## 2. SCHEMA FIRESTORE CHI TIẾT

Quy ước: `itemId` dùng đúng id trong game data hiện tại (`rice`, `bapcai`, `carot`, `cachua`, `seed_bapcai`, `beef`, `egg`, `chicken_meat`, `ga_xao_ot`, `pho_beef`, `salt`, `soysauce`, `hoa_hong`...). Timestamp dùng `Timestamp` của Firestore (Unity SDK map sang `Firebase.Firestore.Timestamp`).

### 2.1 `users/{uid}` — hồ sơ người chơi

Document id = Firebase Auth UID.

| Field | Kiểu | Ghi bởi | Mô tả |
|---|---|---|---|
| `username` | string | client (1 lần, qua transaction với `usernames/`) | Tên hiển thị, unique, lowercase để so khớp. |
| `displayName` | string | client | Tên có dấu/hoa thường tuỳ ý, hiển thị UI. |
| `avatarId` | string | client | Id avatar có sẵn trong build (không phải URL). |
| `gold` | int (number) | **Rules-delta (PA-A) / Functions (PA-B)** | Vàng. Client KHÔNG được set tuỳ ý — xem mục 4. |
| `gems` | int | như trên | Gem. |
| `level` | int | như trên | 1–30. |
| `exp` | int | như trên | EXP trong level hiện tại. |
| `stats.totalDeliveries` | int | như trên | Tổng đơn giao — leaderboard phụ. |
| `stats.dishesCooked` | int | như trên | Tổng món nấu — leaderboard phụ. |
| `stats.marketSales` | int | Functions | Tổng lượt bán chợ thành công. |
| `friendCount` | int | client (PA-A) / Functions | Đếm bạn, chặn quá 100. |
| `lbScore` | int | như `gold` | Điểm leaderboard tổng hợp = `level * 100000 + exp` (xem 2.7). |
| `catalogVersion` | int | client | Version catalog client đang cache (debug/support). |
| `createdAt` | timestamp | client (serverTimestamp) | |
| `updatedAt` | timestamp | client (serverTimestamp) | |
| `flags.suspectedCheater` | bool | Functions only | Anti-cheat flag, client không đọc/ghi. |

```jsonc
// users/a1B2c3D4...
{
  "username": "nongdanmap",
  "displayName": "Nông Dân Mập",
  "avatarId": "avatar_03",
  "gold": 12450,
  "gems": 38,
  "level": 12,
  "exp": 210,             // cần 40 + 10*12 + 12*12 = 304 để lên L13
  "lbScore": 1200210,     // 12*100000 + 210
  "stats": { "totalDeliveries": 87, "dishesCooked": 42, "marketSales": 15 },
  "friendCount": 9,
  "catalogVersion": 7,
  "createdAt": "2026-06-01T09:12:00Z",
  "updatedAt": "2026-08-19T03:40:00Z"
}
```

### 2.2 `users/{uid}/inventorySync/{itemId}` — kho đồ giao dịch được

Chỉ chứa item **được phép lên chợ** (nông sản, sản phẩm chuồng, món ăn — KHÔNG chứa hạt giống mua shop, không chứa vật liệu xây như `go`, `da`, `kinh` nếu quyết định cấm giao dịch chúng). Mỗi item một document → update 1 item chỉ tốn 1 write, và Rules validate được từng field.

```jsonc
// users/a1B2c3D4/inventorySync/bapcai
{ "qty": 34, "updatedAt": "<serverTimestamp>" }

// users/a1B2c3D4/inventorySync/egg
{ "qty": 12, "updatedAt": "<serverTimestamp>" }
```

**Chiến lược sync:** client chỉ ghi khi (a) chuẩn bị tạo listing, (b) sau khi mua hàng, (c) định kỳ khi mở app. Không ghi mỗi lần thu hoạch.

### 2.3 `usernames/{usernameLower}` — khoá unique tên

Firestore không có unique constraint → dùng document id làm khoá. Đặt tên = **batched write** tạo đồng thời `usernames/{name}` + update `users/{uid}.username`; Rules chặn tạo nếu doc đã tồn tại (create-only).

```jsonc
// usernames/nongdanmap
{ "uid": "a1B2c3D4...", "createdAt": "<serverTimestamp>" }
```

Đổi tên = batch: delete doc cũ (chỉ chủ sở hữu) + create doc mới + update `users`. Giới hạn: `^[a-z0-9_]{3,16}$`.

### 2.4 Kết bạn — `friendRequests` + `users/{uid}/friends/{fid}`

**Model:** lời mời là collection gốc (dễ query 2 chiều), danh sách bạn là subcollection nhân đôi 2 phía (đọc danh sách bạn của mình = 1 query, không cần composite).

#### `friendRequests/{fromUid}_{toUid}`

Document id ghép `from_to` → tự chống gửi trùng (create sẽ fail nếu tồn tại).

```jsonc
// friendRequests/a1B2c3D4_x9Y8z7W6
{
  "from": "a1B2c3D4",
  "fromUsername": "nongdanmap",
  "fromLevel": 12,
  "to": "x9Y8z7W6",
  "status": "pending",        // pending | accepted | rejected
  "createdAt": "<serverTimestamp>"
}
```

#### `users/{uid}/friends/{friendUid}`

```jsonc
// users/a1B2c3D4/friends/x9Y8z7W6
{
  "username": "beptruongxinh",
  "level": 15,                 // cache lúc kết bạn, refresh khi mở tab bạn bè
  "since": "<serverTimestamp>"
}
```

**Luồng chấp nhận (PA-A, client làm batch 4 write):**
1. update `friendRequests/{id}.status = "accepted"`
2. create `users/{me}/friends/{from}`
3. create `users/{from}/friends/{me}` ← Rules cho phép người-được-mời ghi vào subcollection người mời **chỉ khi** tồn tại request accepted tương ứng (dùng `get()` trong Rules — xem mục 4).
4. Xoá request (hoặc TTL dọn sau).

Từ chối = update `status = "rejected"` rồi client của người gửi tự dọn.

**Query từ Unity:**
```csharp
// Lời mời đến mình, đang chờ
db.Collection("friendRequests")
  .WhereEqualTo("to", myUid)
  .WhereEqualTo("status", "pending")
  .Limit(50);
// → cần composite index: friendRequests(to ASC, status ASC) — console sẽ tự gợi link tạo.
```

### 2.5 `farmSnapshots/{uid}` — tham quan làng

Một document duy nhất mỗi user, **ghi đè toàn bộ** mỗi lần snapshot (tránh phình). Chỉ chứa layout tĩnh + vài con số trưng bày — KHÔNG chứa timer, không chứa inventory. Mục tiêu < 20 KB (trần Firestore 1 MB, dư rất xa).

```jsonc
// farmSnapshots/a1B2c3D4
{
  "owner": "a1B2c3D4",
  "username": "nongdanmap",
  "level": 12,
  "snapVersion": 3,               // schema version của snapshot, client cũ bỏ qua field lạ
  "takenAt": "<serverTimestamp>",

  // Ô đất trồng trọt: chỉ lưu "đang trồng gì" dạng thô, người xem thấy cây ở stage tượng trưng
  "plots": [
    { "x": 3, "y": 5, "cropId": "bapcai", "stage": 2 },   // stage 0..2 (mầm/lớn/chín) — snapshot tại thời điểm chụp
    { "x": 4, "y": 5, "cropId": "rice",   "stage": 1 },
    { "x": 5, "y": 5, "cropId": null,     "stage": 0 }     // đất trống
  ],

  // Công trình: chuồng, máy chế biến — id trùng penId/machineId trong game data
  "buildings": [
    { "id": "pen_01", "x": 10, "y": 2, "lv": 1 },          // chuồng bò thịt (productItemId: beef)
    { "id": "pen_03", "x": 13, "y": 2, "lv": 2 },          // chuồng gà
    { "id": "machine_mill", "x": 8, "y": 8, "lv": 1 }
  ],

  // Trang trí
  "decor": [ { "id": "deco_fence_wood", "x": 0, "y": 0, "rot": 0 } ],

  // Vài listing đang treo ở "quầy" nhà này (denormalize để người xem thấy ngay, nguồn thật là market/)
  "stallPreview": [
    { "listingId": "L_9fK2...", "itemId": "egg", "qty": 10, "price": 45 }
  ]
}
```

**Khi nào ghi snapshot:** đặt/dời/nâng cấp công trình, lên level, tạo/huỷ listing, và tối đa 1 lần/5 phút khi có thay đổi plots (debounce phía client). Người xem chỉ tốn **1 read** cho cả làng.

### 2.6 `market/{listingId}` — chợ liên server

Document id tự sinh (`db.Collection("market").Document()`).

| Field | Kiểu | Mô tả |
|---|---|---|
| `sellerUid` | string | |
| `sellerUsername` | string | denormalize để render list không tốn read |
| `itemId` | string | phải tồn tại trong `catalog/` |
| `qty` | int | 1–999 |
| `pricePerUnit` | int | vàng/đơn vị, bị kẹp trong khoảng % giá catalog |
| `totalPrice` | int | `qty * pricePerUnit` (denormalize cho query "rẻ nhất") |
| `status` | string | `active` \| `sold` \| `cancelled` |
| `buyerUid` | string \| null | set khi sold |
| `createdAt`, `soldAt` | timestamp | |
| `expiresAt` | timestamp | ví dụ +48h; quá hạn client tự coi là hết, TTL policy dọn |

```jsonc
// market/L_9fK2pQx7...
{
  "sellerUid": "a1B2c3D4",
  "sellerUsername": "nongdanmap",
  "itemId": "egg",
  "qty": 10,
  "pricePerUnit": 45,          // giá gốc catalog egg = 40 → nằm trong ±50% hợp lệ
  "totalPrice": 450,
  "status": "active",
  "buyerUid": null,
  "createdAt": "<serverTimestamp>",
  "expiresAt": "2026-08-21T03:40:00Z"
}
```

**Query từ Unity + index cần tạo:**

```csharp
// Tab "Tất cả" mới nhất
db.Collection("market")
  .WhereEqualTo("status", "active")
  .OrderByDescending("createdAt").Limit(20);
// index: market(status ASC, createdAt DESC)

// Lọc theo item, rẻ nhất trước
db.Collection("market")
  .WhereEqualTo("status", "active")
  .WhereEqualTo("itemId", "egg")
  .OrderBy("pricePerUnit").Limit(20);
// index: market(status ASC, itemId ASC, pricePerUnit ASC)

// Listing của tôi
db.Collection("market")
  .WhereEqualTo("sellerUid", myUid)
  .WhereEqualTo("status", "active");
// index: market(sellerUid ASC, status ASC)
```

Bật **TTL policy** trên field `expiresAt` (Firestore console → TTL) để tự dọn listing hết hạn/đã bán, miễn phí thao tác delete.

### 2.7 Leaderboard — cách làm và giới hạn

**Chọn: field trên `users` + composite index. KHÔNG tạo bảng aggregate ở MVP.**

- Thêm field `lbScore = level * 100000 + exp` (một field số duy nhất → chỉ cần 1 `orderBy`, không dính giới hạn "orderBy nhiều field phải cùng chiều" và không cần tie-break phức tạp). Vì `exp` trong level luôn < 100000 nên công thức không bao giờ đụng trần.
- Bảng phụ: `orderBy("stats.totalDeliveries", DESC)` và `orderBy("stats.dishesCooked", DESC)` — Firestore index được field lồng trong map.

```csharp
// Top 50 theo level/exp
db.Collection("users").OrderByDescending("lbScore").Limit(50);

// Trang tiếp theo (pagination bằng cursor — KHÔNG offset)
db.Collection("users").OrderByDescending("lbScore")
  .StartAfter(lastDocSnapshot).Limit(50);

// Bảng phụ: tổng món nấu
db.Collection("users").OrderByDescending("stats.dishesCooked").Limit(50);
```

**Index cần tạo:** single-field index cho `lbScore`, `stats.totalDeliveries`, `stats.dishesCooked` (mặc định Firestore đã auto-index single field — chỉ cần **exemption** nếu muốn tắt bớt field khác cho rẻ; thực tế không phải tạo gì thêm cho 3 query trên).

**Giới hạn free tier & cách né:**
- 1 lần mở bảng xếp hạng = 50 read. 1000 DAU × 3 lần mở/ngày = 150K read → **vượt 50K/ngày**. Bắt buộc: client **cache leaderboard 10–15 phút** (chỉ refetch khi user bấm refresh và đã quá TTL), chỉ load trang 2+ khi user kéo.
- "Hạng của tôi là #N" chính xác cần đếm toàn collection → đắt. MVP: hiển thị "Top 50/100" + dòng của mình tách riêng không kèm số hạng chính xác (hoặc ước lượng bằng `count()` aggregation: `WhereGreaterThan("lbScore", myScore).Count()` — 1 aggregation read/1000 doc đếm, chấp nhận được ở quy mô nhỏ, nhưng cũng phải cache).
- Khi > ~10K user hoặc cần hạng chính xác: nâng cấp sang **bảng aggregate** `leaderboards/{board}/pages/{n}` do scheduled Function build mỗi 15 phút (mỗi user chỉ tốn 1 read/lần xem). Ghi rõ vào backlog, chưa làm bây giờ.

### 2.8 `catalog/{itemId}` — data tĩnh vật phẩm

Client **chỉ đọc**, và thực tế chỉ đọc khi `config/economy.catalogVersion` đổi (xem 2.9). Nguồn: JSON đội Data xuất từ ScriptableObject, import bằng script mục 6. Field bám theo tên field thật trong asset (`itemID`, `itemName`, `goldPrice`, `sellGold`, `unlockLevel`).

| Field | Kiểu | Mô tả |
|---|---|---|
| `itemId` | string | trùng document id |
| `nameVi` | string | tên tiếng Việt (`itemName` trong asset) |
| `category` | string | `seed` \| `crop` \| `flower` \| `animal_product` \| `dish` \| `ingredient` \| `material` |
| `buyPrice` | int | giá mua shop hệ thống (`goldPrice`); 0 = không bán ở shop |
| `sellPrice` | int | giá bán hệ thống (`sellGold`) — **đây là giá gốc để kẹp giá chợ** |
| `unlockLevel` | int | |
| `spriteKey` | string | Addressables key — quy ước `spriteKey == itemId` (mục 5) |
| `tradable` | bool | được phép lên chợ người chơi không |
| `maxStack` | int | trần số lượng/listing |

```jsonc
// catalog/bapcai
{ "itemId": "bapcai", "nameVi": "Bắp Cải", "category": "crop",
  "buyPrice": 15, "sellPrice": 10, "unlockLevel": 1,
  "spriteKey": "bapcai", "tradable": true, "maxStack": 999 }

// catalog/seed_bapcai
{ "itemId": "seed_bapcai", "nameVi": "Hạt Bắp Cải", "category": "seed",
  "buyPrice": 28, "sellPrice": 0, "unlockLevel": 1,
  "spriteKey": "seed_bapcai", "tradable": false, "maxStack": 999 }

// catalog/ga_xao_ot
{ "itemId": "ga_xao_ot", "nameVi": "Gà Xào Ớt", "category": "dish",
  "buyPrice": 0, "sellPrice": 165, "unlockLevel": 5,
  "spriteKey": "ga_xao_ot", "tradable": true, "maxStack": 99 }
```

> **Lưu ý đồng bộ id:** game hiện có chỗ lệch id (`chicken` vs `chicken_meat` — đã ghi trong `DANH_SACH_NGUYEN_LIEU_CAN_THEM.md`). **Phải chốt bảng id thống nhất TRƯỚC khi import catalog**, vì itemId trên Firestore đổi sau này là migration đau.

### 2.9 `config/{docId}` — cấu hình vận hành

```jsonc
// config/economy
{
  "catalogVersion": 7,          // tăng mỗi lần import catalog → client so sánh, chỉ tải lại khi đổi
  "market": {
    "minPricePct": 50,          // giá listing tối thiểu = 50% sellPrice catalog
    "maxPricePct": 200,         // tối đa = 200%
    "maxActiveListings": 8,     // mỗi user
    "listingTtlHours": 48,
    "taxPct": 0                 // để sẵn nếu sau này muốn sink vàng
  },
  "minVersion": "0.4.0"         // force-update client
}
```

### 2.10 `transactions/{txId}` — log giao dịch (chỉ ở Phương án B)

Chỉ Cloud Functions ghi, client không đọc/ghi. Dùng cho support ("mất vàng oan") và anti-cheat offline analysis.

```jsonc
// transactions/T_x1y2...
{
  "type": "market_purchase",
  "listingId": "L_9fK2pQx7",
  "itemId": "egg", "qty": 10, "totalPrice": 450,
  "buyerUid": "x9Y8z7W6", "sellerUid": "a1B2c3D4",
  "buyerGoldBefore": 2000, "buyerGoldAfter": 1550,
  "at": "<serverTimestamp>"
}
```

### 2.11 Tổng hợp composite index phải tạo (`firestore.indexes.json`)

```jsonc
{
  "indexes": [
    { "collectionGroup": "market",
      "fields": [ {"fieldPath":"status","order":"ASCENDING"},
                  {"fieldPath":"createdAt","order":"DESCENDING"} ] },
    { "collectionGroup": "market",
      "fields": [ {"fieldPath":"status","order":"ASCENDING"},
                  {"fieldPath":"itemId","order":"ASCENDING"},
                  {"fieldPath":"pricePerUnit","order":"ASCENDING"} ] },
    { "collectionGroup": "market",
      "fields": [ {"fieldPath":"sellerUid","order":"ASCENDING"},
                  {"fieldPath":"status","order":"ASCENDING"} ] },
    { "collectionGroup": "friendRequests",
      "fields": [ {"fieldPath":"to","order":"ASCENDING"},
                  {"fieldPath":"status","order":"ASCENDING"} ] }
  ],
  "fieldOverrides": []
}
```

Deploy: `firebase deploy --only firestore:indexes` (hoặc bấm link auto-gợi trong log lỗi lần chạy đầu).

---

## 3. SCHEMA RTDB — PRESENCE

RTDB chỉ làm đúng một việc: bạn bè online. Lý do dùng RTDB: có `OnDisconnect` server-side — client rớt mạng/kill app thì server tự set offline; Firestore không làm được điều này.

```jsonc
// Realtime Database root
{
  "status": {
    "a1B2c3D4": { "state": "online",  "lastSeen": 1755574800000 },  // ServerValue.Timestamp
    "x9Y8z7W6": { "state": "offline", "lastSeen": 1755571200000 }
  }
}
```

**RTDB Rules (`database.rules.json`):**

```json
{
  "rules": {
    "status": {
      ".read": "auth != null",
      "$uid": {
        ".write": "auth != null && auth.uid === $uid",
        ".validate": "newData.hasChildren(['state','lastSeen'])",
        "state":    { ".validate": "newData.isString() && (newData.val() === 'online' || newData.val() === 'offline')" },
        "lastSeen": { ".validate": "newData.isNumber()" },
        "$other":   { ".validate": false }
      }
    },
    "$other": { ".read": false, ".write": false }
  }
}
```

> `.read` mở cho mọi user đã đăng nhập (đọc status người lạ chỉ lộ online/offline — chấp nhận được; nếu muốn chặt hơn phải mirror friend list sang RTDB, không đáng công ở MVP).

**Unity C# — pattern chuẩn:**

```csharp
using Firebase.Database;

public class PresenceService
{
    DatabaseReference _statusRef;

    public void Init(string uid)
    {
        var db = FirebaseDatabase.DefaultInstance;
        _statusRef = db.GetReference($"status/{uid}");

        // .info/connected: biết SDK đang thật sự nối tới RTDB hay không
        db.GetReference(".info/connected").ValueChanged += (s, e) =>
        {
            bool connected = (bool)(e.Snapshot.Value ?? false);
            if (!connected) return;

            // Đăng ký hành vi KHI RỚT KẾT NỐI trước, rồi mới set online
            _statusRef.OnDisconnect().SetValue(new Dictionary<string, object> {
                { "state", "offline" },
                { "lastSeen", ServerValue.Timestamp }
            });
            _statusRef.SetValueAsync(new Dictionary<string, object> {
                { "state", "online" },
                { "lastSeen", ServerValue.Timestamp }
            });
        };
    }

    // Nghe trạng thái từng người bạn (attach khi mở tab bạn bè, DETACH khi đóng)
    public void ListenFriend(string friendUid, Action<bool> onChanged)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference($"status/{friendUid}/state")
            .ValueChanged += (s, e) => onChanged(e.Snapshot.Value as string == "online");
    }
}
```

Chi phí: RTDB free tier 100 kết nối đồng thời, 1 GB lưu, 10 GB/tháng download — node status vài chục byte/user, presence gần như miễn phí. **100 concurrent là trần thật của gói Spark** — với 1000 DAU peak concurrent thường ~5–10% = chạm trần; đây là một lý do nữa để lên Blaze trước soft launch (Blaze: 200K concurrent).

---

## 4. PHÂN TÍCH BẢO MẬT

### 4.0 Nguyên tắc bất biến (áp dụng cho cả A lẫn B)

1. **Client không bao giờ được tự ý ghi `gold`/`gems`/`level`/`exp`/`lbScore` thành giá trị tuỳ chọn.** Mọi thay đổi tiền tệ phải hoặc (A) qua Rules kiểm tra delta, hoặc (B) qua Cloud Functions.
2. `catalog/`, `config/` — client **read-only tuyệt đối**. `transactions/` — client không đọc không ghi.
3. **App Check bật enforce** cho Firestore + RTDB + Functions ngay từ đầu (Play Integrity trên Android, App Attest/DeviceCheck trên iOS; Steam/desktop dùng debug provider lúc dev và cân nhắc custom provider sau — lưu ý App Check chặn script kiddie gọi REST trực tiếp, KHÔNG chặn được app bị mod).
4. Mọi rule đều `request.auth != null` — không có đường đọc/ghi ẩn danh không-auth (Anonymous Auth vẫn là auth).
5. Không bao giờ tin field do client denormalize (`sellerUsername`, `fromLevel`...) cho logic tiền — chúng chỉ để hiển thị.

### 4.1 PHƯƠNG ÁN A — Security Rules only (gói Spark, 0đ)

Toàn bộ logic chạy trên client; Rules là hàng rào duy nhất. Dưới đây là bộ `firestore.rules` đầy đủ, copy-paste được.

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {

    /* ===================== HELPERS ===================== */
    function signedIn() { return request.auth != null; }
    function isSelf(uid) { return signedIn() && request.auth.uid == uid; }
    function incoming() { return request.resource.data; }
    function existing() { return resource.data; }
    // Các field bị thay đổi trong lần ghi này
    function changed() { return incoming().diff(existing()).affectedKeys(); }

    function isInt(v) { return v is int; }
    function inRange(v, lo, hi) { return isInt(v) && v >= lo && v <= hi; }

    function catalog(itemId) {
      return get(/databases/$(database)/documents/catalog/$(itemId)).data;
    }
    function econ() {
      return get(/databases/$(database)/documents/config/economy).data;
    }

    /* ===================== users ===================== */
    match /users/{uid} {
      // Ai đăng nhập cũng đọc được hồ sơ (leaderboard, tham quan, chợ)
      allow read: if signedIn();

      allow create: if isSelf(uid)
        && incoming().username is string
        && incoming().username.matches('^[a-z0-9_]{3,16}$')
        && incoming().displayName is string && incoming().displayName.size() <= 24
        // Giá trị khởi tạo CỐ ĐỊNH — không cho tự phát vàng lúc tạo acc
        && incoming().gold == 500 && incoming().gems == 10
        && incoming().level == 1 && incoming().exp == 0
        && incoming().lbScore == 100000
        && incoming().stats.totalDeliveries == 0
        && incoming().stats.dishesCooked == 0
        && incoming().friendCount == 0;

      allow update: if isSelf(uid)
        // Không được đụng các field cấm
        && !changed().hasAny(['createdAt', 'flags'].toSet())
        // ---- Trick chặn tiền tệ bằng delta ----
        // Vàng: mỗi lần ghi chỉ được thay đổi trong biên độ hợp lý.
        // Kiếm vàng nhanh nhất trong game (~bán 1 mẻ lớn) không quá +5000/lần ghi;
        // chi tiêu 1 lần không quá -10000. Số âm/tràn kiểu đều bị chặn.
        && ( !changed().hasAny(['gold'].toSet()) ||
             ( isInt(incoming().gold) && incoming().gold >= 0
               && incoming().gold - existing().gold <= 5000
               && existing().gold - incoming().gold <= 10000 ) )
        && ( !changed().hasAny(['gems'].toSet()) ||
             ( isInt(incoming().gems) && incoming().gems >= 0
               && incoming().gems - existing().gems <= 20
               && existing().gems - incoming().gems <= 100 ) )
        // Level chỉ được tăng, mỗi lần +<=1, trần 30
        && ( !changed().hasAny(['level'].toSet()) ||
             ( inRange(incoming().level, 1, 30)
               && incoming().level - existing().level == 1 ) )
        // EXP: không âm, mỗi lần ghi tăng tối đa 500 (một hành động lớn nhất ~exp 30-50)
        && ( !changed().hasAny(['exp'].toSet()) ||
             ( isInt(incoming().exp) && incoming().exp >= 0
               && incoming().exp <= 2000
               && ( incoming().exp > existing().exp
                    ? incoming().exp - existing().exp <= 500
                    : changed().hasAny(['level'].toSet()) ) ) )  // exp chỉ được giảm khi lên level (reset)
        // lbScore phải khớp công thức từ level/exp đang ghi
        && ( !changed().hasAny(['lbScore','level','exp'].toSet()) ||
             incoming().lbScore == incoming().level * 100000 + incoming().exp )
        // Stats chỉ tăng, mỗi lần ghi +<=20
        && ( !changed().hasAny(['stats'].toSet()) ||
             ( incoming().stats.totalDeliveries >= existing().stats.totalDeliveries
               && incoming().stats.totalDeliveries - existing().stats.totalDeliveries <= 20
               && incoming().stats.dishesCooked >= existing().stats.dishesCooked
               && incoming().stats.dishesCooked - existing().stats.dishesCooked <= 20 ) )
        && ( !changed().hasAny(['friendCount'].toSet()) ||
             inRange(incoming().friendCount, 0, 100) )
        && ( !changed().hasAny(['username'].toSet()) ||
             incoming().username.matches('^[a-z0-9_]{3,16}$') );

      allow delete: if false;

      /* ---------- inventorySync ---------- */
      match /inventorySync/{itemId} {
        allow read: if isSelf(uid);   // kho là chuyện riêng
        allow write: if isSelf(uid)
          && incoming().qty is int
          && incoming().qty >= 0 && incoming().qty <= 9999
          && exists(/databases/$(database)/documents/catalog/$(itemId));
      }

      /* ---------- friends ---------- */
      match /friends/{friendUid} {
        allow read: if isSelf(uid);
        // Chủ nhà tự ghi (bước 2 của accept), hoặc BÊN KIA ghi vào nhà mình
        // (bước 3) nếu tồn tại request accepted đúng chiều.
        allow create: if signedIn() && (
             request.auth.uid == uid
          || ( request.auth.uid == friendUid
               && get(/databases/$(database)/documents/friendRequests/$(uid)_$(friendUid)).data.status == 'accepted'
             )
          || ( request.auth.uid == friendUid
               && get(/databases/$(database)/documents/friendRequests/$(friendUid)_$(uid)).data.status == 'accepted'
             )
        );
        allow delete: if isSelf(uid) || (signedIn() && request.auth.uid == friendUid); // unfriend 2 chiều
        allow update: if isSelf(uid); // refresh level cache
      }
    }

    /* ===================== usernames ===================== */
    match /usernames/{name} {
      allow read: if signedIn();                    // check trùng tên
      allow create: if signedIn()
        && name.matches('^[a-z0-9_]{3,16}$')
        && incoming().uid == request.auth.uid;      // chỉ claim cho chính mình
      allow delete: if signedIn() && existing().uid == request.auth.uid; // đổi tên
      allow update: if false;
    }

    /* ===================== friendRequests ===================== */
    match /friendRequests/{reqId} {
      allow read: if signedIn()
        && (existing().from == request.auth.uid || existing().to == request.auth.uid);
      allow create: if signedIn()
        && incoming().from == request.auth.uid
        && reqId == incoming().from + '_' + incoming().to   // id ghép chống spam trùng
        && incoming().from != incoming().to
        && incoming().status == 'pending';
      // Chỉ NGƯỜI NHẬN được đổi status, và chỉ từ pending
      allow update: if signedIn()
        && existing().to == request.auth.uid
        && existing().status == 'pending'
        && incoming().status in ['accepted', 'rejected']
        && changed().hasOnly(['status'].toSet());
      allow delete: if signedIn()
        && (existing().from == request.auth.uid || existing().to == request.auth.uid);
    }

    /* ===================== farmSnapshots ===================== */
    match /farmSnapshots/{uid} {
      allow read: if signedIn();          // tham quan mở cho mọi người chơi
      allow write: if isSelf(uid)
        && incoming().owner == uid
        && incoming().plots is list && incoming().plots.size() <= 200
        && incoming().buildings is list && incoming().buildings.size() <= 60
        && incoming().decor is list && incoming().decor.size() <= 100;
    }

    /* ===================== market (PA-A: client tự giao dịch) ===================== */
    match /market/{listingId} {
      allow read: if signedIn();

      // ĐĂNG BÁN
      allow create: if signedIn()
        && incoming().sellerUid == request.auth.uid
        && incoming().status == 'active'
        && incoming().buyerUid == null
        && inRange(incoming().qty, 1, catalog(incoming().itemId).maxStack)
        && catalog(incoming().itemId).tradable == true
        // Kẹp giá theo % giá catalog (đọc từ config/economy)
        && incoming().pricePerUnit * 100 >= catalog(incoming().itemId).sellPrice * econ().market.minPricePct
        && incoming().pricePerUnit * 100 <= catalog(incoming().itemId).sellPrice * econ().market.maxPricePct
        && incoming().totalPrice == incoming().qty * incoming().pricePerUnit;

      // MUA (PA-A): người mua chỉ được "claim" listing — đổi status + gắn buyerUid.
      // Việc trừ/cộng vàng là 2 write khác trên users/, được kiểm bằng delta rule ở trên.
      allow update: if signedIn() && (
        // (1) buyer claim
        ( existing().status == 'active'
          && incoming().status == 'sold'
          && incoming().buyerUid == request.auth.uid
          && existing().sellerUid != request.auth.uid          // không tự mua của mình
          && changed().hasOnly(['status','buyerUid','soldAt'].toSet()) )
        ||
        // (2) seller huỷ
        ( existing().sellerUid == request.auth.uid
          && existing().status == 'active'
          && incoming().status == 'cancelled'
          && changed().hasOnly(['status'].toSet()) )
      );

      allow delete: if signedIn()
        && existing().sellerUid == request.auth.uid
        && existing().status != 'active';   // chỉ dọn listing đã xong
    }

    /* ===================== catalog / config: READ-ONLY ===================== */
    match /catalog/{itemId} { allow read: if signedIn(); allow write: if false; }
    match /config/{docId}   { allow read: if signedIn(); allow write: if false; }

    /* ===================== transactions: đóng hoàn toàn ===================== */
    match /transactions/{txId} { allow read, write: if false; }

    /* Mặc định: chặn hết */
    match /{document=**} { allow read, write: if false; }
  }
}
```

> **Lưu ý kỹ thuật:** mỗi `get()`/`exists()` trong Rules tính là 1 read tính phí và bị trần **10 lượt/request** (single-doc) — rule `market create` ở trên dùng 2 (`catalog` + `econ`), an toàn. Test toàn bộ rules bằng **Firebase Emulator Suite** (`firebase emulators:start`) + Rules Unit Tests trước khi deploy — sai một dấu là hở két.

#### Rủi ro còn lại của PA-A — nói thẳng

| Rủi ro | Chi tiết | Mức độ |
|---|---|---|
| **Client mod fake số dư bằng nhiều bước nhỏ** | Delta rule chặn +1 tỷ vàng/lần, nhưng KHÔNG chặn được script gọi update +5000 vàng × 200 lần = 1 triệu vàng. Rules không có rate-limit theo thời gian (không đọc được "lần ghi trước cách đây bao lâu" một cách tin cậy nếu client tự ghi timestamp). | **Cao** — đây là lỗ hổng bản chất, không vá được bằng Rules. |
| **Giao dịch chợ PA-A không atomic thật** | Mua hàng = 3-4 write (claim listing, trừ vàng mình, cộng vàng seller — mà Rules ở trên còn KHÔNG cho người mua ghi vàng vào `users/{seller}`!). Thực tế PA-A phải chọn một nhượng bộ: hoặc (a) seller chỉ nhận vàng khi mở app và tự "thu hoạch" listing sold của mình (client seller tự cộng, nằm trong delta +5000 → mua bán chậm nhận tiền, vẫn gian lận được), hoặc (b) nới rule cho người mua ghi tăng vàng người khác (hở to hơn). Khuyến nghị PA-A dùng (a): trạng thái `sold` + seller collect sau. | **Trung bình** — mất mát UX + vẫn cheat được. |
| **Fake stats leaderboard** | +20/lần ghi × spam = leo bảng ảo. | Trung bình. |
| **Sybil** | Anonymous auth tạo vô hạn acc, tự bán giá trần cho acc phụ để "rửa" trần delta. | Trung bình. |

**Mức chấp nhận được:** game KHÔNG có tiền thật → cheat chỉ phá leaderboard và kinh tế chợ, không gây thiệt hại tài chính trực tiếp cho ai. Với alpha/friends-test < vài trăm người, PA-A đủ dùng. **Không đem PA-A ra soft launch công khai** — một người cheat vàng vô hạn mua vét chợ là kinh tế chợ chết, và leaderboard rác là mất động lực người chơi thật.

### 4.2 PHƯƠNG ÁN B — Cloud Functions (Blaze, thực tế ~0đ ở quy mô nhỏ)

Blaze bắt buộc để deploy Functions, nhưng free tier của Blaze rất rộng (2M invocation/tháng) — 1000 DAU × 20 giao dịch/ngày = 600K/tháng, vẫn 0đ. Chỉ trả tiền khi vượt.

Nguyên tắc: **mọi thao tác đụng tiền tệ đi qua callable function**, client bị TƯỚC quyền ghi `gold/gems` trong Rules (đổi rule `users` update: `!changed().hasAny(['gold','gems','level','exp','lbScore','stats'].toSet())` — hoặc giữ delta rule cho exp/level nếu muốn lai, xem 4.3).

Các function tối thiểu:

| Function | Việc |
|---|---|
| `createListing(itemId, qty, pricePerUnit)` | Check kho `inventorySync`, trừ kho, tạo `market/` doc, đếm trần `maxActiveListings`. |
| `purchaseListing(listingId)` | Transaction: check vàng buyer → trừ buyer, cộng seller, cộng kho buyer, set sold, ghi `transactions/`. |
| `cancelListing(listingId)` | Hoàn item về kho seller. |
| `claimDailyReward()` | Server tự tính ngày (chống đổi giờ máy), cộng vàng. |
| `reportProgress(deltaExp, deltas...)` | (tuỳ chọn) thay client ghi exp/stats, có sanity check. |

#### Code mẫu TypeScript — `purchaseListing` (đầy đủ, Functions v2)

```typescript
// functions/src/index.ts
import { onCall, HttpsError } from "firebase-functions/v2/https";
import { initializeApp } from "firebase-admin/app";
import { getFirestore, FieldValue, Timestamp } from "firebase-admin/firestore";

initializeApp();
const db = getFirestore();

// ---- Rate limit đơn giản: tối đa 10 lần mua / 60 giây / user ----
async function checkRateLimit(uid: string, action: string, max: number, windowSec: number) {
  const ref = db.doc(`rateLimits/${uid}_${action}`);
  await db.runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const now = Timestamp.now();
    const winStart = snap.exists ? (snap.data()!.winStart as Timestamp) : now;
    const count = snap.exists ? (snap.data()!.count as number) : 0;
    if (now.seconds - winStart.seconds < windowSec && count >= max) {
      throw new HttpsError("resource-exhausted", "Thao tác quá nhanh, thử lại sau.");
    }
    const reset = now.seconds - winStart.seconds >= windowSec;
    tx.set(ref, { winStart: reset ? now : winStart, count: reset ? 1 : count + 1 });
  });
}

export const purchaseListing = onCall(
  { region: "asia-southeast1", enforceAppCheck: true },   // App Check BẮT BUỘC
  async (request) => {
    const buyerUid = request.auth?.uid;
    if (!buyerUid) throw new HttpsError("unauthenticated", "Chưa đăng nhập.");

    const listingId = request.data?.listingId;
    if (typeof listingId !== "string" || listingId.length > 64) {
      throw new HttpsError("invalid-argument", "listingId không hợp lệ.");
    }

    await checkRateLimit(buyerUid, "purchase", 10, 60);

    const listingRef = db.doc(`market/${listingId}`);
    const buyerRef   = db.doc(`users/${buyerUid}`);

    const result = await db.runTransaction(async (tx) => {
      // ---- 1. ĐỌC HẾT trước (Firestore transaction: reads trước writes) ----
      const listingSnap = await tx.get(listingRef);
      if (!listingSnap.exists) throw new HttpsError("not-found", "Món hàng không tồn tại.");
      const listing = listingSnap.data()!;

      if (listing.status !== "active")
        throw new HttpsError("failed-precondition", "Món hàng đã bán hoặc bị huỷ.");
      if (listing.sellerUid === buyerUid)
        throw new HttpsError("failed-precondition", "Không thể tự mua hàng của mình.");
      if (listing.expiresAt && listing.expiresAt.toMillis() < Date.now())
        throw new HttpsError("failed-precondition", "Món hàng đã hết hạn.");

      const sellerRef   = db.doc(`users/${listing.sellerUid}`);
      const buyerInvRef = db.doc(`users/${buyerUid}/inventorySync/${listing.itemId}`);

      const [buyerSnap, sellerSnap, buyerInvSnap] = await Promise.all([
        tx.get(buyerRef), tx.get(sellerRef), tx.get(buyerInvRef),
      ]);
      if (!buyerSnap.exists || !sellerSnap.exists)
        throw new HttpsError("not-found", "Không tìm thấy người chơi.");

      const total = listing.totalPrice as number;
      const buyerGold = buyerSnap.data()!.gold as number;
      if (buyerGold < total)
        throw new HttpsError("failed-precondition", "Không đủ vàng.");

      // ---- Sanity check anti-cheat: vàng seller sau cộng có bất thường? ----
      const sellerGoldAfter = (sellerSnap.data()!.gold as number) + total;
      if (sellerGoldAfter > 5_000_000) {
        tx.update(sellerRef, { "flags.suspectedCheater": true });
      }

      // ---- 2. GHI: atomic toàn bộ ----
      tx.update(buyerRef,  { gold: FieldValue.increment(-total) });
      tx.update(sellerRef, { gold: FieldValue.increment(total),
                             "stats.marketSales": FieldValue.increment(1) });
      tx.set(buyerInvRef,
        { qty: FieldValue.increment(listing.qty), updatedAt: FieldValue.serverTimestamp() },
        { merge: true });
      tx.update(listingRef, {
        status: "sold", buyerUid, soldAt: FieldValue.serverTimestamp(),
      });
      tx.create(db.collection("transactions").doc(), {
        type: "market_purchase", listingId,
        itemId: listing.itemId, qty: listing.qty, totalPrice: total,
        buyerUid, sellerUid: listing.sellerUid,
        buyerGoldBefore: buyerGold, buyerGoldAfter: buyerGold - total,
        at: FieldValue.serverTimestamp(),
      });

      return { itemId: listing.itemId, qty: listing.qty, paid: total,
               goldAfter: buyerGold - total };
    });

    return result; // trả về client
  }
);
```

Ghi chú cho `createListing` (không dán full code, logic tương tự): validate `catalog(itemId).tradable`, kẹp giá `pricePerUnit` trong `[sellPrice × minPricePct%, sellPrice × maxPricePct%]` đọc từ `config/economy`, transaction trừ `inventorySync.qty` (fail nếu âm), đếm listing active của seller ≤ `maxActiveListings`.

#### Code C# Unity gọi callable

```csharp
using Firebase.Functions;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MarketService
{
    readonly FirebaseFunctions _fn =
        FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, "asia-southeast1");

    public async Task<PurchaseResult> PurchaseListingAsync(string listingId)
    {
        var callable = _fn.GetHttpsCallable("purchaseListing");
        try
        {
            var response = await callable.CallAsync(new Dictionary<string, object> {
                { "listingId", listingId }
            });
            var d = (IDictionary<string, object>)response.Data;
            return new PurchaseResult {
                Success = true,
                ItemId  = (string)d["itemId"],
                Qty     = System.Convert.ToInt32(d["qty"]),
                Paid    = System.Convert.ToInt32(d["paid"]),
                GoldAfter = System.Convert.ToInt64(d["goldAfter"])
            };
        }
        catch (FunctionsException e)
        {
            // e.ErrorCode map với HttpsError phía server
            string msg = e.ErrorCode switch {
                FunctionsErrorCode.FailedPrecondition => e.Message, // "Không đủ vàng" / "đã bán"...
                FunctionsErrorCode.ResourceExhausted  => "Thao tác quá nhanh, thử lại sau.",
                FunctionsErrorCode.NotFound           => "Món hàng không còn nữa.",
                _ => "Lỗi mạng, thử lại."
            };
            return new PurchaseResult { Success = false, Error = msg };
        }
    }
}

public struct PurchaseResult
{
    public bool Success; public string Error;
    public string ItemId; public int Qty; public int Paid; public long GoldAfter;
}
```

#### Anti-cheat bổ sung ở PA-B

- **Rate limit** (đã trong code): 10 mua/phút, 5 createListing/phút, 1 claimDaily/ngày.
- **Sanity check định kỳ:** scheduled function (1 lần/ngày) quét `users` có `gold` tăng > X/ngày so với log `transactions/` + biên độ kiếm hợp lệ → set `flags.suspectedCheater`, ẩn khỏi leaderboard (query leaderboard thêm `where flags.suspectedCheater != true` hoặc đơn giản lọc client-side theo danh sách flag).
- **Validate giá listing** trong `createListing` theo `% sellPrice` catalog (chống chuyển vàng lách luật giữa 2 acc bằng listing 1 `rice` giá 999999).
- **Chặn tự mua của mình** + (tuỳ chọn) chặn mua chéo lặp lại giữa 2 acc quá N lần/ngày (đọc `transactions` gần nhất).

### 4.3 KHUYẾN NGHỊ CỦA TECH LEAD — phương án lai + lộ trình

**Chốt: LAI.**

- **Functions (PA-B)** cho: `purchaseListing`, `createListing`, `cancelListing`, `claimDailyReward` — tức mọi đường tiền tệ *liên user* và thưởng định kỳ. Đây là chỗ atomic + authority bắt buộc.
- **Rules-only (PA-A)** cho phần còn lại: profile, friends, snapshot, presence, đọc catalog/market/leaderboard, và **ghi exp/level/stats của chính mình qua delta rule** (chấp nhận fake được leaderboard ở mức giới hạn — game không tiền thật, đây là trade-off có ý thức; siết sau bằng `reportProgress` function nếu leaderboard bị phá).

**Lộ trình:**

| Giai đoạn | Chạy gì |
|---|---|
| MVP / alpha nội bộ (bây giờ) | **PA-A thuần**, gói Spark 0đ. Market dùng mô hình "claim + seller collect". Viết code client tách `IMarketBackend` interface ngay từ đầu để swap. |
| Trước soft launch | Nâng Blaze, deploy 4 functions, siết Rules tước quyền ghi `gold/gems` của client, swap implementation `IMarketBackend`. |
| Sau soft launch nếu leaderboard bị phá | Thêm `reportProgress` + scheduled sanity scan. |

**Bảng so sánh:**

| Tiêu chí | A — Rules only | B — Functions | Lai (chốt) |
|---|---|---|---|
| Chi phí tiền | 0đ (Spark) | ~0đ quy mô nhỏ, cần thẻ + billing Blaze | ~0đ, cần Blaze |
| Công sức dev | Thấp (viết rules + test emulator ~2-3 ngày) | Trung bình (thêm repo functions, TS, deploy, monitor ~1 tuần) | A trước, +3-4 ngày khi nâng B |
| Atomic giao dịch chợ | Không thật sự (claim + collect) | Có (Firestore transaction) | Có (phần chợ) |
| Chống mod client sửa vàng | **Không** (chỉ làm chậm) | Có với đường qua function | Có cho tiền tệ; exp/stats vẫn hở có kiểm soát |
| Độ trễ giao dịch | Nhanh (write thẳng) | +200-500ms cold start (region asia-southeast1, min instances 0) | Chấp nhận được |
| Phù hợp | Alpha nội bộ | Soft launch trở đi | Toàn lộ trình |

---

## 5. ẢNH VẬT PHẨM ĐỂ ĐÂU

**Câu trả lời ngắn: ảnh item KHÔNG đưa lên Firebase. Ảnh nằm trong game build. Firebase chỉ lưu chuỗi `itemId` + `spriteKey`.**

Giải thích cho câu hỏi *"đưa ảnh lên đâu để đồng bộ id vào Firebase"* — đang có hiểu nhầm nhỏ: đồng bộ ở đây là đồng bộ **cái tên (key)**, không phải đồng bộ **file ảnh**. Mọi client đều đã có sẵn toàn bộ sprite trong build (chính là các icon trong `ING_Carot.asset`, `BapCai.asset`... hiện tại). Khi client đọc listing `{ "itemId": "egg" }` từ Firestore, nó chỉ cần biết *"egg thì lấy sprite nào trong máy"* — tức cần một **bảng tra key → sprite nằm trong build**, không cần tải ảnh từ mạng.

Vì sao KHÔNG đưa ảnh lên Firebase Storage:

1. **Vô nghĩa:** mọi client đều có sẵn ảnh — tải lại từ mạng cái mình đang có.
2. **Tốn bandwidth + chậm:** mở chợ 20 listing = 20 request ảnh, màn hình trắng chờ ảnh về, tốn quota download Storage (free 1 GB/ngày nghe nhiều nhưng icon × ngàn user × mỗi lần mở là hết).
3. **Kẹt phiên bản:** ảnh trên Storage đổi mà build cũ chưa đổi → lệch. Ảnh trong build thì luôn khớp code.

**Cách làm chuẩn — Addressables với key = itemId:**

1. Mark toàn bộ sprite icon item là Addressable, đặt **address = chính itemId**: sprite bắp cải → address `bapcai`, trứng → `egg`, hạt bắp cải → `seed_bapcai`. (Một buổi tooling: viết editor script quét các ScriptableObject, lấy `itemID`/`id` và icon reference, set address tự động — đừng đặt tay 200 cái.)
2. Firestore `catalog/{itemId}.spriteKey` = itemId luôn (để dư field này phòng sau này 2 item dùng chung 1 sprite).
3. Client render:

```csharp
using UnityEngine.AddressableAssets;

public async Task<Sprite> LoadItemSpriteAsync(string spriteKey)
{
    // spriteKey lấy từ catalog cache local; fallback icon "?" nếu key không tồn tại
    var handle = Addressables.LoadAssetAsync<Sprite>(spriteKey);
    return await handle.Task;
}
```

4. Nếu chưa muốn động Addressables ở MVP: `Resources/ItemIcons/{itemId}.png` + `Resources.Load<Sprite>($"ItemIcons/{itemId}")` — cùng nguyên tắc, đổi sang Addressables sau không ảnh hưởng database.

**Firebase Storage để dành cho:** nội dung user tạo ra mà build không thể có sẵn — avatar tự chụp, ảnh chia sẻ farm (nếu làm sau này). Lúc đó mới bật Storage + rules riêng. MVP: **không bật Storage**.

**Item mới thêm sau khi phát hành:** ship kèm bản update build (game F2P kiểu này ra content theo patch là bình thường), hoặc dùng Addressables remote catalog (CDN của Unity/CCD) — vẫn không phải việc của Firebase Storage. Quy tắc: `catalog/` trên Firestore chỉ được thêm item MỚI đồng thời hoặc SAU khi build chứa sprite đó đã phát hành; client gặp `spriteKey` không tra được thì hiện icon placeholder + ẩn khỏi chợ.

---

## 6. PIPELINE IMPORT CATALOG

Trả lời thẳng câu *"dùng file JSON import là xong hết hả?"*:

- **Firestore KHÔNG có nút import JSON** trong console. Không có. (Chỉ có export/import backup định dạng riêng qua `gcloud`, không dùng cho việc này được.)
- **RTDB thì có** nút import JSON — nhưng ta không để catalog ở RTDB (RTDB chỉ làm presence; catalog cần query/rules theo document → Firestore).
- **Cách đúng:** một script Node.js dùng `firebase-admin`, đọc file JSON catalog mà đội Data đang xuất từ ScriptableObject, batch-write vào `catalog/`. Chạy lại mỗi khi data đổi. Đây là việc 1 buổi, làm 1 lần dùng mãi.

### 6.1 Chuẩn bị

```bash
mkdir catalog-importer && cd catalog-importer
npm init -y
npm install firebase-admin
```

**Lấy service account key:** Firebase Console → ⚙️ Project settings → **Service accounts** → *Generate new private key* → tải về `serviceAccountKey.json` đặt cạnh script.

> ⚠️ **BẢO MẬT KEY — đọc kỹ:** file này = quyền admin TOÀN BỘ project, bỏ qua mọi Security Rules. **Tuyệt đối không commit vào git** (thêm `serviceAccountKey.json` vào `.gitignore` NGAY khi tạo repo), không gửi qua chat/Drive chung, mỗi người cần thì tự generate key riêng, key lộ thì vào Google Cloud Console → IAM → Service Accounts → xoá key đó ngay. Chỉ 1-2 người giữ (lead + backend).

### 6.2 Định dạng JSON đầu vào (chốt với đội Data)

```jsonc
// catalog_export.json — đội Data xuất từ ScriptableObject (editor script)
{
  "version": 8,
  "items": [
    { "itemId": "bapcai",      "nameVi": "Bắp Cải",     "category": "crop",
      "buyPrice": 15, "sellPrice": 10,  "unlockLevel": 1, "tradable": true,  "maxStack": 999 },
    { "itemId": "seed_bapcai", "nameVi": "Hạt Bắp Cải", "category": "seed",
      "buyPrice": 28, "sellPrice": 0,   "unlockLevel": 1, "tradable": false, "maxStack": 999 },
    { "itemId": "egg",         "nameVi": "Trứng Gà",    "category": "animal_product",
      "buyPrice": 0,  "sellPrice": 40,  "unlockLevel": 4, "tradable": true,  "maxStack": 999 },
    { "itemId": "ga_xao_ot",   "nameVi": "Gà Xào Ớt",   "category": "dish",
      "buyPrice": 0,  "sellPrice": 165, "unlockLevel": 5, "tradable": true,  "maxStack": 99 }
  ]
}
```

### 6.3 `import_catalog.js` — đầy đủ

```javascript
// import_catalog.js
// Chạy: node import_catalog.js catalog_export.json
// Yêu cầu: serviceAccountKey.json cùng thư mục (KHÔNG COMMIT!)
const admin = require("firebase-admin");
const fs = require("fs");

const serviceAccount = require("./serviceAccountKey.json");
admin.initializeApp({ credential: admin.credential.cert(serviceAccount) });
const db = admin.firestore();

const VALID_CATEGORIES = new Set([
  "seed", "crop", "flower", "animal_product", "dish", "ingredient", "material",
]);

function validate(it, i) {
  const err = (m) => { throw new Error(`items[${i}] (${it.itemId ?? "?"}): ${m}`); };
  if (!/^[a-z0-9_]{2,40}$/.test(it.itemId ?? "")) err("itemId sai định dạng");
  if (typeof it.nameVi !== "string" || !it.nameVi) err("thiếu nameVi");
  if (!VALID_CATEGORIES.has(it.category)) err(`category lạ: ${it.category}`);
  for (const f of ["buyPrice", "sellPrice", "unlockLevel", "maxStack"]) {
    if (!Number.isInteger(it[f]) || it[f] < 0) err(`${f} phải là số nguyên >= 0`);
  }
  if (typeof it.tradable !== "boolean") err("tradable phải là boolean");
  if (it.tradable && it.sellPrice <= 0) err("item tradable phải có sellPrice > 0 (để kẹp giá chợ)");
}

async function main() {
  const file = process.argv[2] || "catalog_export.json";
  const data = JSON.parse(fs.readFileSync(file, "utf8"));
  const items = data.items;
  if (!Array.isArray(items) || items.length === 0) throw new Error("items rỗng");
  if (!Number.isInteger(data.version)) throw new Error("thiếu version (số nguyên)");

  // Validate hết trước, phát hiện itemId trùng
  const seen = new Set();
  items.forEach((it, i) => {
    validate(it, i);
    if (seen.has(it.itemId)) throw new Error(`itemId trùng: ${it.itemId}`);
    seen.add(it.itemId);
  });

  // Batch write — trần 500 op/batch
  const BATCH = 450;
  for (let i = 0; i < items.length; i += BATCH) {
    const batch = db.batch();
    for (const it of items.slice(i, i + BATCH)) {
      batch.set(
        db.collection("catalog").doc(it.itemId),
        {
          itemId: it.itemId, nameVi: it.nameVi, category: it.category,
          buyPrice: it.buyPrice, sellPrice: it.sellPrice,
          unlockLevel: it.unlockLevel,
          spriteKey: it.spriteKey ?? it.itemId,   // mặc định spriteKey = itemId
          tradable: it.tradable, maxStack: it.maxStack,
          updatedAt: admin.firestore.FieldValue.serverTimestamp(),
        },
        { merge: true }   // chạy lại khi data đổi: chỉ đè field có mặt, không phá field thêm tay
      );
    }
    await batch.commit();
    console.log(`Đã ghi ${Math.min(i + BATCH, items.length)}/${items.length}`);
  }

  // Bump version — client thấy đổi mới tải lại catalog
  await db.doc("config/economy").set({ catalogVersion: data.version }, { merge: true });
  console.log(`XONG. ${items.length} item. catalogVersion = ${data.version}`);
}

main().catch((e) => { console.error("IMPORT THẤT BẠI:", e.message); process.exit(1); });
```

```bash
node import_catalog.js catalog_export.json
```

### 6.4 Khi data đổi

- Đội Data xuất JSON mới, **tăng `version`**, chạy lại script — `merge: true` nên chạy bao nhiêu lần cũng an toàn (idempotent).
- Item bị XOÁ khỏi game: script trên không tự xoá doc thừa (an toàn mặc định). Đánh dấu `tradable: false` thay vì xoá; muốn xoá thật thì viết thêm chế độ `--prune` diff với danh sách hiện có (làm sau, không cần MVP).
- Client flow: mở app → đọc `config/economy` (1 read) → `catalogVersion` khác bản cache → tải cả collection `catalog` một lần (N read) → lưu JSON local. Ngày thường: đúng **1 read/phiên** cho toàn bộ catalog.

---

## 7. CHECKLIST TRIỂN KHAI

Theo thứ tự, bám milestone. Đánh dấu **[CẦN BẠN]** = việc chỉ owner/tài khoản Google của bạn làm được.

### Giai đoạn 0 — Nền tảng (song song M0-2 SaveSystem)

- [ ] **[CẦN BẠN]** Tạo Firebase project (console.firebase.google.com), đặt region Firestore = `asia-southeast1` (Singapore) — **chọn 1 lần, không đổi được**.
- [ ] **[CẦN BẠN]** Thêm app Android (package name) + iOS (bundle id) + tải `google-services.json` / `GoogleService-Info.plist` đưa cho team đặt vào `Assets/`.
- [ ] **[CẦN BẠN]** Bật Authentication → Sign-in method → **Anonymous** (+ Google, Apple để dành cho link account).
- [ ] **[CẦN BẠN]** Tạo Firestore database (production mode) + Realtime Database (locked mode).
- [ ] Import Firebase SDK for Unity: `FirebaseAuth`, `FirebaseFirestore`, `FirebaseDatabase` (Functions thêm sau ở giai đoạn 3). Kiểm tra build size mobile.
- [ ] Cài `firebase-tools` CLI, `firebase init` repo riêng `firebase/` (rules + indexes + functions), setup Emulator Suite cho dev/test.
- [ ] Code `AuthService`: sign-in anonymous khi mở game, expose `Uid`; flow link account (Google/Apple) để giữ save khi đổi máy.

### Giai đoạn 1 — Catalog + Profile (MVP online đầu tiên)

- [ ] Đội Data chốt **bảng itemId thống nhất** (xử lý vụ `chicken`/`chicken_meat` trước!). Editor script xuất `catalog_export.json`.
- [ ] Chạy `import_catalog.js` (mục 6). **[CẦN BẠN]** generate service account key cho người chạy.
- [ ] Editor script set Addressables address = itemId cho sprite icon (mục 5).
- [ ] Client: cache catalog local + version check.
- [ ] `users/` + `usernames/`: flow tạo hồ sơ, đặt tên. Deploy `firestore.rules` bản PA-A + indexes. Test rules trên emulator.

### Giai đoạn 2 — Social (PA-A, gói Spark)

- [ ] Friend requests + friends subcollection + UI.
- [ ] Presence RTDB + `database.rules.json` (mục 3).
- [ ] `farmSnapshots/`: hàm chụp snapshot từ SaveSystem, debounce; màn hình tham quan render từ snapshot.
- [ ] Leaderboard tab (lbScore + 2 bảng phụ), cache 15 phút, pagination cursor.
- [ ] Market PA-A: create/cancel/claim + seller collect. Client viết sau interface `IMarketBackend`.
- [ ] **[CẦN BẠN]** Bật App Check: đăng ký Play Integrity (Android) / App Attest (iOS) trong console; dev dùng debug token. Bật **enforce** cho Firestore + RTDB sau khi client đã gắn SDK App Check.

### Giai đoạn 3 — Server authority (trước soft launch)

- [ ] **[CẦN BẠN]** Nâng cấp gói **Blaze** (cần thẻ). Đặt **budget alert** (ví dụ cảnh báo ở $5) trong Google Cloud Billing.
- [ ] Viết + deploy 4 functions: `createListing`, `purchaseListing`, `cancelListing`, `claimDailyReward` (region `asia-southeast1`, `enforceAppCheck: true`).
- [ ] Siết `firestore.rules`: tước quyền client ghi `gold/gems`, đổi rule `market` (client chỉ read; create/update qua functions — Admin SDK bỏ qua rules nên chỉ cần đóng đường client).
- [ ] Swap `IMarketBackend` sang bản callable. Regression test bằng emulator (Functions + Firestore emulator chạy chung).
- [ ] Bật TTL policy cho `market.expiresAt`, `rateLimits`.
- [ ] Scheduled sanity scan + flag cheater (có thể sau soft launch).

---

## 8. ƯỚC TÍNH CHI PHÍ

Free tier Firestore (gói Spark, và cũng là phần miễn phí hằng ngày của Blaze): **50K read / 20K write / 20K delete mỗi ngày**, 1 GiB lưu trữ.

### Ước tính mỗi DAU/ngày (đã áp dụng các mẹo bên dưới)

| Hành động | Read | Write |
|---|---|---|
| Mở app: config/economy + hồ sơ mình | 2 | 1 (updatedAt) |
| Catalog (chỉ khi version đổi ~1 lần/tuần) | ~0.5 amortized | 0 |
| Xem leaderboard (cache 15', trung bình 1.5 lần fetch × 50) | 75 | 0 |
| Tab bạn bè (20 bạn = 1 query 20 doc, 2 lần/ngày) | 40 | 0 |
| Tham quan 3 làng (snapshot + hồ sơ) | 6 | 0 |
| Chợ: browse 2 lần × 20 listing | 40 | 0 |
| Chợ: 3 giao dịch (mua/bán, gồm transaction reads) | ~12 | ~12 |
| Sync exp/stats/snapshot (batch, event-based) | 0 | ~10 |
| **Tổng/DAU** | **~175 read** | **~23 write** |

| Quy mô | Read/ngày | Write/ngày | Kết luận |
|---|---|---|---|
| 100 DAU | ~17.5K | ~2.3K | **Thoải mái trong free tier.** |
| 300 DAU | ~52K | ~7K | Read chớm trần — cần cache leaderboard/chợ chặt hơn (TTL 30'). |
| 1000 DAU | ~175K | ~23K | **Vượt free tier.** Trên Blaze: vượt ~125K read + 3K write/ngày ≈ **$0.05–0.15/ngày ≈ $2–5/tháng** cả Firestore + Functions. Vẫn rẻ hơn 1 ly cà phê/tuần. |

(Đơn giá Blaze tham khảo: ~$0.03–0.06/100K read, ~$0.09–0.18/100K write tuỳ region — asia-southeast1 nhỉnh hơn US một chút.)

### Mẹo giảm read — BẮT BUỘC code ngay từ đầu, không phải "tối ưu sau"

1. **Cache catalog local + version check** (mục 6.4): biến N read/phiên thành 1 read/phiên. Mẹo lớn nhất.
2. **Cache leaderboard 15 phút** client-side; chỉ fetch trang 2+ khi user thật sự kéo. Không bao giờ đặt listener realtime lên leaderboard.
3. **Listener có chọn lọc:** KHÔNG `Listen()` cả collection `market` (mỗi listing mới = 1 read × mọi client đang mở). Chợ dùng `GetSnapshotAsync()` + nút refresh (cooldown 10s). Listener realtime chỉ dành cho: doc `users/{me}` (1 doc) và `friendRequests` where `to == me` (ít doc) — và presence thì đã ở RTDB, không tính quota Firestore.
4. **Paginate bằng cursor (`StartAfter`)**, không offset — offset tính tiền cả doc bị bỏ qua.
5. **Denormalize để khỏi join:** `sellerUsername` trong listing, `username/level` trong friend doc — hiển thị list không cần đọc thêm N hồ sơ.
6. **Snapshot làng = 1 doc** thay vì subcollection nhiều doc → tham quan 1 làng = 1 read.
7. **Debounce write:** exp/stats gom batch 1 phút/lần hoặc theo phiên chơi; snapshot tối đa 1 lần/5 phút.
8. Bật **offline persistence** của Firestore SDK (mặc định on trên mobile) — đọc lại doc chưa đổi lấy từ cache, không tính read (với `Source.Cache`).

---

*Hết tài liệu. Câu hỏi về schema/rules gửi Backend Architect trước khi tự đổi — mọi thay đổi collection/field phải cập nhật file này.*
