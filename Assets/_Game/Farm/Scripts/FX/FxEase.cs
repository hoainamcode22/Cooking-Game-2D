using UnityEngine;

/// <summary>
/// HÀM LÀM MỀM (EASING) DÙNG CHUNG CHO BỘ HIỆU ỨNG FX
/// ═══════════════════════════════════════════════════
///
/// VÌ SAO CÓ FILE NÀY: dự án KHÔNG dùng DOTween nên mọi hiệu ứng phải viết bằng coroutine,
/// và mỗi coroutine lại tự chép lại một dòng `1 - Mathf.Pow(1-t, 3)`. Năm bản sao của cùng
/// một công thức là năm cơ hội để chúng lệch nhau (đã thấy: PlacementManager.BackOut dùng
/// c1 = 1.70158, PlacementGhostVisualController.ScaleFrame viết tay ease-out-cubic,
/// CoinFlyFX dùng `k*(2-k)`). Gom về một chỗ, đặt tên rõ, ai đọc cũng biết đang dùng cong gì.
///
/// KHÔNG sửa ba file kể trên — chúng đang chạy đúng. Bộ này chỉ để hiệu ứng MỚI dùng.
/// </summary>
public static class FxEase
{
    /// <summary>
    /// Ease-out-cubic: bật nhanh rồi hãm dần. Đây là "ease-out" mà tài liệu phân tích
    /// Township ghi cho bóng bay (+250px/2.5s) và số thưởng (+90px/1.2s).
    /// </summary>
    public static float OutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u;
    }

    /// <summary>Ease-out-quad — hãm nhẹ hơn OutCubic. Dùng cho pha "bung ra" ngắn.</summary>
    public static float OutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return t * (2f - t);
    }

    /// <summary>Ease-in-cubic — dùng cho pha TẮT (alpha 1 → 0) để phần mờ dồn về cuối.</summary>
    public static float InCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    /// <summary>
    /// Sóng sin đã chuẩn hoá về [0,1], đủ một chu kỳ khi <paramref name="phase01"/> đi từ 0 → 1.
    /// Dùng cho mọi nhịp lặp vô hạn (bob icon, nhấp nháy chevron) để không ai phải nhớ
    /// nhân 2π ở chỗ nào.
    /// </summary>
    public static float Sin01(float phase01)
        => (Mathf.Sin(phase01 * Mathf.PI * 2f) + 1f) * 0.5f;

    // ════════════════════════════════════════════════════════════════════════
    // EASE-OUT-BACK CÓ ĐỈNH CHÍNH XÁC
    // ════════════════════════════════════════════════════════════════════════
    //
    // Công thức chuẩn:  f(t) = 1 + c3·(t−1)³ + c1·(t−1)²   với c3 = c1 + 1
    // f(0) = 0, f(1) = 1, và giữa đường nó VƯỢT QUÁ 1 — chính cái vượt quá đó là toàn bộ
    // vị "nảy" mà tài liệu Township nhấn mạnh (§4.3: "bỏ nó đi là mất hết vị").
    //
    // BẪY: hằng số quen tay c1 = 1.70158 cho đỉnh ~1.10, KHÔNG phải 1.25 như thông số đo
    // được. Nếu cứ dùng 1.70158 rồi bảo "đã có overshoot" thì cú nảy nhẹ đi hơn một nửa.
    //
    // Giải tích: đạo hàm triệt tiêu tại u = −2c1/(3(c1+1)) (u = t−1), thay vào được
    //     ĐỘ VƯỢT o(c1) = 4·c1³ / (27·(c1+1)²)
    // Đảo lại để lấy c1 từ độ vượt mong muốn → BackConstantFor().

    /// <summary>
    /// c1 cho ĐỈNH ĐÚNG 1.25 — thông số của `FloatingNumber` (§4.3 tài liệu Township).
    /// Đây là nghiệm CHÍNH XÁC, không phải số gần đúng: o(3) = 4·27/(27·16) = 1/4 = 0.25.
    /// Kiểm tay: f(0)=1−4+3=0 · f(0.5)=1−0.5+0.75=1.25 · f(1)=1. ✔
    /// </summary>
    public const float BackC1Peak125 = 3f;

    /// <summary>
    /// Tìm c1 để ease-out-back có đỉnh bằng 1 + <paramref name="overshoot"/>.
    /// Giải `4c1³ − 27·o·(c1+1)² = 0` bằng Newton (hội tụ sau ~8 vòng, để 24 cho chắc).
    ///
    /// GỌI MỘT LẦN lúc bắt đầu animation rồi truyền c1 vào <see cref="OutBackRaw"/> —
    /// đừng gọi mỗi frame.
    /// </summary>
    public static float BackConstantFor(float overshoot)
    {
        // Không vượt → c1 = 0 → c3 = 1 → f(t) = 1 + (t−1)³ = đúng ease-out-cubic. Đẹp và an toàn.
        if (overshoot <= 0.0001f) return 0f;

        float o = overshoot;
        float k = 2f;
        for (int i = 0; i < 24; i++)
        {
            float kp1 = k + 1f;
            float f   = 4f * k * k * k - 27f * o * kp1 * kp1;
            float df  = 12f * k * k - 54f * o * kp1;
            if (Mathf.Abs(df) < 1e-6f) break;

            float next = k - f / df;
            if (next < 0.0001f) next = 0.0001f;      // giữ trong miền có nghĩa
            if (Mathf.Abs(next - k) < 1e-6f) { k = next; break; }
            k = next;
        }
        return k;
    }

    /// <summary>Ease-out-back với hằng số c1 đã biết trước. Rẻ, gọi được mỗi frame.</summary>
    public static float OutBackRaw(float t, float c1)
    {
        float u  = Mathf.Clamp01(t) - 1f;
        float c3 = c1 + 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    /// <summary>
    /// Ease-out-back đi từ 0 → (1 + <paramref name="overshoot"/>) → 1.
    /// Bản tiện lợi: tự giải c1 mỗi lần gọi, chỉ dùng khi KHÔNG gọi trong vòng lặp frame.
    /// </summary>
    public static float OutBackPeak(float t, float overshoot)
        => OutBackRaw(t, BackConstantFor(overshoot));

    // ════════════════════════════════════════════════════════════════════════
    // TIỆN ÍCH ALPHA — một chỗ duy nhất biết cách đổi độ mờ của MỌI loại renderer
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Đặt alpha cho mọi thứ vẽ được dưới <paramref name="root"/>: SpriteRenderer (world),
    /// Graphic/Image (UGUI) và TMP_Text.
    ///
    /// VÌ SAO GOM VÀO ĐÂY: bốn component trên đổi alpha bằng bốn cách khác nhau, và cả 4
    /// hiệu ứng của bộ này (bóng bay, số bay, hộp quà, icon) đều cần đúng việc đó. Viết
    /// riêng ở từng file là bốn lần cơ hội quên một loại renderer rồi "hiệu ứng không tắt".
    ///
    /// Truyền <paramref name="cache"/> (mảng lấy sẵn bằng <see cref="CollectFaders"/>) để
    /// không phải GetComponentsInChildren mỗi frame.
    /// </summary>
    public static void SetAlpha(Component[] cache, float alpha)
    {
        if (cache == null) return;

        for (int i = 0; i < cache.Length; i++)
        {
            Component c = cache[i];
            if (c == null) continue;

            if (c is SpriteRenderer sr)
            {
                Color col = sr.color;
                col.a = alpha;
                sr.color = col;
            }
            else if (c is UnityEngine.UI.Graphic g)     // Image, RawImage, TextMeshProUGUI…
            {
                Color col = g.color;
                col.a = alpha;
                g.color = col;
            }
        }
    }

    /// <summary>Thu sẵn danh sách renderer để <see cref="SetAlpha"/> khỏi phải tìm lại mỗi frame.</summary>
    public static Component[] CollectFaders(Transform root)
    {
        if (root == null) return new Component[0];

        var sprites  = root.GetComponentsInChildren<SpriteRenderer>(true);
        var graphics = root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);

        var all = new Component[sprites.Length + graphics.Length];
        for (int i = 0; i < sprites.Length; i++)  all[i] = sprites[i];
        for (int i = 0; i < graphics.Length; i++) all[sprites.Length + i] = graphics[i];
        return all;
    }

    /// <summary>
    /// Lệch pha ỔN ĐỊNH cho một object, suy từ vị trí + InstanceID.
    ///
    /// VÌ SAO KHÔNG DÙNG Random.value: mỗi lần bật lại scene sẽ ra một pha khác, nên cây
    /// cối / icon "nhảy" khác nhau giữa hai lần Play và không ai tái hiện được bug.
    /// Cách này cho pha khác nhau giữa các object nhưng GIỐNG NHAU qua mọi lần chạy.
    /// (Cùng thủ thuật EnvironmentSway.BuildStablePhase đã dùng.)
    /// </summary>
    public static float StablePhase01(Transform t)
    {
        if (t == null) return 0f;
        Vector3 p = t.position;
        float seed = (p.x * 12.9898f) + (p.y * 78.233f) + (t.GetInstanceID() * 0.017f);
        return Mathf.Repeat(Mathf.Sin(seed) * 43758.5453f, 1f);
    }
}
