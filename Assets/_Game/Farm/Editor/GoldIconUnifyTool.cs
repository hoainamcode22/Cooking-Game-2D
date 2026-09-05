// ============================================================================
// GoldIconUnifyTool.cs — DEV-D (tools-programmer) · 2026-09-03
// ----------------------------------------------------------------------------
// ĐỒNG NHẤT ICON VÀNG toàn game về icon chuẩn đang hiện trên HUD:
//   Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/vang-removebg-preview.png
//   guid   : a1c4be4bd781bd74399a37785962ed71
//   fileID : -846414766330871110
//
// 3 menu (Tools/Farm Game/Đồng nhất icon vàng/):
//   1. ★ DRY-RUN (chỉ liệt kê)  — quét scene ĐANG MỞ + mọi prefab + mọi .asset,
//      liệt kê mọi reference sprite trỏ tới 1 trong các icon vàng KHÔNG chuẩn.
//      Không đổi gì. In cả danh sách BỎ QUA + lý do + cảnh báo lệch tỉ lệ.
//   2. ★ APPLY (đổi thật)       — hộp xác nhận → ghi SỔ HOÀN TÁC JSON trước →
//      đổi reference sang sprite chuẩn + bật preserveAspect cho Image bị đổi.
//   3. Hoàn tác (đọc sổ JSON)   — trả từng reference về sprite cũ như trong sổ.
//
// Sổ hoàn tác: production/backup_round2_2026-09-02/goldicon_undo.json
//   (idempotent: chạy APPLY nhiều lần chỉ ghi thêm entry CHƯA có, không ghi đè
//    giá trị cũ đã lưu — nên hoàn tác luôn về đúng trạng thái đầu tiên).
//
// KHÔNG đụng: khungvang (khung), ribbon_banner_gold (ruy băng),
// shop_btn_buy_gold (nền nút), seed_marigold (hạt giống) — các sprite đó không
// nằm trong danh sách guid vàng nên tự động an toàn.
// KHÔNG sửa file .cs runtime nào — chỗ code hardcode icon nằm trong mục
// "CẦN SẾP QUYẾT" của production/KIEM_KE_ICON_VANG_2026-09-02.md.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoldIconUnifyTool
{
    // ------------------------------------------------------------------ hằng
    private const string MENU_ROOT = "Tools/Farm Game/Đồng nhất icon vàng/";
    private const string MENU_DRY  = MENU_ROOT + "★ DRY-RUN (chỉ liệt kê)";
    private const string MENU_APP  = MENU_ROOT + "★ APPLY (đổi thật — có xác nhận)";
    private const string MENU_UNDO = MENU_ROOT + "Hoàn tác (đọc sổ JSON)";
    private const string LOG = "[GoldIconUnify] ";

    private const string STD_PATH   = "Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/vang-removebg-preview.png";
    private const string STD_GUID   = "a1c4be4bd781bd74399a37785962ed71";
    private const long   STD_FILEID = -846414766330871110;

    private const string BACKUP_DIR = "production/backup_round2_2026-09-02";
    private const string UNDO_JSON  = BACKUP_DIR + "/goldicon_undo.json";
    private const string DRY_REPORT = BACKUP_DIR + "/goldicon_dryrun_report.txt";

    // 10 icon vàng KHÔNG chuẩn (guid -> đường dẫn, chỉ để in report)
    private static readonly Dictionary<string, string> NonStandardGold = new Dictionary<string, string>
    {
        { "1bc0450c206a85041b4605f91a538eae", "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/Icon_vang.png" },
        { "c8ec785e216604c448c06f09d41783cf", "Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/assets/Icon_vang.png" },
        { "42a2dff6568d00d498fa5bbf8ba3806d", "Assets/Anh/vang-removebg-preview.png" },
        { "015d7635fcc25b647836f890db7588b9", "Assets/Art/UI/Currency/icon_gold.png" },
        { "4ea8eea6e91225d4990a2f7ac96013f0", "Assets/Assetsgame/Icon_vang.png" },
        { "00ac2a6851d9dae41b0f60b44a65dd99", "Assets/Export_Kitchen_UI_Package/Sprites/icon_gold.png" },
        { "4b7a4f32c89f01743a3c43623de9a649", "Assets/maptitle/AssetsTitl/Sprites/UI/Sprite_coin_icon.png" },
        { "32c171b6a57d3a340948eebf653906ea", "Assets/_Game/Farm/Art/UI_OrderBoard/ob_coin.png" },
        { "36487624dc88e1e408f76f6a00052a14", "Assets/_Game/Farm/Art/UI_Stall/stall_icon_coin.png" },
        { "5fc89d3ffb639594baf21f4584e3ac95", "Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/ShopPopup/assets/Icon_vang.png" },
    };

    // Asset bị BỎ QUA (CẦN SẾP QUYẾT) — tool không đổi, chỉ liệt kê
    private static readonly Dictionary<string, string> SkipAssetPaths = new Dictionary<string, string>
    {
        { "Assets/maptitle/AssetsTitl/Tiles/UI/Sprite_coin_icon.asset",
          "Tile TILEMAP world-art màn title (không phải UI Image) — CẦN SẾP QUYẾT" },
    };

    // Component bị BỎ QUA theo tên class (CẦN SẾP QUYẾT)
    private static readonly Dictionary<string, string> SkipComponentTypes = new Dictionary<string, string>
    {
        { "KitchenSceneV2UI",
          "Skin UI minigame bếp (bộ Export_Kitchen_UI_Package đồng bộ art riêng) — CẦN SẾP QUYẾT" },
    };

    // ------------------------------------------------------------- kiểu dữ liệu
    private class Finding
    {
        public string kind;              // SCENE / PREFAB / ASSET
        public string assetPath;         // đường dẫn file .unity/.prefab/.asset
        public string objectPath;        // hierarchy hoặc "(asset) Tên"
        public string componentType;
        public int componentIndex;
        public string propertyPath;      // vd "m_Sprite", "iconGold", "sprites.Array.data[2]"
        public string oldGuid;
        public long oldFileID;
        public string oldSpritePath;
        public string skipReason;        // null => SẼ ĐỔI
        public string aspectWarn;        // null => OK
        public bool isImage;
        public bool preserveAspectWas;
        public UnityEngine.Object liveTarget; // component/SO đang load (scene + asset)

        public string Key
        {
            get { return assetPath + "|" + objectPath + "|" + componentType + "#" + componentIndex + "|" + propertyPath; }
        }
    }

    [Serializable]
    private class UndoEntry
    {
        public string kind;
        public string assetPath;
        public string objectPath;
        public string componentType;
        public int componentIndex;
        public string propertyPath;
        public string oldGuid;
        public long oldFileID;
        public string oldSpritePath;
        public bool wasImage;
        public bool preserveAspectWas;
    }

    [Serializable]
    private class UndoBook
    {
        public string standardGuid = STD_GUID;
        public long standardFileID = STD_FILEID;
        public string standardPath = STD_PATH;
        public List<UndoEntry> entries = new List<UndoEntry>();
    }

    // ================================================================ MENU 1
    [MenuItem(MENU_DRY, false, 10)]
    private static void MenuDryRun()
    {
        Sprite std = LoadStandardSprite(true);
        List<Finding> findings = ScanAll(std);
        string report = BuildReport(findings, std, true);
        LogChunks(report);
        TryWriteTextFile(ProjectPath(DRY_REPORT), report);
        int change = CountChange(findings);
        EditorUtility.DisplayDialog("DRY-RUN — Đồng nhất icon vàng",
            "Tìm thấy " + findings.Count + " reference icon vàng KHÔNG chuẩn.\n" +
            "SẼ ĐỔI: " + change + "  ·  BỎ QUA (cần Sếp quyết): " + (findings.Count - change) + "\n\n" +
            "Chi tiết: cửa sổ Console + file\n" + DRY_REPORT + "\n\nCHƯA đổi gì cả.", "OK");
    }

    // ================================================================ MENU 2
    [MenuItem(MENU_APP, false, 11)]
    private static void MenuApply()
    {
        Sprite std = LoadStandardSprite(false);
        if (std == null)
        {
            EditorUtility.DisplayDialog("APPLY bị từ chối",
                "Không load được sprite chuẩn:\n" + STD_PATH, "Đóng");
            return;
        }

        List<Finding> findings = ScanAll(std);
        List<Finding> toChange = new List<Finding>();
        foreach (Finding f in findings) if (f.skipReason == null) toChange.Add(f);

        if (toChange.Count == 0)
        {
            EditorUtility.DisplayDialog("APPLY", "Không còn reference nào cần đổi. Mọi thứ đã chuẩn.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("APPLY — Đồng nhất icon vàng",
            "Sẽ đổi " + toChange.Count + " reference sang sprite chuẩn:\n" +
            "vang-removebg-preview (icon HUD)\n\n" +
            "• Image bị đổi sẽ được bật preserveAspect = true.\n" +
            "• Sổ hoàn tác ghi TRƯỚC khi đổi:\n  " + UNDO_JSON + "\n\n" +
            "Nên chạy DRY-RUN đọc kỹ trước. Đổi thật bây giờ?",
            "ĐỔI THẬT", "Huỷ"))
            return;

        // ---- 1) SỔ HOÀN TÁC (ghi trước, idempotent: chỉ thêm entry chưa có)
        UndoBook book = LoadBook();
        HashSet<string> have = new HashSet<string>();
        foreach (UndoEntry e in book.entries) have.Add(EntryKey(e));
        int added = 0;
        foreach (Finding f in toChange)
        {
            if (have.Contains(f.Key)) continue;
            UndoEntry e = new UndoEntry
            {
                kind = f.kind, assetPath = f.assetPath, objectPath = f.objectPath,
                componentType = f.componentType, componentIndex = f.componentIndex,
                propertyPath = f.propertyPath, oldGuid = f.oldGuid, oldFileID = f.oldFileID,
                oldSpritePath = f.oldSpritePath, wasImage = f.isImage,
                preserveAspectWas = f.preserveAspectWas
            };
            book.entries.Add(e);
            have.Add(f.Key);
            added++;
        }
        if (!SaveBook(book))
        {
            EditorUtility.DisplayDialog("APPLY bị từ chối",
                "Không ghi được sổ hoàn tác:\n" + UNDO_JSON + "\nKHÔNG đổi gì cả.", "Đóng");
            return;
        }
        Debug.Log(LOG + "Sổ hoàn tác: +" + added + " entry mới (tổng " + book.entries.Count + ") → " + UNDO_JSON);

        // ---- 2) đổi thật
        StringBuilder rp = new StringBuilder();
        rp.AppendLine("=== APPLY " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
        int okScene = 0, okPrefab = 0, okAsset = 0, fail = 0;
        HashSet<string> dirtyScenes = new HashSet<string>();
        HashSet<string> prefabPaths = new HashSet<string>();

        foreach (Finding f in toChange)
        {
            if (f.kind == "PREFAB") { prefabPaths.Add(f.assetPath); continue; }
            // SCENE + ASSET: sửa trực tiếp trên object đang load
            if (f.liveTarget == null) { fail++; rp.AppendLine("FAIL (mất target): " + f.Key); continue; }
            if (ApplyToObject(f.liveTarget, f.propertyPath, std, true))
            {
                if (f.kind == "SCENE") { okScene++; dirtyScenes.Add(f.assetPath); }
                else { okAsset++; EditorUtility.SetDirty(f.liveTarget); }
                rp.AppendLine("DOI  " + f.kind + " | " + f.assetPath + " | " + f.objectPath + " | " +
                              f.componentType + "." + f.propertyPath + " | " +
                              Path.GetFileName(f.oldSpritePath) + " -> vang-removebg-preview");
            }
            else { fail++; rp.AppendLine("FAIL: " + f.Key); }
        }

        // ---- 3) prefab: LoadPrefabContents → sửa → SaveAsPrefabAsset (try/finally)
        foreach (string ppath in prefabPaths)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(ppath);
                int n = ApplyInsideHierarchy(root, std, rp, ppath);
                if (n > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, ppath);
                    okPrefab += n;
                }
            }
            catch (Exception ex)
            {
                fail++;
                Debug.LogError(LOG + "Lỗi prefab " + ppath + ": " + ex.Message);
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---- 4) lưu
        foreach (string sp in dirtyScenes)
        {
            Scene sc = SceneManager.GetSceneByPath(sp);
            if (sc.IsValid()) EditorSceneManager.MarkSceneDirty(sc);
        }
        if (dirtyScenes.Count > 0) EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = "ĐÃ ĐỔI: scene " + okScene + " · prefab " + okPrefab + " · asset " + okAsset +
                     (fail > 0 ? (" · LỖI " + fail) : "") +
                     "\npreserveAspect=true đã bật cho mọi Image bị đổi." +
                     "\nSổ hoàn tác: " + UNDO_JSON;
        rp.AppendLine(msg);
        LogChunks(rp.ToString());
        EditorUtility.DisplayDialog("APPLY xong", msg, "OK");
    }

    // ================================================================ MENU 3
    [MenuItem(MENU_UNDO, false, 12)]
    private static void MenuUndo()
    {
        UndoBook book = LoadBook();
        if (book.entries.Count == 0)
        {
            EditorUtility.DisplayDialog("Hoàn tác", "Sổ hoàn tác trống hoặc chưa tồn tại:\n" + UNDO_JSON, "Đóng");
            return;
        }
        if (!EditorUtility.DisplayDialog("Hoàn tác icon vàng",
            "Đọc " + book.entries.Count + " entry từ sổ:\n" + UNDO_JSON +
            "\n\nTrả từng reference về sprite CŨ (trước APPLY đầu tiên)?",
            "HOÀN TÁC", "Huỷ"))
            return;

        StringBuilder rp = new StringBuilder();
        rp.AppendLine("=== HOÀN TÁC " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
        int ok = 0, skip = 0, fail = 0;
        HashSet<string> dirtyScenes = new HashSet<string>();
        Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // gom entry prefab theo file để mở 1 lần
        Dictionary<string, List<UndoEntry>> prefabGroups = new Dictionary<string, List<UndoEntry>>();

        foreach (UndoEntry e in book.entries)
        {
            Sprite oldSpr = ResolveSprite(e.oldGuid, e.oldFileID, cache);
            if (oldSpr == null)
            {
                fail++; rp.AppendLine("FAIL (mất sprite cũ " + e.oldSpritePath + "): " + e.assetPath + " | " + e.objectPath);
                continue;
            }
            if (e.kind == "PREFAB")
            {
                if (!prefabGroups.ContainsKey(e.assetPath)) prefabGroups[e.assetPath] = new List<UndoEntry>();
                prefabGroups[e.assetPath].Add(e);
                continue;
            }
            if (e.kind == "SCENE")
            {
                Scene sc = SceneManager.GetSceneByPath(e.assetPath);
                if (!sc.IsValid() || !sc.isLoaded)
                {
                    skip++; rp.AppendLine("BỎ QUA (scene chưa mở — mở scene rồi chạy lại): " + e.assetPath + " | " + e.objectPath);
                    continue;
                }
                UnityEngine.Object target = LocateInScene(sc, e);
                if (target != null && ApplyToObject(target, e.propertyPath, oldSpr, false, e.preserveAspectWas))
                { ok++; dirtyScenes.Add(e.assetPath); rp.AppendLine("TRẢ  " + e.assetPath + " | " + e.objectPath + " | " + e.componentType + "." + e.propertyPath); }
                else { fail++; rp.AppendLine("FAIL (không tìm thấy): " + e.assetPath + " | " + e.objectPath); }
                continue;
            }
            // ASSET
            UnityEngine.Object aTarget = LocateInAsset(e);
            if (aTarget != null && ApplyToObject(aTarget, e.propertyPath, oldSpr, false, e.preserveAspectWas))
            { ok++; EditorUtility.SetDirty(aTarget); rp.AppendLine("TRẢ  " + e.assetPath + " | " + e.objectPath + " | " + e.componentType + "." + e.propertyPath); }
            else { fail++; rp.AppendLine("FAIL (không tìm thấy): " + e.assetPath + " | " + e.objectPath); }
        }

        foreach (KeyValuePair<string, List<UndoEntry>> kv in prefabGroups)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(kv.Key);
                bool changed = false;
                foreach (UndoEntry e in kv.Value)
                {
                    Sprite oldSpr = ResolveSprite(e.oldGuid, e.oldFileID, cache);
                    UnityEngine.Object target = LocateInHierarchy(root, e);
                    if (oldSpr != null && target != null && ApplyToObject(target, e.propertyPath, oldSpr, false, e.preserveAspectWas))
                    { ok++; changed = true; rp.AppendLine("TRẢ  " + kv.Key + " | " + e.objectPath + " | " + e.componentType + "." + e.propertyPath); }
                    else { fail++; rp.AppendLine("FAIL (không tìm thấy trong prefab): " + kv.Key + " | " + e.objectPath); }
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, kv.Key);
            }
            catch (Exception ex)
            {
                fail++;
                Debug.LogError(LOG + "Lỗi hoàn tác prefab " + kv.Key + ": " + ex.Message);
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        foreach (string sp in dirtyScenes)
        {
            Scene sc = SceneManager.GetSceneByPath(sp);
            if (sc.IsValid()) EditorSceneManager.MarkSceneDirty(sc);
        }
        if (dirtyScenes.Count > 0) EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        string msg = "HOÀN TÁC: trả lại " + ok + " · bỏ qua (scene chưa mở) " + skip + " · lỗi " + fail +
                     "\nSổ JSON được GIỮ NGUYÊN (để chạy lại cho scene khác)." ;
        rp.AppendLine(msg);
        LogChunks(rp.ToString());
        EditorUtility.DisplayDialog("Hoàn tác xong", msg, "OK");
    }

    // ================================================================ QUÉT
    private static List<Finding> ScanAll(Sprite std)
    {
        List<Finding> results = new List<Finding>();

        // 1) scene đang mở
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene sc = SceneManager.GetSceneAt(i);
            if (!sc.isLoaded) continue;
            foreach (GameObject root in sc.GetRootGameObjects())
                ScanHierarchy(root, "SCENE", sc.path, null, std, results);
        }

        // 2) mọi prefab (prefilter text theo guid cho nhanh)
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (!FileMentionsGoldGuid(path)) continue;
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) ScanHierarchy(go, "PREFAB", path, null, std, results);
        }

        // 3) mọi .asset (ScriptableObject, Tile...)
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
            if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
            if (!FileMentionsGoldGuid(path)) continue;
            foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj == null || obj is GameObject || obj is Component) continue;
                if (obj is Texture || obj is Sprite) continue;
                ScanObject(obj, "ASSET", path, "(asset) " + obj.name, 0, std, results);
            }
        }
        return results;
    }

    private static void ScanHierarchy(GameObject root, string kind, string assetPath, string ignored, Sprite std, List<Finding> results)
    {
        Component[] comps = root.GetComponentsInChildren<Component>(true);
        foreach (Component c in comps)
        {
            if (c == null) continue; // missing script
            int idx = ComponentIndexOf(c);
            ScanObject(c, kind, assetPath, TransformPath(c.transform), idx, std, results);
        }
    }

    private static void ScanObject(UnityEngine.Object target, string kind, string assetPath,
                                   string objectPath, int compIndex, Sprite std, List<Finding> results)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty it = so.GetIterator();
        bool enter = true;
        while (it.Next(enter))
        {
            enter = true;
            if (it.propertyType == SerializedPropertyType.String) { enter = false; continue; }
            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
            enter = false;
            if (it.name == "m_Script") continue;
            Sprite spr = it.objectReferenceValue as Sprite;
            if (spr == null) continue;
            string g; long fid;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(spr, out g, out fid)) continue;
            if (!NonStandardGold.ContainsKey(g)) continue;

            Finding f = new Finding
            {
                kind = kind, assetPath = assetPath, objectPath = objectPath,
                componentType = target.GetType().Name, componentIndex = compIndex,
                propertyPath = it.propertyPath, oldGuid = g, oldFileID = fid,
                oldSpritePath = NonStandardGold[g], liveTarget = target
            };
            Image img = target as Image;
            f.isImage = img != null;
            f.preserveAspectWas = img != null && img.preserveAspect;
            f.skipReason = GetSkipReason(assetPath, target);
            f.aspectWarn = AspectWarning(spr, std, img);
            results.Add(f);
        }
    }

    private static string GetSkipReason(string assetPath, UnityEngine.Object target)
    {
        string reason;
        if (SkipAssetPaths.TryGetValue(assetPath, out reason)) return reason;
        if (target != null && SkipComponentTypes.TryGetValue(target.GetType().Name, out reason)) return reason;
        return null;
    }

    private static string AspectWarning(Sprite oldSpr, Sprite std, Image img)
    {
        if (oldSpr == null || std == null) return null;
        float rOld = oldSpr.rect.height > 0f ? oldSpr.rect.width / oldSpr.rect.height : 1f;
        float rStd = std.rect.height > 0f ? std.rect.width / std.rect.height : 1f;
        if (Mathf.Abs(rOld - rStd) <= 0.12f) return null; // gần như cùng tỉ lệ → OK
        string note = "tỉ lệ cũ " + rOld.ToString("0.00") + " ≠ chuẩn " + rStd.ToString("0.00");
        if (img != null && !img.preserveAspect)
            note += " (APPLY sẽ bật preserveAspect nên KHÔNG méo, nhưng có thể hở khung)";
        return note;
    }

    // ============================================================ ĐỔI 1 FIELD
    // setPreserveAspect=true (APPLY): Image nào bị đổi thì bật preserveAspect.
    // restorePreserve (chỉ dùng khi hoàn tác): trả preserveAspect về giá trị cũ.
    private static bool ApplyToObject(UnityEngine.Object target, string propertyPath, Sprite sprite,
                                      bool setPreserveAspect, bool restorePreserve = false)
    {
        if (target == null) return false;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(propertyPath);
        if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return false;
        p.objectReferenceValue = sprite;
        if (target is Image)
        {
            SerializedProperty pa = so.FindProperty("m_PreserveAspect");
            if (pa != null)
            {
                if (setPreserveAspect) pa.boolValue = true;
                else pa.boolValue = restorePreserve;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // APPLY bên trong prefab contents: quét lại và đổi tại chỗ
    private static int ApplyInsideHierarchy(GameObject root, Sprite std, StringBuilder rp, string ppath)
    {
        List<Finding> local = new List<Finding>();
        ScanHierarchy(root, "PREFAB", ppath, null, std, local);
        int n = 0;
        foreach (Finding f in local)
        {
            if (f.skipReason != null) continue;
            if (ApplyToObject(f.liveTarget, f.propertyPath, std, true))
            {
                n++;
                rp.AppendLine("DOI  PREFAB | " + ppath + " | " + f.objectPath + " | " +
                              f.componentType + "." + f.propertyPath + " | " +
                              Path.GetFileName(f.oldSpritePath) + " -> vang-removebg-preview");
            }
        }
        return n;
    }

    // ============================================================== TÌM LẠI
    private static UnityEngine.Object LocateInScene(Scene sc, UndoEntry e)
    {
        foreach (GameObject root in sc.GetRootGameObjects())
        {
            UnityEngine.Object hit = LocateUnderRoot(root, e, true);
            if (hit != null) return hit;
        }
        return null;
    }

    private static UnityEngine.Object LocateInHierarchy(GameObject root, UndoEntry e)
    {
        return LocateUnderRoot(root, e, false);
    }

    private static UnityEngine.Object LocateUnderRoot(GameObject root, UndoEntry e, bool rootNameMustMatch)
    {
        string[] parts = e.objectPath.Split('/');
        int i0 = 0;
        Transform t = root.transform;
        if (parts.Length > 0 && parts[0] == root.name) i0 = 1;
        else if (rootNameMustMatch) return null;
        for (int i = i0; i < parts.Length; i++)
        {
            Transform next = null;
            for (int c = 0; c < t.childCount; c++)
                if (t.GetChild(c).name == parts[i]) { next = t.GetChild(c); break; }
            if (next == null) return null;
            t = next;
        }
        Component[] comps = t.GetComponents<Component>();
        int seen = 0;
        foreach (Component c in comps)
        {
            if (c == null) continue;
            if (c.GetType().Name != e.componentType) continue;
            if (seen == e.componentIndex) return c;
            seen++;
        }
        return null;
    }

    private static UnityEngine.Object LocateInAsset(UndoEntry e)
    {
        foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(e.assetPath))
        {
            if (obj == null) continue;
            if (obj.GetType().Name != e.componentType) continue;
            if (("(asset) " + obj.name) == e.objectPath || e.objectPath.Length == 0) return obj;
        }
        return null;
    }

    // ============================================================== TIỆN ÍCH
    private static Sprite LoadStandardSprite(bool quiet)
    {
        Sprite std = AssetDatabase.LoadAssetAtPath<Sprite>(STD_PATH);
        if (std == null)
        {
            Debug.LogError(LOG + "KHÔNG load được sprite chuẩn tại " + STD_PATH);
            return null;
        }
        string g; long fid;
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(std, out g, out fid))
        {
            if (g != STD_GUID)
            {
                Debug.LogError(LOG + "GUID sprite chuẩn LỆCH (" + g + " ≠ " + STD_GUID + ") — DỪNG.");
                return null;
            }
            if (fid != STD_FILEID && !quiet)
                Debug.LogWarning(LOG + "fileID sprite chuẩn " + fid + " ≠ " + STD_FILEID + " (vẫn dùng sprite load được).");
        }
        return std;
    }

    private static Sprite ResolveSprite(string guid, long fileID, Dictionary<string, Sprite> cache)
    {
        string key = guid + ":" + fileID;
        Sprite hit;
        if (cache.TryGetValue(key, out hit)) return hit;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) { cache[key] = null; return null; }
        Sprite found = null;
        foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            Sprite s = obj as Sprite;
            if (s == null) continue;
            string g; long fid;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out g, out fid) && fid == fileID)
            { found = s; break; }
            if (found == null) found = s; // fallback: sprite đầu tiên trong file
        }
        cache[key] = found;
        return found;
    }

    private static bool FileMentionsGoldGuid(string assetPath)
    {
        try
        {
            string full = ProjectPath(assetPath);
            if (!File.Exists(full)) return false;
            string text = File.ReadAllText(full);
            foreach (string g in NonStandardGold.Keys)
                if (text.Contains(g)) return true;
            return false;
        }
        catch { return true; } // đọc lỗi → cứ quét bằng SerializedObject cho chắc
    }

    private static string TransformPath(Transform t)
    {
        StringBuilder sb = new StringBuilder(t.name);
        while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
        return sb.ToString();
    }

    private static int ComponentIndexOf(Component c)
    {
        Component[] all = c.gameObject.GetComponents<Component>();
        int seen = 0;
        foreach (Component x in all)
        {
            if (x == null) continue;
            if (x == c) return seen;
            if (x.GetType().Name == c.GetType().Name) seen++;
        }
        return 0;
    }

    private static int CountChange(List<Finding> fs)
    {
        int n = 0;
        foreach (Finding f in fs) if (f.skipReason == null) n++;
        return n;
    }

    private static string EntryKey(UndoEntry e)
    {
        return e.assetPath + "|" + e.objectPath + "|" + e.componentType + "#" + e.componentIndex + "|" + e.propertyPath;
    }

    private static string ProjectPath(string rel)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", rel));
    }

    private static UndoBook LoadBook()
    {
        try
        {
            string full = ProjectPath(UNDO_JSON);
            if (!File.Exists(full)) return new UndoBook();
            UndoBook b = JsonUtility.FromJson<UndoBook>(File.ReadAllText(full));
            return b != null ? b : new UndoBook();
        }
        catch (Exception ex)
        {
            Debug.LogError(LOG + "Sổ hoàn tác đọc lỗi (" + ex.Message + ") — coi như trống.");
            return new UndoBook();
        }
    }

    private static bool SaveBook(UndoBook book)
    {
        try
        {
            string full = ProjectPath(UNDO_JSON);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, JsonUtility.ToJson(book, true));
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(LOG + "KHÔNG ghi được sổ hoàn tác: " + ex.Message);
            return false;
        }
    }

    private static void TryWriteTextFile(string fullPath, string content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(LOG + "Không ghi được report file: " + ex.Message);
        }
    }

    private static string BuildReport(List<Finding> findings, Sprite std, bool dryRun)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("================ " + (dryRun ? "DRY-RUN" : "SCAN") + " ĐỒNG NHẤT ICON VÀNG — " +
                      DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ================");
        sb.AppendLine("Sprite chuẩn: " + STD_PATH + (std != null ? (" (" + std.rect.width + "x" + std.rect.height + ")") : " (KHÔNG LOAD ĐƯỢC!)"));
        sb.AppendLine("Scene đang mở được quét; scene KHÔNG mở sẽ không hiện ở đây — mở SCN_Farm rồi SampleScene, chạy 2 lần.");
        sb.AppendLine();
        string[] kinds = { "SCENE", "PREFAB", "ASSET" };
        foreach (string k in kinds)
        {
            sb.AppendLine("---- " + k + " ----");
            int n = 0;
            foreach (Finding f in findings)
            {
                if (f.kind != k || f.skipReason != null) continue;
                n++;
                sb.Append("[SẼ ĐỔI] ").Append(f.assetPath).Append(" | ").Append(f.objectPath)
                  .Append(" | ").Append(f.componentType).Append('.').Append(f.propertyPath)
                  .Append(" | ").Append(Path.GetFileName(f.oldSpritePath)).Append(" -> vang-removebg-preview");
                if (f.aspectWarn != null) sb.Append("  ⚠ ").Append(f.aspectWarn);
                sb.AppendLine();
            }
            if (n == 0) sb.AppendLine("(không có)");
            sb.AppendLine();
        }
        sb.AppendLine("---- BỎ QUA (CẦN SẾP QUYẾT — tool không đổi) ----");
        int m = 0;
        foreach (Finding f in findings)
        {
            if (f.skipReason == null) continue;
            m++;
            sb.Append("[BỎ QUA] ").Append(f.assetPath).Append(" | ").Append(f.objectPath)
              .Append(" | ").Append(f.componentType).Append('.').Append(f.propertyPath)
              .Append(" | ").Append(Path.GetFileName(f.oldSpritePath))
              .Append("  — LÝ DO: ").Append(f.skipReason).AppendLine();
        }
        if (m == 0) sb.AppendLine("(không có)");
        sb.AppendLine();
        sb.AppendLine("TỔNG: " + findings.Count + " reference · SẼ ĐỔI " + CountChange(findings) + " · BỎ QUA " + m);
        sb.AppendLine("Code .cs hardcode icon vàng KHÔNG nằm trong tool này — xem production/KIEM_KE_ICON_VANG_2026-09-02.md mục CODE.");
        return sb.ToString();
    }

    private static void LogChunks(string text)
    {
        const int CHUNK = 12000;
        for (int i = 0; i < text.Length; i += CHUNK)
            Debug.Log(LOG + "\n" + text.Substring(i, Math.Min(CHUNK, text.Length - i)));
    }
}
