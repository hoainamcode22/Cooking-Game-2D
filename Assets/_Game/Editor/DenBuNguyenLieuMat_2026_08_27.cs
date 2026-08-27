using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ĐỀN BÙ 1 LẦN — nguyên liệu bị "bốc hơi" ngày 2026-08-27 do bug reentrancy trong
/// WarehousePopupUI.OnTransferKitchenClicked (chuyển MAX → RemoveItem gọi ngược RefreshUI
/// → selectedItemId bị xoá trước khi kịp cộng vào bếp; đã fix cùng ngày).
///
/// Số lượng lấy ĐÚNG từ console log phiên test của Sếp:
///   6x cachua · 5x bapcai · 9x rice · 11x ngo · 21x egg · 1x beef · 10x pork
/// Cộng thẳng vào kho bếp (PlayerPrefs KITCHEN_TRANSFER_SAVE — đúng format
/// TransferSaveData của KitchenTransferManager, saveVersion 1).
///
/// Cách dùng: DỪNG Play → menu Tools/Farm/Đền bù nguyên liệu mất (27-08) → Play lại,
/// mở bếp là thấy hàng. Chỉ chạy được 1 lần (có khoá chống bấm đúp).
/// </summary>
public static class DenBuNguyenLieuMat_2026_08_27
{
    private const string SaveKey = "KITCHEN_TRANSFER_SAVE";
    private const string DoneKey = "DEN_BU_2026_08_27_DA_CHAY";

    [Serializable] private class Entry { public string itemId; public int amount; }
    [Serializable] private class SaveData { public int saveVersion; public List<Entry> entries = new List<Entry>(); }

    [MenuItem("Tools/Farm/Đền bù nguyên liệu mất (27-08)")]
    private static void DenBu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Đền bù", "Dừng Play trước rồi chạy lại menu này (để không bị save đè).", "OK");
            return;
        }
        if (PlayerPrefs.GetInt(DoneKey, 0) == 1)
        {
            EditorUtility.DisplayDialog("Đền bù", "Đã đền bù trước đó rồi — không cộng lần 2.", "OK");
            return;
        }

        var boiThuong = new Dictionary<string, int>
        {
            { "cachua", 6 }, { "bapcai", 5 }, { "rice", 9 }, { "ngo", 11 },
            { "egg", 21 }, { "beef", 1 }, { "pork", 10 },
        };

        SaveData data = null;
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { data = JsonUtility.FromJson<SaveData>(json); } catch { data = null; }
        }
        if (data == null) data = new SaveData();
        if (data.entries == null) data.entries = new List<Entry>();
        data.saveVersion = Mathf.Max(data.saveVersion, 1);

        foreach (var kv in boiThuong)
        {
            Entry e = data.entries.Find(x => x != null && x.itemId == kv.Key);
            if (e != null) e.amount += kv.Value;
            else data.entries.Add(new Entry { itemId = kv.Key, amount = kv.Value });
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();

        Debug.Log("[ĐềnBù 27-08] Đã cộng vào kho bếp: 6 cà chua, 5 bắp cải, 9 lúa, 11 ngô, 21 trứng, 1 thịt bò, 10 thịt heo. Play lại và mở bếp để kiểm tra.");
        EditorUtility.DisplayDialog("Đền bù xong",
            "Đã cộng vào kho bếp:\n6 cà chua · 5 bắp cải · 9 lúa · 11 ngô · 21 trứng · 1 thịt bò · 10 thịt heo\n\nPlay lại và mở bếp để kiểm tra.", "OK");
    }
}
