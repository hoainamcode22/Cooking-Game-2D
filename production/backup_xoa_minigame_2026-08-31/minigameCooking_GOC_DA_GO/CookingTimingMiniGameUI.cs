using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minigame canh thời gian khi nấu.
///
/// ── SỬA CHO MOBILE (game phát hành Android/iOS) ─────────────────────────────
/// TRƯỚC: chỉ dừng được thanh bằng phím Space (stopKey / Keyboard.current) và trong
/// cả file KHÔNG có Button nào ⇒ trên điện thoại người chơi KHÔNG THỂ dừng thanh
/// ⇒ không nấu được món. Đây là lỗi CHẶN GAMEPLAY.
///
/// NAY: <b>chạm bất kỳ đâu trên màn hình = nhấn Space</b>.
///   • Vùng tap là một Image trong suốt phủ kín màn hình + Button, TỰ DỰNG lúc chạy
///     (không phải kéo tay trong scene) trên đúng Canvas của minigame, và được đặt
///     lên trên cùng khi minigame bật. Vùng này chỉ sống lúc minigame chạy.
///   • Có thêm đường dự phòng đọc chạm toàn cục (TouchInput) cho trường hợp không
///     tìm được Canvas để dựng vùng tap — nhường lại nếu ngón đang chạm đúng một
///     Button thật (không cướp nút Close/Pause nếu sau này thêm).
///   • Bàn phím GIỮ NGUYÊN song song: Sếp là dev, cần test bằng bàn phím/chuột.
///   • Có ARMING 0.15s: cú chạm vừa mở minigame (bấm nút "Nấu") không bị tính là
///     cú chạm dừng thanh.
/// Logic nấu / tính thành-bại / callback KHÔNG đổi một dòng nào.
/// </summary>
public class CookingTimingMiniGameUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject miniGameRoot;
    [SerializeField] private RectTransform barBackground;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private RectTransform movingMarker;

    [Header("Easy Settings")]
    [SerializeField] private float easyMarkerSpeed = 350f;
    [SerializeField] private float easyZoneSpeed = 150f;

    [Header("Normal Settings")]
    [SerializeField] private float normalMarkerSpeed = 500f;
    [SerializeField] private float normalZoneSpeed = 250f;

    [Header("Hard Settings")]
    [SerializeField] private float hardMarkerSpeed = 700f;
    [SerializeField] private float hardZoneSpeed = 400f;

    [Header("Input")]
    [SerializeField] private KeyCode stopKey = KeyCode.Space;

    [Header("Input — Mobile (chạm để dừng)")]
    [Tooltip("Bật vùng tap phủ màn hình: chạm đâu cũng dừng thanh. TẮT = chỉ còn bàn phím (đừng tắt ở bản mobile).")]
    [SerializeField] private bool choPhepChamDeDung = true;

    [Tooltip("Vùng tap có sẵn trong scene (tuỳ chọn). Để TRỐNG thì script tự dựng lúc chạy.")]
    [SerializeField] private Button vungTapCoSan;

    [Tooltip("Dòng chữ gợi ý \"Chạm để dừng!\". Để TRỐNG thì script tự dựng lúc chạy.")]
    [SerializeField] private TMP_Text txtGoiYCham;

    [Tooltip("Giây bỏ qua chạm ngay sau khi minigame mở — chống cú chạm mở minigame bị tính là chạm dừng.")]
    [SerializeField] private float giayBoQuaChamDau = 0.15f;

    [Header("Time Limit")]
    [SerializeField] private float miniGameDuration = 5f;

    private float remainingTime;

    private bool isPlaying;
    private bool hasStopped;

    private float markerDirection = 1f;
    private float zoneDirection = -1f;

    private float currentMarkerSpeed;
    private float currentZoneSpeed;

    private Action<bool> onMiniGameFinished;

    [Header("Interaction Blocker")]
    [SerializeField] private GameObject interactionBlocker;
    [SerializeField] private TMP_Text txtTimeRemaining;

    // ── Mobile runtime ──────────────────────────────────────────────────────
    private Button _vungTap;          // vùng tap đang dùng (có sẵn hoặc tự dựng)
    private bool   _daDungVungTap;    // đã thử dựng vùng tap chưa (chỉ thử 1 lần)
    private bool   _yeuCauDungTuTap;  // cờ "người chơi vừa chạm" → Update xử lý
    private float  _thoiDiemChoNhanTap; // trước mốc này thì bỏ qua mọi cú chạm

    private void Start()
    {
        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (hasStopped) return;

        MoveMarker();
        MoveSuccessZone();

        if (IsStopKeyPressed() || DaChamDeDung())
        {
            StopMiniGame();
            return;
        }

        remainingTime -= Time.deltaTime;
        UpdateTimeRemainingUI();

        if (remainingTime <= 0f)
        {
            TimeoutMiniGame();
            return;
        }
    }
    public void StartMiniGame(DishDifficulty difficulty, Action<bool> callback)
    {

        onMiniGameFinished = callback;

        ApplyDifficulty(difficulty);

        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(true);
            interactionBlocker.transform.SetAsLastSibling();
        }

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(true);
            miniGameRoot.transform.SetAsLastSibling();
        }

        isPlaying = true;
        hasStopped = false;

        remainingTime = miniGameDuration;
        UpdateTimeRemainingUI();

        markerDirection = 1f;
        zoneDirection = -1f;

        ResetPositions();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // ── Mobile: bật vùng tap + chữ gợi ý, đặt mốc arming ────────────────
        _yeuCauDungTuTap    = false;
        _thoiDiemChoNhanTap = Time.unscaledTime + Mathf.Max(0f, giayBoQuaChamDau);
        BatVungTap(true);
        BatChuGoiY(true);
    }

    private void ApplyDifficulty(DishDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DishDifficulty.Easy:
                currentMarkerSpeed = easyMarkerSpeed;
                currentZoneSpeed = easyZoneSpeed;
                break;

            case DishDifficulty.Normal:
                currentMarkerSpeed = normalMarkerSpeed;
                currentZoneSpeed = normalZoneSpeed;
                break;

            case DishDifficulty.Hard:
                currentMarkerSpeed = hardMarkerSpeed;
                currentZoneSpeed = hardZoneSpeed;
                break;

            default:
                currentMarkerSpeed = normalMarkerSpeed;
                currentZoneSpeed = normalZoneSpeed;
                break;
        }
    }

    private void MoveMarker()
    {
        MoveRectInsideBar(movingMarker, ref markerDirection, currentMarkerSpeed);
        if (movingMarker != null)
        {
            float scaleX = 1f + 0.15f * Mathf.Sin(Time.time * 25f);
            float scaleY = 1f - 0.15f * Mathf.Sin(Time.time * 25f);
            movingMarker.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }

    private void MoveSuccessZone()
    {
        MoveRectInsideBar(successZone, ref zoneDirection, currentZoneSpeed);
        if (successZone != null)
        {
            var img = successZone.GetComponent<UnityEngine.UI.Image>();
            if(img != null) img.color = Color.Lerp(Color.white, Color.green, (Mathf.Sin(Time.time * 10f) + 1f) / 2f);
        }
    }

    private void MoveRectInsideBar(RectTransform target, ref float direction, float speed)
    {
        if (barBackground == null || target == null) return;

        float barHalfWidth = barBackground.rect.width / 2f;
        float targetHalfWidth = target.rect.width / 2f;

        float leftLimit = -barHalfWidth + targetHalfWidth;
        float rightLimit = barHalfWidth - targetHalfWidth;

        Vector2 pos = target.anchoredPosition;
        pos.x += direction * speed * Time.deltaTime;

        if (pos.x >= rightLimit)
        {
            pos.x = rightLimit;
            direction = -1f;
        }
        else if (pos.x <= leftLimit)
        {
            pos.x = leftLimit;
            direction = 1f;
        }

        target.anchoredPosition = pos;
    }

    private void ResetPositions()
    {
        if (barBackground == null || movingMarker == null || successZone == null) return;

        float barHalfWidth = barBackground.rect.width / 2f;

        float markerHalfWidth = movingMarker.rect.width / 2f;
        float zoneHalfWidth = successZone.rect.width / 2f;

        Vector2 markerPos = movingMarker.anchoredPosition;
        markerPos.x = -barHalfWidth + markerHalfWidth;
        movingMarker.anchoredPosition = markerPos;

        Vector2 zonePos = successZone.anchoredPosition;
        zonePos.x = barHalfWidth - zoneHalfWidth;
        successZone.anchoredPosition = zonePos;
    }

    private void StopMiniGame()
    {
        bool isSuccess = IsMarkerInsideSuccessZone();


        FinishMiniGame(isSuccess);
    }

    private bool IsMarkerInsideSuccessZone()
    {
        if (movingMarker == null || successZone == null) return false;

        float markerX = movingMarker.anchoredPosition.x;

        float zoneCenterX = successZone.anchoredPosition.x;
        float zoneHalfWidth = successZone.rect.width / 2f;

        float zoneMinX = zoneCenterX - zoneHalfWidth;
        float zoneMaxX = zoneCenterX + zoneHalfWidth;


        return markerX >= zoneMinX && markerX <= zoneMaxX;
    }
    private bool IsStopKeyPressed()
    {
        bool pressed = false;

    #if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(stopKey))
        {
            pressed = true;
        }
    #endif

    #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            pressed = true;
        }
    #endif

        return pressed;
    }

    // =========================================================================
    //  MOBILE — chạm để dừng
    // =========================================================================

    /// <summary>
    /// Người chơi đã chạm để dừng? Hai đường:
    ///   1. Cờ từ vùng tap (Button.onClick → <see cref="OnTapStop"/>) — đường CHÍNH.
    ///   2. Đọc chạm toàn cục — chỉ dùng khi KHÔNG dựng được vùng tap (không tìm được
    ///      Canvas). Nhường lại nếu ngón đang ở trên một Button thật, để không cướp
    ///      nút Close/Pause nếu sau này panel có thêm nút.
    /// Cả hai đường đều tôn trọng mốc arming (bỏ qua cú chạm vừa mở minigame).
    /// </summary>
    private bool DaChamDeDung()
    {
        if (!choPhepChamDeDung) return false;
        if (Time.unscaledTime < _thoiDiemChoNhanTap) { _yeuCauDungTuTap = false; return false; }

        if (_yeuCauDungTuTap)
        {
            _yeuCauDungTuTap = false;
            return true;
        }

        // Đường dự phòng: chỉ khi không có vùng tap
        if (_vungTap != null) return false;
        if (!TouchInput.TapDownThisFrame()) return false;
        if (NgonDangTrenNut()) return false;

        return true;
    }

    /// <summary>
    /// API public để wire tay: kéo hàm này vào onClick của một nút/vùng tap trong
    /// scene cũng dừng được thanh (ngoài vùng tap tự dựng).
    /// </summary>
    public void OnTapStop()
    {
        if (!isPlaying || hasStopped) return;
        _yeuCauDungTuTap = true;
    }

    /// <summary>Ngón/chuột đang nằm trên một Button thật (không phải vùng tap của mình)?</summary>
    private bool NgonDangTrenNut()
    {
        if (EventSystem.current == null) return false;

        var data = new PointerEventData(EventSystem.current) { position = TouchInput.PointerScreen() };
        var ketQua = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(data, ketQua);

        for (int i = 0; i < ketQua.Count; i++)
        {
            var go = ketQua[i].gameObject;
            if (go == null) continue;
            if (_vungTap != null && go == _vungTap.gameObject) continue;
            if (go.GetComponentInParent<Button>() != null) return true;
        }
        return false;
    }

    /// <summary>
    /// Bật/tắt vùng tap. Lần đầu bật thì tự dựng: Image trong suốt phủ KÍN canvas +
    /// Button, đặt lên trên cùng để nhận chạm ở mọi chỗ. Alpha 0.004 (gần như vô
    /// hình nhưng vẫn nhận raycast — alpha 0 tuyệt đối vẫn nhận, để 0.004 cho chắc
    /// với mọi phiên bản UI).
    /// </summary>
    private void BatVungTap(bool bat)
    {
        if (!choPhepChamDeDung)
        {
            if (_vungTap != null) _vungTap.gameObject.SetActive(false);
            return;
        }

        if (_vungTap == null && !_daDungVungTap)
        {
            _daDungVungTap = true;

            if (vungTapCoSan != null)
            {
                _vungTap = vungTapCoSan;
            }
            else
            {
                Canvas canvas = LayCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning("[CookingTiming] Không tìm được Canvas để dựng vùng tap — " +
                                     "dùng đường đọc chạm toàn cục thay thế (vẫn chạm được để dừng).");
                }
                else
                {
                    var go = new GameObject("TapZone_ChamDeDung", typeof(RectTransform));
                    go.transform.SetParent(canvas.transform, false);

                    var rt = (RectTransform)go.transform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var img = go.AddComponent<Image>();
                    img.color         = new Color(0f, 0f, 0f, 0.004f);
                    img.raycastTarget = true;

                    _vungTap = go.AddComponent<Button>();
                    _vungTap.transition = Selectable.Transition.None; // không nhấp nháy màu
                    _vungTap.onClick.AddListener(OnTapStop);
                }
            }

            if (_vungTap != null)
                _vungTap.onClick.AddListener(OnTapStop); // vùng có sẵn cũng phải nối
        }

        if (_vungTap == null) return;

        _vungTap.gameObject.SetActive(bat);
        if (bat)
        {
            // Trên cùng: minigameRoot vừa SetAsLastSibling, vùng tap phải nằm trên nó
            // (trong suốt nên không che gì) để cú chạm ở mọi chỗ đều tới được.
            _vungTap.transform.SetAsLastSibling();
        }
    }

    /// <summary>Canvas để gắn vùng tap: ưu tiên canvas của chính panel minigame.</summary>
    private Canvas LayCanvas()
    {
        if (miniGameRoot != null)
        {
            var c = miniGameRoot.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
        }

        var self = GetComponentInParent<Canvas>();
        if (self != null) return self.rootCanvas != null ? self.rootCanvas : self;

        return null;
    }

    /// <summary>
    /// Chữ gợi ý "Chạm để dừng!" — dùng ô chữ Sếp gán, không có thì tự dựng dưới
    /// panel minigame (đáy panel, không đè lên thanh timing).
    /// </summary>
    private void BatChuGoiY(bool bat)
    {
        if (txtGoiYCham == null && bat && miniGameRoot != null)
        {
            var go = new GameObject("Txt_GoiYCham", typeof(RectTransform));
            go.transform.SetParent(miniGameRoot.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(700f, 60f);
            rt.anchoredPosition = new Vector2(0f, -14f); // ngay dưới đáy panel

            txtGoiYCham = go.AddComponent<TextMeshProUGUI>();
            txtGoiYCham.fontSize      = 40f;
            txtGoiYCham.alignment     = TextAlignmentOptions.Center;
            txtGoiYCham.color         = new Color(1f, 0.95f, 0.75f);
            txtGoiYCham.raycastTarget = false; // không được nuốt chạm của vùng tap
        }

        if (txtGoiYCham == null) return;

        txtGoiYCham.text = bat ? "Chạm để dừng!" : string.Empty;
        txtGoiYCham.gameObject.SetActive(bat);
    }

    private void TimeoutMiniGame()
    {
        FinishMiniGame(false);
    }
    private void FinishMiniGame(bool isSuccess)
    {
        if (hasStopped) return;

        hasStopped = true;
        isPlaying = false;

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(false);
        }

        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(false);
        }
        if (txtTimeRemaining != null)
        {
            txtTimeRemaining.text = "";
        }

        // Mobile: tắt vùng tap + chữ gợi ý, không để chắn màn hình sau khi xong
        _yeuCauDungTuTap = false;
        BatVungTap(false);
        BatChuGoiY(false);

        Action<bool> callback = onMiniGameFinished;
        onMiniGameFinished = null;

        callback?.Invoke(isSuccess);
    }
    private void UpdateTimeRemainingUI()
    {
        if (txtTimeRemaining == null) return;

        float time = Mathf.Max(0f, remainingTime);
        txtTimeRemaining.text = Mathf.CeilToInt(time)+"s";
    }

    /// <summary>Object bị tắt giữa lúc chơi → không để vùng tap trong suốt nằm lại chắn màn hình.</summary>
    private void OnDisable()
    {
        if (_vungTap != null) _vungTap.gameObject.SetActive(false);
    }
}
