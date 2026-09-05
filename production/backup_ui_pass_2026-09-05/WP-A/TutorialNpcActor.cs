using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Ba clip diễn của NPC hướng dẫn viên tutorial.</summary>
public enum TutorialNpcClip
{
    /// <summary>Đang nói chuyện — dùng ~80% thời lượng tutorial. Loop kín 12 frame.</summary>
    Talk = 0,
    /// <summary>Vẫy tay chào / ăn mừng xong một chặng. Loop kín 12 frame.</summary>
    Wave = 1,
    /// <summary>Chỉ tay vào thứ cần bấm. Chạy tới frame giữ rồi LẶP ĐOẠN ĐUÔI trong lúc chờ.</summary>
    Point = 2,
}

/// <summary>
/// NHÂN VẬT HƯỚNG DẪN VIÊN TUTORIAL — diễn 3 clip × 12 frame + chớp mắt ngẫu nhiên.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// CHỦ FILE: DEV-ANIM. Không Dev nào khác được sửa file này (luật "mỗi file một chủ",
/// rút ra sau sự cố 31/08: 2 Dev cùng file ⇒ build sạch nhưng chạy code cũ, im lặng hoàn toàn).
///
/// TRIẾT LÝ FALLBACK (giống BuilderWorkerConfig): THIẾU ART VẪN CHẠY.
///   • Không có frame nào  ⇒ giữ nguyên sprite đang có trên Image (placeholder), log 1 lần, KHÔNG crash.
///   • Có ít hơn 12 frame  ⇒ vẫn chạy với số frame đang có.
///   • Không có blinkSprite ⇒ bỏ qua chớp mắt, mọi thứ khác vẫn diễn.
/// Nhờ vậy khung sườn chạy được NGAY hôm nay, đội vẽ giao art là thay sprite, không đụng code.
///
/// KHỚP VỚI PROMPT ĐỘI VẼ (production/PROMPT_SPRITE_FORGE_TUTORIAL_V2_2026-09-04.md):
///   guide_talk_01..12 · guide_wave_01..12 · guide_point_01..12 · guide_blink
///   frame 01 = tư thế nghỉ · frame 12 nối mượt về 01 · clip Point giữ nhịp ở frame 06→12.
///
/// KHÔNG dùng Animator/AnimationClip vì: 3 clip × 12 frame không đáng một Animator Controller,
/// và code cần đổi frame theo unscaledTime (tutorial có lúc chạy khi Time.timeScale = 0).
///
/// [TutorialV2]
/// </summary>
[DisallowMultipleComponent]
public class TutorialNpcActor : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    [Header("◆ Đích vẽ")]
    [Tooltip("Image sẽ được đổi sprite mỗi frame. Bỏ trống thì tự tìm Image trên chính object này.")]
    [SerializeField] private Image targetImage;

    [Header("◆ 3 CLIP × 12 FRAME (đội vẽ giao — để trống vẫn chạy)")]
    [Tooltip("guide_talk_01..12 — đang nói chuyện, dùng nhiều nhất.")]
    [SerializeField] private Sprite[] talkFrames;

    [Tooltip("guide_wave_01..12 — vẫy tay chào / ăn mừng.")]
    [SerializeField] private Sprite[] waveFrames;

    [Tooltip("guide_point_01..12 — chỉ tay vào mục tiêu.")]
    [SerializeField] private Sprite[] pointFrames;

    [Tooltip("guide_blink — CHÍNH XÁC là frame nghỉ nhưng nhắm mắt. Để trống = không chớp mắt.")]
    [SerializeField] private Sprite blinkSprite;

    [Header("◆ Nhịp phát (fps)")]
    [SerializeField] private float talkFps  = 12f;
    [SerializeField] private float waveFps  = 14f;
    [SerializeField] private float pointFps = 12f;

    [Header("◆ Clip Point — giữ nhịp trong lúc chờ người chơi bấm")]
    [Tooltip("Chạy từ frame 1 tới hết, rồi LẶP đoạn từ frame này tới frame cuối (1-based, khớp cách vẽ). " +
             "Đội vẽ vẽ frame 06→12 là 'giữ tư thế chỉ, nhấn nhá rất nhẹ' — lặp lâu không khó chịu.")]
    [SerializeField] private int pointHoldFromFrame = 6;

    [Header("◆ Chớp mắt")]
    [Tooltip("Khoảng cách 2 lần chớp mắt (giây): random trong [x, y].")]
    [SerializeField] private Vector2 blinkEverySeconds = new Vector2(3f, 6f);

    [Tooltip("Mắt nhắm bao lâu. 0.10-0.14s là tự nhiên; lâu hơn nhìn như buồn ngủ.")]
    [SerializeField] private float blinkDuration = 0.12f;

    [Header("◆ Xuất hiện")]
    [Tooltip("Trượt vào từ bên trái bao nhiêu pixel. 0 = không trượt, chỉ hiện.")]
    [SerializeField] private float enterSlidePixels = 90f;

    [Tooltip("Thời lượng trượt vào (giây, unscaled).")]
    [SerializeField] private float enterDuration = 0.32f;

    // ═══════════════════════════════════════════════════════════════════════
    private RectTransform _rt;
    private Coroutine _playRoutine;
    private Coroutine _blinkRoutine;
    private Coroutine _enterRoutine;

    private Sprite _frameHienTai;          // frame clip đang giữ — blink xong trả về đúng cái này
    private bool   _dangChopMat;
    private bool   _daCanhBaoThieuArt;
    private TutorialNpcClip _clipHienTai = TutorialNpcClip.Talk;

    /// <summary>Clip đang diễn (đọc để debug / QA).</summary>
    public TutorialNpcClip ClipHienTai => _clipHienTai;

    // ═══════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        _rt = transform as RectTransform;
        if (targetImage == null) targetImage = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);

        // NPC là trang trí — tuyệt đối không được nuốt click của card/nút Tiếp tục.
        if (targetImage != null) targetImage.raycastTarget = false;
    }

    private void OnDisable()
    {
        DungTatCaCoroutine();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // API công khai — TutorialDialogueCard / TutorialManager gọi vào đây
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Diễn một clip. Gọi lại với clip đang chạy thì KHÔNG khởi động lại (tránh giật
    /// khi nhiều bước liên tiếp cùng dùng clip Talk).
    /// </summary>
    public void Play(TutorialNpcClip clip, bool batBuocKhoiDongLai = false)
    {
        if (!batBuocKhoiDongLai && _playRoutine != null && _clipHienTai == clip) return;

        _clipHienTai = clip;

        if (_playRoutine != null) { StopCoroutine(_playRoutine); _playRoutine = null; }

        Sprite[] frames = LayFrames(clip);
        if (frames == null || frames.Length == 0)
        {
            CanhBaoThieuArtMotLan(clip);
            // Thiếu frame clip KHÔNG có nghĩa là thiếu blink — vẫn cho chớp mắt nếu có
            // (QA 04/09: trước đây return sớm làm mất luôn chớp mắt dù blinkSprite có sẵn).
            BatChopMatNeuCan();
            return; // giữ nguyên sprite placeholder đang có — KHÔNG crash, KHÔNG xoá hình
        }

        if (!gameObject.activeInHierarchy) return;

        _playRoutine = StartCoroutine(ChayClip(frames, LayFps(clip), clip == TutorialNpcClip.Point));
        BatChopMatNeuCan();
    }

    /// <summary>Dừng diễn, giữ nguyên frame cuối (không xoá hình).</summary>
    public void Stop()
    {
        DungTatCaCoroutine();
    }

    /// <summary>
    /// Hiện NPC kèm trượt vào từ trái. Gọi khi card hội thoại mở ở bước đầu tiên.
    /// Các bước sau chỉ cần <see cref="Play"/>, không cần gọi lại hàm này.
    /// </summary>
    public void PlayEnter(TutorialNpcClip clip = TutorialNpcClip.Wave)
    {
        gameObject.SetActive(true);
        Play(clip, batBuocKhoiDongLai: true);

        if (_rt == null || enterSlidePixels <= 0.01f || enterDuration <= 0.01f) return;

        // Cha có thể đang tắt (card chưa mở) → StartCoroutine sẽ ném log đỏ. Bỏ qua trượt vào,
        // NPC vẫn hiện đúng chỗ, không ảnh hưởng tutorial.
        if (!gameObject.activeInHierarchy) return;

        if (_enterRoutine != null) StopCoroutine(_enterRoutine);
        _enterRoutine = StartCoroutine(ChayTruotVao());
    }

    /// <summary>
    /// Nạp frame lúc runtime (Editor tool hoặc loader art dùng). Truyền null cho clip nào
    /// không muốn đổi. Đang diễn clip đó thì tự khởi động lại để ăn frame mới ngay.
    /// </summary>
    public void SetFrames(Sprite[] talk, Sprite[] wave, Sprite[] point, Sprite blink)
    {
        if (talk  != null) talkFrames  = talk;
        if (wave  != null) waveFrames  = wave;
        if (point != null) pointFrames = point;
        if (blink != null) blinkSprite = blink;

        _daCanhBaoThieuArt = false;
        if (_playRoutine != null) Play(_clipHienTai, batBuocKhoiDongLai: true);
    }

    /// <summary>Đã có đủ art thật chưa (QA / tool nghiệm thu hỏi).</summary>
    public bool CoArtThat => talkFrames != null && talkFrames.Length > 0;

    // ═══════════════════════════════════════════════════════════════════════
    // Bên trong
    // ═══════════════════════════════════════════════════════════════════════

    private Sprite[] LayFrames(TutorialNpcClip clip)
    {
        switch (clip)
        {
            case TutorialNpcClip.Wave:  return waveFrames;
            case TutorialNpcClip.Point: return pointFrames;
            default:                    return talkFrames;
        }
    }

    private float LayFps(TutorialNpcClip clip)
    {
        switch (clip)
        {
            case TutorialNpcClip.Wave:  return Mathf.Max(1f, waveFps);
            case TutorialNpcClip.Point: return Mathf.Max(1f, pointFps);
            default:                    return Mathf.Max(1f, talkFps);
        }
    }

    /// <summary>
    /// Chạy frame theo unscaledDeltaTime (tutorial có thể mở lúc Time.timeScale = 0).
    /// Clip Point: chạy hết một lượt rồi chỉ lặp ĐOẠN ĐUÔI (frame giữ → hết) —
    /// vì đó là lúc người chơi đang nhìn tay chỉ và loay hoay tìm nút.
    /// </summary>
    private IEnumerator ChayClip(Sprite[] frames, float fps, bool laClipPoint)
    {
        float khoangFrame = 1f / fps;
        int   soFrame     = frames.Length;

        // 1-based ở Inspector cho khớp tên file guide_point_06 → 0-based trong mảng.
        int mocLap = laClipPoint
            ? Mathf.Clamp(pointHoldFromFrame - 1, 0, Mathf.Max(0, soFrame - 1))
            : 0;

        int idx = 0;

        while (true)
        {
            Sprite sp = frames[idx];
            if (sp != null)
            {
                _frameHienTai = sp;
                if (!_dangChopMat && targetImage != null) targetImage.sprite = sp;
            }

            float doi = 0f;
            while (doi < khoangFrame) { doi += Time.unscaledDeltaTime; yield return null; }

            idx++;
            if (idx >= soFrame)
            {
                // Clip Point: đã diễn trọn 1 lượt ⇒ từ giờ chỉ LẶP ĐOẠN ĐUÔI (frame giữ → hết),
                // vì đó là lúc người chơi đang nhìn tay chỉ và loay hoay tìm nút.
                idx = laClipPoint ? mocLap : 0;
            }
        }
    }

    private IEnumerator ChayTruotVao()
    {
        Vector2 dich = _rt.anchoredPosition;
        Vector2 dau  = dich + new Vector2(-Mathf.Abs(enterSlidePixels), 0f);

        _rt.anchoredPosition = dau;

        float t = 0f;
        while (t < enterDuration)
        {
            t += Time.unscaledDeltaTime;
            float r = Mathf.Clamp01(t / enterDuration);
            // Ease-out-back nhẹ: vọt qua 6% rồi lùi về — cho cảm giác "bước tới", không trôi phẳng.
            float e = EaseOutBack(r, 1.06f);
            _rt.anchoredPosition = Vector2.LerpUnclamped(dau, dich, e);
            yield return null;
        }

        _rt.anchoredPosition = dich;
        _enterRoutine = null;
    }

    /// <summary>Ease-out-back tự viết — dự án không có DOTween/LeanTween (đã kiểm manifest).</summary>
    private static float EaseOutBack(float t, float doVot)
    {
        float c1 = 1.70158f * doVot;
        float c3 = c1 + 1f;
        float p  = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    private void BatChopMatNeuCan()
    {
        if (blinkSprite == null) return;
        if (_blinkRoutine != null) return;
        if (!gameObject.activeInHierarchy) return;
        _blinkRoutine = StartCoroutine(ChayChopMat());
    }

    private IEnumerator ChayChopMat()
    {
        float min = Mathf.Max(0.5f, Mathf.Min(blinkEverySeconds.x, blinkEverySeconds.y));
        float max = Mathf.Max(min + 0.1f, Mathf.Max(blinkEverySeconds.x, blinkEverySeconds.y));

        while (true)
        {
            float cho = Random.Range(min, max);
            float t = 0f;
            while (t < cho) { t += Time.unscaledDeltaTime; yield return null; }

            if (targetImage == null || blinkSprite == null) continue;

            _dangChopMat = true;
            targetImage.sprite = blinkSprite;

            float t2 = 0f;
            float dur = Mathf.Max(0.04f, blinkDuration);
            while (t2 < dur) { t2 += Time.unscaledDeltaTime; yield return null; }

            _dangChopMat = false;
            // Trả về ĐÚNG frame clip đang giữ, không phải frame 0 — nếu không sẽ giật một nhịp.
            if (_frameHienTai != null) targetImage.sprite = _frameHienTai;
        }
    }

    private void DungTatCaCoroutine()
    {
        if (_playRoutine  != null) { StopCoroutine(_playRoutine);  _playRoutine  = null; }
        if (_blinkRoutine != null) { StopCoroutine(_blinkRoutine); _blinkRoutine = null; }
        if (_enterRoutine != null) { StopCoroutine(_enterRoutine); _enterRoutine = null; }
        _dangChopMat = false;
    }

    private void CanhBaoThieuArtMotLan(TutorialNpcClip clip)
    {
        if (_daCanhBaoThieuArt) return;
        _daCanhBaoThieuArt = true;
        Debug.Log($"[TutorialNpcActor] Chưa có frame cho clip '{clip}' → giữ nguyên sprite placeholder, " +
                  "NPC vẫn hiện, tutorial vẫn chạy bình thường. Art về thì chạy " +
                  "'Tools ▸ Farm Game ▸ Tutorial V2 ▸ Nạp art NPC' là xong, không cần sửa code.");
    }
}
