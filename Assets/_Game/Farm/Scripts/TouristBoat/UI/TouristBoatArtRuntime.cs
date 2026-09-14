using UnityEngine;

namespace FarmGame.UI
{
    /// <summary>
    /// Runtime provider cho các sprite bo tròn cao cấp của Bến Tàu và Toast Hint Báo Tàu:
    /// - Tự động tải từ asset nếu có.
    /// - Nếu chưa tạo asset (chưa chạy tool Editor), tự động sinh Texture2D 9-slice mềm mại ngay trong RAM!
    /// Đảm bảo không bao giờ bị null, không bao giờ thiếu ảnh trên bất kỳ máy nào.
    /// </summary>
    public static class TouristBoatArtRuntime
    {
        private static Sprite _dockPlaqueSprite;
        private static Sprite _cardBgSprite;
        private static Sprite _btnSprite;

        /// <summary>Sprite huy hiệu bo tròn cho 3 bến tàu (240x110, border 32).</summary>
        public static Sprite GetDockPlaqueSprite()
        {
            if (_dockPlaqueSprite != null) return _dockPlaqueSprite;

#if UNITY_EDITOR
            _dockPlaqueSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites/dock_unlock_plaque.png");
            if (_dockPlaqueSprite != null) return _dockPlaqueSprite;
#endif
            _dockPlaqueSprite = Resources.Load<Sprite>("UI/dock_unlock_plaque");
            if (_dockPlaqueSprite != null) return _dockPlaqueSprite;

            // Tự sinh procedural texture 9-slice
            _dockPlaqueSprite = CreateProceduralPlaque();
            return _dockPlaqueSprite;
        }

        /// <summary>Sprite khung toast bo tròn báo tàu du lịch (520x150, border 32).</summary>
        public static Sprite GetBoatAnnounceCardSprite()
        {
            if (_cardBgSprite != null) return _cardBgSprite;

#if UNITY_EDITOR
            _cardBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites/boat_announce_card_bg.png");
            if (_cardBgSprite != null) return _cardBgSprite;
#endif
            _cardBgSprite = Resources.Load<Sprite>("UI/boat_announce_card_bg");
            if (_cardBgSprite != null) return _cardBgSprite;

            _cardBgSprite = CreateProceduralCardBg();
            return _cardBgSprite;
        }

        /// <summary>Sprite nút bấm bo tròn màu hổ phách "ĐÃ RÕ" (140x50, border 18).</summary>
        public static Sprite GetBoatAnnounceBtnSprite()
        {
            if (_btnSprite != null) return _btnSprite;

#if UNITY_EDITOR
            _btnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites/boat_announce_btn.png");
            if (_btnSprite != null) return _btnSprite;
#endif
            _btnSprite = Resources.Load<Sprite>("UI/boat_announce_btn");
            if (_btnSprite != null) return _btnSprite;

            _btnSprite = CreateProceduralBtn();
            return _btnSprite;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PROCEDURAL GENERATION (FALLBACK NẾU CHƯA CÓ FILE ASSET)
        // ─────────────────────────────────────────────────────────────────────

        private static Sprite CreateProceduralPlaque()
        {
            const int w = 240, h = 180;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;

            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            tex.SetPixels(px);

            // 1. Cột gỗ cắm ở chân (rộng 24px, cao từ y=4 đến y=100)
            int postW = 24;
            int postX = (w - postW) / 2;
            // Bóng đổ cột
            FillRounded(tex, postX - 2, 0, postW + 4, 95, 6, new Color(0f, 0f, 0f, 0.35f));
            // Viền cột
            FillRounded(tex, postX - 2, 2, postW + 4, 95, 6, new Color(0.24f, 0.13f, 0.05f, 1f));
            // Thân cột gỗ
            FillGradientRounded(tex, postX, 4, postW, 90, 4, new Color(0.42f, 0.26f, 0.12f, 1f), new Color(0.58f, 0.38f, 0.20f, 1f));

            // 2. Tấm bảng gỗ phía trên (y từ 65 tới 175, x từ 8 tới w - 8)
            int boardY = 65;
            int boardH = 110;
            int boardW = w - 16;
            int boardX = 8;

            // Bóng đổ bảng
            FillRounded(tex, boardX, boardY - 5, boardW, boardH, 16, new Color(0f, 0f, 0f, 0.45f));
            // Viền gỗ ngoài sậm màu
            FillRounded(tex, boardX, boardY, boardW, boardH, 16, new Color(0.28f, 0.15f, 0.06f, 1f));
            // Viền vàng kim tinh tế
            FillGradientRounded(tex, boardX + 3, boardY + 3, boardW - 6, boardH - 6, 13, new Color(0.85f, 0.55f, 0.12f, 1f), new Color(0.98f, 0.82f, 0.28f, 1f));
            // Thân ván gỗ ấm áp
            FillGradientRounded(tex, boardX + 6, boardY + 6, boardW - 12, boardH - 12, 10, new Color(0.48f, 0.28f, 0.13f, 1f), new Color(0.68f, 0.44f, 0.24f, 1f));

            // 3. Đinh tán kim loại ở 4 góc bảng
            DrawRivet(tex, boardX + 12, boardY + boardH - 14);
            DrawRivet(tex, boardX + boardW - 14, boardY + boardH - 14);
            DrawRivet(tex, boardX + 12, boardY + 12);
            DrawRivet(tex, boardX + boardW - 14, boardY + 12);

            tex.Apply();
            // Pivot ở chân cột (0.5f, 0.05f) để cắm đứng trên mặt cầu tàu, PPU = 1f để đúng tỉ lệ world units
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 1f);
        }

        private static void DrawRivet(Texture2D tex, int cx, int cy)
        {
            for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
            {
                if (dx * dx + dy * dy <= 9)
                {
                    Color c = (dx <= 0 && dy >= 0) ? new Color(1f, 0.9f, 0.6f, 1f) : new Color(0.2f, 0.1f, 0.03f, 1f);
                    tex.SetPixel(cx + dx, cy + dy, c);
                }
            }
        }

        private static Sprite CreateProceduralCardBg()
        {
            const int w = 240, h = 90;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;

            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            tex.SetPixels(px);

            // Drop shadow
            FillRounded(tex, 4, 0, w - 8, h - 6, 20, new Color(0f, 0f, 0f, 0.50f));
            // Gold trim
            FillGradientRounded(tex, 4, 4, w - 8, h - 6, 20, new Color(0.85f, 0.55f, 0.10f, 1f), new Color(0.98f, 0.80f, 0.25f, 1f));
            // Deep navy body
            FillGradientRounded(tex, 8, 8, w - 16, h - 14, 16, new Color(0.10f, 0.15f, 0.22f, 0.98f), new Color(0.18f, 0.28f, 0.38f, 0.98f));

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
        }

        private static Sprite CreateProceduralBtn()
        {
            const int w = 100, h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;

            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            tex.SetPixels(px);

            // Drop shadow
            FillRounded(tex, 2, 0, w - 4, h - 4, 14, new Color(0f, 0f, 0f, 0.38f));
            // Gold outline
            FillRounded(tex, 2, 3, w - 4, h - 4, 14, new Color(0.98f, 0.90f, 0.60f, 1f));
            // Amber body
            FillGradientRounded(tex, 4, 5, w - 8, h - 8, 12, new Color(0.85f, 0.42f, 0.05f, 1f), new Color(0.98f, 0.68f, 0.15f, 1f));

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
        }

        private static void FillRounded(Texture2D tex, int x, int y, int width, int height, int radius, Color col)
        {
            int xMax = x + width - 1, yMax = y + height - 1;
            for (int py = y; py <= yMax; py++)
                for (int px = x; px <= xMax; px++)
                    if (IsInsideRoundedRect(px, py, x, y, xMax, yMax, radius))
                        BlendPixel(tex, px, py, col);
        }

        private static void FillGradientRounded(Texture2D tex, int x, int y, int width, int height, int radius, Color bCol, Color tCol)
        {
            int xMax = x + width - 1, yMax = y + height - 1;
            for (int py = y; py <= yMax; py++)
            {
                float t = (float)(py - y) / Mathf.Max(1, height - 1);
                Color rowCol = Color.Lerp(bCol, tCol, t);
                for (int px = x; px <= xMax; px++)
                    if (IsInsideRoundedRect(px, py, x, y, xMax, yMax, radius))
                        BlendPixel(tex, px, py, rowCol);
            }
        }

        private static bool IsInsideRoundedRect(int px, int py, int xMin, int yMin, int xMax, int yMax, int radius)
        {
            if (px >= xMin + radius && px <= xMax - radius) return true;
            if (py >= yMin + radius && py <= yMax - radius) return true;
            int cx = (px < xMin + radius) ? xMin + radius : xMax - radius;
            int cy = (py < yMin + radius) ? yMin + radius : yMax - radius;
            int dx = px - cx, dy = py - cy;
            return (dx * dx + dy * dy) <= (radius * radius);
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color src)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
            Color dst = tex.GetPixel(x, y);
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0f) return;
            Color outCol = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
            outCol.a = outA;
            tex.SetPixel(x, y, outCol);
        }
    }
}
