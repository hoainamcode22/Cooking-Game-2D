#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TẠO NÚT ĐÓNG "CHỈNH TAY" cho POPUP GỘP (tab Nhiệm vụ · Điểm danh · Thành tựu).
///
/// VÌ SAO CẦN: UnifiedTaskPopupRoot trong scene KHÔNG có con nào — toàn bộ popup do code
/// dựng lúc Play rồi tự huỷ ở lần dựng sau. Vì vậy ngoài Edit mode Sếp không có gì để kéo.
/// Tool này đặt sẵn MỘT nút đóng THẬT trong scene và gắn vào ô <c>btnCloseChinhTay</c>;
/// từ đó UnifiedTaskPopupUI (bản vá vòng 6) chỉ nối onClick và CHỪA NGUYÊN nhánh này khi
/// dọn cây — nghĩa là mọi chỉnh tay của Sếp (vị trí, cỡ, sprite, màu) giữ nguyên khi Play.
///
/// TÍNH CHẤT: KHÔNG PHÁ. Tool chỉ TẠO khi chưa có, hoặc DÙNG LẠI nút sẵn có.
/// Không bao giờ ghi đè sizeDelta / anchoredPosition / localScale / sprite / color của nút
/// đã tồn tại. Chạy lại nhiều lần cũng không nhân đôi.
///
/// ⚠ ĐÂY KHÔNG PHẢI "Tools/Farm/UI/Dong bo nut dong - 3. APPLY" — menu ĐÓ ép mọi nút đóng
/// trong scene về 64x64 + sprite mặc định và xoá sạch chỉnh tay. Đừng bấm menu đó.
///
/// Menu: Tools/Farm Game/Nut dong CHINH TAY - Popup Nhiem vu
/// </summary>
public static class NutDongChinhTayTool
{
    private const string MENU   = "Tools/Farm Game/Nut dong CHINH TAY - Popup Nhiem vu";
    private const string TenNut = "Btn_Close_ChinhTay";

    [MenuItem(MENU, false, 40)]
    public static void TaoNutDongChinhTay()
    {
        var popup = Object.FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Nut dong chinh tay",
                "Không thấy UnifiedTaskPopupUI trong scene đang mở.\n\n" +
                "Hãy mở SCN_Farm, hoặc chạy Tools/Farm Game/Setup Unified Task Popup trước.", "OK");
            return;
        }

        RectTransform root = popup.GetComponent<RectTransform>();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Nut dong chinh tay",
                "UnifiedTaskPopupRoot thiếu RectTransform — không dựng được nút.", "OK");
            return;
        }

        var so   = new SerializedObject(popup);
        var pGiu = so.FindProperty("giuNutCloseChinhTay");
        var pNut = so.FindProperty("btnCloseChinhTay");
        if (pNut == null)
        {
            EditorUtility.DisplayDialog("Nut dong chinh tay",
                "Bản UnifiedTaskPopupUI.cs này chưa có ô 'btnCloseChinhTay'.\n" +
                "Báo Lead cập nhật code trước khi chạy tool.", "OK");
            return;
        }

        // Cờ giữ chỉnh tay phải BẬT, nếu không code vẫn đi đường dựng lại theo số cứng.
        if (pGiu != null && !pGiu.boolValue) pGiu.boolValue = true;

        Button nut = pNut.objectReferenceValue as Button;
        string tinhTrang;

        if (nut != null)
        {
            tinhTrang = "Ô btnCloseChinhTay đã có sẵn: " + nut.name + " — tool KHÔNG đụng gì.";
        }
        else
        {
            nut = TimNutCoSan(root);
            if (nut != null)
            {
                tinhTrang = "Đã dùng lại nút có sẵn trong scene: " + nut.name +
                            " (giữ nguyên vị trí / cỡ / sprite / màu).";
            }
            else
            {
                nut = DungNutMoi(root);
                tinhTrang = "Đã tạo mới " + TenNut + " ở góc phải trên tấm ván.";
            }
        }

        pNut.objectReferenceValue = nut;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(popup);
        EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);
        Selection.activeGameObject = nut.gameObject;

        {
            Debug.Log("[NutDongChinhTay] " + tinhTrang);
        }

        EditorUtility.DisplayDialog("Nut dong chinh tay",
            tinhTrang + "\n\n" +
            "BÂY GIỜ SẾP LÀM GÌ:\n" +
            "1. Nút đang được chọn sẵn trong Hierarchy — kéo / chỉnh Width-Height / đổi Source Image\n" +
            "   cho tới khi ưng mắt.\n" +
            "2. Ctrl + S để lưu scene.\n" +
            "3. Bấm Play — nút giữ NGUYÊN như vừa chỉnh (code chỉ nối sự kiện bấm).\n\n" +
            "TUYỆT ĐỐI KHÔNG bấm Tools/Farm/UI/'Dong bo nut dong - 3. APPLY' — menu đó ép mọi\n" +
            "nút đóng về 64x64 + sprite mặc định và xoá sạch công chỉnh tay.", "OK");
    }

    /// <summary>Nút đóng ĐÃ CÓ nằm ngay dưới root (cùng luật tên với UnifiedTaskPopupUI).</summary>
    private static Button TimNutCoSan(RectTransform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform con = root.GetChild(i);
            Button b = con.GetComponent<Button>();
            if (b == null) continue;

            string ten = b.name.Replace("_", string.Empty).ToLowerInvariant();
            if (ten == "btnclose" || ten == "close" || ten == "btnx" || ten == "btndong" ||
                ten == "btnclosechinhtay")
                return b;
        }
        return null;
    }

    /// <summary>
    /// Dựng nút theo ĐÚNG chuẩn nút đóng chung của game: 64x64, sprite qua UIStandardSprites
    /// (cấm AssetDatabase.LoadAssetAtPath trực tiếp — đường đó chết trong bản build),
    /// và chữ "X" LUÔN hiện vì btn_red_small là thanh đỏ TRƠN, không có dấu X vẽ sẵn.
    /// Đặt ở góc phải trên tấm ván, cùng toạ độ đường dự phòng của UnifiedTaskPopupUI.
    /// </summary>
    private static Button DungNutMoi(RectTransform root)
    {
        Vector2 kt = UIStandardSprites.CloseSize;
        if (kt.x < 1f || kt.y < 1f) kt = new Vector2(64f, 64f);

        var go = new GameObject(TenNut, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Tao nut dong chinh tay");

        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = kt;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = new Vector2(TaskPopupDesign.BangRong * 0.5f + 32f - kt.x * 0.5f,
                                          TaskPopupDesign.BangCao  * 0.5f + 34f - kt.y * 0.5f);

        Image anh = go.AddComponent<Image>();
        Sprite spr = UIStandardSprites.Close;
        if (spr != null)
        {
            anh.sprite = spr;
            anh.type   = Image.Type.Sliced;
            anh.color  = Color.white;
        }
        else
        {
            anh.color = new Color32(239, 75, 51, 255);
        }
        anh.preserveAspect = false;   // Image.Type.Sliced bỏ qua preserveAspect, bật chỉ làm méo
        anh.raycastTarget  = true;

        Button nut = go.AddComponent<Button>();
        nut.targetGraphic = anh;

        var goX = new GameObject("Txt_X", typeof(RectTransform));
        var rtX = (RectTransform)goX.transform;
        rtX.SetParent(rt, false);
        rtX.anchorMin = rtX.anchorMax = rtX.pivot = new Vector2(0.5f, 0.5f);
        rtX.sizeDelta = kt;
        rtX.anchoredPosition = Vector2.zero;

        var chuX = goX.AddComponent<TextMeshProUGUI>();
        chuX.text          = "X";
        chuX.fontSize      = UIStandardSprites.CloseGlyphSize;
        chuX.fontStyle     = FontStyles.Bold;
        chuX.alignment     = TextAlignmentOptions.Center;
        chuX.color         = Color.white;
        chuX.raycastTarget = false;
        goX.SetActive(true);

        rt.SetAsLastSibling();
        return nut;
    }
}
#endif
