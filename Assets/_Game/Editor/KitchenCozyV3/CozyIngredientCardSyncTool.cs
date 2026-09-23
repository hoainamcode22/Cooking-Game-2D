using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3.Editor
{
    /// <summary>
    /// Tool tạo & áp dụng Prefab card nguyên liệu/gia vị từ Card_beef.
    /// Menu 4: Lưu Card_beef → Prefab
    /// Menu 5: Thay TẤT CẢ Card_* bằng Prefab Instance thật sự
    ///         (giữ icon + tên riêng, mọi thứ khác kế thừa từ Prefab)
    /// </summary>
    public static class CozyIngredientCardSyncTool
    {
        private const string PrefabFolder = "Assets/_Game/Farm/Prefabs/Kitchen";
        private const string PrefabPath   = "Assets/_Game/Farm/Prefabs/Kitchen/PF_IngredientCard_CozyTemplate.prefab";

        // ═══════════════════════════════════════════════════════════════
        //  MENU 4: Lưu Card_beef thành Prefab
        // ═══════════════════════════════════════════════════════════════
        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/4. Lưu Card_beef thành Prefab nguyên liệu")]
        public static void SaveCardBeefPrefab()
        {
            var cardBeef = FindCardBeef();
            if (cardBeef == null)
            {
                EditorUtility.DisplayDialog("Thông báo",
                    "Không tìm thấy 'Card_beef' trong Scene!\n" +
                    "Hãy mở Scene chứa Kitchen UI và đảm bảo Grid_Ingredients có Card_beef.",
                    "OK");
                return;
            }

            // Tạo thư mục nếu chưa có
            string fullDir = Path.Combine(Application.dataPath, "_Game/Farm/Prefabs/Kitchen");
            if (!Directory.Exists(fullDir))
            {
                Directory.CreateDirectory(fullDir);
                AssetDatabase.Refresh();
            }

            // Nếu Card_beef đang là prefab instance cũ → unpack trước khi save mới
            if (PrefabUtility.IsPartOfPrefabInstance(cardBeef))
            {
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(cardBeef);
                if (root == cardBeef)
                {
                    PrefabUtility.UnpackPrefabInstance(
                        root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                }
            }

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                cardBeef, PrefabPath, InteractionMode.UserAction);

            if (prefab != null)
            {
                Debug.Log($"<color=green>[CozyKitchen] ✔ Đã lưu Card_beef thành Prefab: {PrefabPath}</color>");
                EditorUtility.DisplayDialog("Thành công ✔",
                    $"Đã lưu Card_beef thành Prefab tại:\n{PrefabPath}\n\n" +
                    "Card_beef giờ là Prefab gốc. Tất cả khung, số lượng,\n" +
                    "hiệu ứng, shadow… đều nằm trong Prefab.\n\n" +
                    "Bước tiếp:\n" +
                    "Tools > Farm Game > Kitchen Cozy V3 >\n" +
                    "  5. Tạo Prefab Instances cho TẤT CẢ nguyên liệu + gia vị",
                    "OK");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  MENU 5: Thay TẤT CẢ card bằng Prefab Instance thật sự
        // ═══════════════════════════════════════════════════════════════
        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/5. Tạo Prefab Instances cho TẤT CẢ nguyên liệu + gia vị")]
        public static void ReplaceAllWithPrefabInstances()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Thông báo",
                    "Chưa có Prefab!\nHãy chạy menu\n" +
                    "'4. Lưu Card_beef thành Prefab nguyên liệu' trước.",
                    "OK");
                return;
            }

            // ── Tìm Grid Ingredients + Seasonings ──
            var kitchenUI = Object.FindFirstObjectByType<KitchenUIv2.KitchenSceneV2UI>();
            Transform gridIng = FindGridInScene("Grid_Ingredients");
            Transform gridSea = FindGridInScene("Grid_Seasonings");

            if (gridIng == null && gridSea == null)
            {
                EditorUtility.DisplayDialog("Lỗi",
                    "Không tìm thấy Grid_Ingredients hoặc Grid_Seasonings trong Scene!",
                    "OK");
                return;
            }

            // ── Xây bảng tra IngredientData để gán icon + data cho card thiếu ──
            Dictionary<string, IngredientData> dataMap = BuildDataMap(kitchenUI);

            int updated = 0;

            if (gridIng != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(gridIng.gameObject, "Prefab-ify Ingredient Cards");
                updated += ReplaceCardsInGrid(prefab, gridIng, dataMap);
            }
            if (gridSea != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(gridSea.gameObject, "Prefab-ify Seasoning Cards");
                updated += ReplaceCardsInGrid(prefab, gridSea, dataMap);
            }

            // Bật KhoaLayout để Play Mode giữ nguyên UI
            if (kitchenUI != null)
            {
                var so = new SerializedObject(kitchenUI);
                var propKhoa = so.FindProperty("khoaLayout");
                if (propKhoa != null && !propKhoa.boolValue)
                {
                    propKhoa.boolValue = true;
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"<color=green>[CozyKitchen] ✔ Đã thay {updated} card bằng Prefab instances!</color>");
            EditorUtility.DisplayDialog("Thành công ✔",
                $"Đã thay {updated} card bằng PREFAB INSTANCE!\n\n" +
                "✓ Tất cả card giờ là Prefab Instance của Card_beef\n" +
                "✓ Khung, số lượng, shadow, hiệu ứng — giống hệt\n" +
                "✓ Sửa Prefab 1 lần → tất cả card tự cập nhật\n" +
                "✓ Icon + tên riêng được giữ nguyên (override)\n" +
                "✓ Đã bật Khoa Layout = true\n\n" +
                "Nhấn Ctrl+S để lưu Scene.",
                "OK");
        }

        // ═══════════════════════════════════════════════════════════════
        //  CORE: Thay từng card trong Grid bằng Prefab Instance
        // ═══════════════════════════════════════════════════════════════
        private static int ReplaceCardsInGrid(GameObject prefab, Transform grid,
            Dictionary<string, IngredientData> dataMap)
        {
            int count = 0;
            int i = 0;

            while (i < grid.childCount)
            {
                var child = grid.GetChild(i);

                // Bỏ qua object không phải Card_
                if (!child.name.StartsWith("Card_"))
                {
                    i++;
                    continue;
                }

                // Bỏ qua nếu ĐÃ là Prefab Instance của template này
                var source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                if (source == prefab)
                {
                    i++;
                    continue;
                }

                // ── 1. Thu thập dữ liệu riêng của card này ──
                string cardName = child.name;
                string ingId = cardName.Length > 5
                    ? cardName.Substring(5).Trim().ToLower()
                    : "";

                // Icon sprite
                Sprite iconSprite = null;
                var iconT = child.Find("Img_MainIcon");
                if (iconT != null)
                {
                    var img = iconT.GetComponent<Image>();
                    if (img != null) iconSprite = img.sprite;
                }

                // IngredientData từ component
                IngredientData ingData = null;
                var sel = child.GetComponent<SelectableIngredientCard>();
                if (sel != null) ingData = sel.GetIngredientData();

                // Fallback: tra bảng dataMap (allIngredients)
                if (ingData == null && dataMap != null && dataMap.ContainsKey(ingId))
                    ingData = dataMap[ingId];

                // Fallback icon từ IngredientData
                if (iconSprite == null && ingData != null && ingData.icon != null)
                    iconSprite = ingData.icon;

                // Tên hiển thị (Txt_Name)
                string displayName = null;
                var nameT = child.Find("Txt_Name");
                if (nameT != null)
                {
                    var tmp = nameT.GetComponent<TMP_Text>();
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                        displayName = tmp.text;
                }

                // Vị trí hiện tại
                var rt = child.GetComponent<RectTransform>();
                Vector2 pos = rt != null ? rt.anchoredPosition : Vector2.zero;

                // ── 2. Xoá card cũ ──
                Undo.DestroyObjectImmediate(child.gameObject);

                // ── 3. Tạo Prefab Instance mới ──
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grid);
                instance.name = cardName;
                instance.transform.SetSiblingIndex(i);

                // Khôi phục vị trí
                var newRt = instance.GetComponent<RectTransform>();
                if (newRt != null)
                    newRt.anchoredPosition = pos;

                // ── 4. Override icon (mỗi card 1 icon khác nhau) ──
                if (iconSprite != null)
                {
                    var newIconT = instance.transform.Find("Img_MainIcon");
                    if (newIconT != null)
                    {
                        var img = newIconT.GetComponent<Image>();
                        if (img != null)
                        {
                            img.sprite  = iconSprite;
                            img.enabled = true;
                        }
                    }
                }

                // ── 5. Override tên hiển thị ──
                if (!string.IsNullOrEmpty(displayName))
                {
                    var newNameT = instance.transform.Find("Txt_Name");
                    if (newNameT != null)
                    {
                        var tmp = newNameT.GetComponent<TMP_Text>();
                        if (tmp != null) tmp.text = displayName;
                    }
                }

                // ── 6. Gán IngredientData ──
                var newSel = instance.GetComponent<SelectableIngredientCard>();
                if (newSel == null)
                    newSel = instance.AddComponent<SelectableIngredientCard>();
                if (ingData != null)
                    newSel.SetIngredientData(ingData);

                count++;
                i++;
            }

            return count;
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Xây bảng tra IngredientData từ KitchenSceneV2UI.allIngredients (reflection).</summary>
        private static Dictionary<string, IngredientData> BuildDataMap(
            KitchenUIv2.KitchenSceneV2UI kitchenUI)
        {
            if (kitchenUI == null) return null;

            var field = kitchenUI.GetType().GetField("allIngredients",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return null;

            var arr = field.GetValue(kitchenUI) as IngredientData[];
            if (arr == null || arr.Length == 0) return null;

            var map = new Dictionary<string, IngredientData>();
            foreach (var d in arr)
            {
                if (d != null && !string.IsNullOrEmpty(d.id))
                    map[d.id.Trim().ToLower()] = d;
            }
            return map;
        }

        private static GameObject FindCardBeef()
        {
            // Cách 1: user chọn sẵn trên Hierarchy
            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.name == "Card_beef")
                return Selection.activeGameObject;

            // Cách 2: tìm trong scene
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindDeep(root.transform, "Card_beef");
                if (found != null) return found.gameObject;
            }
            return null;
        }

        /// <summary>Tìm Grid bằng tên trong toàn scene.</summary>
        private static Transform FindGridInScene(string gridName)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindDeep(root.transform, gridName);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
