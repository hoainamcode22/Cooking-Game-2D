# 🎨 ART BRIEF & PROMPT: BỘ SPRITE BONG BÓNG THOẠI DU KHÁCH (TOURIST THOUGHT BUBBLE PACK)
> Ban hành ngày 2026-08-31 bởi Tech Lead.
> Dán nguyên văn phần Prompt bên dưới cho GPT / agent-sprite-forge để vẽ bộ asset bong bóng suy nghĩ cho khách du lịch.

---

## 📌 QUY TẮC BẮT BUỘC (STUDIO ART RULES):
1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: Không chữ, không số, không logo, không icon bên trong khung bóng (món ăn và biểu cảm do code Unity render).
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: Alpha trong suốt 100% (PNG 32-bit), không đổ bóng xuống nền, không vệt mờ dưới chân.
3. ✅ **STYLE CHUẨN**: Phong cách hoạt hình ấm áp (cozy casual / cartoon cute style, phù hợp phụ nữ và trẻ em). Outline nâu ấm đậm `#442510` - `#5A3825` dày 2-3px mịn màng, ruột bóng màu trắng kem sữa sáng `#FFFDF8` có highlight phản quang nhẹ góc trên bên trái.
4. ✅ **BÀN GIAO ĐÚNG THƯ MỤC**: Lưu vào thư mục `Assets/Assetsgame/UI_Bubble/` hoặc `Assets/Export_Train_UI_Package/Sprites/`.

---

## 📝 PROMPT DÁN CHO ĐỘI VẼ (GPT / AGENT-SPRITE-FORGE):

```markdown
Role: Lead 2D Game UI Artist
Task: Generate a set of 2D game thought bubble / speech bubble UI sprites for a cozy restaurant cooking & farming simulation game ("Cooking-Game-2D").

Style Reference & Hard Constraints:
- Art Style: Cute, cozy, painterly cartoon UI style (similar to Hay Day, Township, Animal Crossing).
- Color Palette: 
  - Fill: Warm creamy white / light milk-white (#FFFDF8) with very soft subtle ambient gradient.
  - Outline: Warm rich dark brown (#482914 / #523018), fully closed contour, rounded smooth anti-aliased cartoon stroke (NEVER pure black).
  - Highlights: Discrete soft glossy specular highlight on the upper-left curve.
- Alpha: 100% clean transparent background (PNG), no ground shadows, no drop-shadows, no textured background.
- TEXT RULE: Absolutely NO text, NO numbers, NO letters, NO interior symbols inside the bubbles (clean empty interior).

Deliverable Asset List (Individual PNG files):
1. `bubble_thought_frame_large.png` (512x512 px):
   - Description: Large circular / slightly fluffy cloud-round thought bubble container. Clean spacious empty interior to hold dish food icons.
2. `bubble_thought_dot_medium.png` (256x256 px):
   - Description: Medium round floating thought bubble dot with matching dark brown outline and creamy white glossy fill.
3. `bubble_thought_dot_small.png` (128x128 px):
   - Description: Small round floating thought bubble dot connecting from character's head to medium dot.
4. `bubble_face_happy.png` (256x256 px):
   - Description: Sunny warm yellow round emoji with cute friendly smiling cartoon eyes and happy open smile (warm brown lines).
5. `bubble_face_angry.png` (256x256 px):
   - Description: Warm vibrant coral-red round emoji with cute frustrated/angry furrowed diagonal eyebrows and frown expression (cute cartoon style, not scary).
```
