using UnityEngine;

/// <summary>
/// Quáº£n lÃ½ visual cÃ¢y trá»“ng trong 1 plot.
/// Script gáº¯n lÃªn CropGroup (parent cá»§a CropPoint_1..4).
///
/// Hierarchy:
///   CropGroup  â† script nÃ y
///     CropPoint_1  (Transform rá»—ng, localScale = 1,1,1)
///       Visual     (SpriteRenderer, táº¡o tá»± Ä‘á»™ng)
///     CropPoint_2 ...
///
/// Scale cuá»‘i: normalizedFromHeight * cropDataStageScale * globalVisualMultiplier
/// localPosition cá»§a Visual luÃ´n = (0,0,0).
/// </summary>
public class PlotCropVisual : MonoBehaviour
{
    [Header("Points — tự tìm, hoặc gán tay")]
    [SerializeField] private Transform[] cropPoints = new Transform[0];

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Crop";
    [SerializeField] private int    sortingOrder     = 20;

    [Header("Lattice — rải cây đều theo lưới iso (2026-08-27)")]
    // Thay cho việc rải tay CropPoint trong prefab: tính lưới ngay lúc chạy theo
    // ĐÚNG hình thoi của sprite nền plot, nên số cây bao nhiêu cũng đều (6, 12, 16...).
    [SerializeField] private bool      useIsoLattice  = true;
    [SerializeField] private Transform groundRef;                 // trống = tự tìm "GroundSprite"
    [SerializeField] private float     latticeInset   = 0.86f;    // 1 = sát mép ô, nhỏ hơn = thụt vào
    [SerializeField] private Vector2   latticeOffset  = Vector2.zero;
    // Đẩy cả lưới về phía TRƯỚC (xuống dưới) theo % nửa chiều cao ô: gốc cây nằm ở lưới
    // nên tán lá vươn lên trên; dịch xuống một chút thì khối cây phủ giữa ô, không hở mép trước.
    [SerializeField] private float     latticeDepthBias = -0.12f;

    [Header("Ripe Wind Sway")]
    [SerializeField] private bool  enableReadySway = true;
    [SerializeField] private float swayAngle       = 4.5f;
    [SerializeField] private float swaySpeed       = 1.4f;
    [SerializeField] private float swayPhaseRange  = 2.0f;

    // ── [PERF F4.9 — 2026-09-17] ─────────────────────────────────────────────────
    // Do duoc: ~26 instance PlotCropVisual dang sway trong SCN_Farm, cong 34 instance
    // EnvironmentSway ⇒ khoang 60 luot Update ghi transform / frame, phan lon cho o ruong
    // NGOAI khung hinh.
    // KHONG BO HIEU UNG. Chi them hai cong, GIONG HET EnvironmentSway/GentleSway:
    //   ① ngoai khung hinh ⇒ bo qua vong ghi rotation;
    //   ② chay 1 lan moi N frame, moi o ruong mot offset rieng nen khong cung tick.
    // `swayTimer` VAN cong dồn MOI FRAME nen pha khong bao gio troi du co bo frame.
    [Tooltip("BẬT = bỏ qua vòng ghi rotation khi cây trồng nằm ngoài khung hình.")]
    [SerializeField] private bool chiSwayKhiThayDuoc = true;

    [Tooltip("Chạy sway 1 lần mỗi N frame. 2 = 30Hz ở 60fps — mắt không đọc ra vì đây là sin chậm.")]
    [Range(1, 4)]
    [SerializeField] private int swayBuocFrame = 2;

    // ── [VFX 2026-09-24] cay dang lon cung lac nhe + gio theo dot + lap lanh khi chin + nay khi len stage ──
    [Header("VFX nong trai (2026-09-24)")]
    [Tooltip("Cay CHUA chin cung lac nhe theo gio (nho hon cay chin).")]
    [SerializeField] private bool  swayKhiDangLon   = true;
    [SerializeField] private float heSoSwayDangLon  = 0.45f;
    [Tooltip("Goc lac nhan he so gio dung chung (GioNongTrai) -> ca canh dong nghieng theo tung dot gio.")]
    [SerializeField] private bool  theoGioNongTrai  = true;
    [Tooltip("Cay chin: thinh thoang loe 1 ngoi sao lap lanh tren ngon (chi khi dang trong khung hinh).")]
    [SerializeField] private bool  lapLanhKhiChin   = true;
    [SerializeField] private Vector2 lapLanhCachGiay = new Vector2(2.5f, 5f);
    [Tooltip("Cay len stage moi: 'bat' len nhe (co lai roi nay to) + vai hat lap lanh.")]
    [SerializeField] private bool  nayKhiLenStage   = true;

    private float _heSoGoc = 1f;

    // ── [VFX 2026-09-24] Cho buom dau: o dang trong HOA (moi stage) hoac vua GIEO HAT (stage 0) ──
    public static readonly System.Collections.Generic.List<PlotCropVisual> DiemDauBuom =
        new System.Collections.Generic.List<PlotCropVisual>();
    private bool _laDiemDau;
    private int  _oHoa = -1;   // -1 chua xet, 0 khong, 1 co (o/chau hoa) - xet 1 lan, khong GetComponentInParent moi khung
    private float _henLapLanh = -1f;
    private Coroutine _nayRoutine;

    // â”€â”€ Internal â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private CropData         currentCrop;
    private SpriteRenderer[] slotRenderers;
    private Transform[]      slotVisuals;
    private float[]          slotPhase;

    private float swayTimer;
    private int   _swayOffsetFrame = -1;   // [PERF F4.9] rai cac o ruong ra nhieu frame khac nhau.
    private bool  isReadySwayActive;
    private bool  isSetupDone;
    private int   lastLatticeCount = -1;

    // ── HIỆU NĂNG 2026-08-29 ─────────────────────────────────────────────────
    // ShowCrop()/ClearAll() bị PlotController.RefreshVisual() gọi MỖI FRAME cho từng
    // ô ruộng. Mỗi lượt gọi trước đây ghi lại: 12 sprite + 12 localScale + 12
    // localPosition + 12 SetActive — dù cây vẫn đứng nguyên stage đó. 38 ô × 60 fps
    // ⇒ khoảng 2.000 lượt ghi thừa mỗi giây, làm bẩn transform và ép dựng lại batch.
    // Bốn biến dưới đây nhớ "lần vẽ gần nhất đã vẽ gì"; trùng thì bỏ qua.
    // KHÔNG đổi hình ảnh — chỉ bỏ lượt ghi lặp lại y hệt.
    private CropData _lastShownCrop;
    private int      _lastShownStage = -1;
    private int      _lastShownCount = -1;
    private bool     _lastShownReady;
    private bool     _isCleared;

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void Awake()
    {
        EnsureSetup();
    }
    private void OnValidate() => AutoFindPoints();

    private void Update()
    {
        if (!enableReadySway || !isReadySwayActive || slotVisuals == null)
            return;

        // ⚠ CONG DON TRUOC MOI CONG BO-FRAME: neu bo qua dong nay o frame bi skip thi pha
        // sway se CHAY CHAM lai theo ti le buocFrame — cay lac cham dan, khong con dung nhip.
        swayTimer += Time.deltaTime;

        // [PERF F4.9] ① ngoai khung hinh => khong ai nhin thay, bo qua vong ghi rotation.
        // Cac cay trong CUNG mot o nam sat nhau nen chi can hoi renderer hop le dau tien.
        if (chiSwayKhiThayDuoc && slotRenderers != null && !CoCayNaoDangHien())
            return;

        // [VFX 2026-09-24] Lap lanh cay chin (dong ho theo swayTimer, khong cap phat gi)
        if (lapLanhKhiChin && _lastShownReady)
        {
            if (_henLapLanh < 0f) _henLapLanh = swayTimer + Random.Range(lapLanhCachGiay.x, lapLanhCachGiay.y);
            else if (swayTimer >= _henLapLanh)
            {
                _henLapLanh = swayTimer + Random.Range(lapLanhCachGiay.x, lapLanhCachGiay.y);
                PhatLapLanh(1);
            }
        }

        // [PERF F4.9] ② giam nhip. Goc chi phu thuoc swayTimer (da cong du o tren) nen
        // bo frame KHONG lam troi pha.
        int buoc = Mathf.Clamp(swayBuocFrame, 1, 4);
        if (_swayOffsetFrame < 0) _swayOffsetFrame = (int)((uint)GetInstanceID() % 4u);
        if (buoc > 1 && (Time.frameCount % buoc) != (_swayOffsetFrame % buoc))
            return;

        float heSo = _heSoGoc * (theoGioNongTrai ? GioNongTrai.HeSo(transform.position.x) : 1f);
        for (int i = 0; i < slotVisuals.Length; i++)
        {
            Transform      v  = slotVisuals[i];
            SpriteRenderer sr = slotRenderers[i];
            if (v == null || sr == null || !sr.enabled) continue;

            float angle = Mathf.Sin((swayTimer + slotPhase[i]) * swaySpeed) * swayAngle * heSo;
            v.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>
    /// [PERF F4.9] Co it nhat MOT SpriteRenderer cay trong dang nam trong khung hinh khong.
    /// `isVisible` la bool do he render ghi — doc rat re, khong phai phep tinh bounds.
    /// Tra ve TRUE khi khong co renderer nao hop le: khong xac dinh duoc thi cu chay nhu cu.
    /// </summary>
    private bool CoCayNaoDangHien()
    {
        if (slotRenderers == null) return true;

        bool coRendererHopLe = false;
        for (int i = 0; i < slotRenderers.Length; i++)
        {
            SpriteRenderer sr = slotRenderers[i];
            if (sr == null || !sr.enabled) continue;
            coRendererHopLe = true;
            if (sr.isVisible) return true;
        }
        return !coRendererHopLe;
    }

    // â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Coroutine wiggleRoutine;

    /// <summary>
    /// Tính toán scale chuẩn của cây theo stage và loại cây (hoa hoặc nông sản thông thường).
    /// </summary>
    public Vector3 GetCurrentTargetScale(int stage = -1)
    {
        if (currentCrop == null) return Vector3.one;
        if (stage < 0)
        {
            stage = _lastShownStage >= 0 ? _lastShownStage : (currentCrop.StageCount - 1);
        }
        Vector3 targetScale = currentCrop.GetScale(stage);

        bool isFlower = currentCrop.cropCategory == CropCategory.Flower;
        if (!isFlower)
        {
            var plot = GetComponentInParent<PlotController>();
            if (plot != null && plot.Category == PlotCategory.Flower) isFlower = true;
            else if (transform.parent != null && (transform.parent.name.ToLower().Contains("chau") || transform.parent.name.ToLower().Contains("pot") || transform.parent.name.ToLower().Contains("hoa")))
                isFlower = true;
        }
        if (isFlower)
        {
            targetScale *= 0.72f;
        }
        return targetScale;
    }

    public void PlayWiggleAnimation()
    {
        if (currentCrop == null || slotVisuals == null) return;
        if (wiggleRoutine != null) StopCoroutine(wiggleRoutine);
        wiggleRoutine = StartCoroutine(CoWiggle());
    }

    private System.Collections.IEnumerator CoWiggle()
    {
        if (slotVisuals == null || currentCrop == null) yield break;

        bool wasSwayActive = isReadySwayActive;
        isReadySwayActive = false;

        float elapsed = 0f;
        float duration = 0.35f;
        Vector3 baseScale = GetCurrentTargetScale();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float sin = Mathf.Sin(t * Mathf.PI);
            float angle = sin * 6f;

            for (int i = 0; i < slotVisuals.Length; i++)
            {
                if (slotVisuals[i] == null) continue;
                slotVisuals[i].localRotation = Quaternion.Euler(0f, 0f, angle * ((i % 2 == 0) ? 1 : -1));
                // Luôn giữ đúng scale chuẩn của stage hiện tại, KHÔNG nhân hệ số phóng to làm to khổng lồ
                slotVisuals[i].localScale = baseScale;
            }
            yield return null;
        }

        for (int i = 0; i < slotVisuals.Length; i++)
        {
            if (slotVisuals[i] == null) continue;
            slotVisuals[i].localRotation = Quaternion.identity;
            slotVisuals[i].localScale = baseScale;
        }

        isReadySwayActive = wasSwayActive;
    }

    /// <summary>Hiển thị crop theo progress (0..1).</summary>
    public void ShowCrop(CropData crop, float progress01)
    {
        EnsureSetup();
        if (crop == null) { ClearAll(); return; }

        EnsurePointsCount(crop.displayCount);

        currentCrop = crop;
        progress01  = Mathf.Clamp01(progress01);
        // 2026-08-27: số stage do CropData quyết định (bộ mới = 5, cây chưa chuyển = 3).
        int  stage   = crop.StageFromProgress(progress01);
        DatDiemDau(stage == 0 || LaHoa(crop));   // [VFX 2026-09-24] re: chi Add/Remove khi doi trang thai
        bool isReady = stage >= crop.StageCount - 1;

        ApplyLattice(crop.displayCount);      // tự bỏ qua khi số cây không đổi
        SetReadySwayActive(isReady || (swayKhiDangLon && enableReadySway));   // tự bỏ qua khi trạng thái không đổi
        _heSoGoc = isReady ? 1f : heSoSwayDangLon;

        // Không có gì đổi so với lần vẽ trước ⇒ khỏi ghi lại 12 slot.
        bool sameAsLast = !_isCleared
                       && ReferenceEquals(crop, _lastShownCrop)
                       && stage      == _lastShownStage
                       && isReady    == _lastShownReady
                       && crop.displayCount == _lastShownCount;
        if (sameAsLast) return;

        // [VFX 2026-09-24] Cung cay, stage tang (khong tinh lan hien dau / vua trong lai)
        bool lenStage = nayKhiLenStage && !_isCleared && ReferenceEquals(crop, _lastShownCrop)
                        && _lastShownStage >= 0 && stage > _lastShownStage;

        _isCleared      = false;
        _lastShownCrop  = crop;
        _lastShownStage = stage;
        _lastShownReady = isReady;
        _lastShownCount = crop.displayCount;

        UpdateVisual(stage);

        if (!isReady && !swayKhiDangLon)
            foreach (var v in slotVisuals)
                if (v != null) v.localRotation = Quaternion.identity;

        for (int i = 0; i < cropPoints.Length; i++)
            if (cropPoints[i] != null)
                cropPoints[i].gameObject.SetActive(i < crop.displayCount);

        if (lenStage) NayLenStage(isReady);
        if (!isReady) _henLapLanh = -1f;
    }

    // ── [VFX 2026-09-24] ─────────────────────────────────────────────────────

    /// <summary>Loe sao lap lanh tren ngon vai cay ngau nhien trong o (dung pool FarmAmbientFX).</summary>
    private void PhatLapLanh(int soSao)
    {
        if (slotRenderers == null || FarmAmbientFX.Instance == null) return;
        for (int n = 0; n < soSao; n++)
        {
            int batDau = Random.Range(0, slotRenderers.Length);
            for (int k = 0; k < slotRenderers.Length; k++)
            {
                var sr = slotRenderers[(batDau + k) % slotRenderers.Length];
                if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                Bounds b = sr.bounds;
                var p = new Vector3(b.center.x + Random.Range(-0.3f, 0.3f) * b.size.x,
                                    b.max.y - b.size.y * Random.Range(0.1f, 0.35f), 0f);
                FarmAmbientFX.LapLanh(p, 0.9f);
                break;
            }
        }
    }

    private void NayLenStage(bool vuaChin)
    {
        if (!isActiveAndEnabled || slotVisuals == null) return;
        if (chiSwayKhiThayDuoc && !CoCayNaoDangHien()) return;   // ngoai khung hinh: khoi ton
        if (_nayRoutine != null) StopCoroutine(_nayRoutine);
        _nayRoutine = StartCoroutine(CoNayLenStage());
        PhatLapLanh(vuaChin ? 3 : 1);
    }

    private System.Collections.IEnumerator CoNayLenStage()
    {
        Vector3 goc = GetCurrentTargetScale();
        float t = 0f;
        const float D = 0.38f;
        while (t < D)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / D);
            // co lai 0.82 -> vuot 1.12 -> ve 1 (gio tu goc: pivot duoi cua bo stage moi)
            float s = k < 0.35f ? Mathf.Lerp(0.82f, 1.12f, Mathf.Sin(k / 0.35f * Mathf.PI * 0.5f))
                                : Mathf.Lerp(1.12f, 1f, 1f - (1f - (k - 0.35f) / 0.65f) * (1f - (k - 0.35f) / 0.65f));
            Vector3 sc = new Vector3(goc.x * (1f + (s - 1f) * 0.4f), goc.y * s, goc.z);   // ngang it hon doc
            for (int i = 0; i < slotVisuals.Length; i++)
                if (slotVisuals[i] != null) slotVisuals[i].localScale = sc;
            yield return null;
        }
        for (int i = 0; i < slotVisuals.Length; i++)
            if (slotVisuals[i] != null) slotVisuals[i].localScale = goc;
        _nayRoutine = null;
    }

    private void EnsurePointsCount(int requiredCount)
    {
        if (cropPoints != null && cropPoints.Length >= requiredCount) return;
        var list = new System.Collections.Generic.List<Transform>();
        if (cropPoints != null) list.AddRange(cropPoints);

        while (list.Count < requiredCount)
        {
            int idx = list.Count + 1;
            GameObject go = new GameObject($"CropPoint_{idx}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            list.Add(go.transform);
        }
        cropPoints = list.ToArray();
        isSetupDone = false;
        EnsureSetup();
    }

    // ── Lưới iso ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Rải n CropPoint thành lưới đều bên trong hình thoi của ô đất hoặc lòng chậu hoa.
    /// </summary>
    public void ApplyLattice(int n)
    {
        if (!useIsoLattice || cropPoints == null || cropPoints.Length == 0) return;
        n = Mathf.Clamp(n, 1, cropPoints.Length);
        if (n == lastLatticeCount) return;

        SpriteRenderer ground = FindGround();
        if (ground == null) { lastLatticeCount = n; return; }

        Bounds  b = ground.bounds;                                  // world-space
        Vector3 c = transform.InverseTransformPoint(new Vector3(b.center.x, b.center.y, 0f));
        Vector3 eR = transform.InverseTransformPoint(new Vector3(b.max.x, b.center.y, 0f)) - c;
        Vector3 eT = transform.InverseTransformPoint(new Vector3(b.center.x, b.max.y, 0f)) - c;

        // KIỂM TRA XEM ĐÂY CÓ PHẢI LÀ CHẬU HOA (FLOWER POT) KHÔNG
        bool isFlowerPot = false;
        var plot = GetComponentInParent<PlotController>();
        if (plot != null && plot.Category == PlotCategory.Flower)
            isFlowerPot = true;
        else if (transform.parent != null && (transform.parent.name.ToLower().Contains("chau") || transform.parent.name.ToLower().Contains("pot") || transform.parent.name.ToLower().Contains("hoa")))
            isFlowerPot = true;
        else if (ground.sprite != null && (ground.sprite.name.ToLower().Contains("chau") || ground.sprite.name.ToLower().Contains("pot") || ground.sprite.name.ToLower().Contains("khungtrongchauhoa")))
            isFlowerPot = true;

        if (isFlowerPot)
        {
            // TÍNH TOÁN RIÊNG CHO CHẬU HOA:
            // Miệng chậu chứa đất đen nằm ở nửa trên sprite chậu: Y_soil = c + 0.42 * eT
            Vector3 potCenter = c + 0.42f * eT + (Vector3)latticeOffset;
            float rx = eR.x * 0.28f; // Bán kính ngang của lòng chậu
            float ry = eT.y * 0.12f; // Bán kính dọc của lòng chậu

            if (n == 1)
            {
                if (cropPoints[0] != null) cropPoints[0].localPosition = potCenter;
            }
            else if (n == 2)
            {
                // 2 hạt giống / bông hoa nằm ngay ngắn, đối xứng chính giữa lòng chậu
                if (cropPoints[0] != null) cropPoints[0].localPosition = potCenter + new Vector3(-rx * 0.55f, 0f, 0f);
                if (cropPoints[1] != null) cropPoints[1].localPosition = potCenter + new Vector3( rx * 0.55f, 0f, 0f);
            }
            else if (n == 3)
            {
                if (cropPoints[0] != null) cropPoints[0].localPosition = potCenter + new Vector3(0f, ry * 0.45f, 0f);
                if (cropPoints[1] != null) cropPoints[1].localPosition = potCenter + new Vector3(-rx * 0.55f, -ry * 0.35f, 0f);
                if (cropPoints[2] != null) cropPoints[2].localPosition = potCenter + new Vector3( rx * 0.55f, -ry * 0.35f, 0f);
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    if (cropPoints[i] == null) continue;
                    float angle = (i / (float)n) * Mathf.PI * 2f;
                    float px = Mathf.Cos(angle) * rx * 0.55f;
                    float py = Mathf.Sin(angle) * ry * 0.55f;
                    cropPoints[i].localPosition = potCenter + new Vector3(px, py, 0f);
                }
            }

            var orderPot = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n; i++) if (cropPoints[i] != null) orderPot.Add(i);
            orderPot.Sort((a, bb) => cropPoints[bb].position.y.CompareTo(cropPoints[a].position.y));
            for (int rank = 0; rank < orderPot.Count; rank++)
            {
                int idx = orderPot[rank];
                if (slotRenderers != null && idx < slotRenderers.Length && slotRenderers[idx] != null)
                    slotRenderers[idx].sortingOrder = sortingOrder + rank;
            }

            lastLatticeCount = n;
            return;
        }

        // Ô RUỘNG NÔNG SẢN BÌNH THƯỜNG (Lưới hình thoi)
        int nc, nr; GetGrid(n, out nc, out nr);

        for (int i = 0; i < n; i++)
        {
            if (cropPoints[i] == null) continue;
            int   col = i % nc, row = i / nc;
            float s = latticeInset * ((col + 0.5f) / nc - 0.5f);
            float tt = latticeInset * ((row + 0.5f) / nr - 0.5f);
            Vector3 pos = c + (s + tt) * eR + (s - tt) * eT + latticeDepthBias * eT;
            cropPoints[i].localPosition = new Vector3(pos.x + latticeOffset.x,
                                                     pos.y + latticeOffset.y, 0f);
        }

        // Sorting theo độ sâu: y world lớn = ở xa = vẽ trước (order nhỏ).
        var order = new System.Collections.Generic.List<int>();
        for (int i = 0; i < n; i++) if (cropPoints[i] != null) order.Add(i);
        order.Sort((a, bb) => cropPoints[bb].position.y.CompareTo(cropPoints[a].position.y));
        for (int rank = 0; rank < order.Count; rank++)
        {
            int idx = order[rank];
            if (slotRenderers != null && idx < slotRenderers.Length && slotRenderers[idx] != null)
                slotRenderers[idx].sortingOrder = sortingOrder + rank;
        }

        lastLatticeCount = n;
    }

    /// <summary>Chia n cây thành lưới cols×rows. Ưu tiên rộng hơn cao cho khớp hình thoi 2:1.</summary>
    private static void GetGrid(int n, out int nc, out int nr)
    {
        switch (n)
        {
            case 1:  nc = 1; nr = 1; return;
            case 2:  nc = 2; nr = 1; return;
            case 3:  nc = 3; nr = 1; return;
            case 4:  nc = 2; nr = 2; return;
            case 5:  nc = 5; nr = 1; return;
            case 6:  nc = 3; nr = 2; return;
            case 8:  nc = 4; nr = 2; return;
            case 9:  nc = 3; nr = 3; return;
            case 10: nc = 5; nr = 2; return;
            case 12: nc = 4; nr = 3; return;
            case 15: nc = 5; nr = 3; return;
            case 16: nc = 4; nr = 4; return;
            case 20: nc = 5; nr = 4; return;
            case 25: nc = 5; nr = 5; return;
        }
        nc = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(n * 1.35f)));
        nr = Mathf.Max(1, Mathf.CeilToInt(n / (float)nc));
    }

    private SpriteRenderer FindGround()
    {
        if (groundRef != null)
        {
            var g0 = groundRef.GetComponent<SpriteRenderer>();
            if (g0 != null) return g0;
        }
        Transform root = transform.parent != null ? transform.parent : transform;
        Transform t = root.Find("GroundSprite");
        if (t != null)
        {
            var g1 = t.GetComponent<SpriteRenderer>();
            if (g1 != null) { groundRef = t; return g1; }
        }
        // Dự phòng: sprite lớn nhất dưới plot mà KHÔNG nằm trong CropGroup
        SpriteRenderer best = null; float bestArea = 0f;
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            if (sr.transform.IsChildOf(transform)) continue;
            float a = sr.bounds.size.x * sr.bounds.size.y;
            if (a > bestArea) { bestArea = a; best = sr; }
        }
        if (best != null) groundRef = best.transform;
        return best;
    }

    [ContextMenu("Xem trước lưới 12 cây")]
    private void PreviewLattice12() { EnsureSetup(); lastLatticeCount = -1; ApplyLattice(12); }

    [ContextMenu("Xem trước lưới 6 cây")]
    private void PreviewLattice6()  { EnsureSetup(); lastLatticeCount = -1; ApplyLattice(6); }

    /// <summary>Cáº­p nháº­t sprite vÃ  scale cho stage hiá»‡n táº¡i (0=Sprout, 1=Growing, 2=Ready).</summary>
    public void UpdateVisual(int stage)
    {
        if (currentCrop == null || slotRenderers == null) return;

        Vector3 targetScale = GetCurrentTargetScale(stage);

        // Bộ 5 stage mới vẽ pivot Bottom-Center → gốc cây nằm ĐÚNG tại CropPoint,
        // không cần đẩy lên. Cây cũ (3 stage) giữ nguyên offset như trước.
        float offsetY = currentCrop.HasStageSet
                      ? 0f
                      : (targetScale.y - currentCrop.GetScale(0).y) * 0.3f;

        for (int i = 0; i < slotRenderers.Length; i++)
        {
            SpriteRenderer sr = slotRenderers[i];
            if (sr == null) continue;
            sr.sprite  = currentCrop.GetSprite(stage);
            sr.enabled = true;

            Transform visual = slotVisuals[i];
            if (visual == null) continue;
            visual.localScale    = targetScale;
            visual.localPosition = new Vector3(0f, offsetY, 0f);
        }
    }

    /// <summary>Táº¯t toÃ n bá»™ visual.</summary>
    private void OnDisable() => DatDiemDau(false);   // [VFX 2026-09-24]

    private void DatDiemDau(bool co)
    {
        if (co == _laDiemDau) return;
        _laDiemDau = co;
        if (co) DiemDauBuom.Add(this); else DiemDauBuom.Remove(this);
    }

    private bool LaHoa(CropData crop)
    {
        if (crop != null && crop.cropCategory == CropCategory.Flower) return true;
        if (_oHoa < 0)
        {
            var plot = GetComponentInParent<PlotController>();
            string cha = transform.parent != null ? transform.parent.name.ToLower() : "";
            _oHoa = (plot != null && plot.Category == PlotCategory.Flower) || cha.Contains("chau") || cha.Contains("pot") || cha.Contains("hoa") ? 1 : 0;
        }
        return _oHoa == 1;
    }

    /// <summary>Diem buom dau: ngon 1 cay ngau nhien dang hien trong khung hinh. Khong co -> false.</summary>
    public bool LayDiemDau(out Vector3 p)
    {
        p = Vector3.zero;
        if (slotRenderers == null || slotRenderers.Length == 0) return false;
        int batDau = Random.Range(0, slotRenderers.Length);
        for (int k = 0; k < slotRenderers.Length; k++)
        {
            var sr = slotRenderers[(batDau + k) % slotRenderers.Length];
            if (sr == null || !sr.enabled || sr.sprite == null || !sr.isVisible) continue;
            Bounds b = sr.bounds;
            p = new Vector3(b.center.x + Random.Range(-0.15f, 0.15f) * b.size.x, b.max.y - b.size.y * 0.2f, 0f);
            return true;
        }
        return false;
    }

    public void ClearAll()
    {
        DatDiemDau(false);   // [VFX 2026-09-24]
        if (_nayRoutine != null) { StopCoroutine(_nayRoutine); _nayRoutine = null; }   // [VFX 2026-09-24]
        EnsureSetup();
        SetReadySwayActive(false);

        // Ô trống bị RefreshVisual() gọi ClearAll() mỗi frame. Đã dọn rồi thì thôi.
        if (_isCleared) return;
        _isCleared      = true;
        _lastShownCrop  = null;
        _lastShownStage = -1;
        _lastShownCount = -1;
        _lastShownReady = false;

        for (int i = 0; i < slotRenderers.Length; i++)
        {
            SpriteRenderer sr = slotRenderers[i];
            if (sr == null) continue;

            sr.enabled = false;
            sr.sprite  = null;

            if (slotVisuals[i] != null)
            {
                slotVisuals[i].localPosition = Vector3.zero;
                slotVisuals[i].localRotation = Quaternion.identity;
                slotVisuals[i].localScale    = Vector3.one;
            }
        }

        for (int i = 0; i < cropPoints.Length; i++)
        {
            if (cropPoints[i] != null)
                cropPoints[i].gameObject.SetActive(true);
        }
    }

    // â”€â”€ Setup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [ContextMenu("Auto Find Points")]
    public void AutoFindPoints()
    {
        var found = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (t.name.StartsWith("CropPoint_"))
                found.Add(t);
        }
        cropPoints = found.ToArray();
    }

    private void EnsureSetup()
    {
        // 2026-08-27 (hiệu năng): trước đây gọi AutoFindPoints() MỖI FRAME cho MỖI plot
        // (ShowCrop ← RefreshVisual ← PlotController.Update) → GetComponentsInChildren +
        // cấp phát List liên tục. Chỉ quét lại khi thật sự chưa có điểm.
        if (!isSetupDone || cropPoints == null || cropPoints.Length == 0)
            AutoFindPoints();

        bool needRebuild = !isSetupDone
                        || slotRenderers == null
                        || slotRenderers.Length != cropPoints.Length;

        if (!needRebuild) return;

        slotRenderers = new SpriteRenderer[cropPoints.Length];
        slotVisuals   = new Transform[cropPoints.Length];
        slotPhase     = new float[cropPoints.Length];

        for (int i = 0; i < cropPoints.Length; i++)
        {
            Transform point = cropPoints[i];
            if (point == null) continue;

            // Äáº£m báº£o CropPoint khÃ´ng cÃ³ scale láº¡ áº£nh hÆ°á»Ÿng child
            point.localScale = Vector3.one;

            // TÃ¬m hoáº·c táº¡o child "Visual"
            Transform visualTf = point.Find("Visual");
            GameObject go;
            if (visualTf != null)
            {
                go = visualTf.gameObject;
            }
            else
            {
                go = new GameObject("Visual");
                go.transform.SetParent(point, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = Vector3.one;
            }

            // Äáº£m báº£o cÃ³ SpriteRenderer â€” dÃ¹ng explicit null check vÃ¬ Unity ?? khÃ´ng reliable
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();

            // Chá»‰ set properties sau khi sr Ä‘Ã£ khÃ´ng null
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder     = sortingOrder;
            sr.sprite           = null;
            sr.color            = Color.white;
            sr.enabled          = false;

            slotRenderers[i] = sr;
            slotVisuals[i]   = go.transform;
            slotPhase[i]     = Random.Range(-swayPhaseRange, swayPhaseRange);
        }

        swayTimer         = Random.Range(0f, 10f);
        isReadySwayActive = false;
        isSetupDone       = true;
        lastLatticeCount  = -1;   // slot vừa dựng lại → lưới phải tính lại
    }


    private void SetReadySwayActive(bool active)
    {
        if (isReadySwayActive == active) return;
        isReadySwayActive = active;

        if (!isReadySwayActive && slotVisuals != null)
            foreach (var v in slotVisuals)
                if (v != null) v.localRotation = Quaternion.identity;
    }
}
