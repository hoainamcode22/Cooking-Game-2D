using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// [Decor5] ĐIỂM VÀO DUY NHẤT của hệ 5 stage. PlacementManager chỉ gọi đúng 1 dòng:
/// <c>DecorGrowthBootstrap.OnDecorPlaced(spawnedObj, currentItem);</c>
///
/// Mọi thứ khác (nạp config, cấp slotIndex, gắn controller, khôi phục sau khi load scene,
/// định tuyến click cho toàn bộ decor) nằm hết trong file này.
///
/// FEATURE FLAG (CONTRACT §9): config chưa có hoặc enabled == false ⇒ mọi hook return
/// ở DÒNG ĐẦU. Không AddComponent, không PlayerPrefs, không sinh GameObject, và
/// ĐẶC BIỆT không subscribe SceneManager.sceneLoaded (QA vừa bịt đúng lỗi rò rỉ này
/// ở ShipperManager — hook scene phải nằm SAU chốt flag).
/// </summary>
public static class DecorGrowthBootstrap
{
    /// <summary>Đường dẫn Resources của asset cấu hình: Assets/_Game/Resources/DecorGrowthConfig.asset</summary>
    public const string ConfigResourcePath = "DecorGrowthConfig";

    private const string ActiveListKey = "DecorGrowActive";
    private const string SlotCounterPrefix = "DecorGrowSlot_";
    private const float ClickMoveTolerancePixels = 18f;
    private const long StaleEntrySeconds = 7L * 24L * 3600L;   // dọn entry mồ côi sau 7 ngày

    // ── Config (lazy, cache cả trạng thái "đã thử và không có") ───────────────
    private static DecorGrowthConfig _config;
    private static bool _configProbed;

    /// <summary>
    /// Cấu hình dùng chung. Load MỘT lần rồi cache; nếu không tìm thấy asset thì cũng
    /// cache luôn kết quả null để không Resources.Load lại mỗi lần đặt vật.
    /// </summary>
    public static DecorGrowthConfig Config
    {
        get
        {
            if (!_configProbed)
            {
                _configProbed = true;
                _config = Resources.Load<DecorGrowthConfig>(ConfigResourcePath);
                if (_config == null)
                    Debug.Log("[Decor5] Không tìm thấy Resources/DecorGrowthConfig → hệ 5 stage TẮT (đúng mặc định an toàn).");
            }
            return _config;
        }
    }

    /// <summary>Hệ đã bật hay chưa — dùng làm chốt đầu tiên ở mọi điểm vào.</summary>
    public static bool IsEnabled
    {
        get
        {
            DecorGrowthConfig cfg = Config;
            return cfg != null && cfg.enabled;
        }
    }

    // ── Event có REPLAY cho subscriber muộn (QA R3) ───────────────────────────
    private static System.Action<DecorGrowthController> _spawnedHandlers;

    /// <summary>
    /// DEV-B nghe event này để gắn thợ búa. 
    ///
    /// ⚠ QA R3: thứ tự giữa hai <c>[RuntimeInitializeOnLoadMethod]</c> KHÔNG XÁC ĐỊNH.
    /// Nếu <see cref="RestoreAll"/> chạy TRƯỚC <c>HouseWorkerBridge.AutoBoot()</c> thì các
    /// controller đã khôi phục sẽ bắn event khi chưa ai subscribe ⇒ decor không bao giờ có thợ.
    /// Vì vậy accessor <c>add</c> REPLAY ngay toàn bộ controller đang sống cho subscriber mới:
    /// DEV-B không phải sửa dòng nào, và mọi subscriber dù muộn cỡ nào vẫn nhận đủ.
    /// Mỗi handler được bọc try/catch riêng để 1 handler lỗi không chặn cả chuỗi.
    /// </summary>
    public static event System.Action<DecorGrowthController> OnControllerSpawned
    {
        add
        {
            if (value == null) return;
            _spawnedHandlers += value;

            // Replay: chỉ những controller còn việc (chưa Completed) mới cần thợ.
            for (int i = 0; i < _live.Count; i++)
            {
                DecorGrowthController c = _live[i];
                if (c == null) continue;
                if (c.State == DecorGrowthController.DecorState.Completed) continue;
                try { value(c); }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Decor5] OnControllerSpawned replay handler lỗi: {e}");
                }
            }
        }
        remove { _spawnedHandlers -= value; }
    }

    private static void RaiseSpawned(DecorGrowthController c)
    {
        if (c == null || _spawnedHandlers == null) return;

        System.Delegate[] list = _spawnedHandlers.GetInvocationList();
        for (int i = 0; i < list.Length; i++)
        {
            var h = list[i] as System.Action<DecorGrowthController>;
            if (h == null) continue;
            try { h(c); }
            catch (System.Exception e)
            {
                Debug.LogError($"[Decor5] OnControllerSpawned handler lỗi: {e}");
            }
        }
    }

    // ── Chống lùi giờ máy (CONTRACT §4) + cache 1 lần/frame (QA A7) ───────────
    private static long _maxSeenUnix;
    private static float _anchorRealtime;
    private static long _anchorUnix;

    private static int _nowFrame = -1;
    private static long _nowCached;

    /// <summary>
    /// Đồng hồ DUY NHẤT của hệ 5 stage. Không ai được đọc DateTimeOffset.UtcNow trực tiếp.
    /// Lấy max(giờ hệ thống, giờ suy từ realtimeSinceStartup, mốc lớn nhất từng thấy)
    /// ⇒ vặn giờ máy về quá khứ KHÔNG làm remaining nhảy lùi.
    ///
    /// QA A7: kết quả được CACHE theo Time.frameCount. Progress + RemainingSeconds của
    /// mỗi decor đều gọi hàm này, 30 decor sẽ là ~90 syscall/frame; giờ chỉ còn ĐÚNG 1.
    /// </summary>
    public static long NowUnix()
    {
        int frame = Time.frameCount;
        if (_nowFrame == frame) return _nowCached;
        _nowFrame = frame;
        _nowCached = ComputeNowUnix();
        return _nowCached;
    }

    private static long ComputeNowUnix()
    {
        long os = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (_anchorUnix == 0) { _anchorUnix = os; _anchorRealtime = Time.realtimeSinceStartup; }
        long session = _anchorUnix + (long)(Time.realtimeSinceStartup - _anchorRealtime);
        long best = os > session ? os : session;
        if (best < _maxSeenUnix) best = _maxSeenUnix; else _maxSeenUnix = best;
        return best;
    }

    // ── Save key ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Key PlayerPrefs gốc — MỘT nguồn duy nhất cho cả controller và bootstrap.
    /// KHÔNG chứa toạ độ ⇒ di chuyển / xoay vật vẫn giữ nguyên tiến độ (CONTRACT §7).
    /// 3 sub-key: "" = state string · "_start" = long unix · "_dur" = float giây.
    /// </summary>
    public static string SaveKeyFor(int itemID, int slotIndex) => $"DecorGrow_{itemID}_{slotIndex}";

    // ── Slot index ───────────────────────────────────────────────────────────

    /// <summary>
    /// Cấp số thứ tự lần đặt của cùng một itemID và lưu bền vào
    /// PlayerPrefs "DecorGrowSlot_{itemID}".
    /// </summary>
    public static int AllocateSlotIndex(int itemID)
    {
        string k = SlotCounterPrefix + itemID;
        int next = PlayerPrefs.GetInt(k, 0);
        PlayerPrefs.SetInt(k, next + 1);
        PlayerPrefs.Save();
        return next;
    }

    /// <summary>
    /// Xoá sạch save của một slot: 3 sub-key + entry trong sổ theo dõi.
    /// Gọi khi vật hoàn thiện (Completed) hoặc khi người chơi bán / phá vật.
    ///
    /// QA A4: bộ đếm được TRẢ LẠI nếu slot vừa xoá đúng là slot cuối cùng đã cấp
    /// ("pop nếu là đỉnh"). Nhờ vậy vòng mua → xây xong → mua tiếp KHÔNG làm
    /// DecorGrowSlot_{id} tăng vĩnh viễn. Nếu slot ở giữa (vật khác vẫn đang xây)
    /// thì CỐ Ý không giảm — giảm sẽ khiến 2 vật tranh nhau một key.
    /// </summary>
    public static void ReleaseSlotIndex(int itemID, int slotIndex)
    {
        if (slotIndex < 0) return;

        string key = SaveKeyFor(itemID, slotIndex);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(key + "_start");
        PlayerPrefs.DeleteKey(key + "_dur");

        string counterKey = SlotCounterPrefix + itemID;
        int counter = PlayerPrefs.GetInt(counterKey, 0);
        if (counter == slotIndex + 1)
        {
            if (slotIndex == 0) PlayerPrefs.DeleteKey(counterKey);
            else PlayerPrefs.SetInt(counterKey, slotIndex);
        }

        RemoveActive(itemID, slotIndex);
        PlayerPrefs.Save();
    }

    // ── Hook từ PlacementManager ─────────────────────────────────────────────

    /// <summary>
    /// Gắn hệ xây cho vật vừa đặt xuống world. An toàn khi gọi với bất kỳ item nào:
    /// thiếu config / flag tắt / là nhà / là ô đất / bị loại trừ / thời gian xây 0
    /// ⇒ return, không làm gì.
    ///
    /// Bộ art hợp lệ ⇒ FULL 5-STAGE. Không có art (chuồng / máy) ⇒ WORKER-ONLY
    /// (giữ nguyên sprite, chỉ thêm timer + thợ + popup — QUYẾT ĐỊNH LEAD #1).
    /// </summary>
    public static void OnDecorPlaced(GameObject spawned, PlaceableItemData data)
    {
        DecorGrowthConfig cfg = Config;
        if (cfg == null || !cfg.enabled) return;                 // FEATURE FLAG — dòng đầu (§9)
        if (spawned == null || data == null) return;

        if (!cfg.ShouldApply(data, spawned)) return;

        // [FIX CS1503] BaseItemData.itemID là string; hệ Decor5 dùng int -> đổi MỘT LẦN ở đây.
        int itemIdInt = DecorGrowthConfig.ItemIdOf(data);

        DecorStageSet set = cfg.FindSet(itemIdInt);
        bool workerOnly = set == null || !set.IsValid;
        if (workerOnly) set = null;                              // chuẩn hoá: WORKER-ONLY = set null

        // FULL 5-STAGE cần SpriteRenderer để đổi art. WORKER-ONLY thì không đụng sprite nên không cần.
        if (!workerOnly && spawned.GetComponentInChildren<SpriteRenderer>(true) == null)
        {
            Debug.LogWarning($"[Decor5] '{spawned.name}' không có SpriteRenderer → bỏ qua hệ 5 stage.");
            return;
        }

        // CỬA AN TOÀN: 0 giây ⇒ tuyệt đối không gắn controller.
        float seconds = cfg.ResolveBuildSeconds(data);
        if (seconds <= 0.5f) return;

        DecorGrowthController ctrl = spawned.GetComponent<DecorGrowthController>();
        if (ctrl == null) ctrl = spawned.AddComponent<DecorGrowthController>();

        int slot = AllocateSlotIndex(itemIdInt);

        // [FIX TEN — Lead 2026-09-01] NGUON SU THAT cua ten la BaseItemData.itemName,
        // vi do la thu NGUOI CHOI THAY trong shop. stageSet.displayName chi la nhan
        // tien tay dat trong tool -> de troi (vd itemID 9: asset itemName="Heo Vui Ve"
        // nhung file asset ten "Meo vui ve", tool dat displayName="Meo Vui Ve" => popup
        // hien khac shop). Uu tien itemName truoc, displayName chi la du phong.
        string name = !string.IsNullOrEmpty(data.itemName)
            ? data.itemName
            : ((set != null && !string.IsNullOrEmpty(set.displayName)) ? set.displayName : spawned.name);

        ctrl.Initialize(cfg, set, itemIdInt, name, seconds, slot);

        AddActive(itemIdInt, slot, spawned.name, spawned.transform.position);
        EnsureRouter();

        Debug.Log($"[Decor5] Bắt đầu xây '{name}' (itemID=\"{data.itemID}\"->{itemIdInt}, slot={slot}, "
                + $"{(workerOnly ? "WORKER-ONLY" : "FULL 5-STAGE")}) trong {seconds:0}s.");

        RaiseSpawned(ctrl);
    }

    // ── Khôi phục sau khi load scene ─────────────────────────────────────────

    /// <summary>
    /// Điểm vào tự động. CHỈ dựng runtime object khi flag đã bật — nhờ vậy hook
    /// SceneManager.sceneLoaded cũng nằm sau chốt flag (không rò rỉ khi hệ tắt).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        if (!IsEnabled) return;                                  // FEATURE FLAG — dòng đầu (§9)
        EnsureRouter();
    }

    /// <summary>
    /// Khôi phục các vật đang xây / đang là hộp quà.
    ///
    /// ⚠ QA R1: KHÔNG được gọi thẳng từ <c>[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]</c>.
    /// Thứ tự Unity thật là <b>Awake → AfterSceneLoad → Start</b>, nhưng decor lại do
    /// <c>PlacementManager.Start() → LoadBuildings()</c> Instantiate lại ⇒ lúc AfterSceneLoad
    /// quét scene thì decor CHƯA TỒN TẠI, restore trượt sạch và key thành rác.
    /// Vì vậy Decor5Runtime chờ <b>2 frame</b> (yield return null x2) rồi mới gọi hàm này,
    /// và gọi lại mỗi lần <c>SceneManager.sceneLoaded</c>.
    ///
    /// Hàm này IDEMPOTENT: gọi 2 lần không gắn 2 controller lên cùng một object.
    ///
    /// Cách tìm lại object: PlacementManager tự Instantiate lại vật từ save của nó, nhưng
    /// nó KHÔNG gọi hook này (ta chỉ được thêm 1 dòng ở đường ĐẶT MỚI). Vì vậy bootstrap
    /// giữ sổ riêng "DecorGrowActive" gồm itemID | slotIndex | tên object | toạ độ lúc đặt.
    /// Toạ độ ở đây CHỈ để phân biệt nhiều vật cùng tên — nó KHÔNG nằm trong save key,
    /// nên người chơi di chuyển vật vẫn không mất tiến độ.
    /// </summary>
    public static void RestoreAll()
    {
        DecorGrowthConfig cfg = Config;
        if (cfg == null || !cfg.enabled) return;                 // FEATURE FLAG — dòng đầu (§9)

        List<ActiveEntry> entries = ReadActive();
        if (entries.Count == 0) return;

        EnsureRouter();

        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        bool listDirty = false;
        long now = NowUnix();

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            ActiveEntry e = entries[i];
            string key = SaveKeyFor(e.itemID, e.slotIndex);

            // IDEMPOTENT: slot này đã có controller đang sống → bỏ qua, không gắn cái thứ 2.
            if (FindLive(e.itemID, e.slotIndex) != null) continue;

            // Key đã bị xoá (vật hoàn thiện) → dọn sổ.
            if (!PlayerPrefs.HasKey(key))
            {
                entries.RemoveAt(i);
                listDirty = true;
                continue;
            }

            // set có thể null → controller tự vào WORKER-ONLY. KHÔNG loại entry vì thiếu art,
            // nếu không thì chuồng/máy sẽ mất tiến độ sau mỗi lần load scene.
            DecorStageSet set = cfg.FindSet(e.itemID);
            if (set != null && !set.IsValid) set = null;

            Transform target = FindBestTarget(all, e, set != null);
            if (target == null)
            {
                // Chưa thấy object (scene khác, hoặc người chơi đã phá vật).
                // Chỉ dọn khi entry đã quá cũ để không xoá oan lúc đang ở scene khác.
                long start;
                long.TryParse(PlayerPrefs.GetString(key + "_start", "0"), out start);
                if (start > 0 && now - start > StaleEntrySeconds)
                {
                    ReleaseSlotIndex(e.itemID, e.slotIndex);
                    entries.RemoveAt(i);
                    listDirty = true;
                }
                continue;
            }

            DecorGrowthController ctrl = target.GetComponent<DecorGrowthController>();
            if (ctrl == null) ctrl = target.gameObject.AddComponent<DecorGrowthController>();

            string name = (set != null && !string.IsNullOrEmpty(set.displayName)) ? set.displayName : target.name;
            ctrl.RestoreFromSave(cfg, set, e.itemID, name, e.slotIndex);

            RaiseSpawned(ctrl);
        }

        if (listDirty) WriteActive(entries);
    }

    private static DecorGrowthController FindLive(int itemID, int slotIndex)
    {
        for (int i = 0; i < _live.Count; i++)
        {
            DecorGrowthController c = _live[i];
            if (c == null) continue;
            if (c.ItemID == itemID && c.SlotIndex == slotIndex) return c;
        }
        return null;
    }

    private static Transform FindBestTarget(Transform[] all, ActiveEntry e, bool requireRenderer)
    {
        Transform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t.name != e.objectName) continue;
            if (t.GetComponent<DecorGrowthController>() != null) continue;   // đã có chủ
            if (requireRenderer && t.GetComponentInChildren<SpriteRenderer>(true) == null) continue;

            float d = (t.position - new Vector3(e.x, e.y, t.position.z)).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }

    // ── Sổ theo dõi vật đang xây ─────────────────────────────────────────────

    private struct ActiveEntry
    {
        public int itemID;
        public int slotIndex;
        public string objectName;
        public float x;
        public float y;
    }

    private static List<ActiveEntry> ReadActive()
    {
        var list = new List<ActiveEntry>();
        string raw = PlayerPrefs.GetString(ActiveListKey, "");
        if (string.IsNullOrEmpty(raw)) return list;

        string[] rows = raw.Split(';');
        for (int i = 0; i < rows.Length; i++)
        {
            if (string.IsNullOrEmpty(rows[i])) continue;
            string[] f = rows[i].Split('|');
            if (f.Length < 5) continue;

            ActiveEntry e = new ActiveEntry();
            if (!int.TryParse(f[0], out e.itemID)) continue;
            if (!int.TryParse(f[1], out e.slotIndex)) continue;
            e.objectName = f[2];
            float.TryParse(f[3], System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out e.x);
            float.TryParse(f[4], System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out e.y);
            list.Add(e);
        }
        return list;
    }

    private static void WriteActive(List<ActiveEntry> list)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            ActiveEntry e = list[i];
            if (sb.Length > 0) sb.Append(';');
            sb.Append(e.itemID).Append('|')
              .Append(e.slotIndex).Append('|')
              .Append(e.objectName == null ? "" : e.objectName.Replace('|', '_').Replace(';', '_')).Append('|')
              .Append(e.x.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|')
              .Append(e.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (sb.Length == 0) PlayerPrefs.DeleteKey(ActiveListKey);
        else PlayerPrefs.SetString(ActiveListKey, sb.ToString());
        PlayerPrefs.Save();
    }

    private static void AddActive(int itemID, int slotIndex, string objectName, Vector3 pos)
    {
        List<ActiveEntry> list = ReadActive();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].itemID == itemID && list[i].slotIndex == slotIndex) { list.RemoveAt(i); break; }
        }
        list.Add(new ActiveEntry { itemID = itemID, slotIndex = slotIndex, objectName = objectName, x = pos.x, y = pos.y });
        WriteActive(list);
    }

    private static void RemoveActive(int itemID, int slotIndex)
    {
        List<ActiveEntry> list = ReadActive();
        bool changed = false;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].itemID == itemID && list[i].slotIndex == slotIndex) { list.RemoveAt(i); changed = true; }
        }
        if (changed) WriteActive(list);
    }

    /// <summary>
    /// Giữ lại cho tương thích: controller vào Completed đã tự gọi
    /// <see cref="ReleaseSlotIndex"/> (xoá cả 3 sub-key). Hàm này chỉ dọn sổ theo dõi.
    /// </summary>
    public static void NotifyCompleted(int itemID, int slotIndex)
    {
        if (slotIndex < 0) return;
        RemoveActive(itemID, slotIndex);
    }

    // ── Sổ controller đang sống + runtime object ─────────────────────────────

    private static readonly List<DecorGrowthController> _live = new List<DecorGrowthController>();
    private static Decor5Runtime _runtime;

    /// <summary>Danh sách controller đang bật (chỉ đọc — dùng cho DEV-B / debug).</summary>
    public static IReadOnlyList<DecorGrowthController> Live => _live;

    internal static void RegisterLive(DecorGrowthController c)
    {
        if (c == null || _live.Contains(c)) return;
        _live.Add(c);
        EnsureRouter();
    }

    internal static void UnregisterLive(DecorGrowthController c)
    {
        if (c == null) return;
        _live.Remove(c);
    }

    /// <summary>
    /// Sinh runtime object dùng chung (router click + bộ khôi phục) nếu chưa có.
    /// Chỉ chạy khi flag đã bật. Tên giữ nguyên "EnsureRouter" để DEV-B/DEV-D không phải sửa.
    /// </summary>
    public static void EnsureRouter()
    {
        if (!IsEnabled) return;
        if (_runtime != null) return;

        var go = new GameObject("[Decor5] Runtime");
        go.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(go);
        _runtime = go.AddComponent<Decor5Runtime>();
    }

    /// <summary>
    /// RUNTIME DÙNG CHUNG — nested private class NGAY TRONG class Bootstrap.
    /// Gánh 2 việc: (1) router click cho toàn bộ decor, (2) hẹn giờ khôi phục sau load scene.
    ///
    /// VÌ SAO nested mà không là file riêng: CONTRACT §7 chốt "1 class / 1 file, không ngoại lệ",
    /// và §10 chốt DEV-A chỉ được tạo đúng 5 file. Nested class vẫn thoả cả hai vì file này
    /// chỉ có MỘT top-level type (DecorGrowthBootstrap); nó cũng truy cập trực tiếp được
    /// sổ _live private mà không phải mở thêm API public nào.
    ///
    /// VÌ SAO cần router: HouseGrowthController poll Input.GetMouseButtonDown trong Update()
    /// của TỪNG instance — 30 công trình là 30 lần đọc input mỗi frame, và cái nào xử lý
    /// trước thì thắng (bug cũ). Router đọc input MỘT LẦN mỗi frame cho toàn bộ decor,
    /// rồi chỉ gọi HandleClick() của vật gần camera nhất. Nó cũng là fallback cho mobile
    /// touch — OnMouseUpAsButton của Unity không chạy với touch trên New Input System.
    /// </summary>
    private class Decor5Runtime : MonoBehaviour
    {
        private Vector2 _pressPos;
        private bool _pressed;
        private Coroutine _restoreCo;

        private void OnEnable()
        {
            // Hook scene nằm ở ĐÂY (bên trong object chỉ được sinh khi flag bật)
            // nên không bao giờ rò rỉ lúc hệ tắt — QA đã bắt lỗi này ở ShipperManager.
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ScheduleRestore();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // QA A5: trước đây restore chỉ chạy 1 lần cả phiên → đổi scene là mất hệ xây.
            ScheduleRestore();
        }

        private void ScheduleRestore()
        {
            if (_restoreCo != null) StopCoroutine(_restoreCo);
            _restoreCo = StartCoroutine(DeferredRestore());
        }

        /// <summary>
        /// Chờ 2 frame để chắc chắn chạy SAU mọi Start() — kể cả
        /// PlacementManager.Start() → LoadBuildings() Instantiate lại decor (QA R1).
        /// </summary>
        private IEnumerator DeferredRestore()
        {
            yield return null;
            yield return null;
            _restoreCo = null;
            RestoreAll();
        }

        private void Update()
        {
            if (_live.Count == 0) return;

            // QA A1: popup tiến độ đang mở ⇒ KHÔNG nhận bất kỳ click world nào.
            // (Lớp chặn full-screen trong DecorProgressPopupBridge lo phần UI.)
            if (DecorProgressPopupBridge.IsOpen) { _pressed = false; return; }

            Vector2 releasePos;
            if (!PollRelease(out releasePos)) return;

            // Tap chứ không phải kéo map.
            float slopPx = (TouchInput.HasTouchscreen && Screen.dpi > 1f)
                ? Mathf.Max(24f, Screen.dpi * 0.15f) : ClickMoveTolerancePixels;
            if ((releasePos - _pressPos).magnitude > slopPx)
            {
#if UNITY_EDITOR
                Debug.Log($"[Decor5] Tap bị loại vì kéo {(releasePos - _pressPos).magnitude:0}px > {slopPx:0}px (đang pan map?).");
#endif
                return;
            }

            if (EditModeManager.IsEditMode) return;

            // [FIX-CLICK-2026-09-03] Không cướp click của UI — nhưng kiểm CHÍNH XÁC.
            // Bản cũ dùng IsPointerOverGameObject(): trên máy Sếp nó trả true cả trên
            // world trống (cùng nguyên nhân từng giết click hộp quà NHÀ — mọi hệ trong
            // project dùng TouchInput/Input System đều sống, riêng 2 chỗ dùng chốt này
            // thì chết). Thay bằng RaycastAll: chỉ coi là "trên UI" khi phần tử trúng
            // THẬT SỰ là nút bấm (Selectable) — panel/catcher tàng hình không chặn nữa.
            if (LaTrenNutUI(releasePos)) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(releasePos.x, releasePos.y, 0f));

            DecorGrowthController best = null;
            float bestDist = float.MaxValue;
            Vector3 camPos = cam.transform.position;

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                DecorGrowthController c = _live[i];
                if (c == null) { _live.RemoveAt(i); continue; }
                if (!c.CanAcceptClick()) continue;
                if (!c.ContainsWorldPoint(world)) continue;

                float d = (c.transform.position - camPos).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }

            if (best != null) best.HandleClick();
        }

        /// <summary>
        /// Đọc chuột VÀ touch, trả true đúng lúc nhả. Giữ _pressPos để đo khoảng di chuyển.
        /// </summary>
        private bool PollRelease(out Vector2 releasePos)
        {
            releasePos = Vector2.zero;

            if (Input.touchSupported && Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    _pressPos = t.position;
                    _pressed = true;
                }
                else if (t.phase == TouchPhase.Ended && _pressed)
                {
                    _pressed = false;
                    releasePos = t.position;
                    return true;
                }
                else if (t.phase == TouchPhase.Canceled)
                {
                    _pressed = false;
                }
                return false;
            }

            // [FIX-CLICK-2026-09-03] Nhánh chuột đi qua TouchInput (helper chuẩn studio,
            // hệ nhà đã dùng và click sống) thay vì poll Input.GetMouseButton* thô.
            if (TouchInput.TapDownThisFrame())
            {
                _pressPos = TouchInput.PointerScreen();
                _pressed = true;
            }
            else if (TouchInput.TapUpThisFrame() && _pressed)
            {
                _pressed = false;
                releasePos = TouchInput.PointerScreen();
                return true;
            }
            return false;
        }

        // ─── [FIX-CLICK-2026-09-03] các mảnh hỗ trợ ────────────────────────────

        private static readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>
            _uiHits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(16);

        /// <summary>
        /// true CHỈ KHI con trỏ đang nằm trên phần tử UI thật sự tương tác được
        /// (Selectable: Button/Toggle/Slider... ở chính nó hoặc cha). Ảnh trang trí,
        /// catcher tàng hình, panel nền... KHÔNG chặn click world.
        /// </summary>
        private static bool LaTrenNutUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;

            var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = screenPos };
            _uiHits.Clear();
            es.RaycastAll(ped, _uiHits);

            for (int i = 0; i < _uiHits.Count; i++)
            {
                GameObject go = _uiHits[i].gameObject;
                if (go == null) continue;
                if (go.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                {
#if UNITY_EDITOR
                    Debug.Log($"[Decor5] Click bị UI nuốt bởi nút: {go.name} (đúng thiết kế).");
#endif
                    return true;
                }
            }
            return false;
        }
    }
}
