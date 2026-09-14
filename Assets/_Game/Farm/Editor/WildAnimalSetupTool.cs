#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ============================================================================
/// Tools/Map45/25. Gan Di Lai Cho Thu Hoang
/// ============================================================================
///
/// LAM GI
/// ------
/// Quet scene DANG MO, tim cac doi tuong nhin ten la con thu (bo / bo nau / heo /
/// ga / vit / cuu / cho / meo / chim / buom...), hien danh sach TICK CHON kem
/// tinh trang tung con, roi gan <see cref="WildAnimalWander"/> voi thong so mac
/// dinh RIENG THEO LOAI.
///
/// AN TOAN
/// -------
///   • Mac dinh chi XEM TRUOC — khong sua gi cho toi khi bam nut ap dung va
///     xac nhan them mot lan nua trong hop thoai.
///   • THU TRONG CHUONG bi loai tu dong: bat ky con nao co LivestockAI (o chinh
///     no hoac o cay cha) hoac nam duoi mot doi tuong ten bat dau bang "Pen_"
///     deu bi danh dau BO QUA va khong the tick. Thu trong chuong da co AI rieng,
///     gan them script di lai tu do se keo no ra khoi rao.
///   • Doi tuong khong co SpriteRenderer cung bi bo qua (khong co gi de nhin thay).
///   • Con nao da co WildAnimalWander thi bao "da co" va bo qua.
///   • Moi thay doi deu di qua Undo — Ctrl+Z mot phat la tra lai nhu cu.
///
/// KHONG DUNG cho prefab asset — tool chi sua doi tuong TRONG SCENE dang mo.
/// </summary>
public class WildAnimalSetupTool : EditorWindow
{
    // ── Mac dinh theo loai ───────────────────────────────────────────────

    /// <summary>Bo thong so mac dinh cho mot loai thu.</summary>
    private class MacDinhLoai
    {
        public string tenLoai;
        public string[] tuKhoa;      // chu thuong, khong dau
        public float banKinh;
        public float tocDo;
        public float dungYenMin, dungYenMax;
        public float gamCoMin, gamCoMax;
        public float xacSuatGamCo;
    }

    // Ban kinh tinh theo o iso 300 x 150 world unit:
    //   ga   ~ 0.6 o  (loang quanh, buoc nhanh, moi lan an rat ngan)
    //   heo  ~ 0.8 o
    //   bo   ~ 1.1 o  (di cham, gam co lau — dung nhat voi bo tha dong)
    private static readonly MacDinhLoai[] BangLoai =
    {
        new MacDinhLoai {
            tenLoai = "Bo",  tuKhoa = new[] { "cow", "_bo", "conbo", "bosua", "bothit" },
            banKinh = 330f, tocDo = 32f,
            dungYenMin = 2.0f, dungYenMax = 5.5f,
            gamCoMin = 4.0f, gamCoMax = 9.0f, xacSuatGamCo = 0.70f
        },
        new MacDinhLoai {
            tenLoai = "Heo", tuKhoa = new[] { "pig", "piggy", "heo" },
            banKinh = 240f, tocDo = 38f,
            dungYenMin = 1.5f, dungYenMax = 4.0f,
            gamCoMin = 3.0f, gamCoMax = 6.5f, xacSuatGamCo = 0.65f
        },
        new MacDinhLoai {
            tenLoai = "Ga",  tuKhoa = new[] { "chicken", "_ga", "conga", "gacon", "gatrong", "gamai" },
            banKinh = 170f, tocDo = 58f,
            dungYenMin = 0.6f, dungYenMax = 2.0f,
            gamCoMin = 1.0f, gamCoMax = 2.8f, xacSuatGamCo = 0.75f
        },
        new MacDinhLoai {
            tenLoai = "Vit", tuKhoa = new[] { "duck", "vit" },
            banKinh = 190f, tocDo = 48f,
            dungYenMin = 0.8f, dungYenMax = 2.4f,
            gamCoMin = 1.5f, gamCoMax = 3.5f, xacSuatGamCo = 0.70f
        },
        new MacDinhLoai {
            tenLoai = "Cuu", tuKhoa = new[] { "sheep", "cuu", "conde" },
            banKinh = 300f, tocDo = 34f,
            dungYenMin = 1.8f, dungYenMax = 5.0f,
            gamCoMin = 4.0f, gamCoMax = 8.0f, xacSuatGamCo = 0.75f
        },
        new MacDinhLoai {
            tenLoai = "Cho/Meo", tuKhoa = new[] { "dog", "_cat", "concho", "conmeo", "chocon", "meocon" },
            banKinh = 380f, tocDo = 70f,
            dungYenMin = 0.8f, dungYenMax = 3.0f,
            gamCoMin = 1.5f, gamCoMax = 4.0f, xacSuatGamCo = 0.25f
        },
        new MacDinhLoai {
            tenLoai = "Chim/Buom", tuKhoa = new[] { "bird", "butterfly", "chim", "buom" },
            banKinh = 260f, tocDo = 85f,
            dungYenMin = 0.4f, dungYenMax = 1.6f,
            gamCoMin = 0.8f, gamCoMax = 2.0f, xacSuatGamCo = 0.35f
        }
    };

    private static readonly MacDinhLoai LoaiChung = new MacDinhLoai
    {
        tenLoai = "Khac",
        tuKhoa = new string[0],
        banKinh = 280f, tocDo = 40f,
        dungYenMin = 1.5f, dungYenMax = 4.5f,
        gamCoMin = 2.5f, gamCoMax = 6.5f, xacSuatGamCo = 0.55f
    };

    /// <summary>Chuoi ten (chu thuong) khien mot doi tuong CHAC CHAN khong phai con thu.</summary>
    private static readonly string[] TenLoaiTru =
    {
        "scarecrow", "bunhin", "chuong", "pen_", "icon", "btn_", "img_", "txt_", "popup", "canvas"
    };

    // ── Ket qua quet ─────────────────────────────────────────────────────

    private class UngVien
    {
        public GameObject go;
        public MacDinhLoai loai;
        public bool tick;
        public bool boQua;        // khong duoc phep gan
        public string lyDo;       // vi sao bo qua / ghi chu
    }

    private readonly List<UngVien> _dsUngVien = new List<UngVien>();
    private Vector2 _cuon;
    private bool _daQuet;
    private bool _chiHienGanDuoc = false;

    // ── Cua so ───────────────────────────────────────────────────────────

    [MenuItem("Tools/Map45/25. Gan Di Lai Cho Thu Hoang", false, 25)]
    private static void MoCuaSo()
    {
        WildAnimalSetupTool w = GetWindow<WildAnimalSetupTool>(true, "25. Gan Di Lai Cho Thu Hoang");
        w.minSize = new Vector2(560f, 420f);
        w.Quet();
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Quet scene DANG MO, tim cac doi tuong ten giong con thu va gan script " +
            "WildAnimalWander (di lai + gam co quanh cho no dang dung).\n\n" +
            "Thu TRONG CHUONG (co LivestockAI, hoac nam duoi Pen_xx) bi loai tu dong — " +
            "chung da co AI rieng.\n\n" +
            "Buoc 1: xem truoc va tick. Buoc 2: bam AP DUNG (con hoi xac nhan).",
            MessageType.Info);

        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Quet lai scene", GUILayout.Height(24f))) Quet();
            _chiHienGanDuoc = GUILayout.Toggle(_chiHienGanDuoc, "Chi hien con gan duoc",
                                               "Button", GUILayout.Height(24f), GUILayout.Width(180f));
        }

        if (!_daQuet)
        {
            EditorGUILayout.LabelField("Chua quet.");
            return;
        }

        int soGanDuoc = 0, soTick = 0;
        for (int i = 0; i < _dsUngVien.Count; i++)
        {
            if (_dsUngVien[i].boQua) continue;
            soGanDuoc++;
            if (_dsUngVien[i].tick) soTick++;
        }

        EditorGUILayout.LabelField(
            string.Format("Tim thay {0} doi tuong giong con thu — gan duoc {1}, dang tick {2}.",
                          _dsUngVien.Count, soGanDuoc, soTick),
            EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Tick tat ca")) DatTickTatCa(true);
            if (GUILayout.Button("Bo tick tat ca")) DatTickTatCa(false);
        }

        EditorGUILayout.Space(4f);

        _cuon = EditorGUILayout.BeginScrollView(_cuon);
        for (int i = 0; i < _dsUngVien.Count; i++)
        {
            UngVien uv = _dsUngVien[i];
            if (uv.go == null) continue;
            if (_chiHienGanDuoc && uv.boQua) continue;

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                using (new EditorGUI.DisabledScope(uv.boQua))
                {
                    uv.tick = EditorGUILayout.Toggle(uv.tick, GUILayout.Width(18f));
                }

                EditorGUILayout.ObjectField(uv.go, typeof(GameObject), true, GUILayout.Width(190f));
                EditorGUILayout.LabelField(uv.loai.tenLoai, GUILayout.Width(70f));
                EditorGUILayout.LabelField(
                    uv.boQua ? ("BO QUA — " + uv.lyDo)
                             : string.Format("R={0:0} toc do={1:0}  {2}",
                                             uv.loai.banKinh, uv.loai.tocDo, uv.lyDo));
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);

        using (new EditorGUI.DisabledScope(soTick == 0))
        {
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button(string.Format("AP DUNG — gan WildAnimalWander cho {0} con", soTick),
                                 GUILayout.Height(30f)))
            {
                ApDung(soTick);
            }
            GUI.backgroundColor = Color.white;
        }
    }

    // ── Quet ─────────────────────────────────────────────────────────────

    private void Quet()
    {
        _dsUngVien.Clear();
        _daQuet = true;

        Transform[] tatCa = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                FindObjectsSortMode.None);
        for (int i = 0; i < tatCa.Length; i++)
        {
            Transform t = tatCa[i];
            if (t == null) continue;

            GameObject go = t.gameObject;
            MacDinhLoai loai = DoanLoai(go.name);
            if (loai == null) continue;

            // Chi lay GOC cua con thu: neu cha cung la mot con thu thi day chi la
            // bo phan (head / body...) — bo qua khong liet ke.
            if (t.parent != null && DoanLoai(t.parent.gameObject.name) != null) continue;

            UngVien uv = new UngVien { go = go, loai = loai, lyDo = "" };

            if (go.GetComponent<WildAnimalWander>() != null)
            {
                uv.boQua = true; uv.lyDo = "da co WildAnimalWander";
            }
            else if (NamTrongChuong(t))
            {
                uv.boQua = true; uv.lyDo = "thu trong chuong (LivestockAI / Pen_xx)";
            }
            else if (go.GetComponentInChildren<SpriteRenderer>(true) == null)
            {
                uv.boQua = true; uv.lyDo = "khong co SpriteRenderer";
            }
            else
            {
                uv.tick = true;
                uv.lyDo = (go.GetComponentInChildren<Animator>(true) != null)
                    ? "co Animator" : "khong co Animator (van di duoc)";
            }

            _dsUngVien.Add(uv);
        }

        _dsUngVien.Sort(delegate (UngVien a, UngVien b)
        {
            int c = string.CompareOrdinal(a.loai.tenLoai, b.loai.tenLoai);
            return c != 0 ? c : string.CompareOrdinal(a.go.name, b.go.name);
        });
    }

    /// <summary>Doan loai theo ten (chu thuong, bo dau cach/gach). Null = khong phai con thu.</summary>
    private static MacDinhLoai DoanLoai(string ten)
    {
        if (string.IsNullOrEmpty(ten)) return null;

        string s = ten.ToLowerInvariant().Replace(" ", "").Replace("-", "");

        // Chan nham lan da gap that trong SCN_Farm: "Prefab_Scarecrow" chua chuoi "cow"
        // nhung la BU NHIN, khong phai con bo.
        for (int k = 0; k < TenLoaiTru.Length; k++)
        {
            if (s.Contains(TenLoaiTru[k])) return null;
        }

        for (int i = 0; i < BangLoai.Length; i++)
        {
            string[] tk = BangLoai[i].tuKhoa;
            for (int j = 0; j < tk.Length; j++)
            {
                if (s.Contains(tk[j])) return BangLoai[i];
            }
        }

        // "Prefab_Cow_Brown" da khop "cow" o tren; nhanh nay danh cho ten dat rieng
        // co chu "animal"/"thu" ma khong ro loai.
        if (s.Contains("animal") || s.Contains("thuhoang")) return LoaiChung;

        return null;
    }

    /// <summary>
    /// Con thu nay co thuoc mot chuong khong? True neu chinh no hoac bat ky doi cha nao
    /// mang LivestockAI, hoac ten mot doi cha bat dau bang "Pen_".
    /// So sanh LivestockAI theo TEN KIEU nen file nay khong phu thuoc namespace cua no.
    /// </summary>
    private static bool NamTrongChuong(Transform t)
    {
        Transform cur = t;
        while (cur != null)
        {
            if (cur.name.StartsWith("Pen_")) return true;

            MonoBehaviour[] mbs = cur.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++)
            {
                if (mbs[i] == null) continue;
                if (mbs[i].GetType().Name == "LivestockAI") return true;
            }

            cur = cur.parent;
        }
        return false;
    }

    private void DatTickTatCa(bool bat)
    {
        for (int i = 0; i < _dsUngVien.Count; i++)
        {
            if (_dsUngVien[i].boQua) continue;
            _dsUngVien[i].tick = bat;
        }
    }

    // ── Ap dung ──────────────────────────────────────────────────────────

    private void ApDung(int soTick)
    {
        bool ok = EditorUtility.DisplayDialog(
            "Gan di lai cho thu hoang",
            string.Format(
                "Se gan script WildAnimalWander cho {0} con thu trong scene dang mo.\n\n" +
                "Thu trong chuong KHONG bi dong vao.\n" +
                "Co the hoan tac bang Ctrl+Z.\n\nTiep tuc?", soTick),
            "Gan di", "Huy");

        if (!ok) return;

        int nhom = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Gan WildAnimalWander cho thu hoang");

        int daGan = 0;
        for (int i = 0; i < _dsUngVien.Count; i++)
        {
            UngVien uv = _dsUngVien[i];
            if (uv.boQua || !uv.tick || uv.go == null) continue;

            WildAnimalWander w = Undo.AddComponent<WildAnimalWander>(uv.go);
            if (w == null) continue;

            w.banKinhDiLai = uv.loai.banKinh;
            w.tocDoDiBo = uv.loai.tocDo;
            w.dungYenMin = uv.loai.dungYenMin;
            w.dungYenMax = uv.loai.dungYenMax;
            w.gamCoMin = uv.loai.gamCoMin;
            w.gamCoMax = uv.loai.gamCoMax;
            w.xacSuatGamCo = uv.loai.xacSuatGamCo;

            EditorUtility.SetDirty(uv.go);
            daGan++;
        }

        Undo.CollapseUndoOperations(nhom);

        if (daGan > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log(string.Format("[ThuHoang] Da gan WildAnimalWander cho {0} con. " +
                                "Nho Ctrl+S de luu scene.", daGan));

        EditorUtility.DisplayDialog(
            "Xong",
            string.Format("Da gan cho {0} con thu.\n\nBam Ctrl+S de luu scene, roi Play thu.", daGan),
            "OK");

        Quet();
    }
}
#endif
