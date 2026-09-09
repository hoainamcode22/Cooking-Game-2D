#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools/Map45/10. Nguyen Lieu Xay Dung — dien chi phi go/da/kinh/dinh cho cong trinh.
///
/// Quy tac mac dinh (theo gia vang, cang dat cang nhieu nguyen lieu):
///     &lt;  500 vang : khong can
///     &lt; 1500      : 6 go, 4 da
///     &lt; 3000      : 12 go, 8 da, 4 dinh
///     &gt;= 3000     : 20 go, 14 da, 8 dinh, 6 kinh
/// He so nhan duoc chinh trong cua so.
/// </summary>
public class BuildMaterialCostTool : EditorWindow
{
    private class Row
    {
        public PlaceableItemData data;
        public int gold;
        public List<BuildMaterialCost> current;
        public List<BuildMaterialCost> suggest;
        public bool apply = true;
    }

    private readonly List<Row> rows = new List<Row>();
    private Vector2 scroll;
    private float multiplier = 1f;
    private bool onlyEmpty = true;

    [MenuItem("Tools/Map45/10. Nguyen Lieu Xay Dung — dien tu dong", false, 10)]
    public static void Open() => GetWindow<BuildMaterialCostTool>("Nguyen Lieu");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Dien nguyen lieu (go/da/kinh/dinh) cho cong trinh theo gia vang.\n" +
            "Nguoi choi phai chay tau lua gom du nguyen lieu moi xay duoc.",
            MessageType.Info);

        EditorGUILayout.Space();
        multiplier = EditorGUILayout.Slider("He so nhan", multiplier, 0.25f, 3f);
        onlyEmpty  = EditorGUILayout.Toggle(
            new GUIContent("Chi dien cho asset chua co", "Bo tick de ghi de ca asset da dien tay"),
            onlyEmpty);

        EditorGUILayout.Space();
        if (GUILayout.Button("Quet cong trinh", GUILayout.Height(28))) Scan();
        if (rows.Count == 0) return;

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var r in rows)
        {
            EditorGUILayout.BeginHorizontal();
            r.apply = EditorGUILayout.Toggle(r.apply, GUILayout.Width(22));
            EditorGUILayout.ObjectField(r.data, typeof(PlaceableItemData), false, GUILayout.Width(150));
            GUILayout.Label($"{r.gold:n0} vang", GUILayout.Width(90));
            GUILayout.Label(BuildMaterials.Describe(r.current), GUILayout.Width(180));
            GUILayout.Label("->", GUILayout.Width(20));
            GUILayout.Label(BuildMaterials.Describe(r.suggest));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("AP DUNG", GUILayout.Height(30))) Apply();
        GUI.backgroundColor = Color.white;
    }

    private void Scan()
    {
        rows.Clear();
        var seen = new HashSet<string>();
        foreach (var filter in new[] { "t:PlaceableItemData", "t:BuildingData", "t:DecorData" })
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                if (!seen.Add(guid)) continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(path);
                if (d == null) continue;
                if (onlyEmpty && d.RequiresMaterials) continue;

                rows.Add(new Row
                {
                    data = d,
                    gold = d.goldPrice,
                    current = new List<BuildMaterialCost>(d.materialCosts ?? new List<BuildMaterialCost>()),
                    suggest = Suggest(d.goldPrice)
                });
            }
        }
        rows.Sort((a, b) => a.gold.CompareTo(b.gold));
        Debug.Log($"[NguyenLieu] Quet duoc {rows.Count} cong trinh.");
    }

    private List<BuildMaterialCost> Suggest(int gold)
    {
        var list = new List<BuildMaterialCost>();
        void Add(string id, int n)
        {
            int v = Mathf.Max(1, Mathf.RoundToInt(n * multiplier));
            list.Add(new BuildMaterialCost(id, v));
        }
        if (gold < 500) return list;
        if (gold < 1500) { Add("go", 6);  Add("da", 4); }
        else if (gold < 3000) { Add("go", 12); Add("da", 8);  Add("dinh", 4); }
        else { Add("go", 20); Add("da", 14); Add("dinh", 8); Add("kinh", 6); }
        return list;
    }

    private void Apply()
    {
        int n = 0;
        foreach (var r in rows)
        {
            if (!r.apply || r.data == null) continue;
            Undo.RecordObject(r.data, "Dien nguyen lieu xay dung");
            r.data.materialCosts = new List<BuildMaterialCost>(r.suggest);
            EditorUtility.SetDirty(r.data);
            n++;
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Nguyen Lieu",
            $"Da dien nguyen lieu cho {n} cong trinh.\n\n" +
            "Nguoi choi se phai gom du go/da/kinh/dinh tu tau lua moi xay duoc.", "OK");
    }
}
#endif
