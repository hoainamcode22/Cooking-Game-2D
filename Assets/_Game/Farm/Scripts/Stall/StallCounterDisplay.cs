using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BÀY HÀNG LÊN MẶT QUẦY (B2) — MẶC ĐỊNH TẮT.
///
/// Ý tưởng ban đầu: nhìn từ ngoài map là biết quầy đang bán gì, khỏi mở popup. Thực tế
/// chạy lên thì hỏng — icon vật phẩm là ảnh vẽ cho ô UI 68px, đặt vào world nó phủ kín
/// cả mái quầy (một miếng thịt to bằng cái nhà). Muốn làm cho đẹp thì phải vẽ riêng một
/// bộ sprite "hàng bày trên kệ" theo đúng góc nghiêng isometric của quầy, chứ không dùng
/// lại được icon kho.
///
/// Nên component vẫn còn nguyên nhưng <see cref="hienHangTrenQuay"/> mặc định `false`:
/// công trình ngoài map sạch sẽ đúng như art vẽ, hàng hoá chỉ hiện trong popup. Ai vẽ
/// xong bộ sprite riêng thì bật cờ lên là dùng lại được toàn bộ logic dưới đây.
///
/// Các ô bày hàng là <see cref="SpriteRenderer"/> DỰNG SẴN trong prefab rồi bật/tắt.
/// Không sinh GameObject lúc chạy: quầy nằm ngay giữa map, mỗi lần đăng/huỷ hàng mà
/// sinh-huỷ object là một nhịp giật hình ngay trước mắt người chơi.
/// </summary>
public class StallCounterDisplay : MonoBehaviour
{
    [Header("Bật/tắt bày hàng ngoài map")]
    [Tooltip("TẮT (mặc định): quầy ngoài map chỉ hiện art công trình, không vẽ icon hàng " +
             "lên trên. Chỉ bật khi đã có bộ sprite hàng hoá vẽ riêng cho mặt quầy — dùng " +
             "lại icon kho thì ảnh to phủ kín cả công trình.")]
    [SerializeField] private bool hienHangTrenQuay = false;

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
        // Tắt hẳn: dọn sạch mọi ô rồi thoát. Đặt ở đầu hàm chứ không chặn ở chỗ gọi, để
        // dù ai đó bật cờ lúc đang chạy rồi tắt lại thì mặt quầy vẫn về đúng trạng thái.
        if (!hienHangTrenQuay)
        {
            TatHetOBayHang();
            if (emptySign != null) emptySign.SetActive(false);
            return;
        }

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
            TatHetOBayHang();
        }

        if (emptySign != null) emptySign.SetActive(shown == 0);
    }

    private void TatHetOBayHang()
    {
        for (int i = 0; i < displaySlots.Count; i++)
            if (displaySlots[i] != null) displaySlots[i].enabled = false;
    }
}
