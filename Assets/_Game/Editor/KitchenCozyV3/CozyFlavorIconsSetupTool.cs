using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3.Editor
{
    public static class CozyFlavorIconsSetupTool
    {
        private static readonly string[] IconPaths = new string[]
        {
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_sweet.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_spicy.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_sour.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_umami.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_texture.png"
        };

        private static readonly string[] FlavorNames = new string[]
        {
            "Ngọt",
            "Cay",
            "Chua",
            "Đậm",
            "Giòn"
        };

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/8. Gắn 5 Icon Gia Vị (Flavor) vào chi tiết món ăn")]
        public static void ApplyFlavorIconsToSceneMenu()
        {
            ApplyFlavorIconsToScene(false);
        }

        public static void ApplyFlavorIconsToScene(bool silent = false)
        {
            // 1. Tải 5 Sprite
            Sprite[] sprites = new Sprite[5];
            for (int i = 0; i < 5; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(IconPaths[i]);
                if (sprites[i] == null)
                {
                    Debug.LogWarning($"[CozyKitchen] Chưa tìm thấy sprite tại: {IconPaths[i]}");
                }
            }

            int count = 0;

            // 2. Tìm tất cả các Flavor_Row_0..4 trên toàn bộ Scene
            for (int i = 0; i < 5; i++)
            {
                string rowName = $"Flavor_Row_{i}";
                var rows = FindAllDeep(rowName);

                foreach (var rowT in rows)
                {
                    Undo.RegisterFullObjectHierarchyUndo(rowT.gameObject, "Apply Flavor Icon");

                    // A. Gắn icon vào Dot
                    Transform dotT = rowT.Find("Dot");
                    if (dotT != null)
                    {
                        var dotRt = (RectTransform)dotT;
                        dotRt.sizeDelta = new Vector2(22f, 22f); // Kích thước icon rõ nét, cân đối
                        dotRt.anchoredPosition = new Vector2(4f, 0f);

                        var dotImg = dotT.GetComponent<Image>();
                        if (dotImg != null)
                        {
                            if (sprites[i] != null)
                            {
                                dotImg.sprite = sprites[i];
                                dotImg.color = Color.white; // Màu gốc của icon
                                dotImg.preserveAspect = true;
                            }
                        }
                    }

                    // B. Chỉnh nhãn Label
                    Transform labelT = rowT.Find("Label");
                    if (labelT != null)
                    {
                        var lblRt = (RectTransform)labelT;
                        lblRt.anchoredPosition = new Vector2(30f, 0f);
                        lblRt.sizeDelta = new Vector2(48f, 22f);

                        var lblTxt = labelT.GetComponent<TMP_Text>();
                        if (lblTxt != null)
                        {
                            lblTxt.text = FlavorNames[i];
                            lblTxt.fontSize = 13;
                            lblTxt.fontStyle = FontStyles.Bold;
                            lblTxt.color = new Color(0.36f, 0.20f, 0.09f, 1f); // Nâu ấm sẫm
                            lblTxt.alignment = TextAlignmentOptions.Left;
                        }
                    }

                    // C. Đẩy Track sang phải một chút để không chạm text
                    Transform trackT = rowT.Find("Track");
                    if (trackT != null)
                    {
                        var trkRt = (RectTransform)trackT;
                        trkRt.anchoredPosition = new Vector2(82f, 0f);
                        trkRt.sizeDelta = new Vector2(150f, 14f);
                    }

                    count++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"<color=green>[CozyKitchen] ✔ Đã gắn thành công 5 icon gia vị vào {count} hàng Flavor trên Scene!</color>");

            if (!silent)
            {
                EditorUtility.DisplayDialog("Thành công ✔",
                    $"Đã gắn 5 icon gia vị vào bảng chi tiết món ăn (Flavor)!\n\n" +
                    "1. Ngọt (Sweet): Viên kẹo hồng ngọt ngào\n" +
                    "2. Cay (Spicy): Quả ớt đỏ ngọn lửa\n" +
                    "3. Chua (Sour): Lát chanh xanh mọng nước\n" +
                    "4. Đậm (Umami): Bát súp hầm nghi ngút khói\n" +
                    "5. Giòn (Texture): Bánh quy snack giòn rụm\n\n" +
                    "✓ Icon đã hiển thị màu sắc gốc rực rỡ (Color.white)\n" +
                    "✓ Nhãn tên vị tiếng Việt rõ ràng, thẳng hàng đẹp mắt!\n\n" +
                    "Nhấn Ctrl+S để lưu Scene.",
                    "OK");
            }
        }

        private static System.Collections.Generic.List<Transform> FindAllDeep(string name)
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                FindRecursive(root.transform, name, list);
            }
            return list;
        }

        private static void FindRecursive(Transform parent, string name, System.Collections.Generic.List<Transform> list)
        {
            if (parent.name == name) list.Add(parent);
            for (int i = 0; i < parent.childCount; i++)
            {
                FindRecursive(parent.GetChild(i), name, list);
            }
        }
    }
}
