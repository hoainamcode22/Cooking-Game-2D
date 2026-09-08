using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Task #39b (vong 13, DevAI). CONG TAC TAT DUOC — Sep bam Apply de xem truoc,
/// bam Hoan Tac de tra ve nguyen trang bat cu luc nao. KHONG tu dong chay khi mo Unity.
///
/// BOI CANH: 19 prefab decor + Chauhoa_1..4 + khungtrongchauhoa_0 dang dung sprite
/// pivot GIUA (0.5,0.5) thay vi pivot DAY (0.5,0) nhu chuan du an (xem
/// PlacementManager.cs dong 284, 408). Cong cu nay dua pivot ve DAY va doi
/// BoxCollider2D.m_Offset cho khop.
///
/// RUI RO DA PHAT HIEN (doc ky truoc khi bam Apply):
/// 14/19 decor (xem mang TRACKED_BY_GROWTH ben duoi) co mat trong
/// Assets/_Game/Resources/DecorGrowthConfig.asset. Luc xay dung,
/// DecorGrowthBootstrap.OnDecorPlaced() se AddComponent&lt;DecorGrowthController&gt;
/// va doi SpriteRenderer.sprite sang anh khac nam o
/// Assets/Art/Decor/Stages/&lt;ten&gt;/stage_1..5.png (KHONG PHAI anh dang bake trong
/// prefab). 75 file stage_1..5 do VAN con pivot GIUA (chua sua). Nghia la voi
/// 14 muc nay, bam Apply chi sua duoc anh "truoc khi xay" — sau khi cong trinh
/// xay xong, sprite se bi DecorGrowthController ghi de bang anh Stages pivot GIUA,
/// va vat se "nhay" vi tri. Muon het RUI RO HOAN TOAN cho 14 muc nay phai sua
/// them 75 file stage_1..5.png.meta trong Assets/Art/Decor/Stages/ — NGOAI PHAM VI
/// task nay, DevAI CHUA dung vao (can Tech Lead duyet rieng vi anh huong ca
/// he thong xay dung dang chay that).
/// 5 muc con lai (Bang hieu, Ghe vai, Ho ca, Meo vui ve, Vit) + Chauhoa_1..4 +
/// khungtrongchauhoa_0 KHONG co trong DecorGrowthConfig => an toan, Apply la du.
/// </summary>
public static class DecorPivotAnchorTool
{
    private const string ROOT = "Assets/_Game/Farm/CÔNG TRÌNH/";
    private const string UNDO_GROUP = "Decor Pivot Anchor Tool";

    private class Target
    {
        public string label;
        public string prefabFile;      // ten file trong ROOT
        public string spriteMode;      // "single" (spriteMode 1, spritePivot cap cao nhat)
                                        // hoac "multi" (spriteMode 2, spriteSheet.sprites[0])
        public float colliderOffsetY;  // gia tri MOI cho BoxCollider2D.m_Offset.y (local units)
        public bool trackedByGrowth;   // co trong DecorGrowthConfig.asset khong

        public Target(string label, string prefabFile, string spriteMode, float colliderOffsetY, bool trackedByGrowth)
        {
            this.label = label;
            this.prefabFile = prefabFile;
            this.spriteMode = spriteMode;
            this.colliderOffsetY = colliderOffsetY;
            this.trackedByGrowth = trackedByGrowth;
        }
    }

    // Cot/gia tri lay tu bang audit vong 13 (DevAI, 2026-09-07). colliderOffsetY =
    // "can dich len (world)" / 100 — rieng khungtrongchauhoa_0 chia 200 vi rootScale 200.
    private static readonly Target[] TARGETS = new[]
    {
        new Target("Bù nhìn 1",            "Bù nhìn 1.prefab",            "multi", 2.82f,  true),
        new Target("Bảng hiệu",            "Bảng hiệu.prefab",            "multi", 2.435f, false),
        new Target("Chân Hoa",             "Chân Hoa.prefab",             "multi", 2.975f, true),
        new Target("Decor_binhtuoihoa",    "Decor_binhtuoihoa.prefab",    "single",2.375f, true),
        new Target("Decor_chaucaythu",     "Decor_chaucaythu.prefab",     "single",2.415f, true),
        new Target("Decor_chulun",         "Decor_chulun.prefab",         "single",2.345f, true),
        new Target("Decor_giabanrau",      "Decor_giabanrau.prefab",      "single",2.33f,  true),
        new Target("Ghế vải",              "Ghế vải.prefab",              "multi", 1.655f, false),
        new Target("Heo may mắn",          "Heo may mắn.prefab",          "multi", 2.22f,  true),
        new Target("Hồ cá",                "Hồ cá.prefab",                "multi", 1.765f, false),
        new Target("Mèo vui vẻ",           "Mèo vui vẻ.prefab",           "multi", 2.34f,  false),
        new Target("Rơm",                  "Rơm.prefab",                  "multi", 2.02f,  true),
        new Target("Vòng hoa",             "Vòng hoa.prefab",             "multi", 3.01f,  true),
        new Target("Vịt",                  "Vịt.prefab",                  "multi", 1.745f, false),
        new Target("cối xoay gió",         "cối xoay gió.prefab",         "multi", 2.985f, true),
        new Target("cột đèn",              "cột đèn.prefab",              "multi", 2.815f, true),
        new Target("giếng_01",             "giếng_01.prefab",             "multi", 2.305f, true),
        new Target("xe hoa",               "xe hoa.prefab",               "multi", 2.13f,  true),
        new Target("Đài nước",             "Đài nước.prefab",             "multi", 2.415f, true),
        new Target("khungtrongchauhoa_0",  "khungtrongchauhoa_0.prefab",  "multi", 1.89f,  false),
    };

    private static readonly string[] CHAUHOA_FILES =
    {
        "Chauhoa_1.prefab", "Chauhoa_2.prefab", "Chauhoa_3.prefab", "Chauhoa_4.prefab"
    };
    private const float CHAUHOA_GROUND_Y = 138f; // world/local units (root scale = 1)

    [MenuItem("Tools/Farm Game/13. Neo art decor xuong chan o")]
    public static void ApplyAll()
    {
        int ok = 0, fail = 0;
        var log = new List<string>();

        foreach (var t in TARGETS)
        {
            try
            {
                string prefabPath = ROOT + t.prefabFile;
                bool changedSprite = SetSpritePivotBottomCenter(prefabPath, t.spriteMode, out string spriteOld, out string spriteNew, out string spritePath);
                bool changedCollider = SetRootColliderOffsetY(prefabPath, t.colliderOffsetY, out float colOld);

                string risk = t.trackedByGrowth
                    ? "CANH BAO: co trong DecorGrowthConfig — sau khi xay xong sprite se bi doi sang Assets/Art/Decor/Stages/ (pivot GIUA, CHUA SUA)."
                    : "an toan (khong bi DecorGrowthController doi sprite).";

                log.Add($"{t.label}: sprite {spriteOld} -> {spriteNew} ({spritePath}); collider offset.y {colOld} -> {t.colliderOffsetY}. {risk}");
                if (changedSprite || changedCollider) ok++;
            }
            catch (System.Exception e)
            {
                log.Add($"{t.label}: LOI - {e.Message}");
                fail++;
            }
        }

        foreach (var f in CHAUHOA_FILES)
        {
            try
            {
                string prefabPath = ROOT + f;
                bool changed = SetChildLocalPositionY(prefabPath, "GroundSprite", CHAUHOA_GROUND_Y, out float oldY);
                log.Add($"{f}: GroundSprite.localPosition.y {oldY} -> {CHAUHOA_GROUND_Y}. CHUA tu dong sua collider — Chauhoa co 2 BoxCollider2D voi ty le khac nhau, DevAI chua xac dinh chac chan cai nao la occupancy o vong 13, Sep/Tech Lead kiem tra thu cong truoc khi ban.");
                if (changed) ok++;
            }
            catch (System.Exception e)
            {
                log.Add($"{f}: LOI - {e.Message}");
                fail++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DecorPivotAnchorTool] Da xu ly {ok} muc, {fail} loi. Chi tiet:\n" + string.Join("\n", log));
        EditorUtility.DisplayDialog("Neo art decor xuong chan o",
            $"Da chay xong. {ok} muc OK, {fail} loi.\nXem chi tiet + canh bao rui ro trong Console (Debug.Log).\n" +
            "LUU Y: 14 muc bi DecorGrowthController quan ly se van 'nhay' vi tri sau khi xay xong, " +
            "vi anh Stages/ chua duoc sua trong lan nay. Dung menu Hoan Tac neu can quay lai.",
            "OK");
    }

    [MenuItem("Tools/Farm Game/Hoan tac neo art decor")]
    public static void UndoAll()
    {
        int ok = 0, fail = 0;
        var log = new List<string>();

        foreach (var t in TARGETS)
        {
            try
            {
                string prefabPath = ROOT + t.prefabFile;
                SetSpritePivotCenter(prefabPath, t.spriteMode, out string spriteOld, out string spriteNew, out string spritePath);
                SetRootColliderOffsetY(prefabPath, 0f, out float colOld);
                log.Add($"{t.label}: sprite {spriteOld} -> {spriteNew} ({spritePath}); collider offset.y {colOld} -> 0.");
                ok++;
            }
            catch (System.Exception e)
            {
                log.Add($"{t.label}: LOI - {e.Message}");
                fail++;
            }
        }

        foreach (var f in CHAUHOA_FILES)
        {
            try
            {
                string prefabPath = ROOT + f;
                SetChildLocalPositionY(prefabPath, "GroundSprite", 0f, out float oldY);
                log.Add($"{f}: GroundSprite.localPosition.y {oldY} -> 0.");
                ok++;
            }
            catch (System.Exception e)
            {
                log.Add($"{f}: LOI - {e.Message}");
                fail++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DecorPivotAnchorTool] HOAN TAC xong. {ok} muc OK, {fail} loi. Chi tiet:\n" + string.Join("\n", log));
        EditorUtility.DisplayDialog("Hoan tac neo art decor",
            $"Da tra ve nguyen trang. {ok} muc OK, {fail} loi. Xem Console de biet chi tiet.", "OK");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string FindSpriteTexturePath(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var sr = root.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null) return null;
            return AssetDatabase.GetAssetPath(sr.sprite);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool SetSpritePivotBottomCenter(string prefabPath, string mode, out string oldVal, out string newVal, out string texPath)
    {
        texPath = FindSpriteTexturePath(prefabPath);
        oldVal = newVal = "(?)";
        if (string.IsNullOrEmpty(texPath)) return false;

        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer == null) return false;

        if (mode == "single")
        {
            oldVal = $"pivot={importer.spritePivot}";
            importer.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.spritePivot = new Vector2(0.5f, 0f);
            newVal = "pivot=(0.5, 0)";
        }
        else
        {
            var sheet = importer.spritesheet;
            if (sheet == null || sheet.Length == 0) return false;
            oldVal = $"alignment={sheet[0].alignment} pivot={sheet[0].pivot}";
            sheet[0].alignment = (int)SpriteAlignment.Custom;
            sheet[0].pivot = new Vector2(0.5f, 0f);
            importer.spritesheet = sheet;
            newVal = "alignment=Custom pivot=(0.5, 0)";
        }
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return true;
    }

    private static bool SetSpritePivotCenter(string prefabPath, string mode, out string oldVal, out string newVal, out string texPath)
    {
        texPath = FindSpriteTexturePath(prefabPath);
        oldVal = newVal = "(?)";
        if (string.IsNullOrEmpty(texPath)) return false;

        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer == null) return false;

        if (mode == "single")
        {
            oldVal = $"pivot={importer.spritePivot}";
            importer.spriteAlignment = (int)SpriteAlignment.Center;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            newVal = "pivot=(0.5, 0.5)";
        }
        else
        {
            var sheet = importer.spritesheet;
            if (sheet == null || sheet.Length == 0) return false;
            oldVal = $"alignment={sheet[0].alignment} pivot={sheet[0].pivot}";
            sheet[0].alignment = (int)SpriteAlignment.Center;
            sheet[0].pivot = new Vector2(0.5f, 0.5f);
            importer.spritesheet = sheet;
            newVal = "alignment=Center pivot=(0.5, 0.5)";
        }
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return true;
    }

    private static bool SetRootColliderOffsetY(string prefabPath, float newY, out float oldY)
    {
        oldY = 0f;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var box = root.GetComponent<BoxCollider2D>();
            if (box == null) box = root.GetComponentInChildren<BoxCollider2D>(true);
            if (box == null) return false;

            oldY = box.offset.y;
            box.offset = new Vector2(box.offset.x, newY);
            EditorUtility.SetDirty(box);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool SetChildLocalPositionY(string prefabPath, string childName, float newY, out float oldY)
    {
        oldY = 0f;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform child = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == childName) { child = t; break; }
            }
            if (child == null) return false;

            oldY = child.localPosition.y;
            var p = child.localPosition;
            p.y = newY;
            child.localPosition = p;
            EditorUtility.SetDirty(child);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
