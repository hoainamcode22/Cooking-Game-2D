using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3.Editor
{
    public static class CozyDishRowPrefabSyncTool
    {
        private const string PrefabFolderPath = "Assets/_Game/Farm/Prefabs/Kitchen";
        private const string PrefabPath = "Assets/_Game/Farm/Prefabs/Kitchen/PF_DishRow_CozyTemplate.prefab";

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/1. Lưu Row_bap_cai_xao_nam thành Prefab")]
        public static void SaveTemplatePrefab()
        {
            GameObject template = FindRowTemplate();
            if (template == null)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy 'Row_bap_cai_xao_nam' trên Scene active!\nHãy chọn GameObject hoặc mở Scene có Recipe Book.", "OK");
                return;
            }

            string fullDir = Path.Combine(Application.dataPath, "_Game/Farm/Prefabs/Kitchen");
            if (!Directory.Exists(fullDir))
            {
                Directory.CreateDirectory(fullDir);
                AssetDatabase.Refresh();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(template, PrefabPath, InteractionMode.UserAction);
            if (prefab != null)
            {
                Debug.Log($"<color=green>[CozyKitchen] Đã tạo Prefab thành công tại: {PrefabPath}</color>");
                EditorUtility.DisplayDialog("Thành công", $"Đã lưu Prefab thành công tại:\n{PrefabPath}", "OK");
            }
        }

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/2. Áp dụng khung Row_bap_cai_xao_nam cho TẤT CẢ món ăn")]
        public static void SyncTemplateToAllRows()
        {
            GameObject template = FindRowTemplate();
            if (template == null)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy 'Row_bap_cai_xao_nam' trên Scene active!", "OK");
                return;
            }

            Transform parent = template.transform.parent;
            if (parent == null)
            {
                Debug.LogError("[CozyKitchen] Template không có parent container!");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(parent.gameObject, "Sync Template to All Dish Rows");

            var tRt = template.GetComponent<RectTransform>();
            var tImg = template.GetComponent<Image>();
            var tBtn = template.GetComponent<Button>();

            int updatedCount = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.gameObject == template) continue;

                // 1. Sync Root RectTransform
                var cRt = child.GetComponent<RectTransform>();
                if (cRt != null && tRt != null)
                {
                    cRt.sizeDelta = tRt.sizeDelta;
                    cRt.pivot = tRt.pivot;
                    cRt.localScale = tRt.localScale;
                }

                // 2. Sync Root Image
                var cImg = child.GetComponent<Image>();
                if (cImg != null && tImg != null)
                {
                    cImg.sprite = tImg.sprite;
                    cImg.type = tImg.type;
                    cImg.color = tImg.color;
                    cImg.material = tImg.material;
                    cImg.raycastTarget = tImg.raycastTarget;
                }

                // 3. Sync Root Button
                var cBtn = child.GetComponent<Button>();
                if (cBtn != null && tBtn != null)
                {
                    cBtn.transition = tBtn.transition;
                    cBtn.colors = tBtn.colors;
                    cBtn.spriteState = tBtn.spriteState;
                }

                // 4. Sync ALL child components
                SyncAllChildren(template.transform, child);

                updatedCount++;
            }

            var kitchenUI = Object.FindObjectOfType<KitchenUIv2.KitchenSceneV2UI>();
            if (kitchenUI != null)
            {
                var so = new SerializedObject(kitchenUI);
                var propKhoa = so.FindProperty("khoaLayout");
                if (propKhoa != null)
                {
                    propKhoa.boolValue = true;
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=green>[CozyKitchen] Đã đồng bộ {updatedCount} món ăn theo 'Row_bap_cai_xao_nam' thành công!</color>");
            EditorUtility.DisplayDialog("Thành công", $"Đã đồng bộ {updatedCount} món ăn theo 'Row_bap_cai_xao_nam'!\n- Scale và kích thước Icon to đã đồng bộ 100%.\n- Đã tự động bật 'Khoa Layout = true' để giữ nguyên thiết kế khi Play!", "OK");
        }

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/3. Căn chỉnh bố cục Bảng Chi Tiết gọn đẹp (Beautify Board_Detail)")]
        public static void BeautifyBoardDetail()
        {
            var boardDetailGo = GameObject.Find("Board_Detail");
            if (boardDetailGo == null)
            {
                // Search deep
                foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var found = FindDeepChild(root.transform, "Board_Detail");
                    if (found != null) { boardDetailGo = found.gameObject; break; }
                }
            }

            if (boardDetailGo == null)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy GameObject 'Board_Detail' trên Scene active!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(boardDetailGo, "Beautify Board_Detail Layout");

            // Make sure Board_Detail is active for editing
            boardDetailGo.SetActive(true);

            // 1. Align Board_Detail Root RectTransform matching parent stand
            var rtBoard = boardDetailGo.GetComponent<RectTransform>();
            if (rtBoard != null)
            {
                // Match sibling Board_List if possible
                var siblingList = boardDetailGo.transform.parent.Find("Board_List") as RectTransform;
                if (siblingList != null)
                {
                    rtBoard.anchorMin = siblingList.anchorMin;
                    rtBoard.anchorMax = siblingList.anchorMax;
                    rtBoard.pivot = siblingList.pivot;
                    rtBoard.anchoredPosition = siblingList.anchoredPosition;
                    rtBoard.sizeDelta = siblingList.sizeDelta;
                }
                else
                {
                    rtBoard.anchorMin = new Vector2(0f, 0f);
                    rtBoard.anchorMax = new Vector2(1f, 1f);
                    rtBoard.pivot = new Vector2(0.5f, 0.5f);
                    rtBoard.anchoredPosition = Vector2.zero;
                    rtBoard.sizeDelta = Vector2.zero;
                }
            }

            // 2. Align Back Button (< Back)
            var btnBack = boardDetailGo.transform.Find("Btn_OtherDish");
            if (btnBack != null)
            {
                var rt = btnBack.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(35f, -30f);
                    rt.sizeDelta = new Vector2(100f, 40f);
                }
                var txt = btnBack.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.text = "‹ Back";
                    txt.fontSize = 18f;
                    txt.fontStyle = FontStyles.Bold;
                    txt.alignment = TextAlignmentOptions.Center;
                }
            }

            // 3. Align Dish Name & Meta
            var txtName = boardDetailGo.transform.Find("Txt_DishName")?.GetComponent<TMP_Text>();
            if (txtName != null)
            {
                var rt = txtName.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(40f, -28f);
                rt.sizeDelta = new Vector2(-160f, 32f);
                txtName.alignment = TextAlignmentOptions.Center;
                txtName.fontSize = 24f;
                txtName.fontStyle = FontStyles.Bold;
                txtName.color = new Color(0.24f, 0.14f, 0.06f);
            }

            var txtMeta = boardDetailGo.transform.Find("Txt_DishMeta")?.GetComponent<TMP_Text>();
            if (txtMeta != null)
            {
                var rt = txtMeta.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(40f, -62f);
                rt.sizeDelta = new Vector2(-160f, 22f);
                txtMeta.alignment = TextAlignmentOptions.Center;
                txtMeta.fontSize = 14f;
                txtMeta.color = new Color(0.56f, 0.38f, 0.22f);
            }

            // 4. Align "Ingredients Needed" header
            var txtNeed = boardDetailGo.transform.Find("Txt_NeedTitle")?.GetComponent<TMP_Text>();
            if (txtNeed != null)
            {
                var rt = txtNeed.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(38f, -95f);
                rt.sizeDelta = new Vector2(-76f, 24f);
                txtNeed.text = "Ingredients Needed";
                txtNeed.fontSize = 15f;
                txtNeed.fontStyle = FontStyles.Bold;
                txtNeed.color = new Color(0.36f, 0.20f, 0.09f);
                txtNeed.alignment = TextAlignmentOptions.Left;
            }

            // 5. Align Need_Chips container
            var needChips = boardDetailGo.transform.Find("Need_Chips");
            if (needChips != null)
            {
                var rt = needChips.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -125f);
                    rt.sizeDelta = new Vector2(-70f, 75f);
                }

                var gl = needChips.GetComponent<GridLayoutGroup>();
                if (gl != null)
                {
                    gl.cellSize = new Vector2(68f, 68f);
                    gl.spacing = new Vector2(10f, 8f);
                    gl.childAlignment = TextAnchor.MiddleLeft;
                    gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gl.constraintCount = 4;
                }
            }

            // 6. Align "Flavor Profile" header
            var txtTaste = boardDetailGo.transform.Find("Txt_TasteTitle")?.GetComponent<TMP_Text>();
            if (txtTaste != null)
            {
                var rt = txtTaste.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(38f, -215f);
                rt.sizeDelta = new Vector2(-76f, 24f);
                txtTaste.text = "Flavor Profile";
                txtTaste.fontSize = 15f;
                txtTaste.fontStyle = FontStyles.Bold;
                txtTaste.color = new Color(0.36f, 0.20f, 0.09f);
                txtTaste.alignment = TextAlignmentOptions.Left;
            }

            // 7. Align 5 Flavor Rows
            string[] flavorNames = { "Sweet", "Spicy", "Sour", "Savoury", "Texture" };
            for (int i = 0; i < 5; i++)
            {
                var row = boardDetailGo.transform.Find($"Flavor_Row_{i}");
                if (row != null)
                {
                    var rt = row.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0f, 1f);
                        rt.anchorMax = new Vector2(1f, 1f);
                        rt.pivot = new Vector2(0.5f, 1f);
                        rt.anchoredPosition = new Vector2(0f, -248f - (i * 32f));
                        rt.sizeDelta = new Vector2(-70f, 26f);
                    }

                    // Dot
                    var dot = row.Find("Dot") as RectTransform ?? row.Find($"Flavor_Dot_{i}") as RectTransform;
                    if (dot != null)
                    {
                        dot.anchorMin = dot.anchorMax = new Vector2(0f, 0.5f);
                        dot.pivot = new Vector2(0.5f, 0.5f);
                        dot.anchoredPosition = new Vector2(8f, 0f);
                        dot.sizeDelta = new Vector2(14f, 14f);
                    }

                    // Label
                    var lbl = row.Find("Label")?.GetComponent<TMP_Text>() ?? row.Find($"Flavor_Label_{i}")?.GetComponent<TMP_Text>();
                    if (lbl != null)
                    {
                        lbl.rectTransform.anchorMin = lbl.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                        lbl.rectTransform.pivot = new Vector2(0f, 0.5f);
                        lbl.rectTransform.anchoredPosition = new Vector2(24f, 0f);
                        lbl.rectTransform.sizeDelta = new Vector2(75f, 24f);
                        lbl.text = flavorNames[i];
                        lbl.fontSize = 14f;
                        lbl.fontStyle = FontStyles.Bold;
                        lbl.color = new Color(0.28f, 0.16f, 0.08f);
                        lbl.alignment = TextAlignmentOptions.Left;
                    }

                    // Track
                    var track = row.Find("Track") as RectTransform ?? row.Find($"Flavor_Track_{i}") as RectTransform;
                    if (track != null)
                    {
                        track.anchorMin = new Vector2(0f, 0.5f);
                        track.anchorMax = new Vector2(1f, 0.5f);
                        track.pivot = new Vector2(0f, 0.5f);
                        track.anchoredPosition = new Vector2(105f, 0f);
                        track.sizeDelta = new Vector2(-160f, 14f);
                    }

                    // Value
                    var val = row.Find("Value")?.GetComponent<TMP_Text>() ?? row.Find($"Flavor_Value_{i}")?.GetComponent<TMP_Text>();
                    if (val != null)
                    {
                        val.rectTransform.anchorMin = val.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                        val.rectTransform.pivot = new Vector2(1f, 0.5f);
                        val.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                        val.rectTransform.sizeDelta = new Vector2(45f, 22f);
                        val.fontSize = 13f;
                        val.fontStyle = FontStyles.Bold;
                        val.color = new Color(0.4f, 0.25f, 0.12f);
                        val.alignment = TextAlignmentOptions.Right;
                    }
                }
            }

            // 8. Align Bottom Rewards & Projection
            var txtRewards = boardDetailGo.transform.Find("Txt_Rewards")?.GetComponent<TMP_Text>();
            if (txtRewards != null)
            {
                var rt = txtRewards.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 55f);
                rt.sizeDelta = new Vector2(-70f, 26f);
                txtRewards.alignment = TextAlignmentOptions.Center;
                txtRewards.fontSize = 15f;
                txtRewards.fontStyle = FontStyles.Bold;
                txtRewards.color = new Color(0.72f, 0.38f, 0.08f);
            }

            var txtProj = boardDetailGo.transform.Find("Txt_Projection")?.GetComponent<TMP_Text>();
            if (txtProj != null)
            {
                var rt = txtProj.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 25f);
                rt.sizeDelta = new Vector2(-70f, 22f);
                txtProj.alignment = TextAlignmentOptions.Center;
                txtProj.fontSize = 14f;
                txtProj.color = new Color(0.48f, 0.35f, 0.22f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[CozyKitchen] Đã sắp xếp lại bố cục 'Board_Detail' cực kỳ gọn gàng, cân đối và thẩm mỹ!</color>");
            EditorUtility.DisplayDialog("Thành công", "Đã sắp xếp lại toàn bộ bố cục 'Board_Detail'!\n- Khung vừa khít với bảng đứng\n- Nút Back, Tên món, Nguyên liệu và 5 thanh vị đã được căn lề cân đối, đẹp mắt.", "OK");
        }

        private static GameObject FindRowTemplate()
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.name.Contains("Row_"))
            {
                return Selection.activeGameObject;
            }

            var go = GameObject.Find("Row_bap_cai_xao_nam");
            if (go != null) return go;

            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindDeepChild(root.transform, "Row_bap_cai_xao_nam");
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindDeepChild(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void SyncAllChildren(Transform template, Transform target)
        {
            var tImages = template.GetComponentsInChildren<Image>(true);
            var cImages = target.GetComponentsInChildren<Image>(true);

            for (int i = 1; i < tImages.Length; i++)
            {
                var tSubImg = tImages[i];
                Image cSubImg = null;
                Transform foundChild = target.Find(tSubImg.gameObject.name);
                if (foundChild != null)
                {
                    cSubImg = foundChild.GetComponent<Image>();
                }
                else if (i < cImages.Length)
                {
                    cSubImg = cImages[i];
                }

                if (cSubImg != null)
                {
                    var tSubRt = tSubImg.GetComponent<RectTransform>();
                    var cSubRt = cSubImg.GetComponent<RectTransform>();
                    if (tSubRt != null && cSubRt != null)
                    {
                        cSubRt.anchorMin = tSubRt.anchorMin;
                        cSubRt.anchorMax = tSubRt.anchorMax;
                        cSubRt.pivot = tSubRt.pivot;
                        cSubRt.anchoredPosition = tSubRt.anchoredPosition;
                        cSubRt.sizeDelta = tSubRt.sizeDelta;
                        cSubRt.localScale = tSubRt.localScale;
                    }

                    bool isDishIcon = tSubImg.gameObject.name.ToLower().Contains("dish") || 
                                     tSubImg.gameObject.name.ToLower().Contains("icon") || 
                                     tSubImg.gameObject.name.ToLower().Contains("avatar");

                    if (!isDishIcon)
                    {
                        cSubImg.sprite = tSubImg.sprite;
                        cSubImg.type = tSubImg.type;
                    }
                    cSubImg.preserveAspect = tSubImg.preserveAspect;
                    cSubImg.color = tSubImg.color;
                    cSubImg.material = tSubImg.material;
                }
            }

            var tTexts = template.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tTexts.Length; i++)
            {
                var tTxt = tTexts[i];
                TMP_Text cTxt = null;

                Transform foundChild = target.Find(tTxt.gameObject.name);
                if (foundChild != null)
                {
                    cTxt = foundChild.GetComponent<TMP_Text>();
                }
                else
                {
                    var cAllTexts = target.GetComponentsInChildren<TMP_Text>(true);
                    if (i < cAllTexts.Length) cTxt = cAllTexts[i];
                }

                if (cTxt != null)
                {
                    cTxt.font = tTxt.font;
                    cTxt.fontSize = tTxt.fontSize;
                    cTxt.fontSizeMin = tTxt.fontSizeMin;
                    cTxt.fontSizeMax = tTxt.fontSizeMax;
                    cTxt.enableAutoSizing = tTxt.enableAutoSizing;
                    cTxt.color = tTxt.color;
                    cTxt.alignment = tTxt.alignment;
                    cTxt.enableWordWrapping = tTxt.enableWordWrapping;

                    var cRt = cTxt.GetComponent<RectTransform>();
                    var tRt = tTxt.GetComponent<RectTransform>();
                    if (cRt != null && tRt != null)
                    {
                        cRt.anchorMin = tRt.anchorMin;
                        cRt.anchorMax = tRt.anchorMax;
                        cRt.pivot = tRt.pivot;
                        cRt.anchoredPosition = tRt.anchoredPosition;
                        cRt.sizeDelta = tRt.sizeDelta;
                        cRt.localScale = tRt.localScale;
                    }
                }
            }
        }
    }
}
