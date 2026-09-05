using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý tiến trình xây dựng nhà 6 giai đoạn (tương tự trồng cây / nuôi thú).
/// Stage 1 (0..33%): Khung sườn mộc
/// Stage 2 (33..66%): Nền móng, tường gạch & giàn giáo
/// Stage 3 (66..100%): Tường hoàn thiện, vì kèo mái
/// Stage 5 (100% / Gem): Hộp quà bọc kín chờ click
/// Stage 6 (Click): Hộp bung nắp + Đại tiệc pháo hoa Lana FX bắn BÙM BÙM liên hồi bay cao
/// Stage 4: Ngôi nhà hoàn thành 100%
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class HouseGrowthController : MonoBehaviour
{
    public enum GrowthState
    {
        Building,      // Đang xây (Stage 1..3)
        ReadyToReveal, // Đã xây xong 100%, thành Hộp Quà (Stage 5) chờ mở
        Completed      // Đã mở hộp quà, hiện nhà hoàn chỉnh (Stage 4)
    }

    [Header("Cấu hình Sprites 6 Giai Đoạn")]
    public Sprite stage1_Frame;
    public Sprite stage2_Foundation;
    public Sprite stage3_HalfBuilt;
    public Sprite stage4_Complete;
    public Sprite stage5_GiftBox;
    public Sprite stage6_BoxOpen;

    [Header("Dữ liệu & Thời gian")]
    public BuildingData data;
    public string houseId = "House_01";
    public float defaultBuildDuration = 60f;

    [Header("Trạng thái hiện tại")]
    [SerializeField] private GrowthState state = GrowthState.Completed;
    [SerializeField] private long startUnix;
    [SerializeField] private float duration = 60f;

    [Header("Hiệu ứng Pháo Hoa")]
    [SerializeField] public GameObject fireworksVfxPrefab;

    private SpriteRenderer _sr;
    private BoxCollider2D _collider;
    private Coroutine _revealCo;
    private float _bobTimer;
    private Vector3 _initialScale = Vector3.one;

    private Vector2 _pressScreenPos;
    private bool _isPressed;
    private bool _initializedAtRuntime;

    public GrowthState State => state;
    public long StartUnix => startUnix;
    public float Duration => duration;
    public string HouseName => data != null && !string.IsNullOrEmpty(data.itemName) ? data.itemName : houseId;

    public float RemainingSeconds
    {
        get
        {
            if (state != GrowthState.Building) return 0f;
            if (startUnix <= 0) return duration;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            float elapsed = Mathf.Max(0f, (float)(now - startUnix));
            return Mathf.Max(0f, duration - elapsed);
        }
    }

    public float Progress
    {
        get
        {
            if (state == GrowthState.Completed || state == GrowthState.ReadyToReveal) return 1f;
            if (duration <= 0.01f) return 1f;
            if (startUnix <= 0) return 0f;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            float elapsed = Mathf.Max(0f, (float)(now - startUnix));
            return Mathf.Clamp01(elapsed / duration);
        }
    }

    public int SpeedUpGemCost
    {
        get
        {
            if (state != GrowthState.Building) return 0;
            float rem = RemainingSeconds;
            if (rem <= 0.5f) return 0;
            // 1 Kim Cương mỗi 20 giây (Tối thiểu 2 Kim Cương)
            return Mathf.Max(2, Mathf.CeilToInt(rem / 20f));
        }
    }

    public string GetSaveKey()
    {
        Vector3 pos = transform.position;
        return $"HouseSave_{houseId}_{Mathf.RoundToInt(pos.x * 10)}_{Mathf.RoundToInt(pos.y * 10)}";
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider2D>();
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = true;
        }

        _initialScale = transform.localScale;

        if (fireworksVfxPrefab == null)
        {
            fireworksVfxPrefab = Resources.Load<GameObject>("VFX/Confetti_blast_multicolor") 
                              ?? Resources.Load<GameObject>("VFX/LevelUp_Confetti_Lana02");
        }
    }

    private void Start()
    {
        // Nếu vừa được spawn từ Shop và gọi Initialize thì không ghi đè
        if (_initializedAtRuntime) return;

        string key = GetSaveKey();
        if (PlayerPrefs.HasKey(key))
        {
            string saved = PlayerPrefs.GetString(key, "");
            if (saved == "Completed")
            {
                state = GrowthState.Completed;
            }
            else if (saved == "ReadyToReveal")
            {
                state = GrowthState.ReadyToReveal;
            }
            else if (saved == "Building")
            {
                state = GrowthState.Building;
                long.TryParse(PlayerPrefs.GetString(key + "_start", "0"), out startUnix);
                duration = PlayerPrefs.GetFloat(key + "_dur", defaultBuildDuration);
            }
        }
        else
        {
            // Nhà có sẵn trong Scene trước đó mặc định là Completed
            state = GrowthState.Completed;
        }

        UpdateVisuals();
    }

    private void Update()
    {
        if (state == GrowthState.Building)
        {
            if (RemainingSeconds <= 0f && startUnix > 0)
            {
                // Hết giờ xây -> Chuyển sang Stage 5 (Hộp quà)
                FinishBuildingNow();
            }
            else
            {
                UpdateVisuals();
            }
        }
        else if (state == GrowthState.ReadyToReveal)
        {
            // Hiệu ứng thở nhẹ của hộp quà để mời gọi click
            _bobTimer += Time.deltaTime * 3.5f;
            float scaleY = 1f + Mathf.Sin(_bobTimer) * 0.04f;
            float scaleX = 1f - Mathf.Sin(_bobTimer) * 0.02f;
            transform.localScale = new Vector3(_initialScale.x * scaleX, _initialScale.y * scaleY, _initialScale.z);
        }

        CheckInputClick();
    }

    private void CheckInputClick()
    {
        if (state == GrowthState.Completed) return;

        // [FIX-HOPQUA 2026-09-02] Đọc input qua TouchInput (Core/TouchInput.cs — helper
        // DÙNG CHUNG, thứ tự Touchscreen → Mouse → Input legacy) thay vì chỉ Input legacy.
        // Đây là world-click DUY NHẤT còn poll Input.GetMouseButton* sau đợt rà soát
        // 2026-08-31 (RA_SOAT_INPUT_MOBILE.md §1.3/§4.2 xếp file này "ưu tiên chuyển sớm"):
        // trên điện thoại Mouse.current = null và mô phỏng chuột từ ngón tay không đáng
        // tin, trong khi plot (FarmPlotInput) đã đọc Input System nên "plot bấm được,
        // hộp quà thì không". Editor/PC: TouchInput rơi về đúng GetMouseButton* cũ.
        if (TouchInput.TapDownThisFrame())
        {
            _isPressed = true;
            _pressScreenPos = TouchInput.PointerScreen();
        }

        if (TouchInput.TapUpThisFrame() && _isPressed)
        {
            _isPressed = false;
            Vector2 releasePos = TouchInput.PointerScreen();

            // [FIX-HOPQUA 2026-09-02] 18f là ngưỡng CỨNG theo pixel — trên màn ~450dpi
            // 18px ≈ 1mm nên tap thật rất dễ bị loại oan (Android touch-slop chuẩn là
            // 8dp ≈ 0.05 inch). Máy cảm ứng: quy theo kích thước vật lý ~3.8mm
            // (0.15 inch, tối thiểu 24px như BoatDockSlot). Editor/PC giữ nguyên 18px.
            float slopPx = (TouchInput.HasTouchscreen && Screen.dpi > 1f)
                ? Mathf.Max(24f, Screen.dpi * 0.15f)
                : 18f;

            if ((releasePos - _pressScreenPos).magnitude <= slopPx)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 worldPoint = cam.ScreenToWorldPoint(releasePos);
                    Vector2 world2D = new Vector2(worldPoint.x, worldPoint.y);

                    bool hit = false;
                    if (_collider != null && _collider.OverlapPoint(world2D))
                        hit = true;
                    // [FIX-HOPQUA 2026-09-02] worldPoint.z = mặt phẳng camera (không phải
                    // z của sprite) nên bounds.Contains(worldPoint) 3D trước đây LUÔN false
                    // — lưới an toàn chết im lặng. Ép z về đúng z của bounds để phép thử
                    // thành 2D thật sự.
                    else if (_sr != null && _sr.bounds.Contains(
                                 new Vector3(world2D.x, world2D.y, _sr.bounds.center.z)))
                        hit = true;

                    if (hit)
                    {
                        HandleClick();
                    }
                }
            }
        }
    }

    public void Initialize(BuildingData bData, string id, float buildDuration)
    {
        _initializedAtRuntime = true;
        data = bData;
        houseId = id;
        duration = buildDuration > 0 ? buildDuration : defaultBuildDuration;
        startUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        state = GrowthState.Building;

        string key = GetSaveKey();
        PlayerPrefs.SetString(key, "Building");
        PlayerPrefs.SetString(key + "_start", startUnix.ToString());
        PlayerPrefs.SetFloat(key + "_dur", duration);
        PlayerPrefs.Save();

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) return;

        switch (state)
        {
            case GrowthState.Building:
                float p = Progress;
                if (p < 0.33f)
                    _sr.sprite = stage1_Frame != null ? stage1_Frame : _sr.sprite;
                else if (p < 0.66f)
                    _sr.sprite = stage2_Foundation != null ? stage2_Foundation : _sr.sprite;
                else
                    _sr.sprite = stage3_HalfBuilt != null ? stage3_HalfBuilt : _sr.sprite;
                break;

            case GrowthState.ReadyToReveal:
                _sr.sprite = stage5_GiftBox != null ? stage5_GiftBox : _sr.sprite;
                break;

            case GrowthState.Completed:
                transform.localScale = _initialScale;
                _sr.sprite = stage4_Complete != null ? stage4_Complete : _sr.sprite;
                break;
        }

        if (_collider != null && _sr.sprite != null)
        {
            _collider.size = _sr.sprite.rect.size / _sr.sprite.pixelsPerUnit;
            _collider.offset = new Vector2(0, _collider.size.y * 0.5f);
        }
    }

    public void HandleClick()
    {
        if (state == GrowthState.Building)
        {
            var cropPopup = CropProcessPopupUI.Instance ?? FindFirstObjectByType<CropProcessPopupUI>(FindObjectsInactive.Include);
            if (cropPopup != null)
            {
                cropPopup.OpenForHouse(this);
            }
        }
        else if (state == GrowthState.ReadyToReveal)
        {
            if (_revealCo == null)
            {
                _revealCo = StartCoroutine(RevealCelebrationRoutine());
            }
        }
    }

    public bool TrySpeedUpWithGem()
    {
        if (state != GrowthState.Building) return false;

        int cost = SpeedUpGemCost;
        if (cost <= 0)
        {
            FinishBuildingNow();
            return true;
        }

        if (FarmEconomyManager.Instance != null)
        {
            if (FarmEconomyManager.Instance.Gems < cost)
            {
                FarmUIManager.Instance?.ShowHint($"Cần {cost} kim cương để tăng tốc.");
                return false;
            }

            if (!FarmEconomyManager.Instance.SpendGems(cost))
            {
                return false;
            }
        }

        FinishBuildingNow();
        return true;
    }

    public void FinishBuildingNow()
    {
        state = GrowthState.ReadyToReveal;
        PlayerPrefs.SetString(GetSaveKey(), "ReadyToReveal");
        PlayerPrefs.Save();

        UpdateVisuals();

        var cropPopup = CropProcessPopupUI.Instance ?? FindFirstObjectByType<CropProcessPopupUI>(FindObjectsInactive.Include);
        if (cropPopup != null && cropPopup.IsOpen)
        {
            cropPopup.ClosePopup();
        }
    }

    private IEnumerator RevealCelebrationRoutine()
    {
        var cropPopup = CropProcessPopupUI.Instance ?? FindFirstObjectByType<CropProcessPopupUI>(FindObjectsInactive.Include);
        if (cropPopup != null && cropPopup.IsOpen)
        {
            cropPopup.ClosePopup();
        }

        // 1. Chuyển sang Stage 6: Hộp quà bung mở nắp
        _sr.sprite = stage6_BoxOpen != null ? stage6_BoxOpen : _sr.sprite;

        // 2. Hiệu ứng nảy bật hộp quà nhấp nhô
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.35f;
            float pop = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f;
            transform.localScale = _initialScale * pop;
            yield return null;
        }

        transform.localScale = _initialScale;

        // 3. Pháo hoa khánh thành 3.5s — [FIX-HOPQUA 2026-09-02] dùng FX chung của studio
        //    (CONTRACT §3): tự đo bounds nhà, tự resolve sorting theo CHÍNH công trình
        //    (layer của sprite có order cao nhất + 100) nên nổi RÕ TRÊN nhà, kích thước
        //    tính bằng world-unit thật (map 1 ô = 100 unit), tự Destroy khi xong.
        //    Chuỗi EpicFireworksSequence cũ spawn prefab demo Lana: chỉ ghi đè sortingOrder
        //    mà KHÔNG đổi sorting LAYER → hạt kẹt ở "Default" (dưới "Objects"/500 của nhà
        //    do PlacementManager.FixBuildingRenderSorting gán) + hạt cỡ đơn vị demo li ti
        //    → người chơi không thấy gì. Giữ code cũ bên dưới làm tài liệu, KHÔNG gọi nữa.
        ConstructionCelebrationFX.Play(transform);

        yield return new WaitForSecondsRealtime(1.0f);

        // 4. Lộ diện Ngôi Nhà Hoàn Thành 100% (Stage 4)
        state = GrowthState.Completed;
        PlayerPrefs.SetString(GetSaveKey(), "Completed");
        PlayerPrefs.Save();

        UpdateVisuals();

        _revealCo = null;
    }

    private float GetRoofTopY()
    {
        if (_sr != null && _sr.sprite != null)
        {
            return _sr.bounds.max.y;
        }
        return transform.position.y + 4.5f;
    }

    private IEnumerator EpicFireworksSequence()
    {
        // Tải toàn bộ kho vũ khí VFX Lana chất lượng cao
        GameObject vfxConfettiBlast = Resources.Load<GameObject>("VFX/Confetti_blast_multicolor") ?? fireworksVfxPrefab;
        GameObject vfxLevelUp = Resources.Load<GameObject>("VFX/LevelUp_Confetti_Lana02") ?? fireworksVfxPrefab;
        GameObject vfxDirectional = Resources.Load<GameObject>("VFX/Confetti_directional_multicolor") ?? vfxConfettiBlast;
        GameObject vfxFlash = Resources.Load<GameObject>("VFX/LevelUp_Flash_Lana03") ?? Resources.Load<GameObject>("VFX/Flash_magic_blue_pink");
        GameObject vfxSparkle = Resources.Load<GameObject>("VFX/Sparkle_ellow") ?? Resources.Load<GameObject>("VFX/Area_star_ellow");

        float topY = GetRoofTopY();
        float posX = transform.position.x;
        float baseY = transform.position.y;

        // ═══════════════════════════════════════════════════════════════════════════
        // ── ĐỢT 1: ĐỒNG LOẠT 5-6 ĐIỂM BẮN NỔ TUNG XUNG QUANH TOÀN BỘ NGÔI NHÀ
        // ═══════════════════════════════════════════════════════════════════════════
        // 1. Điểm 1: Chính giữa Đỉnh Nóc Nhà (Center Roof)
        SpawnSingleBurstWorld(vfxConfettiBlast, new Vector3(posX, topY + 1.2f, -4f), 4.8f, 3.2f, 7.0f);
        if (vfxFlash != null) SpawnSingleBurstWorld(vfxFlash, new Vector3(posX, topY + 0.6f, -4f), 3.5f, 2.0f, 3.0f);

        // 2. Điểm 2: Góc Mái Trái (Top-Left Roof)
        SpawnSingleBurstWorld(vfxDirectional, new Vector3(posX - 2.2f, topY + 0.8f, -4f), 4.2f, 3.0f, 6.5f);

        // 3. Điểm 3: Góc Mái Phải (Top-Right Roof)
        SpawnSingleBurstWorld(vfxDirectional, new Vector3(posX + 2.2f, topY + 0.8f, -4f), 4.2f, 3.0f, 6.5f);

        // 4. Điểm 4: Sân Hông Bên Trái (Left Flank)
        SpawnSingleBurstWorld(vfxLevelUp, new Vector3(posX - 2.8f, baseY + 1.2f, -4f), 4.5f, 3.0f, 7.0f);

        // 5. Điểm 5: Sân Hông Bên Phải (Right Flank)
        SpawnSingleBurstWorld(vfxLevelUp, new Vector3(posX + 2.8f, baseY + 1.2f, -4f), 4.5f, 3.0f, 7.0f);

        // 6. Điểm 6: Sân Trước Thềm Nhà (Front Porch)
        if (vfxSparkle != null) SpawnSingleBurstWorld(vfxSparkle, new Vector3(posX, baseY + 0.5f, -4f), 3.5f, 2.2f, 5.0f);

        yield return new WaitForSecondsRealtime(0.28f);

        // ═══════════════════════════════════════════════════════════════════════════
        // ── ĐỢT 2: ĐẠI BÁC 4 GÓC BẮN VỌT LÊN TRỜI CAO 4.5M - 5.5M
        // ═══════════════════════════════════════════════════════════════════════════
        SpawnSingleBurstWorld(vfxLevelUp, new Vector3(posX - 2.0f, topY + 3.2f, -4f), 4.5f, 3.2f, 6.5f);
        SpawnSingleBurstWorld(vfxLevelUp, new Vector3(posX + 2.0f, topY + 3.2f, -4f), 4.5f, 3.2f, 6.5f);
        SpawnSingleBurstWorld(vfxConfettiBlast, new Vector3(posX, topY + 4.2f, -4f), 5.2f, 3.5f, 7.0f);

        yield return new WaitForSecondsRealtime(0.32f);

        // ═══════════════════════════════════════════════════════════════════════════
        // ── ĐỢT 3: 3 QUẢ ĐẠI PHÁO BÙM BÙM NỔ TUNG TRÊN ĐỈNH TRỜI (+6.5M)
        // ═══════════════════════════════════════════════════════════════════════════
        SpawnSingleBurstWorld(vfxConfettiBlast, new Vector3(posX - 1.8f, topY + 5.5f, -4f), 5.5f, 3.6f, 7.5f);
        SpawnSingleBurstWorld(vfxConfettiBlast, new Vector3(posX + 1.8f, topY + 5.5f, -4f), 5.5f, 3.6f, 7.5f);
        SpawnSingleBurstWorld(vfxConfettiBlast, new Vector3(posX, topY + 6.8f, -4f), 6.0f, 4.0f, 8.0f);

        yield return new WaitForSecondsRealtime(0.38f);

        // ═══════════════════════════════════════════════════════════════════════════
        // ── ĐỢT 4: MƯA PHÁO HOA TỎA SÁNG 360 ĐỘ BAO PHỦ TOÀN BỘ NGÔI NHÀ
        // ═══════════════════════════════════════════════════════════════════════════
        SpawnSingleBurstWorld(vfxLevelUp, new Vector3(posX - 3.2f, topY + 2.0f, -4f), 4.2f, 3.0f, 6.0f);
        SpawnSingleBurstWorld(vfxLevelUp, new Vector3(posX + 3.2f, topY + 2.0f, -4f), 4.2f, 3.0f, 6.0f);
        SpawnSingleBurstWorld(vfxDirectional, new Vector3(posX, topY + 3.0f, -4f), 4.8f, 3.2f, 6.5f);

        yield return new WaitForSecondsRealtime(0.45f);

        // ═══════════════════════════════════════════════════════════════════════════
        // ── ĐỢT 5: HẠ MÀN BÙNG NỔ HOÀNG KIM CỰC ĐẠI
        // ═══════════════════════════════════════════════════════════════════════════
        if (vfxFlash != null) SpawnSingleBurstWorld(vfxFlash, new Vector3(posX, topY + 1.8f, -4f), 4.5f, 2.5f, 3.5f);
        SpawnSingleBurstWorld(vfxConfettiBlast, new Vector3(posX, topY + 2.5f, -4f), 5.5f, 3.5f, 7.0f);
    }

    private void SpawnSingleBurstWorld(GameObject prefab, Vector3 worldPos, float objectScale, float particleSizeMul = 2.2f, float durationSec = 6.0f)
    {
        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, worldPos, Quaternion.identity);
        fx.transform.localScale = Vector3.one * objectScale;

        // Ép Sorting Order cực đại và chỉnh hạt to x2 x3, bay lâu
        // [FIX-HOPQUA 2026-09-02] Phải ép cả sorting LAYER: order 32767 ở layer "Default"
        // vẫn nằm DƯỚI toàn bộ layer "Objects"/"ObjectsFront"/"Foreground" (bài học
        // CHAN_DOAN_PHAOHOA_2026-09-01 + pattern LevelUpPopupUI.cs:794-800).
        ParticleSystemRenderer[] psrs = fx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        string fxLayer = TouristSortingLayers.Resolve(TouristSortingLayers.Overlay);
        foreach (var psr in psrs)
        {
            psr.sortingLayerName = fxLayer;
            psr.sortingOrder = 32767;
        }

        ParticleSystem[] pss = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            var main = ps.main;
            main.startSizeMultiplier *= particleSizeMul;
            main.startLifetimeMultiplier *= 1.8f;
            main.startSpeedMultiplier *= 1.5f;
            main.gravityModifierMultiplier = 0.30f; // Rơi cực kỳ chậm lãng mạn
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ps.Play();
        }

        Destroy(fx, durationSec);
    }
}
