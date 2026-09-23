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
    public static class CozyKitchenV3LayoutSuite
    {
        private const string PrefabPath = "Assets/_Game/Farm/Prefabs/Kitchen/PF_IngredientCard_CozyTemplate.prefab";
        private const string PlankSpritePath = "Assets/Art/UI/KitchenCozyV3/Cut/plank_paper_order_card.png";
        private const string ClockSpritePath = "Assets/Art/UI/KitchenCozyV3/Cut/anim_clock_01.png";
        private const string RoundedCardSpritePath = "Assets/Art/UI/KitchenCozyV3/Cut/card_rounded_white.png";

        // ═══════════════════════════════════════════════════════════════
        //  MENU 6: Sửa bố cục số lượng (Txt_Quantity vào Qty_Badge)
        // ═══════════════════════════════════════════════════════════════
        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/6. Sửa bố cục số lượng (Txt_Quantity vào Qty_Badge)")]
        public static void FixQuantityLayoutInPrefabAndSceneMenu()
        {
            FixQuantityLayoutInPrefabAndScene(false);
        }

        public static void FixQuantityLayoutInPrefabAndScene(bool silent = false)
        {
            // ── 1. SỬA PREFAB TEMPLATE TRƯỚC ──
            if (File.Exists(PrefabPath))
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                if (prefabRoot != null)
                {
                    try
                    {
                        FormatSingleCard(prefabRoot.transform);
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                        Debug.Log("<color=green>[CozyKitchen] ✔ Đã cập nhật layout chuẩn cho Prefab PF_IngredientCard_CozyTemplate!</color>");
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }

            // ── 2. SỬA TẤT CẢ CARD TRÊN SCENE ──
            Transform gridIng = FindGridInScene("Grid_Ingredients");
            Transform gridSea = FindGridInScene("Grid_Seasonings");

            int updated = 0;
            if (gridIng != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(gridIng.gameObject, "Fix Quantity Layout Ingredients");
                updated += FixCardsInGrid(gridIng);
            }
            if (gridSea != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(gridSea.gameObject, "Fix Quantity Layout Seasonings");
                updated += FixCardsInGrid(gridSea);
            }

            // Bật khoaLayout = true
            var kitchenUI = Object.FindFirstObjectByType<KitchenUIv2.KitchenSceneV2UI>();
            if (kitchenUI != null)
            {
                var so = new SerializedObject(kitchenUI);
                var prop = so.FindProperty("khoaLayout");
                if (prop != null)
                {
                    prop.boolValue = true;
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            if (!silent)
            {
                EditorUtility.DisplayDialog("Hoàn tất ✔",
                    $"Đã sửa bố cục số lượng cho {updated} thẻ nguyên liệu & gia vị!\n\n" +
                    "✓ Txt_Quantity đã được đưa GỌN GÀNG vào trong khung chữ nhật màu nâu Qty_Badge\n" +
                    "✓ Căn giữa (Center/Middle), chữ trắng kem nổi bật, font đậm rõ nét\n" +
                    "✓ Tên nguyên liệu canh gọn ở góc trái, icon cân đối ở giữa\n" +
                    "✓ Đã lưu cập nhật vào Prefab và Scene!",
                    "OK");
            }
        }

        private static int FixCardsInGrid(Transform grid)
        {
            int count = 0;
            for (int i = 0; i < grid.childCount; i++)
            {
                var child = grid.GetChild(i);
                if (!child.name.StartsWith("Card_")) continue;

                FormatSingleCard(child);
                count++;
            }
            return count;
        }

        /// <summary>
        /// Chuẩn hóa cấu trúc 1 Card nguyên liệu:
        /// - Img_MainIcon: giữa tâm (0, 8), size 76x76
        /// - Txt_Name: góc trái dưới (8, 6), size (w-58, 20)
        /// - Qty_Badge: góc phải dưới (-6, 6), size (50, 24), nền nâu bo góc
        /// - Txt_Quantity: con của Qty_Badge, stretch full, chữ trắng kem sáng, bold, center
        /// </summary>
        public static void FormatSingleCard(Transform cardT)
        {
            var cardRt = cardT.GetComponent<RectTransform>();
            float cardWidth = cardRt != null && cardRt.rect.width > 0 ? cardRt.rect.width : 112f;

            // 1. Img_MainIcon
            var iconT = cardT.Find("Img_MainIcon");
            if (iconT != null)
            {
                var rt = (RectTransform)iconT;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 8f);
                rt.sizeDelta = new Vector2(76f, 76f);

                var img = iconT.GetComponent<Image>();
                if (img != null) img.preserveAspect = true;
            }

            // 2. Qty_Badge (khung chữ nhật màu nâu sẫm ở góc dưới phải)
            var badgeT = cardT.Find("Qty_Badge");
            if (badgeT == null)
            {
                var badgeGO = new GameObject("Qty_Badge", typeof(RectTransform), typeof(Image));
                badgeGO.transform.SetParent(cardT, false);
                badgeT = badgeGO.transform;
            }

            var badgeRt = (RectTransform)badgeT;
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot = new Vector2(1f, 0f);
            badgeRt.anchoredPosition = new Vector2(-6f, 6f);
            badgeRt.sizeDelta = new Vector2(50f, 24f);

            var badgeImg = badgeT.GetComponent<Image>();
            if (badgeImg != null)
            {
                badgeImg.color = new Color(0.29f, 0.18f, 0.10f, 0.95f); // Nâu đậm ấm cúng
                badgeImg.raycastTarget = false;
            }

            // 3. Txt_Quantity (phải nằm trong Qty_Badge)
            Transform qtyT = badgeT.Find("Txt_Quantity");
            if (qtyT == null)
            {
                // Nếu đang nằm ở root card thì chuyển vào trong badge
                qtyT = cardT.Find("Txt_Quantity");
                if (qtyT != null)
                {
                    qtyT.SetParent(badgeT, false);
                }
                else
                {
                    var qtyGO = new GameObject("Txt_Quantity", typeof(RectTransform), typeof(TextMeshProUGUI));
                    qtyGO.transform.SetParent(badgeT, false);
                    qtyT = qtyGO.transform;
                }
            }

            var qtyRt = (RectTransform)qtyT;
            qtyRt.anchorMin = Vector2.zero;
            qtyRt.anchorMax = Vector2.one;
            qtyRt.pivot = new Vector2(0.5f, 0.5f);
            qtyRt.offsetMin = Vector2.zero;
            qtyRt.offsetMax = Vector2.zero;

            var qtyTxt = qtyT.GetComponent<TMP_Text>();
            if (qtyTxt != null)
            {
                qtyTxt.text = string.IsNullOrEmpty(qtyTxt.text) ? "x0" : qtyTxt.text;
                qtyTxt.fontSize = 13;
                qtyTxt.fontStyle = FontStyles.Bold;
                qtyTxt.alignment = TextAlignmentOptions.Center;
                qtyTxt.color = new Color(1f, 0.97f, 0.90f, 1f); // Màu kem trắng sáng cực rõ ràng
                qtyTxt.raycastTarget = false;
            }

            // 4. Txt_Name (nằm ở góc dưới bên trái, tránh đè Qty_Badge)
            var nameT = cardT.Find("Txt_Name");
            if (nameT != null)
            {
                var rt = (RectTransform)nameT;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(8f, 6f);
                rt.sizeDelta = new Vector2(Mathf.Max(40f, cardWidth - 58f), 20f);

                var txt = nameT.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    txt.fontSize = 11;
                    txt.fontStyle = FontStyles.Normal;
                    txt.alignment = TextAlignmentOptions.Left;
                    txt.color = new Color(0.36f, 0.20f, 0.09f, 1f);
                    txt.raycastTarget = false;
                }
            }

            // 5. Nối reference trong SelectableIngredientCard
            var sel = cardT.GetComponent<SelectableIngredientCard>();
            if (sel != null && qtyTxt != null)
            {
                var fld = typeof(SelectableIngredientCard).GetField("txtQuantity",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                fld?.SetValue(sel, qtyTxt);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  MENU 7: Thiết kế thanh Order chuẩn mẫu Tomato Pasta
        // ═══════════════════════════════════════════════════════════════
        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/7. Thiết kế thanh Order mẫu Tomato Pasta (Bảng gỗ + Đĩa mì + Nguyên liệu + Đồng hồ)")]
        public static void SetupTomatoPastaOrderBannerMenu()
        {
            SetupTomatoPastaOrderBanner(false);
        }

        public static void SetupTomatoPastaOrderBanner(bool silent = false)
        {
            Transform bannerT = FindInScene("Order_Banner");
            if (bannerT == null)
            {
                if (!silent) EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy 'Order_Banner' trong Scene!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(bannerT.gameObject, "Setup Tomato Pasta Order Banner");

            // ── 1. Cấu hình bảng gỗ Order_Banner ──
            var bannerRt = (RectTransform)bannerT;
            bannerRt.anchorMin = bannerRt.anchorMax = new Vector2(0.5f, 1f);
            bannerRt.pivot = new Vector2(0.5f, 1f);
            bannerRt.anchoredPosition = new Vector2(30f, -120f);
            bannerRt.sizeDelta = new Vector2(640f, 132f);

            var bannerImg = bannerT.GetComponent<Image>();
            if (bannerImg == null) bannerImg = bannerT.gameObject.AddComponent<Image>();

            Sprite plankSp = AssetDatabase.LoadAssetAtPath<Sprite>(PlankSpritePath);
            if (plankSp != null)
            {
                bannerImg.sprite = plankSp;
                bannerImg.type = Image.Type.Simple;
                bannerImg.color = Color.white;
            }

            // Ẩn Ribbon cũ nếu có
            var ribbon = bannerT.Find("Ribbon");
            if (ribbon != null) ribbon.gameObject.SetActive(false);

            // Tìm hoặc tạo Order_Card (vùng chứa nội dung trên mặt giấy)
            Transform cardT = bannerT.Find("Order_Card");
            if (cardT == null)
            {
                var cardGO = new GameObject("Order_Card", typeof(RectTransform));
                cardGO.transform.SetParent(bannerT, false);
                cardT = cardGO.transform;
            }

            var cardRt = (RectTransform)cardT;
            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.offsetMin = new Vector2(16f, 10f);
            cardRt.offsetMax = new Vector2(-16f, -10f);

            // Tắt Image của Order_Card để không che nền giấy của bảng gỗ
            var cardImg = cardT.GetComponent<Image>();
            if (cardImg != null) cardImg.color = Color.clear;

            // Ẩn Avatar và các chip cũ 0..4, rewards cũ
            var av = cardT.Find("Img_Avatar"); if (av != null) av.gameObject.SetActive(false);
            var rew = cardT.Find("Txt_Rewards"); if (rew != null) rew.gameObject.SetActive(false);
            for (int i = 0; i < 5; i++)
            {
                var chip = cardT.Find($"Chip_{i}");
                if (chip != null) chip.gameObject.SetActive(false);
            }

            // ── 2. Dish_Frame & Img_Dish (Đĩa mì bên trái) ──
            Transform dishFrameT = cardT.Find("Dish_Frame");
            if (dishFrameT == null)
            {
                var frameGO = new GameObject("Dish_Frame", typeof(RectTransform), typeof(Image));
                frameGO.transform.SetParent(cardT, false);
                dishFrameT = frameGO.transform;
            }

            var frameRt = (RectTransform)dishFrameT;
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = new Vector2(66f, 0f);
            frameRt.sizeDelta = new Vector2(86f, 86f);

            var frameImg = dishFrameT.GetComponent<Image>();
            if (frameImg != null)
            {
                frameImg.color = new Color(0.93f, 0.88f, 0.79f, 0.65f); // Kem nhạt bo viền
                frameImg.raycastTarget = false;
            }

            Transform dishImgT = cardT.Find("Img_Dish");
            if (dishImgT == null)
            {
                var dishGO = new GameObject("Img_Dish", typeof(RectTransform), typeof(Image));
                dishGO.transform.SetParent(cardT, false);
                dishImgT = dishGO.transform;
            }

            var dishRt = (RectTransform)dishImgT;
            dishRt.anchorMin = dishRt.anchorMax = new Vector2(0f, 0.5f);
            dishRt.pivot = new Vector2(0.5f, 0.5f);
            dishRt.anchoredPosition = new Vector2(66f, 0f);
            dishRt.sizeDelta = new Vector2(76f, 76f);

            var dishImg = dishImgT.GetComponent<Image>();
            if (dishImg != null)
            {
                dishImg.preserveAspect = true;
                dishImg.raycastTarget = false;
                // Thử gán sprite món ăn đầu tiên có trong game nếu đang trống
                if (dishImg.sprite == null)
                {
                    dishImg.sprite = FindFirstDishSprite();
                }
            }

            // ── 3. Txt_Name (Tên món ăn "Tomato Pasta") ──
            Transform nameT = cardT.Find("Txt_Name");
            if (nameT == null)
            {
                var nameGO = new GameObject("Txt_Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameGO.transform.SetParent(cardT, false);
                nameT = nameGO.transform;
            }

            var nameRt = (RectTransform)nameT;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0f, 0.5f);
            nameRt.pivot = new Vector2(0f, 0.5f);
            nameRt.anchoredPosition = new Vector2(124f, 26f);
            nameRt.sizeDelta = new Vector2(280f, 32f);

            var nameTxt = nameT.GetComponent<TMP_Text>();
            if (nameTxt != null)
            {
                nameTxt.text = "Tomato Pasta";
                nameTxt.fontSize = 22;
                nameTxt.fontStyle = FontStyles.Bold;
                nameTxt.alignment = TextAlignmentOptions.Left;
                nameTxt.color = new Color(0.24f, 0.14f, 0.06f, 1f); // Nâu đen ấm sẫm
                nameTxt.raycastTarget = false;
            }

            // ── 4. Group_RequiredIngredients (Dãy 3-4 ô nguyên liệu cần thiết) ──
            Transform reqGroupT = cardT.Find("Group_RequiredIngredients");
            if (reqGroupT == null)
            {
                var grpGO = new GameObject("Group_RequiredIngredients", typeof(RectTransform), typeof(Image));
                grpGO.transform.SetParent(cardT, false);
                reqGroupT = grpGO.transform;
            }

            var reqRt = (RectTransform)reqGroupT;
            reqRt.anchorMin = reqRt.anchorMax = new Vector2(0f, 0.5f);
            reqRt.pivot = new Vector2(0f, 0.5f);
            reqRt.anchoredPosition = new Vector2(124f, -18f);
            reqRt.sizeDelta = new Vector2(330f, 44f);

            var reqBgImg = reqGroupT.GetComponent<Image>();
            if (reqBgImg != null)
            {
                reqBgImg.color = new Color(0.95f, 0.91f, 0.82f, 0.7f); // Nền màu kem nhạt đồng bộ ảnh mẫu
                reqBgImg.raycastTarget = false;
            }

            // Tạo sẵn 4 slot nguyên liệu bên trong (0..3)
            Sprite[] sampleIcons = FindSampleIngredientIcons();
            for (int i = 0; i < 4; i++)
            {
                Transform slotT = reqGroupT.Find($"Slot_{i}");
                if (slotT == null)
                {
                    var slotGO = new GameObject($"Slot_{i}", typeof(RectTransform));
                    slotGO.transform.SetParent(reqGroupT, false);
                    slotT = slotGO.transform;
                }

                var slotRt = (RectTransform)slotT;
                slotRt.anchorMin = slotRt.anchorMax = new Vector2(0f, 0.5f);
                slotRt.pivot = new Vector2(0f, 0.5f);
                slotRt.anchoredPosition = new Vector2(8f + i * 80f, 0f);
                slotRt.sizeDelta = new Vector2(76f, 38f);

                // Icon nguyên liệu
                Transform sIconT = slotT.Find("Img_Icon");
                if (sIconT == null)
                {
                    var sIconGO = new GameObject("Img_Icon", typeof(RectTransform), typeof(Image));
                    sIconGO.transform.SetParent(slotT, false);
                    sIconT = sIconGO.transform;
                }
                var sIconRt = (RectTransform)sIconT;
                sIconRt.anchorMin = sIconRt.anchorMax = new Vector2(0f, 0.5f);
                sIconRt.pivot = new Vector2(0f, 0.5f);
                sIconRt.anchoredPosition = new Vector2(2f, 0f);
                sIconRt.sizeDelta = new Vector2(32f, 32f);

                var sImg = sIconT.GetComponent<Image>();
                if (sImg != null)
                {
                    sImg.preserveAspect = true;
                    sImg.raycastTarget = false;
                    if (sampleIcons != null && i < sampleIcons.Length)
                        sImg.sprite = sampleIcons[i];
                }

                // Text số lượng "0/1"
                Transform sQtyT = slotT.Find("Txt_Qty");
                if (sQtyT == null)
                {
                    var sQtyGO = new GameObject("Txt_Qty", typeof(RectTransform), typeof(TextMeshProUGUI));
                    sQtyGO.transform.SetParent(slotT, false);
                    sQtyT = sQtyGO.transform;
                }
                var sQtyRt = (RectTransform)sQtyT;
                sQtyRt.anchorMin = sQtyRt.anchorMax = new Vector2(0f, 0.5f);
                sQtyRt.pivot = new Vector2(0f, 0.5f);
                sQtyRt.anchoredPosition = new Vector2(38f, 0f);
                sQtyRt.sizeDelta = new Vector2(36f, 28f);

                var sTxt = sQtyT.GetComponent<TMP_Text>();
                if (sTxt != null)
                {
                    sTxt.text = "0/1";
                    sTxt.fontSize = 15;
                    sTxt.fontStyle = FontStyles.Bold;
                    sTxt.color = new Color(0.29f, 0.18f, 0.10f, 1f);
                    sTxt.alignment = TextAlignmentOptions.Left;
                    sTxt.raycastTarget = false;
                }

                // Mẫu 3 nguyên liệu như trong ảnh (slot 3 ẩn tạm)
                slotT.gameObject.SetActive(i < 3);
            }

            // ── 5. Group_CookingTime (Icon đồng hồ + "2m" bên phải) ──
            Transform timeGroupT = cardT.Find("Group_CookingTime");
            if (timeGroupT == null)
            {
                var timeGO = new GameObject("Group_CookingTime", typeof(RectTransform));
                timeGO.transform.SetParent(cardT, false);
                timeGroupT = timeGO.transform;
            }

            var timeRt = (RectTransform)timeGroupT;
            timeRt.anchorMin = timeRt.anchorMax = new Vector2(1f, 0.5f);
            timeRt.pivot = new Vector2(1f, 0.5f);
            timeRt.anchoredPosition = new Vector2(-28f, 0f);
            timeRt.sizeDelta = new Vector2(90f, 48f);

            // Icon đồng hồ
            Transform clockT = timeGroupT.Find("Img_Clock");
            if (clockT == null)
            {
                var clockGO = new GameObject("Img_Clock", typeof(RectTransform), typeof(Image));
                clockGO.transform.SetParent(timeGroupT, false);
                clockT = clockGO.transform;
            }
            var clockRt = (RectTransform)clockT;
            clockRt.anchorMin = clockRt.anchorMax = new Vector2(0f, 0.5f);
            clockRt.pivot = new Vector2(0f, 0.5f);
            clockRt.anchoredPosition = new Vector2(4f, 0f);
            clockRt.sizeDelta = new Vector2(36f, 36f);

            var clockImg = clockT.GetComponent<Image>();
            if (clockImg != null)
            {
                Sprite clockSp = AssetDatabase.LoadAssetAtPath<Sprite>(ClockSpritePath);
                if (clockSp != null) clockImg.sprite = clockSp;
                clockImg.preserveAspect = true;
                clockImg.raycastTarget = false;
            }

            // Text thời gian "2m"
            Transform timeTxtT = timeGroupT.Find("Txt_Time");
            if (timeTxtT == null)
            {
                var timeTxtGO = new GameObject("Txt_Time", typeof(RectTransform), typeof(TextMeshProUGUI));
                timeTxtGO.transform.SetParent(timeGroupT, false);
                timeTxtT = timeTxtGO.transform;
            }
            var timeTxtRt = (RectTransform)timeTxtT;
            timeTxtRt.anchorMin = timeTxtRt.anchorMax = new Vector2(0f, 0.5f);
            timeTxtRt.pivot = new Vector2(0f, 0.5f);
            timeTxtRt.anchoredPosition = new Vector2(44f, 0f);
            timeTxtRt.sizeDelta = new Vector2(44f, 28f);

            var timeTxt = timeTxtT.GetComponent<TMP_Text>();
            if (timeTxt != null)
            {
                timeTxt.text = "2m";
                timeTxt.fontSize = 17;
                timeTxt.fontStyle = FontStyles.Bold;
                timeTxt.color = new Color(0.29f, 0.18f, 0.10f, 1f);
                timeTxt.alignment = TextAlignmentOptions.Left;
                timeTxt.raycastTarget = false;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            if (!silent)
            {
                EditorUtility.DisplayDialog("Thành công ✔",
                    "Đã tạo thanh Order mẫu 'Tomato Pasta' y hệt ảnh mẫu!\n\n" +
                    "✓ Bảng gỗ có 2 chiếc lá xanh & cuộn giấy kem sáng\n" +
                    "✓ Đĩa mì trong khung bo viền mềm mại ở bên trái\n" +
                    "✓ Tên món 'Tomato Pasta' chữ nâu đậm nổi bật\n" +
                    "✓ Dãy nguyên liệu cần thiết: icon + số lượng 0/1\n" +
                    "✓ Đồng hồ quả quýt + thời gian nấu 2m ở bên phải\n" +
                    "✓ Tự động đồng bộ món ăn khi vào Play Mode!",
                    "OK");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════
        private static Transform FindGridInScene(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindDeep(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindInScene(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindDeep(root.transform, name);
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

        private static Sprite FindFirstDishSprite()
        {
            string[] guids = AssetDatabase.FindAssets("t:DishData");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var dish = AssetDatabase.LoadAssetAtPath<DishData>(path);
                if (dish != null && dish.dishSprite != null)
                    return dish.dishSprite;
            }
            return null;
        }

        private static Sprite[] FindSampleIngredientIcons()
        {
            var list = new List<Sprite>();
            string[] guids = AssetDatabase.FindAssets("t:IngredientData");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var ing = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
                if (ing != null && ing.icon != null)
                {
                    list.Add(ing.icon);
                    if (list.Count >= 4) break;
                }
            }
            return list.ToArray();
        }
    }
}
