#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool tự động:
/// 1. Bổ sung 100% Icon còn thiếu vào tất cả 29 asset LevelReward_L2..L30 (Nấm, Ngô, Thịt Gà, Đá, Bò, Heo...)
/// 2. Đồng bộ 4 nhân vật ăn mừng (Puppet Art) 2 bên trái/phải cùng phong cách 100%
/// 3. Gộp toàn bộ ô quà tặng vào chung khung dải màu trắng (Dai_MoKhoa) với các ô Mở Khóa NEW, tắt hẳn hàng lơ lửng cũ!
/// </summary>
[InitializeOnLoad]
public static class LevelRewardIconAutoFixer
{
    private const string MenuPath = "Tools/Farm Game/Level Up Popup/★ Tự Động Sửa Icon & Gộp Quà & Đồng Bộ Nhân Vật";

    static LevelRewardIconAutoFixer()
    {
        EditorApplication.delayCall += () => FixAll(false);
    }

    [MenuItem(MenuPath, false, 5)]
    public static void FixAllMenu()
    {
        FixAll(true);
    }

    public static void FixAll(bool showDialog = true)
    {
        var report = new StringBuilder();
        report.AppendLine("═══════ SỬA ICON & GỘP QUÀ LEVEL UP POPUP ═══════");

        // 1. TỪ ĐIỂN TRA CỨU SPRITE TOÀN DIỆN CHO TẤT CẢ ITEM TRONG GAME
        var iconDict = BuildCompleteIconDictionary();

        // 2. GÁN ICON VÀO TẤT CẢ 29 ASSET LEVEL REWARD
        string[] assetGuids = AssetDatabase.FindAssets("LevelReward_L", new[] { "Assets/_Game/Farm/data/Lever Game" });
        int fixedEntries = 0;

        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(path);
            if (cfg == null) continue;

            bool modified = false;
            SerializedObject soCfg = new SerializedObject(cfg);
            var giftItemsProp = soCfg.FindProperty("giftItems");

            if (giftItemsProp != null)
            {
                for (int i = 0; i < giftItemsProp.arraySize; i++)
                {
                    var itemElem = giftItemsProp.GetArrayElementAtIndex(i);
                    var idProp = itemElem.FindPropertyRelative("itemId");
                    var iconProp = itemElem.FindPropertyRelative("icon");
                    var nameProp = itemElem.FindPropertyRelative("displayName");

                    string id = idProp != null ? idProp.stringValue : "";
                    if (iconProp != null && iconProp.objectReferenceValue == null)
                    {
                        Sprite resolved = ResolveSprite(id, nameProp != null ? nameProp.stringValue : "", iconDict);
                        if (resolved != null)
                        {
                            iconProp.objectReferenceValue = resolved;
                            modified = true;
                            fixedEntries++;
                            report.AppendLine($"  ✓ {cfg.name} -> {nameProp?.stringValue} [{id}] = {resolved.name}");
                        }
                    }
                }
            }

            if (modified)
            {
                soCfg.ApplyModifiedProperties();
                EditorUtility.SetDirty(cfg);
            }
        }

        AssetDatabase.SaveAssets();
        report.AppendLine($"Đã bổ sung thành công {fixedEntries} icon vật phẩm vào các asset LevelReward_L*.asset.");

        // 3. ĐỒNG BỘ 4 NHÂN VẬT ĂN MỪNG VÀO TRỰC TIẾP 4 Ô TRỐNG TRÊN SCN_FARM
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "SCN_Farm")
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/SCN_Farm.unity", OpenSceneMode.Single);
        }

        LevelUpPopupUI popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            Transform content = popup.transform.Find("PopupRoot/Panel_Dim/Content")
                             ?? popup.transform.Find("PopupRoot/Content")
                             ?? popup.transform.Find("Content");

            string[] avatarPaths = new string[]
            {
                "Assets/Art/UI/LevelUpV2/characters/char_01/char_01_master.png", // 1. Cậu bé nông dân (transparent)
                "Assets/Art/UI/LevelUpV2/characters/char_02/char_02_master.png", // 2. Cô bé đầu bếp (transparent)
                "Assets/Art/UI/LevelUpV2/characters/char_03/char_03_master.png", // 3. Cô bé thám hiểm (transparent)
                "Assets/Art/UI/LevelUpV2/characters/char_04/char_04_master.png"  // 4. Cậu bé nón lá (transparent)
            };

            // Dọn sạch các khung trắng gợi ý '_KhungGoiY' và chữ 'NHÂN VẬT'
            var allHints = popup.GetComponentsInChildren<Transform>(true);
            foreach (var h in allHints)
            {
                if (h == null) continue;
                if (h.name.Contains("_KhungGoiY") || h.name.Contains("_ChuGoiY") || h.name.Contains("Slot_Trai_2") || h.name.Contains("Slot_Phai_2"))
                {
                    Undo.DestroyObjectImmediate(h.gameObject);
                }
            }

            // Xóa các slot thừa ngoài lề nếu có
            foreach (var h in allHints)
            {
                if (h == null) continue;
                if (h.name.StartsWith("V2_CelebrationSlot_"))
                {
                    Undo.DestroyObjectImmediate(h.gameObject);
                }
            }

            // Tìm hoặc tạo 4 slot nhân vật chuẩn xác
            Transform layerSau = content?.Find("Layer_NhanVat_Sau  ◄ THẢ ART VÀO ĐÂY") ?? content;
            Transform layerTruoc = content?.Find("Layer_NhanVat_Truoc  ◄ THẢ ART VÀO ĐÂY") ?? content;

            CelebrationCharacterSlot[] slots = new CelebrationCharacterSlot[4];

            // Slot 1: Trái Sau (Cậu bé nông dân)
            slots[0] = SetupCharSlot(layerSau, "Slot_Trai_1", new Vector2(-210f, 245f), new Vector2(240f, 240f), avatarPaths[0]);

            // Slot 1 (2): Trái Trước (Cô bé đầu bếp)
            slots[1] = SetupCharSlot(layerTruoc, "Slot_Truoc_Trai", new Vector2(-340f, 165f), new Vector2(240f, 240f), avatarPaths[1]);

            // Slot 3: Phải Sau (Cô bé làm hoa)
            slots[2] = SetupCharSlot(layerSau, "Slot_Phai_1", new Vector2(210f, 245f), new Vector2(240f, 240f), avatarPaths[2]);

            // Slot 4: Phải Trước (Chàng trai cao bồi)
            slots[3] = SetupCharSlot(layerTruoc, "Slot_Truoc_Phai", new Vector2(340f, 165f), new Vector2(240f, 240f), avatarPaths[3]);

            var soPopup = new SerializedObject(popup);
            var slotsProp = soPopup.FindProperty("celebrationSlots");
            if (slotsProp != null)
            {
                slotsProp.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                }
            }

            // GỘP VÙNG QUÀ: Đảm bảo unlockSlotsContainer trỏ đúng vào Content của Dai_MoKhoa
            Transform daiMoKhoa = popup.transform.Find("PopupRoot/Dai_MoKhoa")
                               ?? popup.transform.Find("PopupRoot/Panel_Dim/Dai_MoKhoa")
                               ?? content?.Find("Dai_MoKhoa");
            if (daiMoKhoa != null)
            {
                Transform viewContent = daiMoKhoa.Find("ScrollView/Viewport/Content") ?? daiMoKhoa;
                var unlockContainerProp = soPopup.FindProperty("unlockSlotsContainer");
                if (unlockContainerProp != null)
                {
                    unlockContainerProp.objectReferenceValue = viewContent;
                }

                var giftContainerProp = soPopup.FindProperty("giftItemsContainer");
                if (giftContainerProp != null && giftContainerProp.objectReferenceValue != null)
                {
                    var giftGo = (giftContainerProp.objectReferenceValue as Transform)?.gameObject;
                    if (giftGo != null) giftGo.SetActive(false);
                }
            }

            // NÂNG NGÔI SAO LÊN CAO ĐỂ KHÔNG CHE CHỮ + TẠO CHỮ NỔI 3D ĐỔ BÓNG
            Transform ngoiSao = content?.Find("NgoiSao") ?? popup.transform.Find("PopupRoot/Content/NgoiSao");
            if (ngoiSao != null)
            {
                var rtStar = ngoiSao as RectTransform;
                if (rtStar != null) rtStar.anchoredPosition = new Vector2(0f, 275f);
            }

            Transform bangRon = content?.Find("BangRon") ?? popup.transform.Find("PopupRoot/Content/BangRon");
            if (bangRon != null)
            {
                TMP_Text txtTieuDe = bangRon.GetComponentInChildren<TMP_Text>(true);
                if (txtTieuDe != null)
                {
                    var rtTitle = txtTieuDe.rectTransform;
                    rtTitle.anchoredPosition = new Vector2(0f, -14f);
                    txtTieuDe.fontSize = 82;
                    txtTieuDe.color = new Color32(255, 245, 185, 255);
                    txtTieuDe.fontStyle = FontStyles.Bold;
                    
                    // 3D Shadow & Outline
                    var shadow = txtTieuDe.GetComponent<Shadow>() ?? txtTieuDe.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color32(10, 45, 80, 240);
                    shadow.effectDistance = new Vector2(0f, -6f);
                }
            }

            soPopup.ApplyModifiedProperties();
            EditorUtility.SetDirty(popup);
            EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);
            EditorSceneManager.SaveScene(popup.gameObject.scene);
            report.AppendLine("Đã nâng Ngôi sao lên cao (không che chữ), tạo hiệu ứng chữ nổi 3D đổ bóng sắc nét & gộp quà vào dải trắng!");
        }

        Debug.Log("[LevelRewardIconAutoFixer] " + report);
        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công", 
                "Đã xử lý hoàn tất:\n" +
                $"1. Bổ sung {fixedEntries} icon còn thiếu vào LevelReward_L2..L30\n" +
                "2. Đặt 4 nhân vật trực tiếp vào 4 ô chuẩn (Cậu bé, Đầu bếp, Cô bé hoa, Cao bồi), xóa sạch khung trắng 'NHÂN VẬT'\n" +
                "3. Gộp toàn bộ quà tặng vào chung khung dải màu trắng với các ô Mở Khóa NEW!", "Tuyệt vời!");
        }
    }

    private static CelebrationCharacterSlot SetupCharSlot(Transform parent, string slotName, Vector2 pos, Vector2 size, string spritePath)
    {
        if (parent == null) return null;
        Transform tr = parent.Find(slotName);
        GameObject go;
        if (tr == null)
        {
            go = new GameObject(slotName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = tr.gameObject;
        }

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;

        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (s != null)
        {
            img.sprite = s;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        img.raycastTarget = false;

        var slotComp = go.GetComponent<CelebrationCharacterSlot>();
        if (slotComp == null) slotComp = go.AddComponent<CelebrationCharacterSlot>();
        
        var soSlot = new SerializedObject(slotComp);
        var masterProp = soSlot.FindProperty("puppetMaster");
        if (masterProp != null && s != null) masterProp.objectReferenceValue = s;
        var framesProp = soSlot.FindProperty("frames");
        if (framesProp != null) framesProp.arraySize = 0;
        soSlot.ApplyModifiedProperties();

        go.SetActive(true);
        EditorUtility.SetDirty(go);
        return slotComp;
    }

    private static Dictionary<string, Sprite> BuildCompleteIconDictionary()
    {
        var dict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        // 1. Quét tất cả CropData trong Hat_giong
        string[] cropGuids = AssetDatabase.FindAssets("t:CropData", new[] { "Assets/_Game/Farm/data/Hat_giong" });
        foreach (string guid in cropGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(p);
            if (crop != null)
            {
                Sprite s = crop.harvestIcon ?? crop.itemIcon ?? crop.readySprite;
                if (s != null)
                {
                    if (!string.IsNullOrEmpty(crop.itemID)) dict[crop.itemID] = s;
                    if (!string.IsNullOrEmpty(crop.cropId)) dict[crop.cropId] = s;
                    if (!string.IsNullOrEmpty(crop.itemName)) dict[crop.itemName] = s;
                }
            }
        }

        // 2. Quét tất cả InventoryItemData trong Farm_dong_vat & Farm_May_Che_Bien
        string[] itemGuids = AssetDatabase.FindAssets("t:InventoryItemData", new[] { "Assets/_Game/Farm/data" });
        foreach (string guid in itemGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(p);
            if (item != null && item.icon != null)
            {
                if (!string.IsNullOrEmpty(item.itemId)) dict[item.itemId] = item.icon;
                if (!string.IsNullOrEmpty(item.displayName)) dict[item.displayName] = item.icon;
            }
        }

        // 3. Bổ sung các sprite đặc thù (Đá, Vàng, Kim Cương...)
        AddIfExist(dict, "da", "Assets/maptitle/title_da-removebg-preview.png");
        AddIfExist(dict, "Đá", "Assets/maptitle/title_da-removebg-preview.png");
        AddIfExist(dict, "stone", "Assets/maptitle/title_da-removebg-preview.png");
        AddIfExist(dict, "rock", "Assets/maptitle/title_da-removebg-preview.png");
        AddIfExist(dict, "mushroom", "Assets/_Game/Farm/data/Hat_giong/nam.asset");
        AddIfExist(dict, "Nấm", "Assets/_Game/Farm/data/Hat_giong/nam.asset");
        AddIfExist(dict, "ngo", "Assets/_Game/Farm/data/Hat_giong/Ngo.asset");
        AddIfExist(dict, "Ngô", "Assets/_Game/Farm/data/Hat_giong/Ngo.asset");
        AddIfExist(dict, "chicken_meat", "Assets/_Game/Farm/data/Farm_dong_vat/Item_ChickenMeat.asset");
        AddIfExist(dict, "Thịt Gà", "Assets/_Game/Farm/data/Farm_dong_vat/Item_ChickenMeat.asset");

        return dict;
    }

    private static void AddIfExist(Dictionary<string, Sprite> dict, string key, string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            if (path.EndsWith(".asset"))
            {
                var crop = AssetDatabase.LoadAssetAtPath<CropData>(path);
                if (crop != null) s = crop.harvestIcon ?? crop.itemIcon ?? crop.readySprite;
                if (s == null)
                {
                    var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
                    if (item != null) s = item.icon;
                }
            }
        }
        if (s != null) dict[key] = s;
    }

    private static Sprite ResolveSprite(string id, string name, Dictionary<string, Sprite> dict)
    {
        if (!string.IsNullOrEmpty(id) && dict.TryGetValue(id, out Sprite s1)) return s1;
        if (!string.IsNullOrEmpty(name) && dict.TryGetValue(name, out Sprite s2)) return s2;

        // Thử tìm theo tên tương tự
        string cleanId = id.ToLowerInvariant().Replace("seed_", "").Replace("item_", "");
        if (dict.TryGetValue(cleanId, out Sprite s3)) return s3;

        return null;
    }
}
#endif
