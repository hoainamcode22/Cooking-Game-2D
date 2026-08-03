using UnityEngine;

/// <summary>
/// Đánh dấu một object là "chỉ để nhìn trong Editor".
/// Object hiện bình thường trong Scene view để bạn biết chỗ đặt art,
/// nhưng tự tắt ngay khi vào Play → người chơi không bao giờ thấy.
///
/// Dùng cho các khung gợi ý (placeholder) do tool sinh ra.
/// </summary>
[DisallowMultipleComponent]
public class EditorOnlyHint : MonoBehaviour
{
    [Tooltip("Tắt object khi vào Play Mode. Bỏ tick nếu muốn giữ để debug.")]
    public bool hideOnPlay = true;

    private void Awake()
    {
        if (hideOnPlay) gameObject.SetActive(false);
    }
}
