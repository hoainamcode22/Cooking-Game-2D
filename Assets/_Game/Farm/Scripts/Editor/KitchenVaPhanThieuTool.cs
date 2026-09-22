// ============================================================================
//  KITCHEN: VA PHAN THIEU  (Tools > Farm Game > Kitchen: Va phan thieu)
// ----------------------------------------------------------------------------
//  VAN DE (22/09/2026): scene SampleScene bi luu de mat 'Tray' (khay nguyen lieu
//  + gia vi), 'Btn_Action', 'Btn_BackFarm', 'Cat_Chef'. Vi 'Order_Banner' van
//  con nen Start() luon chon nhanh BindExistingHierarchy() — nhanh nay CHI NOI
//  tham chieu chu khong dung gi — nen khay va nut khong bao gio quay lai, ke ca
//  trong Play mode.
//
//  TOOL NAY KHAC HAN tool "Dong bang": no KHONG goi RebuildNow(), KHONG xoa mot
//  GameObject nao. No chi chay ham Build cua DUNG NHUNG NHANH DANG KHONG TON TAI.
//  Moi thu Sep da keo tha bang tay deu giu nguyen.
//
//  CO UNDO: bam sai thi Ctrl+Z tra lai nguyen trang.
//  Chay xong Ctrl+S mot lan la luu vinh vien vao scene.
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenVaPhanThieuTool
{
    private const string MENU = "Tools/Farm Game/Kitchen: Va phan thieu (KHONG xoa gi)";

    [MenuItem(MENU)]
    public static void Va()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Kitchen",
                "Tool nay danh cho EDIT MODE. Hay thoat Play roi bam lai.", "OK");
            return;
        }

        var ui = Object.FindFirstObjectByType<KitchenUIv2.KitchenSceneV2UI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Kitchen",
                "Khong tim thay KitchenSceneV2UI trong scene dang mo.\n" +
                "Hay mo Assets/_Game/Scenes/SampleScene.unity roi bam lai.", "OK");
            return;
        }

        if (!ui.gameObject.activeSelf)
        {
            Undo.RecordObject(ui.gameObject, "Bat Kitchen_UI_v2");
            ui.gameObject.SetActive(true);
        }

        // LOP AN TOAN 1: bat Sep luu truoc, roi COPY nguyen file scene ra thu muc backup.
        // Ly do: Undo cua Unity co the truot (doi scene, compile lai, Unity crash). Mot ban
        // copy tren dia thi khong bao gio truot.
        var scene = ui.gameObject.scene;
        if (scene.isDirty)
        {
            int chon = EditorUtility.DisplayDialogComplex("Kitchen: va phan thieu",
                "Scene dang co thay doi CHUA LUU (cong can chinh tay cua Sep).\n\n" +
                "Phai luu truoc thi tool moi sao luu duoc file scene ra dia.",
                "Luu roi chay tool", "Huy", "Chay luon (khong sao luu)");
            if (chon == 1) return;
            if (chon == 0) EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(scene.path))
        {
            try
            {
                string thuMuc = "_Backup_Scene_TruocKhiVa";
                System.IO.Directory.CreateDirectory(thuMuc);
                string dich = System.IO.Path.Combine(thuMuc,
                    System.IO.Path.GetFileNameWithoutExtension(scene.path) + "_" +
                    System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
                System.IO.File.Copy(scene.path, dich, true);
                Debug.Log("[KitchenVaPhanThieu] Da sao luu scene -> " + dich);
            }
            catch (System.Exception e)
            {
                if (!EditorUtility.DisplayDialog("Kitchen",
                        "KHONG sao luu duoc file scene:\n" + e.Message +
                        "\n\nVan chay tool chu?", "Chay", "Huy"))
                    return;
            }
        }

        // Undo cho CA CAY con — bam nham thi Ctrl+Z la ve nhu cu.
        Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Kitchen: va phan thieu");

        string ketQua;
        try
        {
            ketQua = ui.VaPhanThieuChoEditor();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e, ui);
            EditorUtility.DisplayDialog("Kitchen",
                "Va that bai — xem Console. Scene CHUA duoc luu nen Sep co the Ctrl+Z\n" +
                "hoac File > Open Scene > Don't Save de quay lai nguyen trang.\n\n" + e.Message, "OK");
            return;
        }

        // Don rac: "~LocRuntimeInterceptor" duoc tao luc CHAY (DontDestroyOnLoad + HideInHierarchy).
        // Neu scene bi luu trong luc dang Play thi no bi ghi thang vao file scene. SampleScene
        // dang giu 3 ban nhu vay; luc chay sinh them 1 ban nua = 4 Update cung quet toan bo
        // TMP_Text moi nhip. Xoa het, ban that se tu sinh lai khi Play.
        int soRac = 0;
        foreach (var go in ui.gameObject.scene.GetRootGameObjects())
        {
            if (go != null && go.name.StartsWith("~LocRuntimeInterceptor"))
            {
                Undo.DestroyObjectImmediate(go);
                soRac++;
            }
        }
        if (soRac > 0) ketQua += "\nDa xoa " + soRac + " ban sao thua cua ~LocRuntimeInterceptor.";

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);

        Debug.Log("[KitchenVaPhanThieu] " + ketQua, ui);
        EditorUtility.DisplayDialog("Kitchen: va phan thieu",
            ketQua + "\n\nBAY GIO BAM Ctrl+S DE LUU VINH VIEN.\n" +
            "Khong vua y thi Ctrl+Z (chua luu thi khong mat gi).", "OK");

        Selection.activeGameObject = ui.gameObject;
        EditorGUIUtility.PingObject(ui.gameObject);
    }
}
