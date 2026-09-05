using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ★ SỬA FONT GÃY SCENE COOKING (DRY-RUN / APPLY) — DEV-H round 2, 2026-09-02.
///
/// VÌ SAO CÓ TOOL NÀY: Sếp nghi "đổi phông chữ làm bếp trống trơn". Điều tra round 2
/// cho thấy nguyên nhân THẬT là NullReference ở KitchenSceneV2UI.BindExistingHierarchy
/// (đã fix trong code), font Baloo2 SDF vẫn còn nguyên. Tool này để Sếp TỰ KIỂM CHỨNG
/// và là lưới an toàn cho lần sau: quét mọi TMP_Text trong scene bếp + các prefab thẻ
/// liên quan, node nào MẤT FONT (null / asset đã xoá) thì gán font thay thế.
///
/// CÁCH DÙNG: Tools → Farm Game → Cooking → ★ Sửa font gãy scene Cooking…
///   1. Tool tự dò font thay thế = font đang được DÙNG NHIỀU NHẤT trong scene
///      (thiếu thì rơi về TMP_Settings.defaultFontAsset). Sếp đổi được trong ô Font.
///   2. Bấm [DRY-RUN] — chỉ IN BÁO CÁO, không sửa gì.
///   3. Ưng rồi bấm [APPLY] — gán font + lưu scene/prefab. Mỗi node sửa đều được liệt kê.
///
/// AN TOÀN: không đụng gì ngoài field font của TMP_Text; APPLY hỏi xác nhận;
/// scene đang mở dở dang sẽ được hỏi lưu trước khi tool mở SampleScene.
/// </summary>
public class CookingFontRepairTool : EditorWindow
{
    private const string ScenePath = "Assets/_Game/Scenes/SampleScene.unity";

    /// <summary>Các prefab thẻ mà scene bếp Instantiate lúc chạy — quét cùng scene.</summary>
    private static readonly string[] PrefabPaths =
    {
        "Assets/_Game/Prefab/DishCard.prefab",
        "Assets/_Game/Prefab/ui/Item_Ingredient_Beef.prefab",
        "Assets/_Game/Prefab/ui/Item_Seasoning_FishSauce.prefab",
        "Assets/_Game/Prefab/ui/PF_Item_SeasoningCard.prefab",
    };

    private TMP_FontAsset _fontThayThe;
    private Vector2 _scroll;
    private string _baoCao = "Chưa chạy. Bấm DRY-RUN để quét (không sửa gì).";

    [MenuItem("Tools/Farm Game/Cooking/★ Sửa font gãy scene Cooking (DRY-RUN - APPLY)")]
    private static void Open()
    {
        var w = GetWindow<CookingFontRepairTool>("Sửa font Cooking");
        w.minSize = new Vector2(560f, 420f);
        w._fontThayThe = TuDoFontMacDinh();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Quét mọi TMP_Text trong SampleScene (scene bếp) + prefab thẻ liên quan.\n" +
            "Node nào MẤT font sẽ được gán font dưới đây. DRY-RUN chỉ in báo cáo.",
            MessageType.Info);

        _fontThayThe = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "Font thay thế", _fontThayThe, typeof(TMP_FontAsset), false);

        if (_fontThayThe == null && GUILayout.Button("Tự dò font mặc định (dùng nhiều nhất trong scene)"))
            _fontThayThe = TuDoFontMacDinh();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("DRY-RUN (chỉ báo cáo)", GUILayout.Height(32f)))
            _baoCao = Chay(false);
        GUI.enabled = _fontThayThe != null;
        if (GUILayout.Button("APPLY (gán font + lưu)", GUILayout.Height(32f)))
        {
            if (EditorUtility.DisplayDialog("Sửa font scene Cooking",
                    $"Gán font '{(_fontThayThe != null ? _fontThayThe.name : "?")}' cho mọi TMP_Text mất font\n" +
                    "trong SampleScene + prefab thẻ, rồi LƯU scene/prefab?", "Sửa và lưu", "Thôi"))
                _baoCao = Chay(true);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_baoCao, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    /// <summary>Font được TMP_Text dùng nhiều nhất trong scene bếp; thiếu thì TMP default.</summary>
    private static TMP_FontAsset TuDoFontMacDinh()
    {
        var dem = new Dictionary<TMP_FontAsset, int>();
        Scene scene = LayHoacMoScene(false);
        if (scene.IsValid() && scene.isLoaded)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
                    if (t != null && t.font != null)
                        dem[t.font] = dem.TryGetValue(t.font, out int n) ? n + 1 : 1;
        }

        TMP_FontAsset tot = null; int max = 0;
        foreach (var kv in dem)
            if (kv.Value > max) { max = kv.Value; tot = kv.Key; }
        if (tot != null) return tot;

        return TMP_Settings.defaultFontAsset; // lưới cuối — có thể vẫn null, UI sẽ bắt
    }

    /// <summary>Scene bếp: đang mở thì dùng luôn; chưa mở thì (tuỳ chọn) mở — có hỏi lưu scene dở.</summary>
    private static Scene LayHoacMoScene(bool moNeuChuaCo)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.path == ScenePath && s.isLoaded) return s;
        }
        if (!moNeuChuaCo) return default;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return default; // Sếp bấm Cancel — không mở đè
        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private string Chay(bool apDung)
    {
        var bc = new StringBuilder();
        bc.AppendLine(apDung ? "== APPLY — đã sửa và lưu ==" : "== DRY-RUN — không sửa gì ==");
        bc.AppendLine($"Font thay thế: {(_fontThayThe != null ? _fontThayThe.name : "(chưa chọn — chỉ báo cáo)")}");
        bc.AppendLine();

        int tongOk = 0, tongGay = 0, tongSua = 0;

        // ── Scene bếp ──
        Scene scene = LayHoacMoScene(true);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            bc.AppendLine($"⚠ Không mở được scene: {ScenePath}");
        }
        else
        {
            bool sceneDoi = false;
            bc.AppendLine($"— SCENE {scene.name} —");
            foreach (GameObject root in scene.GetRootGameObjects())
                QuetCay(root.transform, DuongDan(root.transform), bc,
                        apDung, ref tongOk, ref tongGay, ref tongSua, ref sceneDoi);

            if (apDung && sceneDoi)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                bc.AppendLine($"  → Đã lưu {scene.name}.unity");
            }
        }

        // ── Prefab liên quan ──
        foreach (string duong in PrefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(duong);
            if (prefab == null) { bc.AppendLine($"⚠ Không thấy prefab: {duong}"); continue; }

            bool doi = false;
            bc.AppendLine($"— PREFAB {duong} —");
            QuetCay(prefab.transform, prefab.name, bc,
                    apDung, ref tongOk, ref tongGay, ref tongSua, ref doi);
            if (apDung && doi)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                bc.AppendLine("  → Đã lưu prefab.");
            }
        }

        bc.AppendLine();
        bc.AppendLine($"KẾT QUẢ: {tongOk} node font OK · {tongGay} node MẤT font" +
                      (apDung ? $" · {tongSua} node đã gán '{(_fontThayThe != null ? _fontThayThe.name : "?")}'" : " (DRY-RUN, chưa sửa)"));
        if (tongGay == 0)
            bc.AppendLine("→ Font scene Cooking LÀNH LẶN. Nếu bếp vẫn lỗi, nguyên nhân KHÔNG phải font.");
        Debug.Log($"[CookingFontRepair] {(apDung ? "APPLY" : "DRY-RUN")}: OK={tongOk}, gãy={tongGay}, sửa={tongSua}");
        return bc.ToString();
    }

    private void QuetCay(Transform goc, string duongGoc, StringBuilder bc, bool apDung,
                         ref int ok, ref int gay, ref int sua, ref bool coThayDoi)
    {
        foreach (TMP_Text t in goc.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null) continue;
            if (t.font != null) { ok++; continue; }

            gay++;
            string duong = DuongDan(t.transform);
            if (apDung && _fontThayThe != null)
            {
                Undo.RecordObject(t, "Sửa font Cooking");
                t.font = _fontThayThe;
                EditorUtility.SetDirty(t);
                sua++; coThayDoi = true;
                bc.AppendLine($"  ✔ ĐÃ GÁN  {duong}   (text: \"{CatNgan(t.text)}\")");
            }
            else
            {
                bc.AppendLine($"  ✘ MẤT FONT  {duong}   (text: \"{CatNgan(t.text)}\")");
            }
        }
    }

    private static string DuongDan(Transform t)
    {
        var phan = new List<string>();
        while (t != null) { phan.Add(t.name); t = t.parent; }
        phan.Reverse();
        return string.Join("/", phan);
    }

    private static string CatNgan(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace('\n', ' ');
        return s.Length <= 40 ? s : s.Substring(0, 40) + "…";
    }
}
