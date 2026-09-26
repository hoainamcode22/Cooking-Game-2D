// ============================================================================
//  Tools > Farm Game > Dung Cu (Riu Keo Bua)   (2026-09-25)
//  1. Tao / cap nhat config: Resources/WorldClearConfig.asset, lay icon Rìu / Kéo / Búa tu ToolData (tab Cong cu cua Shop).
//  2. Dung khay dung cu vao Hierarchy (Canvas_HUD/WorldClearTray) de Sep keo chinh tay. Co Undo. Xong Ctrl+S.
//  3. Rai 6 tang da tu nhien (art HappyHarvest Rocks) quanh GIUA Scene view -> Sep keo vao cho muon. Co Undo.
//  4. Xoa tien do chat / cat / dap (test lai tu dau).
//  5. [Play] Tang 5 cai moi dung cu vao kho (test).
//  Khong chay menu nao van choi duoc: config mac dinh + khay tu dung luc Play.
// ============================================================================
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldClearSetupTool
{
    private const string Goc = "Tools/Farm Game/Dung Cu (Riu Keo Bua)/";
    private const string DuongConfig = "Assets/_Game/Resources/WorldClearConfig.asset";

    [MenuItem(Goc + "1. Tao - cap nhat config (icon tu Shop)", false, 1)]
    private static void TaoConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<WorldClearConfig>(DuongConfig);
        if (cfg == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DuongConfig));
            cfg = ScriptableObject.CreateInstance<WorldClearConfig>();
            AssetDatabase.CreateAsset(cfg, DuongConfig);
        }
        int co = 0;
        foreach (var g in AssetDatabase.FindAssets("t:ToolData"))
        {
            var td = AssetDatabase.LoadAssetAtPath<ToolData>(AssetDatabase.GUIDToAssetPath(g));
            if (td == null || td.itemIcon == null) continue;
            foreach (var l in cfg.loai)
                if (l != null && l.toolItemId == td.itemID) { l.icon = td.itemIcon; co++; }
        }
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        Selection.activeObject = cfg;
        EditorUtility.DisplayDialog("Dung cu", $"Config: {DuongConfig}\nDa gan {co}/3 icon dung cu tu ToolData.\n\nChinh thoi gian / phan thuong / ten vat trong Inspector.", "OK");
    }

    [MenuItem(Goc + "2. Dung khay dung cu vao Hierarchy", false, 2)]
    private static void DungKhay()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Dung cu", "Thoat Play mode truoc.", "OK"); return; }
        foreach (var t in Resources.FindObjectsOfTypeAll<WorldClearTrayUI>())
            if (t != null && !EditorUtility.IsPersistent(t) && t.gameObject.scene.IsValid())
            {
                Selection.activeObject = t.gameObject;
                EditorGUIUtility.PingObject(t.gameObject);
                EditorUtility.DisplayDialog("Dung cu", "Khay da co san trong Hierarchy (da chon). Muon dung lai thi xoa no truoc.", "OK");
                return;
            }
        var hud = GameObject.Find("Canvas_HUD");
        if (hud == null || hud.GetComponent<Canvas>() == null)
        {
            EditorUtility.DisplayDialog("Dung cu", "Khong thay Canvas_HUD (mo SCN_Farm roi bam lai).", "OK");
            return;
        }
        var tray = WorldClearTrayUI.Dung(hud.transform);
        Undo.RegisterCreatedObjectUndo(tray.gameObject, "Dung khay dung cu");
        EditorSceneManager.MarkSceneDirty(hud.scene);
        Selection.activeObject = tray.gameObject;
        EditorUtility.DisplayDialog("Dung cu", "Da dung Canvas_HUD/WorldClearTray (dang tat, luc Play tu bat khi cham cay).\nBam Ctrl+S de luu.", "OK");
    }

    [MenuItem(Goc + "3. Rai 6 tang da quanh giua Scene view", false, 3)]
    private static void RaiDa()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Dung cu", "Thoat Play mode truoc.", "OK"); return; }
        string[] ten = { "Prefab_RocksBig", "Prefab_RockMedium", "Prefab_RocksSmall" };
        var prefabs = new GameObject[ten.Length];
        for (int i = 0; i < ten.Length; i++)
            foreach (var g in AssetDatabase.FindAssets(ten[i] + " t:Prefab"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == ten[i]) { prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(p); break; }
            }
        if (prefabs[0] == null && prefabs[1] == null && prefabs[2] == null)
        {
            EditorUtility.DisplayDialog("Dung cu", "Khong thay prefab da (HappyHarvest Rocks).", "OK");
            return;
        }
        var sv = SceneView.lastActiveSceneView;
        Vector3 tam = sv != null ? sv.pivot : Vector3.zero; tam.z = 0f;
        var cha = GameObject.Find("WorldClear_Rocks");
        if (cha == null) { cha = new GameObject("WorldClear_Rocks"); Undo.RegisterCreatedObjectUndo(cha, "Rai da"); }
        int n = 0;
        for (int i = 0; i < 6; i++)
        {
            var pf = prefabs[i % 3] ?? prefabs[0] ?? prefabs[1] ?? prefabs[2];
            if (pf == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, cha.scene);
            go.transform.SetParent(cha.transform, true);
            float goc = i / 6f * Mathf.PI * 2f;
            go.transform.position = tam + new Vector3(Mathf.Cos(goc) * 260f, Mathf.Sin(goc) * 130f, 0f);
            go.name = pf.name + " (" + i + ")";
            Undo.RegisterCreatedObjectUndo(go, "Rai da");
            n++;
        }
        EditorSceneManager.MarkSceneDirty(cha.scene);
        Selection.activeObject = cha;
        EditorUtility.DisplayDialog("Dung cu", $"Da rai {n} tang da quanh giua Scene view (cha: WorldClear_Rocks).\nKeo tung cuc vao cho muon (trong khu dat da mo) roi Ctrl+S.\nTen bat dau 'Prefab_Rock...' nen Bua dap duoc.", "OK");
    }

    [MenuItem(Goc + "4. Xoa tien do chat - cat - dap (test lai)", false, 4)]
    private static void XoaSave()
    {
        if (!EditorUtility.DisplayDialog("Dung cu", "Xoa het tien do? Cay / bui / da da don se hien lai o lan Play sau.", "Xoa", "Thoi")) return;
        int n = WorldClearManager.XoaSaveTatCa();
        Debug.Log($"[WorldClear] Da xoa tien do cua {n} vat.");
    }

    [MenuItem(Goc + "5. [Play] Tang 5 moi dung cu vao kho (test)", false, 5)]
    private static void TangDungCu()
    {
        var inv = FarmInventoryManager.Instance;
        if (!Application.isPlaying || inv == null) { EditorUtility.DisplayDialog("Dung cu", "Bam Play truoc.", "OK"); return; }
        inv.AddItem("tool_axe", 5);
        inv.AddItem("tool_scissors", 5);
        inv.AddItem("tool_hammer", 5);
        Debug.Log("[WorldClear] +5 Rìu, +5 Kéo, +5 Búa.");
    }
}
