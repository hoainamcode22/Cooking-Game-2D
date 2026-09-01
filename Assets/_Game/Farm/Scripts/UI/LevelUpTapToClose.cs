using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [V2] "CHẠM BẤT KỲ ĐÂU ĐỂ ĐÓNG" cho popup Lên Cấp.
///
/// CƠ CHẾ:
///   • Component nằm trên 1 Image TRONG SUỐT full-screen, raycastTarget = true,
///     đặt ở sibling index THẤP trong popup → nút "Nhận Quà" và mọi UI khác vẫn
///     đứng TRÊN nên bấm nút vẫn ăn bình thường; chỉ vùng "nền trống" mới rơi vào đây.
///   • Nhận tap qua <see cref="IPointerClickHandler"/> — đi qua EventSystem của
///     New Input System (InputSystemUIInputModule), KHÔNG polling Input.
///   • Có <see cref="minOpenDelay"/>: sau khi <see cref="Arm"/> ít nhất ~0.8s mới
///     nhận tap — tránh người chơi lỡ tay tắt oan popup ngay khi nó vừa bung ra.
///   • Chỉ bắn callback 1 LẦN mỗi lần Arm (chống double-tap nhận quà 2 lần).
///
/// LevelUpPopupUI gọi <c>Arm(ClaimAndClose)</c> khi mở popup và <c>Disarm()</c> khi đóng.
/// </summary>
public class LevelUpTapToClose : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Số giây tối thiểu sau khi popup mở mới nhận tap (chống tắt oan).")]
    [SerializeField] private float minOpenDelay = 0.8f;

    private Action _onTap;
    private float  _armedAt = -1f;   // Time.unscaledTime lúc Arm; < 0 = chưa vũ trang
    private bool   _fired;

    private void Awake()
    {
        // Bảo đảm có Graphic để nhận raycast. Image màu clear = tàng hình nhưng
        // vẫn chặn/bắt raycast theo RECT (UGUI không kiểm tra alpha khi raycast).
        var g = GetComponent<Graphic>();
        if (g == null)
        {
            var img = gameObject.AddComponent<Image>();
            img.color = Color.clear;
            g = img;
        }
        g.raycastTarget = true;
    }

    /// <summary>
    /// Vũ trang bộ bắt tap: bắt đầu đếm <see cref="minOpenDelay"/> từ BÂY GIỜ.
    /// Gọi mỗi lần popup mở (kể cả popup thứ 2, 3 trong hàng đợi lên nhiều cấp liền).
    /// </summary>
    public void Arm(Action onTap)
    {
        _onTap   = onTap;
        _armedAt = Time.unscaledTime;
        _fired   = false;
    }

    /// <summary>Tắt bộ bắt tap (popup đang đóng / không dùng tap-to-close).</summary>
    public void Disarm()
    {
        _onTap   = null;
        _armedAt = -1f;
        _fired   = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_fired || _onTap == null || _armedAt < 0f) return;

        // Chưa đủ delay tối thiểu → nuốt tap, coi như chưa bấm gì.
        if (Time.unscaledTime - _armedAt < Mathf.Max(0f, minOpenDelay)) return;

        _fired = true;             // khoá TRƯỚC khi invoke — chống re-entry
        Action cb = _onTap;
        cb?.Invoke();
    }
}
