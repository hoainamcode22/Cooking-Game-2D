using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Tool 5: gắn Hồ Câu vào SCN_Farm (đang mở). CHỦ FILE: Dev D.
    /// (a) Tab_Fishing trong Canvas_HUD/BottomLeft_Nav_Group: bản sao Tab_Cooking (giữ style/anchor/sprite), xoá mọi listener + component
    ///     không thuộc UI cơ bản (không kéo logic tab Bếp theo), TMP → "HỒ CÂU", + FishingHudTabButton. Có rồi → chỉ bổ sung, không đổi vị trí.
    /// (b) Canvas_FishingPopup (Overlay order 410) + FishingEntryPopup / FishCounterPopup / FishingInviteHint (BuildIfEmpty của Dev C).
    /// (c) FishCounter world object gần cổng bếp (494,-2367)+(350,0), sprite tạm, BoxCollider2D, FishCounterWorldObject.
    /// Undo.RegisterCreatedObjectUndo mọi object mới, MarkSceneDirty, KHÔNG SaveScene (Sếp Ctrl+S). Có DRY-RUN.
    /// </summary>
    public static class FishingFarmHookSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuApply = MenuRoot + "5. Gắn vào SCN_Farm (tab HUD + quầy cá + popup)";
        private const string MenuDry = MenuRoot + "5b. Gắn vào SCN_Farm (DRY-RUN, chỉ báo)";
        private const string UndoLabel = "Hồ Câu — Gắn vào SCN_Farm";

        private const string HudCanvasName = "Canvas_HUD";
        private const string NavGroupName = "BottomLeft_Nav_Group";
        private const string TabTemplateName = "Tab_Cooking";
        private const string TabShopName = "Tab_Shop";
        private const string TabWarehouseName = "Tab_Warehouse";
        private const string TabLabelVi = "HỒ CÂU";
        private const float DefaultTabGap = 12f;

        /// <summary>Cổng bếp đo từ SCN_Farm (TouristBoatOneClickSetup) + lệch phải 350.</summary>
        private static readonly Vector2 CounterPos = new Vector2(494f + 350f, -2367f);
        /// <summary>Farm dùng world ×150 (khách cao 170 unit) → quầy tạm rộng ~120 unit mới nhìn thấy.</summary>
        private const float CounterTargetWidth = 120f;
        private static readonly Color CounterColor = new Color(0.55f, 0.35f, 0.18f, 1f);

        /// <summary>Component được GIỮ trên bản sao tab (mọi thứ khác bị gỡ để không kéo logic tab Bếp).</summary>
        private static readonly Type[] KeepOnTabCopy =
        {
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RawImage), typeof(Button), typeof(LayoutElement),
            typeof(TMP_Text), typeof(Text), typeof(Shadow), typeof(Outline), typeof(ContentSizeFitter), typeof(LayoutGroup),
            typeof(CanvasGroup), typeof(Mask), typeof(RectMask2D), typeof(FishingHudTabButton),
        };

        [MenuItem(MenuApply, false, 50)]
        private static void MenuRunApply() { Run(true); }

        [MenuItem(MenuDry, false, 51)]
        private static void MenuRunDry() { Run(false); }

        /// <summary>apply=false: DRY-RUN chỉ liệt kê sẽ làm gì. Trả báo cáo.</summary>
        public static string Run(bool apply)
        {
            var report = new StringBuilder();
            report.AppendLine(apply ? "── GẮN VÀO SCN_FARM (APPLY) ──" : "── GẮN VÀO SCN_FARM (DRY-RUN, không đổi gì) ──");

            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return "Đang Play."; }
            var scene = SceneManager.GetActiveScene();
            if (scene.name != FishingIds.FarmSceneName)
            {
                string loi = "Scene đang mở là '" + scene.name + "', cần mở " + FishingIds.FarmSceneName + " (Assets/_Game/Scenes/SCN_Farm.unity) rồi chạy lại menu này.";
                EditorUtility.DisplayDialog("Hồ Câu — Gắn vào SCN_Farm", loi, "OK");
                return loi;
            }

            if (apply && !EditorUtility.DisplayDialog("Gắn Hồ Câu vào SCN_Farm",
                "Sẽ thêm vào scene ĐANG MỞ:\n• Tab_Fishing (bản sao Tab_Cooking, đặt cạnh phải) + FishingHudTabButton\n• Canvas_FishingPopup (order " + FishingIds.FarmPopupCanvasOrder + ") + 3 popup\n• Object FishCounter (quầy cá tạm) gần cổng bếp\n\nKhông xoá/đổi gì đã có. Scene KHÔNG tự lưu — Sếp Ctrl+S. Ctrl+Z hoàn tác.", "Chạy", "Huỷ"))
            { return "Huỷ."; }

            if (apply) { Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(UndoLabel); }
            var thieu = new List<string>();
            GameObject ping = null;
            try
            {
                ping = StepTab(apply, report, thieu);
                StepPopupCanvas(apply, report, thieu);
                StepCounter(apply, report, thieu);
                if (apply) { EditorSceneManager.MarkSceneDirty(scene); }
            }
            catch (Exception e)
            {
                report.AppendLine("✖ LỖI NGOÀI DỰ KIẾN: " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                if (apply) { Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); }
            }

            var head = new StringBuilder();
            head.AppendLine(apply ? (thieu.Count == 0 ? "✔ Đã gắn Hồ Câu vào SCN_Farm." : "⚠ Đã gắn nhưng còn thiếu:") : "DRY-RUN xong (chưa đổi gì).");
            for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
            if (apply)
            {
                head.AppendLine();
                head.AppendLine("SẾP LÀM: 1) Ctrl+S lưu scene. 2) Kéo Tab_Fishing đến vị trí muốn (tool không đè). 3) Kéo FishCounter tới chỗ muốn + thay sprite quầy. 4) Play → tab HỒ CÂU.");
            }
            head.AppendLine("Chi tiết ở Console (lọc FishingSetup).");
            Debug.Log(FishingIds.SetupLogTag + " Gắn vào SCN_Farm:\n" + report);
            EditorUtility.DisplayDialog("Hồ Câu — Gắn vào SCN_Farm", head.ToString(), "OK");
            if (apply && ping != null) { Selection.activeGameObject = ping; EditorGUIUtility.PingObject(ping); }
            return report.ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        //  (a) Tab HUD
        // ─────────────────────────────────────────────────────────────────

        private static GameObject StepTab(bool apply, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("(a) Tab_Fishing");
            Transform nav = FindNavGroup();
            Transform cooking = nav != null ? nav.Find(TabTemplateName) : FindByName(TabTemplateName);
            if (nav == null && cooking != null) { nav = cooking.parent; }
            if (nav == null || cooking == null)
            {
                report.AppendLine("  ✖ Không thấy " + HudCanvasName + "/" + NavGroupName + "/" + TabTemplateName + " — bỏ qua tab. Sếp tạo tay nút và gắn FishingHudTabButton.");
                thieu.Add("Không thấy nhóm tab HUD (" + NavGroupName + "/" + TabTemplateName + ") — thêm nút tay + FishingHudTabButton");
                return null;
            }

            Transform exist = nav.Find(FishingIds.FarmHudTabName);
            if (exist != null)
            {
                report.AppendLine("  · " + FishingIds.FarmHudTabName + " đã có — chỉ bổ sung component thiếu, KHÔNG đổi vị trí.");
                if (apply)
                {
                    var go = exist.gameObject;
                    if (go.GetComponent<Button>() == null) { Undo.AddComponent<Button>(go); report.AppendLine("    + Button"); }
                    if (go.GetComponent<FishingHudTabButton>() == null) { Undo.AddComponent<FishingHudTabButton>(go); report.AppendLine("    + FishingHudTabButton"); }
                }
                return exist.gameObject;
            }

            // Vị trí: cạnh phải Tab_Cooking, khoảng cách đo từ Shop→Warehouse
            var rtCook = cooking as RectTransform;
            float gap = DefaultTabGap;
            var shop = nav.Find(TabShopName) as RectTransform;
            var ware = nav.Find(TabWarehouseName) as RectTransform;
            if (shop != null && ware != null)
            {
                float g = ware.anchoredPosition.x - shop.anchoredPosition.x - shop.rect.width;
                if (g > 0f && g < 200f) { gap = g; }
            }
            bool hasLayout = nav.GetComponent<LayoutGroup>() != null;
            Vector2 pos = rtCook != null ? rtCook.anchoredPosition + new Vector2(rtCook.rect.width + gap, 0f) : Vector2.zero;
            report.AppendLine("  + Sẽ tạo " + FishingIds.FarmHudTabName + " = bản sao " + TabTemplateName + (hasLayout ? " (nhóm có LayoutGroup → xếp sau Tab_Cooking)" : " tại anchoredPosition " + Fmt(pos) + " (gap " + gap.ToString("0.#", CultureInfo.InvariantCulture) + ")"));
            if (!apply) { return null; }

            GameObject copy = UnityEngine.Object.Instantiate(cooking.gameObject, nav, false);
            Undo.RegisterCreatedObjectUndo(copy, UndoLabel);
            copy.name = FishingIds.FarmHudTabName;
            if (PrefabUtility.IsPartOfPrefabInstance(copy)) { PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); }
            copy.SetActive(true);

            var rt = copy.transform as RectTransform;
            if (rt != null && rtCook != null)
            {
                rt.anchorMin = rtCook.anchorMin; rt.anchorMax = rtCook.anchorMax; rt.pivot = rtCook.pivot;
                rt.sizeDelta = rtCook.sizeDelta; rt.localScale = rtCook.localScale;
                rt.anchoredPosition = pos;
            }
            copy.transform.SetSiblingIndex(cooking.GetSiblingIndex() + 1);

            // Gỡ component không thuộc UI cơ bản trên BẢN SAO MÌNH VỪA TẠO (được phép DestroyImmediate)
            int removed = StripCopy(copy);

            // Xoá mọi listener persistent của Button (không kéo onClick tab Bếp)
            var btn = copy.GetComponent<Button>();
            if (btn == null) { btn = copy.AddComponent<Button>(); var img = copy.GetComponent<Image>(); if (img != null) { btn.targetGraphic = img; } }
            btn.onClick = new Button.ButtonClickedEvent();
            var btnsCon = copy.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < btnsCon.Length; i++) { if (btnsCon[i] != btn) { btnsCon[i].onClick = new Button.ButtonClickedEvent(); } }

            // Nhãn
            var tmp = copy.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = TabLabelVi; }
            else
            {
                var txt = copy.GetComponentInChildren<Text>(true);
                if (txt != null) { txt.text = TabLabelVi; }
            }

            if (copy.GetComponent<FishingHudTabButton>() == null) { copy.AddComponent<FishingHudTabButton>(); }
            EditorUtility.SetDirty(copy);
            report.AppendLine("  ✓ Tạo " + Path(copy.transform) + " — gỡ " + removed.ToString(CultureInfo.InvariantCulture) + " component thừa, onClick trống, nhãn \"" + TabLabelVi + "\", + FishingHudTabButton");
            report.AppendLine("    Sếp kéo vị trí nếu HUD chật (tool chạy lại KHÔNG đè). Icon: thay Source Image của Image.");
            return copy;
        }

        private static int StripCopy(GameObject root)
        {
            int removed = 0;
            // Lặp vài lần vì RequireComponent chặn xoá component bị phụ thuộc ở lượt đầu.
            for (int pass = 0; pass < 6; pass++)
            {
                bool any = false;
                var comps = root.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    var c = comps[i];
                    if (c == null || c is Transform) { continue; }
                    if (IsKept(c.GetType())) { continue; }
                    if (IsRequiredByOther(c)) { continue; }   // để lượt sau, khi component phụ thuộc đã bị gỡ
                    try
                    {
                        UnityEngine.Object.DestroyImmediate(c);
                        if (c == null) { removed++; any = true; }
                    }
                    catch (Exception) { /* thử lại lượt sau */ }
                }
                if (!any) { break; }
            }
            return removed;
        }

        /// <summary>Có component khác trên cùng GameObject khai [RequireComponent] tới kiểu này không (Unity sẽ chặn DestroyImmediate).</summary>
        private static bool IsRequiredByOther(Component c)
        {
            var siblings = c.gameObject.GetComponents<Component>();
            Type ct = c.GetType();
            for (int i = 0; i < siblings.Length; i++)
            {
                var s = siblings[i];
                if (s == null || s == c) { continue; }
                var attrs = s.GetType().GetCustomAttributes(typeof(RequireComponent), true);
                for (int a = 0; a < attrs.Length; a++)
                {
                    var rc = attrs[a] as RequireComponent;
                    if (rc == null) { continue; }
                    if (Req(rc.m_Type0, ct) || Req(rc.m_Type1, ct) || Req(rc.m_Type2, ct)) { return true; }
                }
            }
            return false;
        }

        private static bool Req(Type required, Type candidate)
        {
            return required != null && required.IsAssignableFrom(candidate);
        }

        private static bool IsKept(Type t)
        {
            for (int i = 0; i < KeepOnTabCopy.Length; i++)
            {
                if (KeepOnTabCopy[i].IsAssignableFrom(t)) { return true; }
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────
        //  (b) Canvas popup riêng
        // ─────────────────────────────────────────────────────────────────

        private static void StepPopupCanvas(bool apply, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("(b) " + FishingIds.FarmPopupCanvasName);
            Transform canvasT = null;
            foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c != null && c.name == FishingIds.FarmPopupCanvasName) { canvasT = c.transform; break; }
            }
            if (canvasT == null)
            {
                report.AppendLine("  + Sẽ tạo canvas root-level Overlay order " + FishingIds.FarmPopupCanvasOrder + ", 1920x1080 match 0.5");
                if (!apply) { return; }
                var go = new GameObject(FishingIds.FarmPopupCanvasName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.layer = LayerMask.NameToLayer("UI");
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = FishingIds.FarmPopupCanvasOrder;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
                canvasT = go.transform;
            }
            else { report.AppendLine("  · canvas đã có — giữ nguyên order/scaler."); }

            EnsurePopupChild<FishingEntryPopupUI>(canvasT, "FishingEntryPopup", apply, report, thieu, ui => ui.BuildIfEmpty());
            EnsurePopupChild<FishCounterPopupUI>(canvasT, "FishCounterPopup", apply, report, thieu, ui => ui.BuildIfEmpty());
            EnsurePopupChild<FishingInviteHintUI>(canvasT, "FishingInviteHint", apply, report, thieu, ui => ui.BuildIfEmpty());
        }

        private static void EnsurePopupChild<T>(Transform canvas, string name, bool apply, StringBuilder report, List<string> thieu, Action<T> build) where T : Component
        {
            Transform t = canvas != null ? canvas.Find(name) : null;
            if (t == null)
            {
                report.AppendLine("  + Sẽ tạo " + name + " (+" + typeof(T).Name + " → BuildIfEmpty)");
                if (!apply) { return; }
                var go = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.layer = LayerMask.NameToLayer("UI");
                go.transform.SetParent(canvas, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                t = go.transform;
            }
            else { report.AppendLine("  · " + name + " đã có."); }
            if (!apply) { return; }

            var comp = t.GetComponent<T>();
            if (comp == null) { comp = Undo.AddComponent<T>(t.gameObject); report.AppendLine("    + " + typeof(T).Name); }
            try { build(comp); report.AppendLine("    ✓ " + typeof(T).Name + ".BuildIfEmpty()"); }
            catch (Exception e)
            {
                report.AppendLine("    ✖ " + typeof(T).Name + ".BuildIfEmpty() lỗi: " + e.Message);
                thieu.Add(typeof(T).Name + ".BuildIfEmpty() lỗi — " + e.Message);
                Debug.LogException(e);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  (c) Quầy cá world object
        // ─────────────────────────────────────────────────────────────────

        private static void StepCounter(bool apply, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("(c) " + FishingIds.FarmCounterName);
            Transform exist = FindByName(FishingIds.FarmCounterName);
            if (exist != null)
            {
                report.AppendLine("  · " + FishingIds.FarmCounterName + " đã có tại " + Fmt(exist.position) + " — chỉ bổ sung component thiếu.");
                if (!apply) { return; }
                if (exist.GetComponent<Collider2D>() == null) { Undo.AddComponent<BoxCollider2D>(exist.gameObject); report.AppendLine("    + BoxCollider2D"); }
                if (exist.GetComponent<FishCounterWorldObject>() == null) { Undo.AddComponent<FishCounterWorldObject>(exist.gameObject); report.AppendLine("    + FishCounterWorldObject"); }
                return;
            }

            report.AppendLine("  + Sẽ tạo " + FishingIds.FarmCounterName + " tại " + Fmt(CounterPos) + " (cổng bếp (494,-2367) lệch +350), sprite tạm Knob nâu rộng ~" + CounterTargetWidth.ToString("0", CultureInfo.InvariantCulture) + " unit, layer Objects/500");
            if (!apply) { return; }

            var go = new GameObject(FishingIds.FarmCounterName);
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.position = new Vector3(CounterPos.x, CounterPos.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            Sprite knob = null;
            try { knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); }
            catch (Exception e) { report.AppendLine("    ⚠ Không lấy được sprite built-in Knob: " + e.Message); }
            sr.sprite = knob;
            sr.color = CounterColor;
            sr.sortingLayerName = TouristSortingLayers.ResolveOrOverride("Objects", TouristSortingLayers.Visitor);
            sr.sortingOrder = 500;

            // Farm world ×150: scale để quầy tạm rộng ~120 unit (spec gốc (3,3) chỉ đúng với world 1 unit).
            float w = knob != null ? knob.bounds.size.x : 1f;
            float k = w > 0.0001f ? CounterTargetWidth / w : 3f;
            go.transform.localScale = new Vector3(k, k, 1f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = knob != null ? (Vector2)knob.bounds.size : Vector2.one;
            col.offset = Vector2.zero;

            go.AddComponent<FishCounterWorldObject>();
            report.AppendLine("  ✓ Tạo " + FishingIds.FarmCounterName + " (scale " + k.ToString("0.#", CultureInfo.InvariantCulture) + "). Sếp kéo tới vị trí muốn và thay sprite quầy + chỉnh requiredLevel.");
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private static Transform FindNavGroup()
        {
            Transform fallback = null;
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.name != NavGroupName) { continue; }
                Transform p = t.parent;
                bool underHud = false;
                while (p != null) { if (p.name == HudCanvasName) { underHud = true; break; } p = p.parent; }
                if (underHud) { return t; }
                if (fallback == null) { fallback = t; }
            }
            return fallback;
        }

        private static Transform FindByName(string name)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t != null && t.name == name) { return t; }
            }
            return null;
        }

        private static string Path(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        private static string Fmt(Vector2 v)
        {
            return "(" + v.x.ToString("0.#", CultureInfo.InvariantCulture) + ", " + v.y.ToString("0.#", CultureInfo.InvariantCulture) + ")";
        }
    }
}
