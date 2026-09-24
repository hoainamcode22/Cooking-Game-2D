// ============================================================================
//  TmpVuaKhung — chu DAI (vd tieng Anh dai hon tieng Viet) tu CO NHO lai vua khung (2026-09-24)
//  Giu nguyen co chu Sep dat o Edit mode lam co TOI DA; chi khi chu tran khung TMP moi tu co
//  (toi thieu 60%). Chu vua khung thi khong doi gi -> bo cuc chinh tay van y nguyen.
//  Moi o chu chi xu ly 1 lan (nho theo instanceID) -> quet lai dinh ky rat re.
// ============================================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class TmpVuaKhung
{
    private static readonly HashSet<int> _daXuLy = new HashSet<int>();
    private static readonly List<TMP_Text> _tam = new List<TMP_Text>(256);
    public const float TI_LE_NHO_NHAT = 0.6f;

    /// <summary>Ap dung cho moi o chu TMP duoi goc (bo qua lop hieu ung "Fx_*").</summary>
    public static int ApDung(Transform goc)
    {
        if (goc == null) return 0;
        _tam.Clear();
        goc.GetComponentsInChildren(true, _tam);
        int n = 0;
        for (int i = 0; i < _tam.Count; i++)
            if (ApDung(_tam[i])) n++;
        _tam.Clear();
        return n;
    }

    public static bool ApDung(TMP_Text t)
    {
        if (t == null) return false;
        int id = t.GetInstanceID();
        if (_daXuLy.Contains(id)) return false;
        _daXuLy.Add(id);
        if (LaFx(t.transform)) return false;

        float co = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
        if (co <= 0f) return false;
        float nho = Mathf.Max(9f, co * TI_LE_NHO_NHAT);
        if (t.enableAutoSizing && t.fontSizeMin <= nho) return false;   // da tu co san, khong dong vao
        t.fontSizeMax = co;
        t.fontSizeMin = nho;
        t.enableAutoSizing = true;
        return true;
    }

    private static bool LaFx(Transform t)
    {
        for (int k = 0; k < 4 && t != null; k++, t = t.parent)
            if (t.name.StartsWith("Fx_")) return true;
        return false;
    }
}
