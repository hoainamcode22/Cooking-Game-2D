using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BÀY HÀNG LÊN MẶT QUẦY (B2) — nhìn từ ngoài map là biết đang bán gì, KHÔNG cần mở popup.
///
/// Chi tiết này nhỏ nhưng là thứ làm cái quầy "sống": một cái quầy trống trơn chỉ là
/// một nút bấm có hình; một cái quầy bày ba món hàng là một cửa tiệm đang buôn bán.
/// Nó cũng nhắc người chơi rằng hàng đang chờ bán — không cần mở popup mới nhớ ra.
///
/// Các ô bày hàng là <see cref="SpriteRenderer"/> DỰNG SẴN trong prefab rồi bật/tắt.
/// Không sinh GameObject lúc chạy: quầy nằm ngay giữa map, mỗi lần đăng/huỷ hàng mà
/// sinh-huỷ object là một nhịp giật hình ngay trước mắt người chơi.
/// </summary>
public class StallCounterDisplay : MonoBehaviour
{
    [Header("Các ô bày hàng trên mặt quầy (dựng sẵn trong prefab)")]
    [SerializeField] private List<SpriteRenderer> displaySlots = new List<SpriteRenderer>();

    [Header("Chỗ chờ art")]
    [Tooltip("Hiện khi quầy đang trống — biển 'chưa bán gì'. Có thể để trống.")]
    [SerializeField] private GameObject emptySign;

    private void OnEnable()
    {
        if (PlayerStallManager.Instance != null)
            PlayerStallManager.Instance.OnStallChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerStallManager.Instance != null)
            PlayerStallManager.Instance.OnStallChanged -= Refresh;
    }

    private void Start()
    {
        // Manager có thể Awake SAU component này (thứ tự Awake giữa các object không
        // bảo đảm), khi đó OnEnable đã bỏ lỡ lần đăng ký. Thử lại ở Start là đủ.
        if (PlayerStallManager.Instance != null)
        {
            PlayerStallManager.Instance.OnStallChanged -= Refresh;   // tránh đăng ký hai lần
            PlayerStallManager.Instance.OnStallChanged += Refresh;
        }

        Refresh();
    }

    public void Refresh()
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        StallItemCatalog   catalog = StallItemCatalog.Instance;

        int shown = 0;

        if (stall != null && catalog != null)
        {
            IReadOnlyList<PlayerListing> active = stall.GetActiveListings();

            for (int i = 0; i < displaySlots.Count; i++)
            {
                SpriteRenderer sr = displaySlots[i];
                if (sr == null) continue;

                if (i >= active.Count)
                {
                    sr.enabled = false;
                    continue;
                }

                Sprite icon = catalog.GetIcon(active[i].itemId);

                // Không có icon thì TẮT hẳn ô thay vì hiện ô vuông trắng — mặt quầy
                // nằm giữa map, một ô trắng ở đó trông như lỗi hiển thị.
                sr.sprite  = icon;
                sr.enabled = icon != null;
                if (icon != null) shown++;
            }
        }
        else
        {
            for (int i = 0; i < displaySlots.Count; i++)
            {
                if (displaySlots[i] != null) displaySlots[i].enabled = false;
            }
        }

        if (emptySign != null) emptySign.SetActive(shown == 0);
    }
}
