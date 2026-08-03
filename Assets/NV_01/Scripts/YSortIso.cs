using UnityEngine;

/// <summary>
/// Y-sort: vật ở DƯỚI (y nhỏ hơn) vẽ ĐÈ lên vật ở trên → nhân vật che/đứng sau công trình đúng.
/// sortingOrder = round(-y * 100). Gắn lên root nhân vật (và dùng chung công thức cho công trình).
/// </summary>
public class YSortIso : MonoBehaviour
{
    [Tooltip("Để trống = tự lấy SpriteRenderer ở chính object/đời con.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Hệ số nhân Y → order. LƯU Ý: Order in Layer giới hạn ±32767. Map toạ độ lớn (vài nghìn) " +
             "thì để ~1 (không thì order tràn/kẹp sai). Map nhỏ mới để 100.")]
    [SerializeField] private float sortScale = 1f;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * sortScale);
    }
}
