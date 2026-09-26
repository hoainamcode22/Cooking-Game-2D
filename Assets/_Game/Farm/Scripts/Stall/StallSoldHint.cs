// ============================================================================
//  StallSoldHint — BAO "khach da mua hang o quay" (2026-09-25)
//  Nghe PlayerStallManager.OnListingSold: hien 1 dong chu noi len giua man hinh roi mo dan (LockedHintFX),
//  vd "Grandma Rose just bought your 45 Corn!  +765". Ten khach + cau chu doi ngau nhien cho do nham chan.
//  Tu khoi dong o moi scene co PlayerStallManager. Khong can keo tha, khong sua scene.
// ============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StallSoldHint : MonoBehaviour
{
    private static StallSoldHint _inst;

    // Ten khach (hien nguyen van, khong dich)
    private static readonly string[] KHACH =
    {
        "Grandma Rose", "Farmer Tom", "Little Mia", "Chef Hana", "Uncle Bao", "Aunt Linh",
        "Oliver", "Rosie", "Grandpa Nam", "Lily", "Baker Leo", "Miss Daisy",
    };

    // Cau mau tieng Viet (khoa Loc) - {0} khach, {1} so luong, {2} ten hang
    private static readonly string[] CAU =
    {
        "{0} vừa mua {1} {2} của bạn!",
        "{1} {2} của bạn đã về tay {0}!",
        "{0} rất ưng {2} của bạn, mua luôn {1}!",
        "Ting ting! {0} đã mua {1} {2}!",
    };

    private PlayerStallManager _ql;
    private int _cauCu = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuKhoiDong()
    {
        SceneManager.sceneLoaded -= KhiNap;
        SceneManager.sceneLoaded += KhiNap;
        Tao();
    }

    private static void KhiNap(Scene s, LoadSceneMode m) => Tao();

    private static void Tao()
    {
        if (_inst != null) return;
        var go = new GameObject("[StallSoldHint]");
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<StallSoldHint>();
    }

    private IEnumerator Start()
    {
        while (true)
        {
            var ql = PlayerStallManager.Instance;
            if (ql != _ql)
            {
                if (_ql != null) _ql.OnListingSold -= KhiBan;
                _ql = ql;
                if (_ql != null) _ql.OnListingSold += KhiBan;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private void OnDestroy()
    {
        if (_ql != null) _ql.OnListingSold -= KhiBan;
    }

    private void KhiBan(PlayerListing l, int vang)
    {
        if (l == null) return;
        string ten = l.itemId;
        var cat = StallItemCatalog.Instance;
        if (cat != null) { var t = cat.GetDisplayName(l.itemId); if (!string.IsNullOrEmpty(t)) ten = t; }
        ten = Loc.T(ten);

        string khach = KHACH[Random.Range(0, KHACH.Length)];
        int k = Random.Range(0, CAU.Length);
        if (k == _cauCu) k = (k + 1) % CAU.Length;
        _cauCu = k;

        string chu = Loc.TF(CAU[k], "<color=#FFE08A>" + khach + "</color>", Mathf.Max(1, l.quantity), ten);
        if (vang > 0) chu += "   <color=#FFD54A>+" + vang.ToString("N0") + "</color>";
        LockedHintFX.Show(chu, new Vector2(Screen.width * 0.5f, Screen.height * 0.62f));
        AudioManager.Instance?.PlayCoinTing();
    }
}
