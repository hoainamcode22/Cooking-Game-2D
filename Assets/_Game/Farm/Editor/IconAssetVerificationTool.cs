#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class IconAssetVerificationTool
{
    [InitializeOnLoadMethod]
    private static void OnProjectLoaded()
    {
        EditorApplication.delayCall += RunFullVerification;
    }

    [MenuItem("Tools/Farm Game/Icons/★ KIỂM TRA VÀ GÁN ICON THẬT VÀO TOÀN BỘ ASSETS", false, 1)]
    public static void RunFullVerification()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== BÁO CÁO KIỂM TRA ICON TOÀN BỘ DỰ ÁN ===");
        report.AppendLine($"Thời gian: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        int importedCount = 0;
        int errorCount = 0;

        // 1. REIMPORT ALL PNGs AS SPRITES
        string[] folders = new string[]
        {
            "Assets/Art/UI/Icons/Daily",
            "Assets/Art/UI/Icons/Missions",
            "Assets/Art/UI/Icons/Achievements",
            "Assets/_Game/GeneratedUI/Mill/Icons"
        };

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            string[] files = Directory.GetFiles(folder, "*.png");
            foreach (var file in files)
            {
                string assetPath = file.Replace("\\", "/");
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (changed)
                    {
                        importer.SaveAndReimport();
                        importedCount++;
                    }

                    // Verify Sprite load
                    Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (spr != null)
                    {
                        report.AppendLine($"[SPRITE OK] {Path.GetFileName(assetPath)} -> Kích thước: {spr.rect.width}x{spr.rect.height}, Name: {spr.name}");
                    }
                    else
                    {
                        report.AppendLine($"[SPRITE LỖI] {Path.GetFileName(assetPath)} -> KHÔNG LOAD ĐƯỢC SPRITE!");
                        errorCount++;
                    }
                }
            }
        }

        report.AppendLine();
        report.AppendLine($"Đã cấu hình & kiểm tra {importedCount} textures mới.");

        // 2. CHECK & WIRE MISSIONS
        string[] missionDirs = new string[]
        {
            "Assets/_Game/Farm/data/Data_Ewa/Achievements",
            "Assets/_Game/Farm/data/Data_Ewa/Daily_Missions",
            "Assets/_Game/Farm/data/Data_Ewa/Main_L1_L10"
        };

        Sprite iconHarvest = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_harvest.png");
        Sprite iconPlant   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_plant.png");
        Sprite iconCook    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_cook.png");
        Sprite iconAnimal  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_animal.png");
        Sprite iconProcess = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_process.png");
        Sprite iconDeliver = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_deliver.png");
        Sprite iconShop    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Missions/mission_shop.png");

        Sprite achTrophy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Achievements/achievement_trophy.png");
        Sprite achLevel  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Achievements/achievement_level.png");
        Sprite achChef   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Achievements/achievement_chef.png");
        Sprite achFarmer = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/Achievements/achievement_farmer.png");

        int missionOk = 0;
        int missionFixed = 0;
        int missionNull = 0;

        foreach (var dir in missionDirs)
        {
            if (!Directory.Exists(dir)) continue;
            string[] assetFiles = Directory.GetFiles(dir, "*.asset");
            foreach (var assetPath in assetFiles)
            {
                string normPath = assetPath.Replace("\\", "/");
                MissionData data = AssetDatabase.LoadAssetAtPath<MissionData>(normPath);
                if (data != null)
                {
                    if (data.missionIcon != null)
                    {
                        missionOk++;
                    }
                    else
                    {
                        bool isAch = dir.Contains("Achievements");
                        string nameLower = (data.missionName ?? "").ToLowerInvariant();
                        string idLower = (data.missionId ?? "").ToLowerInvariant();

                        Sprite chosen = null;
                        if (isAch)
                        {
                            if (nameLower.Contains("nấu") || nameLower.Contains("bếp") || idLower.Contains("cook") || idLower.Contains("chef"))
                                chosen = achChef;
                            else if (nameLower.Contains("thu hoạch") || nameLower.Contains("trồng") || nameLower.Contains("nông dân") || idLower.Contains("harvest") || idLower.Contains("farmer"))
                                chosen = achFarmer;
                            else if (nameLower.Contains("cấp") || idLower.Contains("level") || idLower.Contains("reach"))
                                chosen = achLevel;
                            else if (nameLower.Contains("order") || idLower.Contains("deliver") || idLower.Contains("orders"))
                                chosen = iconDeliver;
                            else if (nameLower.Contains("process") || idLower.Contains("proc"))
                                chosen = iconProcess;
                            else
                                chosen = achTrophy;
                        }
                        else
                        {
                            if (data.eventType == MissionEventType.HarvestItem) chosen = iconHarvest;
                            else if (data.eventType == MissionEventType.PlantCrop) chosen = iconPlant;
                            else if (data.eventType == MissionEventType.DeliverOrder || data.eventType == MissionEventType.LoadTrainCargo) chosen = iconDeliver;
                            else if (data.eventType == MissionEventType.CookDish) chosen = iconCook;
                            else if (data.eventType == MissionEventType.FeedAnimal || data.eventType == MissionEventType.CollectAnimalProduct) chosen = iconAnimal;
                            else if (data.eventType == MissionEventType.BuyShopItem || data.eventType == MissionEventType.BuySeed || data.eventType == MissionEventType.SellAtStall) chosen = iconShop;
                            else if (data.eventType == MissionEventType.ReachLevel) chosen = achLevel;
                            else chosen = iconHarvest;
                        }

                        if (chosen != null)
                        {
                            data.missionIcon = chosen;
                            EditorUtility.SetDirty(data);
                            missionFixed++;
                        }
                        else
                        {
                            missionNull++;
                        }
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();

        report.AppendLine();
        report.AppendLine($"=== KẾT QUẢ KIỂM TRA MISSION SCRIPTABLEOBJECTS ===");
        report.AppendLine($"- Tổng Mission có Icon hợp lệ: {missionOk + missionFixed}");
        report.AppendLine($"- Mission vừa được gán trực tiếp: {missionFixed}");
        report.AppendLine($"- Mission bị null: {missionNull}");

        // 3. CHECK DAILY ICONS
        report.AppendLine();
        report.AppendLine("=== KIỂM TRA DAILY REWARD ICONS (7 NGÀY) ===");
        for (int i = 1; i <= 7; i++)
        {
            string p = $"Assets/Art/UI/Icons/Daily/daily_reward_day{i}.png";
            Sprite dSpr = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            report.AppendLine($"Ngày {i}: {(dSpr != null ? $"[OK] {dSpr.name} ({dSpr.rect.width}x{dSpr.rect.height})" : "[LỖI] Null Sprite!")}");
        }

        // 4. CHECK FEED SACKS (4 CON VẬT)
        report.AppendLine();
        report.AppendLine("=== KIỂM TRA 4 BAO CÁM VẬT NUÔI ===");
        string[] feedNames = new string[] { "feed_cam_ga.png", "feed_cam_heo.png", "feed_co_tron_bo.png", "feed_cam_bo_sua.png" };
        foreach (var fn in feedNames)
        {
            string fp = $"Assets/_Game/GeneratedUI/Mill/Icons/{fn}";
            Sprite fSpr = AssetDatabase.LoadAssetAtPath<Sprite>(fp);
            report.AppendLine($"Bao cám {fn}: {(fSpr != null ? $"[OK] {fSpr.name} ({fSpr.rect.width}x{fSpr.rect.height})" : "[LỖI] Null Sprite!")}");
        }

        string reportDir = "production";
        if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);
        string reportPath = Path.Combine(reportDir, "ICON_VERIFICATION_REPORT.txt");
        File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);

        Debug.Log("[IconAssetVerificationTool] ĐÃ HOÀN THÀNH KIỂM TRA VÀ GHI BÁO CÁO!\n" + report.ToString());
    }
}
#endif
