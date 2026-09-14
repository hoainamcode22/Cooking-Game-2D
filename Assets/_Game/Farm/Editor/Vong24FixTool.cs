#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools/Map45/23. Sua Nhac Nen + San Pham Chuong
///
/// VONG 24 - hai viec Sep giao:
///   VIEC 1 (nhac nen): gan clip "soft click.mp3" vao o bgmMain cua MOI AudioManager
///           dang nam san trong scene, mo/luu tung scene, tra lai scene dang mo ban dau.
///   VIEC 2 (san pham chuong): bo sua chi ra SUA, bo thit chi ra THIT - sua ca id san
///           pham lan icon san pham trong cac asset PenMiniPanelConfig.
///
/// CANH BAO QUAN TRONG (da kiem chung tren file, KHONG doan):
///   AudioManager co [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] AutoInit() tu tao
///   mot singleton DontDestroyOnLoad TRUOC khi scene dau tien duoc nap. Instance dat trong
///   scene chay Awake sau do se thay _instance != this va TU HUY. Vi vay o bgmMain gan
///   trong scene KHONG bao gio duoc dung luc chay that; clip that su phat la ket qua cua
///   LoadDefaultClipsIfMissing() -> Resources.Load("Audio/Morning_Garden_Waltz").
///   Nut 1 van gan dung nhu Sep yeu cau (va co tac dung khi bam Play trong Editor o mot
///   scene truoc khi domain reload, hoac neu sau nay bo AutoInit), nhung muon nhac nen
///   THAT SU doi sang "soft click.mp3" thi phai sua 1 dong trong AudioManager.cs hoac
///   thay file trong Resources/Audio - viec do KHONG nam trong tool nay.
/// </summary>
public class Vong24FixTool : EditorWindow
{
    // ───────────────────────────── HANG SO DA KIEM CHUNG ─────────────────────────────

    // Clip nhac nen Sep muon: Assets/_Game/Audio/soft click.mp3
    private const string BGM_GUID = "3d007f0ef76c6704a99ada93d3621692";
    private const string BGM_PATH = "Assets/_Game/Audio/soft click.mp3";

    // GUID script AudioManager (doc tu Assets/_Game/Audio/AudioManager.cs.meta)
    private const string AUDIOMANAGER_SCRIPT_GUID = "7b592ce0e1b2c5147a7a21241bde44da";

    // Sprite THIT BO: Assets/Assetsgame/Thit/thitbo-removebg-preview.png (sub-sprite _0)
    private const string BEEF_TEX_GUID = "7c146c31094766a4888de50bc8013caa";

    // Sprite SUA: Assets/Assetsgame/suamilk.png (sub-sprite suamilk_0)
    private const string MILK_TEX_GUID = "309bc08c5417bfc48a42313d16d33afb";

    // Sprite THIT HEO / THIT GA - chi dung de doi chieu, tool khong tu doi
    private const string PORK_TEX_GUID = "de16df85269be62459a51c70a1ac2585";
    private const string CHICKEN_TEX_GUID = "888289491956e5347952a87ca3ec06d1";

    // Bang san pham DUNG cho tung chuong (chot theo yeu cau cua Sep)
    private class PenSpec
    {
        public string penId;
        public string tenChuong;
        public string productId;
        public string secondProductId; // rong = chuong nay KHONG co san pham thu 2
        public string iconTexGuid;     // rong = tool khong dung toi icon chinh
    }

    private static readonly PenSpec[] BANG_CHUAN = new PenSpec[]
    {
        new PenSpec { penId = "pen_01", tenChuong = "Bo thit",  productId = "beef",         secondProductId = "",    iconTexGuid = BEEF_TEX_GUID },
        new PenSpec { penId = "pen_02", tenChuong = "Heo",      productId = "pork",         secondProductId = "",    iconTexGuid = PORK_TEX_GUID },
        new PenSpec { penId = "pen_03", tenChuong = "Ga",       productId = "chicken_meat", secondProductId = "egg", iconTexGuid = CHICKEN_TEX_GUID },
        new PenSpec { penId = "pen_04", tenChuong = "Bo sua",   productId = "milk",         secondProductId = "",    iconTexGuid = MILK_TEX_GUID },
    };

    // ───────────────────────────── TRANG THAI CUA SO ─────────────────────────────

    private Vector2 scrollChanDoan;
    private Vector2 scrollBaoCao;
    private readonly List<string> baoCao = new List<string>();

    private string cacheChanDoanScene = "";
    private double lanQuetSceneCuoi = -999;
    private const double CHU_KY_QUET_SCENE = 3.0; // giay - tranh doc file .unity moi frame

    [MenuItem("Tools/Map45/23. Sua Nhac Nen + San Pham Chuong", false, 23)]
    public static void MoCuaSo()
    {
        var w = GetWindow<Vong24FixTool>(false, "23. Nhac Nen + San Pham", true);
        w.minSize = new Vector2(660f, 520f);
        w.LamMoiChanDoan();
        w.Show();
    }

    private void OnEnable()
    {
        LamMoiChanDoan();
    }

    private void LamMoiChanDoan()
    {
        lanQuetSceneCuoi = -999;
        cacheChanDoanScene = "";
        cacheSprite.Clear();
        Repaint();
    }

    // ───────────────────────────── GIAO DIEN ─────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Vòng 24 — Sửa nhạc nền & sản phẩm chuồng bò",
            new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
        EditorGUILayout.LabelField("Chỉ chạm vào: o bgmMain trong scene + các asset PenMiniPanelConfig.",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(6f);

        VeBangChanDoan();

        EditorGUILayout.Space(8f);
        VeCacNut();

        EditorGUILayout.Space(8f);
        VeBaoCao();
    }

    private void VeBangChanDoan()
    {
        EditorGUILayout.LabelField("CHẨN ĐOÁN (chỉ đọc — trạng thái dự án ngay lúc này)", EditorStyles.boldLabel);

        scrollChanDoan = EditorGUILayout.BeginScrollView(scrollChanDoan, GUILayout.MinHeight(200f));
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        try
        {
            EditorGUILayout.LabelField("① NHẠC NỀN", EditorStyles.boldLabel);

            var clip = LayClipNhacNen();
            EditorGUILayout.LabelField(clip != null
                ? "   Clip đích: " + BGM_PATH + "  ✔ tìm thấy"
                : "   Clip đích: " + BGM_PATH + "  ✘ KHÔNG tìm thấy (không bấm nút 1 được)");

            // Doc trang thai scene tu FILE .unity (khong mo scene -> khong lam ban project)
            if (EditorApplication.timeSinceStartup - lanQuetSceneCuoi > CHU_KY_QUET_SCENE)
            {
                cacheChanDoanScene = QuetSceneTheoVanBan();
                lanQuetSceneCuoi = EditorApplication.timeSinceStartup;
            }
            int dongScene = 1;
            foreach (char ch in cacheChanDoanScene) if (ch == '\n') dongScene++;
            EditorGUILayout.LabelField(cacheChanDoanScene, EditorStyles.wordWrappedLabel,
                GUILayout.Height(dongScene * 15f + 6f));

            EditorGUILayout.HelpBox(
                "LƯU Ý THẬT: AudioManager có AutoInit() chạy BeforeSceneLoad, tự tạo singleton " +
                "DontDestroyOnLoad TRƯỚC scene đầu tiên. Bản AudioManager đặt sẵn trong scene sẽ " +
                "tự huỷ trong Awake. Nên ô bgmMain trong scene KHÔNG quyết định nhạc chạy thật — " +
                "nhạc thật đến từ Resources/Audio/Morning_Garden_Waltz.mp3 qua " +
                "LoadDefaultClipsIfMissing(). Nút 1 vẫn gán đúng như Sếp yêu cầu, nhưng muốn đổi " +
                "hẳn nhạc nền thì phải sửa AudioManager.cs (~dòng 332) hoặc thay file trong Resources.",
                MessageType.Warning);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("② SẢN PHẨM CHUỒNG", EditorStyles.boldLabel);
            VeChanDoanChuong();
        }
        catch (Exception e)
        {
            EditorGUILayout.HelpBox("Lỗi khi vẽ chẩn đoán: " + e.Message, MessageType.Error);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void VeChanDoanChuong()
    {
        var list = TimTatCaPenConfig();
        if (list.Count == 0)
        {
            EditorGUILayout.LabelField("   Không tìm thấy asset PenMiniPanelConfig nào trong dự án.");
            return;
        }

        // Dem xem icon nao bi DUNG CHUNG boi nhieu chuong -> do la dau hieu loi Sep bao
        var demIcon = new Dictionary<string, int>();
        foreach (var c in list)
        {
            string k = KhoaSprite(c.productIcon);
            if (string.IsNullOrEmpty(k)) continue;
            demIcon[k] = demIcon.ContainsKey(k) ? demIcon[k] + 1 : 1;
        }

        foreach (var c in list)
        {
            if (c == null) continue;
            string khoa = KhoaSprite(c.productIcon);
            bool trung = !string.IsNullOrEmpty(khoa) && demIcon.ContainsKey(khoa) && demIcon[khoa] > 1;

            var sb = new StringBuilder();
            sb.Append("   ").Append(string.IsNullOrEmpty(c.penId) ? "(không có penId)" : c.penId);
            sb.Append(" | ").Append(c.name);
            sb.Append("\n        sản phẩm: ").Append(Chuoi(c.productItemId));
            sb.Append(" x").Append(c.productAmount);
            if (!string.IsNullOrEmpty(c.secondProductItemId))
                sb.Append("   + phụ: ").Append(c.secondProductItemId).Append(" x").Append(c.secondProductAmount);
            sb.Append("\n        icon sp: ").Append(TenSprite(c.productIcon)).Append(trung ? "   ⚠ DÙNG CHUNG với chuồng khác" : "");
            sb.Append("\n        thức ăn: ").Append(Chuoi(c.food1ItemId)).Append(" / cám: ").Append(Chuoi(c.premiumFoodItemId));
            sb.Append("  (icon: ").Append(TenSprite(c.food1Icon)).Append(" / ").Append(TenSprite(c.premiumFoodIcon)).Append(")");

            var spec = TimSpec(c);
            string lech = SoSanhVoiChuan(c, spec);
            if (!string.IsNullOrEmpty(lech)) sb.Append("\n        ✘ LỆCH CHUẨN: ").Append(lech);
            else sb.Append("\n        ✔ đúng chuẩn");

            string noiDung = sb.ToString();
            int soDong = 1;
            foreach (char ch in noiDung) if (ch == '\n') soDong++;
            EditorGUILayout.LabelField(noiDung, EditorStyles.wordWrappedLabel,
                GUILayout.Height(soDong * 15f + 6f));
        }
    }

    private void VeCacNut()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUI.backgroundColor = new Color(0.75f, 0.90f, 1f);
        if (GUILayout.Button("1. Gán nhạc nền cho mọi scene", GUILayout.Height(32f)))
            ChayGanNhacNen();

        GUI.backgroundColor = new Color(1f, 0.88f, 0.72f);
        if (GUILayout.Button("2. Sửa sản phẩm chuồng bò", GUILayout.Height(32f)))
            ChaySuaSanPhamChuong();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Quét thử (không ghi gì)", GUILayout.Height(26f)))
            ChayQuetThu();

        if (GUILayout.Button("Làm mới chẩn đoán", GUILayout.Height(20f)))
            LamMoiChanDoan();

        EditorGUILayout.EndVertical();
    }

    private void VeBaoCao()
    {
        EditorGUILayout.LabelField("BÁO CÁO (trước → sau)", EditorStyles.boldLabel);
        scrollBaoCao = EditorGUILayout.BeginScrollView(scrollBaoCao, GUILayout.MinHeight(140f));
        if (baoCao.Count == 0)
            EditorGUILayout.LabelField("   (chưa chạy gì)");
        else
            EditorGUILayout.TextArea(string.Join("\n", baoCao.ToArray()), EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
    }

    // ───────────────────────────── NUT 1: NHAC NEN ─────────────────────────────

    private void ChayGanNhacNen()
    {
        var clip = LayClipNhacNen();
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Thiếu file nhạc",
                "Không tìm thấy clip:\n" + BGM_PATH + "\n\nHãy kiểm tra lại đường dẫn rồi chạy lại.", "Đóng");
            return;
        }

        var duongDanScene = LayDanhSachScene();
        if (duongDanScene.Count == 0)
        {
            EditorUtility.DisplayDialog("Không có scene",
                "Build Settings chưa có scene nào và cũng không tìm thấy scene dự phòng.", "Đóng");
            return;
        }

        if (!EditorUtility.DisplayDialog("Gán nhạc nền cho mọi scene",
            "Tool sẽ MỞ và LƯU từng scene sau đây, gán clip \"soft click.mp3\" vào ô bgmMain của " +
            "mọi AudioManager có sẵn trong scene:\n\n" + string.Join("\n", duongDanScene.ToArray()) +
            "\n\nScene đang mở sẽ được mở lại sau khi xong. Tiếp tục?", "Làm đi", "Huỷ"))
            return;

        // Bat nguoi dung luu viec dang lam truoc khi tool doi scene
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Ghi("[NHAC NEN] Đã huỷ: scene hiện tại chưa được lưu.");
            return;
        }

        string sceneGoc = "";
        try { sceneGoc = SceneManager.GetActiveScene().path; }
        catch (Exception e) { Debug.LogWarning("[Vong24] Không đọc được scene đang mở: " + e.Message); }

        baoCao.Clear();
        Ghi("═══ NÚT 1 — GÁN NHẠC NỀN ═══");
        Ghi("Clip đích: " + BGM_PATH);
        Ghi("Scene đang mở ban đầu: " + (string.IsNullOrEmpty(sceneGoc) ? "(scene chưa lưu)" : sceneGoc));

        int soSua = 0, soBoQua = 0, soSceneKhongCo = 0;

        foreach (string path in duongDanScene)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Ghi("  ✘ " + path + " — không tồn tại trên đĩa, bỏ qua.");
                    continue;
                }

                Scene sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (!sc.IsValid() || !sc.isLoaded)
                {
                    Ghi("  ✘ " + path + " — mở không được, bỏ qua.");
                    continue;
                }

                var dsAM = GomAudioManager(sc);
                if (dsAM.Count == 0)
                {
                    soSceneKhongCo++;
                    Ghi("  – " + path + " — KHÔNG có AudioManager đặt sẵn (scene này dùng singleton runtime).");
                    continue;
                }

                bool banScene = false;
                foreach (var am in dsAM)
                {
                    try
                    {
                        var so = new SerializedObject(am);
                        var prop = so.FindProperty("bgmMain");
                        if (prop == null)
                        {
                            Ghi("  ✘ " + path + " / " + am.name + " — không tìm thấy field bgmMain.");
                            continue;
                        }

                        var truoc = prop.objectReferenceValue as AudioClip;
                        if (truoc == clip)
                        {
                            soBoQua++;
                            Ghi("  = " + path + " / " + am.name + " — đã đúng clip rồi, bỏ qua.");
                            continue;
                        }

                        Undo.RecordObject(am, "Vong24 gan bgmMain");
                        prop.objectReferenceValue = clip;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(am);
                        banScene = true;
                        soSua++;
                        Ghi("  ✔ " + path + " / " + am.name + " — bgmMain: " +
                            (truoc == null ? "(trống)" : truoc.name) + "  →  " + clip.name);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[Vong24] Lỗi khi gán bgmMain ở " + path + ": " + e);
                        Ghi("  ✘ " + path + " — lỗi: " + e.Message);
                    }
                }

                if (banScene)
                {
                    EditorSceneManager.MarkSceneDirty(sc);
                    bool luu = EditorSceneManager.SaveScene(sc);
                    Ghi(luu ? "     → đã lưu scene." : "     → LƯU THẤT BẠI!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Vong24] Lỗi ở scene " + path + ": " + e);
                Ghi("  ✘ " + path + " — lỗi: " + e.Message);
            }
        }

        MoLaiSceneGoc(sceneGoc);

        Ghi("── TỔNG KẾT: sửa " + soSua + " · bỏ qua (đã đúng) " + soBoQua +
            " · scene không có AudioManager " + soSceneKhongCo);
        Ghi("NHẮC: singleton AutoInit vẫn huỷ bản trong scene lúc chạy thật — xem cảnh báo vàng ở trên.");
        InRaConsole();
        LamMoiChanDoan();
    }

    private void MoLaiSceneGoc(string sceneGoc)
    {
        try
        {
            if (!string.IsNullOrEmpty(sceneGoc) && File.Exists(sceneGoc))
            {
                if (SceneManager.GetActiveScene().path != sceneGoc)
                    EditorSceneManager.OpenScene(sceneGoc, OpenSceneMode.Single);
                Ghi("Đã mở lại scene ban đầu: " + sceneGoc);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Ghi("Scene ban đầu chưa từng được lưu — mở một scene rỗng thay thế.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Vong24] Không mở lại được scene ban đầu: " + e);
            Ghi("✘ Không mở lại được scene ban đầu: " + e.Message);
        }
    }

    // ───────────────────────────── NUT 2: SAN PHAM CHUONG ─────────────────────────────

    private void ChaySuaSanPhamChuong()
    {
        var list = TimTatCaPenConfig();
        if (list.Count == 0)
        {
            EditorUtility.DisplayDialog("Không có dữ liệu",
                "Không tìm thấy asset PenMiniPanelConfig nào.", "Đóng");
            return;
        }

        var spriteBo = LaySpriteTheoGuid(BEEF_TEX_GUID);
        var spriteSua = LaySpriteTheoGuid(MILK_TEX_GUID);

        var thieu = new List<string>();
        if (spriteBo == null) thieu.Add("sprite THỊT BÒ (guid " + BEEF_TEX_GUID + ")");
        if (spriteSua == null) thieu.Add("sprite SỮA (guid " + MILK_TEX_GUID + " — Assets/Assetsgame/suamilk.png)");
        if (thieu.Count > 0)
        {
            if (!EditorUtility.DisplayDialog("Thiếu sprite",
                "Không nạp được:\n  • " + string.Join("\n  • ", thieu.ToArray()) +
                "\n\nTool vẫn có thể sửa phần ID sản phẩm (bỏ thịt khỏi bò sữa), nhưng ô icon " +
                "tương ứng sẽ giữ nguyên. Tiếp tục?", "Cứ sửa phần ID", "Huỷ"))
                return;
        }
        else if (!EditorUtility.DisplayDialog("Sửa sản phẩm chuồng bò",
            "Tool sẽ ghi vào các asset PenMiniPanelConfig:\n" +
            "  • pen_04 (bò sữa) → sản phẩm \"milk\", icon = suamilk, XOÁ sản phẩm phụ\n" +
            "  • pen_01 (bò thịt) → sản phẩm \"beef\", icon = thitbo, XOÁ sản phẩm phụ\n" +
            "  • pen_02 (heo) → \"pork\", pen_03 (gà) → \"chicken_meat\" + phụ \"egg\"\n\n" +
            "Chỉ ô nào SAI mới bị ghi đè. Tiếp tục?", "Làm đi", "Huỷ"))
            return;

        baoCao.Clear();
        Ghi("═══ NÚT 2 — SỬA SẢN PHẨM CHUỒNG ═══");
        int soO = 0, soAsset = 0;

        foreach (var c in list)
        {
            if (c == null) continue;
            try
            {
                var spec = TimSpec(c);
                if (spec == null)
                {
                    Ghi("  – " + c.name + " (penId=" + Chuoi(c.penId) + ") — không nằm trong bảng chuẩn, bỏ qua.");
                    continue;
                }

                var so = new SerializedObject(c);
                bool doi = false;
                var log = new List<string>();

                doi |= DoiChuoi(so, "productItemId", spec.productId, log);
                doi |= DoiChuoi(so, "secondProductItemId", spec.secondProductId, log);

                // Icon chinh: chi doi khi tool nap duoc sprite chuan
                Sprite spriteChuan = LaySpriteTheoGuid(spec.iconTexGuid);
                if (spriteChuan != null)
                    doi |= DoiSprite(so, "productIcon", spriteChuan, log);
                else
                    log.Add("productIcon: giữ nguyên (không nạp được sprite chuẩn)");

                // Chuong khong co san pham phu thi icon phu cung phai rong
                if (string.IsNullOrEmpty(spec.secondProductId))
                    doi |= DoiSprite(so, "secondProductIcon", null, log);

                if (!doi)
                {
                    Ghi("  = " + c.name + " — đã đúng hết, không ghi gì.");
                    continue;
                }

                Undo.RecordObject(c, "Vong24 sua san pham chuong");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(c);
                soAsset++;
                soO += log.Count;

                Ghi("  ✔ " + c.name + " (" + spec.tenChuong + ")");
                foreach (var d in log) Ghi("        " + d);
            }
            catch (Exception e)
            {
                Debug.LogError("[Vong24] Lỗi khi sửa " + (c != null ? c.name : "?") + ": " + e);
                Ghi("  ✘ " + (c != null ? c.name : "?") + " — lỗi: " + e.Message);
            }
        }

        try
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError("[Vong24] SaveAssets lỗi: " + e);
            Ghi("✘ SaveAssets lỗi: " + e.Message);
        }

        Ghi("── TỔNG KẾT: ghi " + soAsset + " asset · " + soO + " ô dữ liệu.");
        InRaConsole();
        LamMoiChanDoan();
    }

    private bool DoiChuoi(SerializedObject so, string ten, string moi, List<string> log)
    {
        var p = so.FindProperty(ten);
        if (p == null) { log.Add(ten + ": KHÔNG có field này"); return false; }
        string cu = p.stringValue == null ? "" : p.stringValue;
        string dich = moi == null ? "" : moi;
        if (cu == dich) return false;
        p.stringValue = dich;
        log.Add(ten + ": \"" + cu + "\"  →  \"" + dich + "\"");
        return true;
    }

    private bool DoiSprite(SerializedObject so, string ten, Sprite moi, List<string> log)
    {
        var p = so.FindProperty(ten);
        if (p == null) { log.Add(ten + ": KHÔNG có field này"); return false; }
        var cu = p.objectReferenceValue as Sprite;
        if (cu == moi) return false;
        p.objectReferenceValue = moi;
        log.Add(ten + ": " + TenSprite(cu) + "  →  " + TenSprite(moi));
        return true;
    }

    // ───────────────────────────── QUET THU ─────────────────────────────

    private void ChayQuetThu()
    {
        baoCao.Clear();
        Ghi("═══ QUÉT THỬ — KHÔNG GHI GÌ ═══");

        try
        {
            var clip = LayClipNhacNen();
            Ghi("① Nhạc nền — clip đích " + BGM_PATH + (clip != null ? "  ✔ có" : "  ✘ KHÔNG có"));
            foreach (string dong in QuetSceneTheoVanBan().Split('\n')) Ghi("   " + dong);
            Ghi("   (bản trong scene sẽ bị singleton AutoInit huỷ lúc chạy — xem cảnh báo trong cửa sổ)");
        }
        catch (Exception e)
        {
            Debug.LogError("[Vong24] Quét scene lỗi: " + e);
            Ghi("✘ Quét scene lỗi: " + e.Message);
        }

        try
        {
            Ghi("② Sản phẩm chuồng");
            foreach (var c in TimTatCaPenConfig())
            {
                if (c == null) continue;
                var spec = TimSpec(c);
                string lech = SoSanhVoiChuan(c, spec);
                Ghi("   " + Chuoi(c.penId) + " | " + c.name +
                    " | sp=" + Chuoi(c.productItemId) +
                    " | phụ=" + Chuoi(c.secondProductItemId) +
                    " | icon=" + TenSprite(c.productIcon) +
                    (string.IsNullOrEmpty(lech) ? "   ✔" : "   ✘ " + lech));
            }
            Ghi("   Sprite THỊT BÒ nạp được: " + (LaySpriteTheoGuid(BEEF_TEX_GUID) != null ? "có" : "KHÔNG"));
            Ghi("   Sprite SỮA nạp được:     " + (LaySpriteTheoGuid(MILK_TEX_GUID) != null ? "có" : "KHÔNG"));
        }
        catch (Exception e)
        {
            Debug.LogError("[Vong24] Quét chuồng lỗi: " + e);
            Ghi("✘ Quét chuồng lỗi: " + e.Message);
        }

        InRaConsole();
        LamMoiChanDoan();
    }

    // ───────────────────────────── HAM PHU ─────────────────────────────

    /// <summary>Doc file .unity nhu VAN BAN de biet scene nao co AudioManager va bgmMain tro vao dau.
    /// Cach nay khong phai mo scene nen khong lam ban project khi chi xem chan doan.</summary>
    private string QuetSceneTheoVanBan()
    {
        var sb = new StringBuilder();
        var ds = LayDanhSachScene();
        if (ds.Count == 0) return "   (Build Settings chưa có scene nào)";

        foreach (string path in ds)
        {
            try
            {
                if (!File.Exists(path)) { sb.AppendLine("   " + path + " — ✘ không có trên đĩa"); continue; }

                string[] dong = File.ReadAllLines(path);
                int soAM = 0;
                var moTa = new List<string>();

                for (int i = 0; i < dong.Length; i++)
                {
                    if (dong[i].IndexOf(AUDIOMANAGER_SCRIPT_GUID, StringComparison.Ordinal) < 0) continue;
                    soAM++;
                    string bgm = "(không thấy dòng bgmMain)";
                    for (int j = i; j < Mathf.Min(i + 40, dong.Length); j++)
                    {
                        string t = dong[j].Trim();
                        if (!t.StartsWith("bgmMain:", StringComparison.Ordinal)) continue;
                        bgm = t.Contains("fileID: 0") && !t.Contains("guid:") ? "TRỐNG" : DocGuidTrongDong(t);
                        break;
                    }
                    moTa.Add(bgm);
                }

                if (soAM == 0)
                    sb.AppendLine("   " + path + " — không có AudioManager đặt sẵn (dùng singleton runtime)");
                else
                    sb.AppendLine("   " + path + " — " + soAM + " AudioManager · bgmMain = " +
                                  string.Join(", ", moTa.ToArray()));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Vong24] Không đọc được " + path + ": " + e.Message);
                sb.AppendLine("   " + path + " — ✘ lỗi đọc: " + e.Message);
            }
        }
        return sb.ToString().TrimEnd();
    }

    private string DocGuidTrongDong(string dong)
    {
        int k = dong.IndexOf("guid:", StringComparison.Ordinal);
        if (k < 0) return "TRỐNG";
        string phanConLai = dong.Substring(k + 5).Trim();
        int dauPhay = phanConLai.IndexOf(',');
        string guid = dauPhay > 0 ? phanConLai.Substring(0, dauPhay).Trim() : phanConLai.Trim();

        if (guid == BGM_GUID) return "soft click.mp3  ✔ ĐÃ ĐÚNG";
        string p = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(p) ? ("guid " + guid + " (không tra được)") : Path.GetFileName(p);
    }

    private List<string> LayDanhSachScene()
    {
        var ds = new List<string>();
        try
        {
            var bs = EditorBuildSettings.scenes;
            if (bs != null)
                foreach (var s in bs)
                    if (s != null && !string.IsNullOrEmpty(s.path) && !ds.Contains(s.path))
                        ds.Add(s.path);
        }
        catch (Exception e) { Debug.LogWarning("[Vong24] Đọc Build Settings lỗi: " + e.Message); }

        if (ds.Count == 0)
        {
            // Du phong: quet toan bo scene trong project
            try
            {
                foreach (string g in AssetDatabase.FindAssets("t:Scene"))
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (!string.IsNullOrEmpty(p) && p.StartsWith("Assets/", StringComparison.Ordinal) && !ds.Contains(p))
                        ds.Add(p);
                }
            }
            catch (Exception e) { Debug.LogWarning("[Vong24] Quét scene dự phòng lỗi: " + e.Message); }
        }
        return ds;
    }

    private List<AudioManager> GomAudioManager(Scene sc)
    {
        var kq = new List<AudioManager>();
        try
        {
            foreach (var root in sc.GetRootGameObjects())
            {
                if (root == null) continue;
                foreach (var am in root.GetComponentsInChildren<AudioManager>(true))
                    if (am != null && !kq.Contains(am)) kq.Add(am);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Vong24] Gom AudioManager lỗi ở " + sc.path + ": " + e);
        }
        return kq;
    }

    private AudioClip LayClipNhacNen()
    {
        try
        {
            string p = AssetDatabase.GUIDToAssetPath(BGM_GUID);
            if (string.IsNullOrEmpty(p)) p = BGM_PATH;
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (c != null) return c;
        }
        catch (Exception e) { Debug.LogWarning("[Vong24] Nạp clip lỗi: " + e.Message); }

        try
        {
            foreach (string g in AssetDatabase.FindAssets("\"soft click\" t:AudioClip"))
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
                if (c != null) return c;
            }
        }
        catch (Exception e) { Debug.LogWarning("[Vong24] Tìm clip dự phòng lỗi: " + e.Message); }
        return null;
    }

    // Nho lai sprite da nap de OnGUI khong goi AssetDatabase moi frame.
    private readonly Dictionary<string, Sprite> cacheSprite = new Dictionary<string, Sprite>();

    /// <summary>Nap sprite dau tien trong mot texture (anh cat Multiple thi lay sub-sprite _0).</summary>
    private Sprite LaySpriteTheoGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;

        Sprite daCo;
        if (cacheSprite.TryGetValue(guid, out daCo) && daCo != null) return daCo;

        Sprite tim = NapSpriteTuDia(guid);
        cacheSprite[guid] = tim;
        return tim;
    }

    private Sprite NapSpriteTuDia(string guid)
    {
        try
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(p)) return null;

            var truc = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (truc != null) return truc;

            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                var s = o as Sprite;
                if (s != null) return s;
            }
        }
        catch (Exception e) { Debug.LogWarning("[Vong24] Nạp sprite " + guid + " lỗi: " + e.Message); }
        return null;
    }

    private List<PenMiniPanelConfig> TimTatCaPenConfig()
    {
        var kq = new List<PenMiniPanelConfig>();
        try
        {
            foreach (string g in AssetDatabase.FindAssets("t:PenMiniPanelConfig"))
            {
                var a = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(AssetDatabase.GUIDToAssetPath(g));
                if (a != null) kq.Add(a);
            }
            kq.Sort((a, b) => string.Compare(Chuoi(a.penId) + a.name, Chuoi(b.penId) + b.name, StringComparison.Ordinal));
        }
        catch (Exception e) { Debug.LogError("[Vong24] Tìm PenMiniPanelConfig lỗi: " + e); }
        return kq;
    }

    private PenSpec TimSpec(PenMiniPanelConfig c)
    {
        if (c == null) return null;
        foreach (var s in BANG_CHUAN)
        {
            if (!string.IsNullOrEmpty(c.penId) && c.penId == s.penId) return s;
            if (string.IsNullOrEmpty(c.penId) && !string.IsNullOrEmpty(c.name) &&
                c.name.IndexOf(s.penId.Replace("pen_", "Pen"), StringComparison.OrdinalIgnoreCase) >= 0) return s;
        }
        return null;
    }

    private string SoSanhVoiChuan(PenMiniPanelConfig c, PenSpec spec)
    {
        if (c == null) return "";
        if (spec == null) return "penId lạ, không có trong bảng chuẩn";

        var loi = new List<string>();
        if (Chuoi(c.productItemId) != spec.productId)
            loi.Add("sản phẩm phải là \"" + spec.productId + "\"");
        if (Chuoi(c.secondProductItemId) != spec.secondProductId)
            loi.Add(string.IsNullOrEmpty(spec.secondProductId)
                ? "chuồng này KHÔNG được có sản phẩm phụ"
                : "sản phẩm phụ phải là \"" + spec.secondProductId + "\"");

        var chuan = LaySpriteTheoGuid(spec.iconTexGuid);
        if (chuan != null && c.productIcon != chuan)
            loi.Add("icon phải là \"" + chuan.name + "\"");
        if (string.IsNullOrEmpty(spec.secondProductId) && c.secondProductIcon != null)
            loi.Add("icon phụ phải trống");

        return string.Join("; ", loi.ToArray());
    }

    private static string KhoaSprite(Sprite s)
    {
        if (s == null) return "";
        try
        {
            string guid; long id;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out guid, out id))
                return guid + ":" + id;
        }
        catch (Exception e) { Debug.LogWarning("[Vong24] Khoá sprite lỗi: " + e.Message); }
        return s.name;
    }

    private static string TenSprite(Sprite s)
    {
        return s == null ? "(trống)" : s.name;
    }

    private static string Chuoi(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim();
    }

    private void Ghi(string dong)
    {
        baoCao.Add(dong);
    }

    private void InRaConsole()
    {
        Debug.Log("[Vong24FixTool]\n" + string.Join("\n", baoCao.ToArray()));
        Repaint();
    }
}
#endif
