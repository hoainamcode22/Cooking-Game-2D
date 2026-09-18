#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ============================================================================
/// Tools/Map45/11. Chia Lo Dat — tu hang rao + PHU KIN BAN DO
/// ============================================================================
///
/// HAI NGUON LO DAT
/// ----------------
///   A. LO VE TAY  — designer ve DUONG KE khep kin (lop nao cung duoc; trong du
///                   an nay luoi chia lo nam tren Tilemap_IsoDock, con
///                   Tilemap_IsoFence lai la VIEN DAO — ten bi nguoc voi noi dung).
///                   Tool flood-fill ben trong moi vong ke => mot lo.
///   B. LO PHU KIN — phan dat con lai cua ban do (o nao co tile nen) duoc cat
///                   thanh cac block vuong NxN => lo. Nho vay CA BAN DO deu
///                   chia duoc lo, khong chi rieng cho da ve rao.
///
/// SAU KHI SINH
/// ------------
///   • Moi lo = mot asset LandRegionData trong Assets/_Game/Farm/Land.
///   • Gia + cap mo tang dan theo khoang cach toi lo trung tam.
///   • Dieu kien mo: phai mo mot lo KE BEN truoc (mo rong lan toa, khong nhay coc).
///   • Bang chi duong Prefab_Sprite_Sign_right duoc cam giua moi lo luc chay game.
///
/// VONG 15 — SUA:
///   • Truoc day tool gan nham Tilemap_IsoDock vao field "hang rao" cua
///     LandExpansionManager => vao game tat nham renderer cau tau, hang rao van
///     hien. Nay tool CHI gan tilemap co chu "Fence" trong ten, va canh bao do
///     neu ban chon nham.
///   • Tu danh dau scene dirty + hoi luu, de field khong bi mat khi doi scene.
/// </summary>
public class IsoFenceLotScanner : EditorWindow
{
    private const string AssetFolder = "Assets/_Game/Farm/Land";
    private const string SignPrefabPath =
        "Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Art/Environment/Signs/Sign Right/Prefab_Sprite_Sign_right.prefab";

    // ── Nguon ────────────────────────────────────────────────────────────
    // LUU Y TEN LOP BI NGUOC VOI NOI DUNG trong du an nay (da do scene):
    //   Tilemap_IsoFence = VIEN DAO (hinh thoi lon bao quanh ban do)
    //   Tilemap_IsoDock  = LUOI CHIA LO ma designer ve tay
    // Nen tool cho chon TU DO, khong ep theo ten.
    private Tilemap fenceTilemap;         // lop VE DUONG KE CHIA LO
    private Tilemap borderTilemap;        // lop VIEN BAN DO (dung lam tuong chan)
    private bool useBorderAsWall = true;
    private bool hideAllDividerCells = true;
    private int minLotCells = 6;
    private int maxLotCells = 4000;

    // VONG 16 — CHIA DEU: bo qua hinh lo ve tay, cat toan bo dat thanh luoi deu nhau.
    // Sep chon cach nay vi luoi ve tay khong deu; duong ke van duoc AN khi vao game.
    private bool evenGridWholeMap = true;

    private readonly List<Vector2Int> dividerHideCells = new List<Vector2Int>();
    private readonly List<bool> lotHasBuilding = new List<bool>();   // lo dang co nha/ruong cua nguoi choi
    private int buildingCellCount;

    // ── Phu kin ban do ───────────────────────────────────────────────────
    private bool fillWholeMap = true;
    [SerializeField] private int  blockSize = 10;   // canh moi lo luoi (o) — 10x10 = ~100 o, bang co lo cu
    private int  maxLots      = 80;      // chan tren cho khoi sinh ca nghin asset

    // ═══════════════════════════════════════════════════════════════════
    // VONG 18 — CHIA LO TOAN BAN DO + TU DONG NE 3 VUNG CAM
    // -------------------------------------------------------------------
    // Sep duyet: cat CA BAN DO thanh lo co ~100 o (bang co lo cu), TRU:
    //   (1) khu duong ray / tau lua      (2) khu giua dang co cong trinh
    //   (3) cau tau + bien + bai cat
    // Khong bat Sep nhap toa do — tool tu do lay tu scene.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Bat = quay ve cach cu (chi lay dung cac khung rao ve tay).</summary>
    [SerializeField] private bool cheDoCu_KhungRaoVeTay = false;

    /// <summary>Bat = ap dung bo loc loai tru tu dong khi cat lo luoi.</summary>
    [SerializeField] private bool apDungLoaiTruTuDong = true;

    [SerializeField] private bool loaiTruDuongRay = true;
    [SerializeField] private int  banKinhNoRongDuongRay = 2;   // o, de lo khong sat ray

    // ═══════════════════════════════════════════════════════════════════
    // VONG 4 (DevAI) — MO KHOA VUNG BAC DUONG RAY
    // -------------------------------------------------------------------
    // Vung co rong phia BAC duong ray khong ra lo nao, KHONG phai vi thieu
    // hang rao (runtime khong doc hinh hoc hang rao), ma vi hai bo loc:
    //   1. BuildGreenLandMask() chi nhan tilemap co ten chua
    //      grass/dirt/stone/co_/dat_nen/mong  -> lop co phia bac ten khac bi bo.
    //   2. BuildDuongRayMask() lay HOP BAO RENDERER cua object ten rail/train/tau
    //      roi no rong them banKinhNoRongDuongRay o. Mot duong tau CHEO dai co
    //      hop bao om ca mot dai rong -> nuot luon vung bac.
    // Ba tuy chon duoi day de Sep mo khoa CO Y THUC. MAC DINH GIU NGUYEN
    // HANH VI CU (list rong + hai co tat) nen khong anh huong ban dang chay.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Ten (hoac mot phan ten) tilemap nhan THEM lam DAT XANH.</summary>
    [SerializeField] private List<string> tenLopDatXanhThem = new List<string>();

    /// <summary>
    /// Bat = lay O THAT cua duong ray (tung Collider2D / tung Renderer rieng le)
    /// thay vi MOT hop bao om het. Chinh xac hon voi duong tau cheo dai.
    /// </summary>
    [SerializeField] private bool dungOThatCuaRay = false;

    /// <summary>Bat = chi cam trong khung duoi day; ngoai khung khong cam.</summary>
    [SerializeField] private bool gioiHanVungCamRay = false;

    /// <summary>Khung gioi han vung cam duong ray (toa do O LUOI).</summary>
    [SerializeField] private RectInt rectGioiHanVungCamRay = new RectInt(-1000, -1000, 2000, 2000);

    private bool showTuyChonBac = false;

    [SerializeField] private bool loaiTruVungDaDatDo = true;
    [SerializeField] private int  banKinhNoRongVungDaDat = 1;

    [SerializeField] private bool loaiTruBienCatCauTau = true;

    /// <summary>Bat = coi MOI SpriteRenderer tren map la vat da dat (rat manh tay).</summary>
    [SerializeField] private bool coiMoiSpriteLaVatDaDat = false;

    /// <summary>Vung Sep tu tay khoanh them de KHONG chia lo (toa do O LUOI).</summary>
    [SerializeField] private List<RectInt> vungLoaiTruThem = new List<RectInt>();

    private bool showLoaiTru = true;

    /// <summary>So lieu cua lan loc gan nhat — dung cho bao cao quet thu.</summary>
    private class ThongKeLoaiTru
    {
        public int oDatThoTruocLoc;
        public int oXanh;
        public int boBienCatDock;
        public int boDuongRay;
        public int boVungDaDat;
        public int boThuCong;
        public int oConLai;
        public RectInt bienBanDo;
        public int soTilemapRay;
        public int soObjectRay;
        public int soObjectDaDat;
        public string ghiChu = "";
    }

    private ThongKeLoaiTru tkLoaiTru = new ThongKeLoaiTru();

    private readonly List<Tilemap> groundMaps = new List<Tilemap>();
    private readonly List<bool>    groundUse  = new List<bool>();
    private bool showGround = true;

    // ── Kinh te ──────────────────────────────────────────────────────────
    private enum PriceCurve { LuyThua_MuotHon, Nhan_TangNhanh }
    private PriceCurve priceCurve = PriceCurve.LuyThua_MuotHon;
    private int   basePrice     = 2000;
    private float priceExponent = 1.6f;   // dung cho LuyThua
    private float priceGrowth   = 1.6f;   // dung cho Nhan
    private int   baseUnlockLevel = 5;
    private int   maxUnlockLevel  = 60;
    private int   clearSeconds    = 300;

    private readonly List<List<Vector2Int>> lots = new List<List<Vector2Int>>();
    private int fencedCount;              // bao nhieu lo dau tien la lo ve tay
    private Vector2 scroll;
    private string status = "";

    [MenuItem("Tools/Map45/11. Chia Lo Dat (hang rao + phu kin map)", false, 11)]
    public static void Open() => GetWindow<IsoFenceLotScanner>("Chia Lo Dat");

    private void OnEnable() { AutoFindTilemaps(); CollectGroundMaps(); }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Doan nhan hai lop: VIEN BAN DO va LUOI CHIA LO.
    /// Vien = lop ten co 'Fence'. Luoi chia lo = lop con lai ten co 'Dock'/'Fence'
    /// va nhieu tile nhat (o day la Tilemap_IsoDock voi 1820 tile).
    /// </summary>
    private void AutoFindTilemaps()
    {
        var grid = GameObject.Find(IsoGrid.IsoGridObjectName);
        if (grid == null) return;
        var maps = grid.GetComponentsInChildren<Tilemap>(true);

        if (borderTilemap == null)
            foreach (var tm in maps) if (IsFence(tm)) { borderTilemap = tm; break; }

        Tilemap best = null; int bestCount = 0;
        foreach (var tm in maps)
        {
            if (tm == borderTilemap) continue;
            string n = tm.name.ToLowerInvariant();
            if (!n.Contains("dock") && !n.Contains("fence")) continue;
            int c = CountTiles(tm);
            if (c > bestCount) { bestCount = c; best = tm; }
        }
        if (best != null) fenceTilemap = best;
    }

    private void CollectGroundMaps()
    {
        groundMaps.Clear(); groundUse.Clear();
        var grid = GameObject.Find(IsoGrid.IsoGridObjectName);
        if (grid == null) return;

        foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            string n = tm.name.ToLowerInvariant();
            bool skip = n.Contains("fence") || n.Contains("locked") ||
                        n.Contains("water") || n.Contains("underwater") ||
                        n.Contains("overlay");
            groundMaps.Add(tm);
            groundUse.Add(!skip);
        }
    }

    private static bool IsFence(Tilemap t)
        => t != null && t.name.ToLowerInvariant().Contains("fence");

    private static int CountTiles(Tilemap t)
    {
        if (t == null) return 0;
        int c = 0;
        var b = t.cellBounds;
        foreach (var p in b.allPositionsWithin)
            if (t.GetTile(p) != null) c++;
        return c;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "A. Doc DUONG KE ban ve tay (lop nao cung duoc) -> tach lo khep kin.\n" +
            "B. Phan dat con lai cua ban do -> cat thanh lo luoi de PHU KIN MAP.\n" +
            "Sau do sinh du lieu + cam bang chi duong giua moi lo.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("A. Lo tu duong ke ve tay", EditorStyles.boldLabel);

        fenceTilemap = (Tilemap)EditorGUILayout.ObjectField(
            "Lop VE DUONG KE chia lo", fenceTilemap, typeof(Tilemap), true);
        borderTilemap = (Tilemap)EditorGUILayout.ObjectField(
            "Lop VIEN ban do", borderTilemap, typeof(Tilemap), true);
        useBorderAsWall = EditorGUILayout.ToggleLeft(
            "Dung vien ban do lam tuong chan (nen bat — lo sat mep moi khong bi loai)",
            useBorderAsWall);

        if (fenceTilemap != null)
        {
            EditorGUILayout.HelpBox(
                $"Duong ke chia lo doc tu: {fenceTilemap.name}\n" +
                (useBorderAsWall && borderTilemap != null && borderTilemap != fenceTilemap
                    ? $"Vien ban do (chi lam tuong, KHONG bi an): {borderTilemap.name}\n"
                    : "") +
                "Ten lop khong quan trong — tool khong loc theo chu 'Fence' nua.",
                MessageType.Info);
        }
        if (GUILayout.Button("Tu doan lai 2 lop")) AutoFindTilemaps();

        using (new EditorGUI.IndentLevelScope())
        {
            minLotCells = EditorGUILayout.IntField("O toi thieu moi lo", minLotCells);
            maxLotCells = EditorGUILayout.IntField("O toi da moi lo", maxLotCells);
            hideAllDividerCells = EditorGUILayout.ToggleLeft(
                "Vao game an HET tile tren lop duong ke (tat = chi an o giap lo)",
                hideAllDividerCells);
        }

        // ── B ────────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("B. Chia luoi", EditorStyles.boldLabel);
        evenGridWholeMap = EditorGUILayout.ToggleLeft(
            "CHIA DEU toan map — bo qua hinh lo ve tay (khuyen dung)", evenGridWholeMap);
        using (new EditorGUI.DisabledScope(evenGridWholeMap))
            fillWholeMap = EditorGUILayout.ToggleLeft("Cat not phan dat chua co lo thanh lo luoi", fillWholeMap);
        using (new EditorGUI.DisabledScope(!fillWholeMap && !evenGridWholeMap))
        using (new EditorGUI.IndentLevelScope())
        {
            blockSize = Mathf.Max(3, EditorGUILayout.IntField("Canh moi lo luoi (o)", blockSize));
            maxLots   = Mathf.Max(1, EditorGUILayout.IntField("Toi da bao nhieu lo", maxLots));

            showGround = EditorGUILayout.Foldout(showGround,
                $"Lop nen tinh la DAT ({groundMaps.Count} tilemap)", true);
            if (showGround)
            {
                if (groundMaps.Count == 0 && GUILayout.Button("Quet lai danh sach tilemap"))
                    CollectGroundMaps();
                for (int i = 0; i < groundMaps.Count; i++)
                {
                    if (groundMaps[i] == null) continue;
                    groundUse[i] = EditorGUILayout.ToggleLeft("   " + groundMaps[i].name, groundUse[i]);
                }
            }
        }

        // ── Kinh te ──────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Kinh te", EditorStyles.boldLabel);
        priceCurve = (PriceCurve)EditorGUILayout.EnumPopup("Kieu tang gia", priceCurve);
        basePrice  = EditorGUILayout.IntField("Gia lo dau tien", basePrice);
        if (priceCurve == PriceCurve.LuyThua_MuotHon)
            priceExponent = EditorGUILayout.Slider("Do doc (luy thua)", priceExponent, 1.1f, 2.5f);
        else
            priceGrowth = EditorGUILayout.FloatField("He so nhan moi lo", priceGrowth);
        baseUnlockLevel = EditorGUILayout.IntField("Cap mo lo gan nhat", baseUnlockLevel);
        maxUnlockLevel  = EditorGUILayout.IntField("Cap mo lo xa nhat",  maxUnlockLevel);
        clearSeconds    = EditorGUILayout.IntField("Giay don dep moi lo", clearSeconds);

        // ── VONG 18 — CHIA LO TOAN BAN DO + TU DONG NE VUNG CAM ──────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("C. Chia lô TOÀN BẢN ĐỒ (tự động né vùng cấm)",
                                   EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Cắt cả bản đồ thành lô ~" + (blockSize * blockSize) + " ô, TRỪ: đường ray / tàu lửa, " +
            "khu giữa đang có công trình, và cầu tàu + biển + bãi cát.\n" +
            "Không cần nhập toạ độ — tool tự đọc từ scene.",
            MessageType.Info);

        cheDoCu_KhungRaoVeTay = EditorGUILayout.ToggleLeft(
            "Quay về CÁCH CŨ (chỉ lấy đúng các khung rào vẽ tay — 28 lô)",
            cheDoCu_KhungRaoVeTay);

        showLoaiTru = EditorGUILayout.Foldout(showLoaiTru, "Vùng loại trừ tự động", true);
        if (showLoaiTru)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                apDungLoaiTruTuDong = EditorGUILayout.ToggleLeft(
                    "Bật bộ lọc loại trừ tự động (nên bật)", apDungLoaiTruTuDong);

                using (new EditorGUI.DisabledScope(!apDungLoaiTruTuDong))
                {
                    loaiTruDuongRay = EditorGUILayout.ToggleLeft(
                        "Né ĐƯỜNG RAY / TÀU LỬA", loaiTruDuongRay);
                    banKinhNoRongDuongRay = Mathf.Clamp(
                        EditorGUILayout.IntField(
                            new GUIContent("   Nới rộng quanh ray (ô)",
                                "NỚI RỘNG dải cấm quanh đường ray thêm N ô về MỌI phía. " +
                                "Mỗi +1 làm dải cấm phình ra 1 ô — đây là một trong hai lý do " +
                                "vùng cỏ phía Bắc không ra lô nào. Đặt 0 để chỉ cấm đúng vệt ray."),
                            banKinhNoRongDuongRay), 0, 20);

                    loaiTruVungDaDatDo = EditorGUILayout.ToggleLeft(
                        "Né KHU GIỮA đang có công trình / đồ đã đặt", loaiTruVungDaDatDo);
                    banKinhNoRongVungDaDat = Mathf.Clamp(
                        EditorGUILayout.IntField("   Nới rộng quanh công trình (ô)", banKinhNoRongVungDaDat), 0, 20);
                    coiMoiSpriteLaVatDaDat = EditorGUILayout.ToggleLeft(
                        "   Mạnh tay: coi MỌI sprite trên map là đồ đã đặt", coiMoiSpriteLaVatDaDat);

                    loaiTruBienCatCauTau = EditorGUILayout.ToggleLeft(
                        "Né CẦU TÀU / BIỂN / BÃI CÁT (chỉ giữ ô đất xanh)", loaiTruBienCatCauTau);

                    // ── VONG 4 — mo khoa vung Bac (mac dinh GIU NGUYEN hanh vi cu) ──
                    EditorGUILayout.Space(2);
                    showTuyChonBac = EditorGUILayout.Foldout(showTuyChonBac,
                        "Mở khoá vùng BẮC đường ray (nâng cao)", true);
                    if (showTuyChonBac)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.HelpBox(
                                "Vùng cỏ phía Bắc không ra lô vì HAI bộ lọc, không phải vì thiếu " +
                                "hàng rào:\n" +
                                "1) Lớp đất xanh chỉ nhận tên chứa grass/dirt/stone/co_/dat_nen/mong.\n" +
                                "2) Dải cấm đường ray lấy HỘP BAO renderer của object tên " +
                                "rail/train/tàu rồi nới rộng thêm — đường tàu chéo dài nuốt cả " +
                                "một dải rộng.\n" +
                                "Ba tuỳ chọn dưới đây MẶC ĐỊNH TẮT/RỖNG, bật rồi mới đổi kết quả.",
                                MessageType.Info);

                            EditorGUILayout.LabelField("Nhận THÊM tilemap làm đất xanh (một phần tên)",
                                                       EditorStyles.miniBoldLabel);
                            int xoaDongLop = -1;
                            for (int i = 0; i < tenLopDatXanhThem.Count; i++)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    tenLopDatXanhThem[i] = EditorGUILayout.TextField(tenLopDatXanhThem[i]);
                                    if (GUILayout.Button("−", GUILayout.Width(24))) xoaDongLop = i;
                                }
                            }
                            // xoa SAU vong lap de khong pha cau truc layout cua IMGUI
                            if (xoaDongLop >= 0) tenLopDatXanhThem.RemoveAt(xoaDongLop);
                            if (GUILayout.Button("+ Thêm tên lớp")) tenLopDatXanhThem.Add("");

                            EditorGUILayout.Space(2);
                            dungOThatCuaRay = EditorGUILayout.ToggleLeft(
                                new GUIContent("Cấm theo Ô THẬT của ray (collider/renderer rời)",
                                    "Tắt = gộp mọi renderer thành MỘT hộp bao (cách cũ, over-exclude " +
                                    "với đường tàu chéo). Bật = lấy từng Collider2D (không có thì từng " +
                                    "Renderer) riêng lẻ nên chỉ cấm đúng vệt ray."),
                                dungOThatCuaRay);

                            gioiHanVungCamRay = EditorGUILayout.ToggleLeft(
                                new GUIContent("Giới hạn dải cấm ray trong một khung ô",
                                    "Bật = mọi ô cấm nằm NGOÀI khung dưới đây sẽ được thả ra."),
                                gioiHanVungCamRay);
                            using (new EditorGUI.DisabledScope(!gioiHanVungCamRay))
                            {
                                rectGioiHanVungCamRay = EditorGUILayout.RectIntField(
                                    "   Khung cấm (ô)", rectGioiHanVungCamRay);
                            }
                        }
                    }

                    EditorGUILayout.LabelField(
                        $"Vùng khoanh tay thêm: {(vungLoaiTruThem == null ? 0 : vungLoaiTruThem.Count)}");
                    if (vungLoaiTruThem == null) vungLoaiTruThem = new List<RectInt>();
                    for (int i = 0; i < vungLoaiTruThem.Count; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            vungLoaiTruThem[i] = EditorGUILayout.RectIntField($"   Vùng {i}", vungLoaiTruThem[i]);
                            if (GUILayout.Button("X", GUILayout.Width(24)))
                            { vungLoaiTruThem.RemoveAt(i); break; }
                        }
                    }
                    if (GUILayout.Button("   + Thêm vùng loại trừ thủ công"))
                        vungLoaiTruThem.Add(new RectInt(0, 0, 10, 10));
                }
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("QUÉT THỬ (không ghi gì)", GUILayout.Height(30)))
            QuetThuChiaLoToanMap();

        GUI.backgroundColor = new Color(1f, 0.75f, 0.55f);
        if (GUILayout.Button("GHI THẬT — chia lô toàn bản đồ (sinh lại mã lô)",
                             GUILayout.Height(30)))
            GhiThatChiaLoToanMap(luuScene: true);
        GUI.backgroundColor = Color.white;

        // ── Nut ──────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        if (GUILayout.Button("1. Quet ban do -> tim lo", GUILayout.Height(30))) ScanLots();

        EditorGUILayout.Space();
        if (lots.Count > 0)
        {
            EditorGUILayout.LabelField(
                $"Tim thay {lots.Count} lo  ({fencedCount} tu hang rao, {lots.Count - fencedCount} lo luoi):",
                EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(200));
            for (int i = 0; i < lots.Count; i++)
            {
                var c = CenterCellOf(lots[i]);
                string kind = i < fencedCount ? "rao " : "luoi";
                bool open = i == 0 || (i < lotHasBuilding.Count && lotHasBuilding[i]);
                EditorGUILayout.LabelField(
                    $"  Lo {i,2} [{kind}]  {lots[i].Count,4} o  ·  tam ({c.x}, {c.y})  ·  " +
                    (open ? "MO SAN (dang co nha)" : $"{PriceOf(i, lots.Count):n0} vang  ·  cap {LevelOf(i, lots.Count)}"));
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.LabelField("Chua co lo nao — bam nut 1 o tren truoc.",
                                       EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(lots.Count == 0))
        {
            GUI.backgroundColor = lots.Count > 0 ? new Color(0.6f, 1f, 0.6f) : Color.white;
            if (GUILayout.Button(lots.Count > 0
                    ? $"2. Sinh du lieu {lots.Count} lo + cam bang chi duong"
                    : "2. Sinh du lieu lo + cam bang chi duong  (can quet truoc)",
                    GUILayout.Height(30)))
                GenerateRegions();
            GUI.backgroundColor = Color.white;
        }

        // Id cua lo la theo CHI SO (lot_00, lot_01...). Sinh lai lo => id cu trong
        // ban luu PlayerPrefs se tro nham sang vung dat khac. Nut nay xoa cho sach.
        EditorGUILayout.Space();
        if (GUILayout.Button("Xoa tien do mua dat da luu (PlayerPrefs)"))
        {
            PlayerPrefs.DeleteKey("FARM_UNLOCKED_REGIONS");
            for (int i = 0; i < 400; i++) PlayerPrefs.DeleteKey($"LAND_CLEAR_lot_{i:00}");
            PlayerPrefs.Save();
            status = "Da xoa tien do mua dat + cac cong truong dang don do.";
            Debug.Log($"[LoDat] {status}");
        }

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // QUET
    // ─────────────────────────────────────────────────────────────────────
    private void ScanLots()
    {
        lots.Clear();
        fencedCount = 0;
        dividerHideCells.Clear();
        status = "";

        lotHasBuilding.Clear();

        var taken = new HashSet<Vector2Int>();   // o da thuoc mot lo nao do
        var walls = new HashSet<Vector2Int>();   // TUONG = duong ke + vien
        var dividerWalls = new HashSet<Vector2Int>();  // rieng phan duong ke (se an)
        var borderWalls  = new HashSet<Vector2Int>();  // rieng phan vien dao (giu hien)
        string reportA = "A. Duong ke: bo qua (chua chon lop).\n";

        // nha / ruong nguoi choi da dat (doc tu save) -> lo chua chung se MO SAN
        var buildingCells = LoadPlacedBuildingCells();
        buildingCellCount = buildingCells.Count;

        // VONG 17 — DAT XANH = o co tile tren lop nen co / dat / da.
        // KHONG tinh cat, cau tau, nuoc: vung nao khong du dat xanh coi la BIEN, bo qua.
        var greenLand = BuildGreenLandMask();
        var homeCells = new HashSet<Vector2Int>();
        int soVungNha = 0;

        // ── A. Lo tu duong ke ve tay ─────────────────────────────────────
        if (fenceTilemap != null)
        {
            foreach (var p in fenceTilemap.cellBounds.allPositionsWithin)
                if (fenceTilemap.GetTile(p) != null)
                {
                    var c2 = new Vector2Int(p.x, p.y);
                    walls.Add(c2); dividerWalls.Add(c2);
                }

            // vien ban do CHI lam tuong chan, khong bi an trong game
            int borderCount = 0;
            if (useBorderAsWall && borderTilemap != null && borderTilemap != fenceTilemap)
            {
                foreach (var p in borderTilemap.cellBounds.allPositionsWithin)
                    if (borderTilemap.GetTile(p) != null)
                    {
                        var c2 = new Vector2Int(p.x, p.y);
                        walls.Add(c2); borderWalls.Add(c2); borderCount++;
                    }
            }

            if (evenGridWholeMap)
            {
                reportA = $"A. Che do CHIA DEU: bo qua hinh lo ve tay ({dividerWalls.Count} tile duong ke " +
                          "chi dung de an khi vao game).\n";
            }
            else if (walls.Count == 0)
            {
                reportA = "A. Duong ke: lop nay khong co tile nao.\n";
            }
            else
            {
                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                foreach (var w in walls)
                {
                    minX = Mathf.Min(minX, w.x); maxX = Mathf.Max(maxX, w.x);
                    minY = Mathf.Min(minY, w.y); maxY = Mathf.Max(maxY, w.y);
                }
                minX -= 2; maxX += 2; minY -= 2; maxY += 2;

                const int HardLimit = 600;
                if (maxX - minX + 1 > HardLimit || maxY - minY + 1 > HardLimit)
                {
                    int cx0 = (minX + maxX) / 2, cy0 = (minY + maxY) / 2;
                    minX = cx0 - HardLimit / 2; maxX = cx0 + HardLimit / 2;
                    minY = cy0 - HardLimit / 2; maxY = cy0 + HardLimit / 2;
                }

                var visited = new HashSet<Vector2Int>();
                int outdoor = 0, tooSmall = 0, tooBig = 0, keep = 0, biecBien = 0;

                for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                {
                    var start = new Vector2Int(x, y);
                    if (walls.Contains(start) || visited.Contains(start)) continue;

                    var region = new List<Vector2Int>();
                    var stack = new Stack<Vector2Int>();
                    stack.Push(start); visited.Add(start);
                    bool reachesOutside = false;

                    while (stack.Count > 0)
                    {
                        var c = stack.Pop();
                        region.Add(c);
                        foreach (var d in Dirs)
                        {
                            var n = c + d;
                            if (n.x < minX || n.x > maxX || n.y < minY || n.y > maxY)
                            { reachesOutside = true; continue; }
                            if (walls.Contains(n) || visited.Contains(n)) continue;
                            visited.Add(n); stack.Push(n);
                        }
                    }

                    if (reachesOutside)              { outdoor++;  continue; }
                    if (region.Count < minLotCells)  { tooSmall++; continue; }
                    if (region.Count > maxLotCells)  { tooBig++;   continue; }

                    // ── VÒNG 17 — NÉ 2 VÙNG THEO YÊU CẦU CỦA SẾP ────────────
                    // (1) Vùng ĐANG CHƠI: có nhà/ruộng người chơi đứng trong đó.
                    //     Đây là đất nhà, phải MỞ SẴN, không được bày biển bán.
                    //     Trước đây bản "chia đều 10x10" cắt cả vùng giữa làng
                    //     thành 152 lô đè lên chỗ Sếp đang chơi — chính là cái sai.
                    // (2) Vùng BIỂN / BÃI CÁT / CẦU TÀU: không phải đất trồng được,
                    //     nhận ra bằng cách đếm ô nằm trên lớp nền XANH (cỏ/đất/đá).
                    bool coNha = false;
                    foreach (var c in region) if (buildingCells.Contains(c)) { coNha = true; break; }

                    int oXanh = 0;
                    foreach (var c in region) if (greenLand.Contains(c)) oXanh++;
                    bool duDatXanh = oXanh >= region.Count * 0.5f;

                    if (!coNha && !duDatXanh) { biecBien++; continue; }

                    if (coNha) { homeCells.UnionWith(region); soVungNha++; }

                    lots.Add(region); keep++;
                    foreach (var c in region) taken.Add(c);
                }

                fencedCount = keep;
                reportA = $"A. Duong ke '{fenceTilemap.name}': {dividerWalls.Count} tile" +
                          (borderCount > 0 ? $" + {borderCount} tile vien '{borderTilemap.name}'" : "") +
                          $" -> {keep} lo\n" +
                          $"   (loai {outdoor} vung ngoai troi, {tooSmall} qua nho, {tooBig} qua to, " +
                          $"{biecBien} vung bien/bai cat)\n" +
                          $"   {soVungNha} vung dang co nha -> DAT NHA, mo san, khong cam bien.\n";
            }
        }

        // ── B. Phu kin phan dat con lai ──────────────────────────────────
        string reportB = "B. Phu kin: tat.\n";
        if (fillWholeMap || evenGridWholeMap)
        {
            var land = apDungLoaiTruTuDong ? BuildLandMaskDaLoc() : BuildLandMask();
            land.ExceptWith(taken);
            // Chia deu: duong ke se bi AN nen o do van la dat xai duoc -> chi bo vien dao.
            // Che do ve tay: bo ca duong ke (no la ranh giua cac lo).
            land.ExceptWith(evenGridWholeMap ? borderWalls : walls);

            if (land.Count == 0)
            {
                reportB = "B. Phu kin: khong con o dat nao trong (kiem tra lai cac lop nen da tick).\n";
            }
            else
            {
                // gom theo block vuong, roi tach cac manh roi nhau trong cung block
                var blocks = new Dictionary<Vector2Int, List<Vector2Int>>();
                foreach (var c in land)
                {
                    var key = new Vector2Int(FloorDiv(c.x, blockSize), FloorDiv(c.y, blockSize));
                    if (!blocks.TryGetValue(key, out var l)) blocks[key] = l = new List<Vector2Int>();
                    l.Add(c);
                }

                int added = 0, dropped = 0;
                var extra = new List<List<Vector2Int>>();
                foreach (var kv in blocks)
                {
                    foreach (var piece in SplitConnected(kv.Value))
                    {
                        if (piece.Count < minLotCells) { dropped++; continue; }
                        extra.Add(piece);
                    }
                }

                // gan NHA NGUOI CHOI truoc (neu co save), khong thi gan tam ban do —
                // lo xa se bi cat neu vuot maxLots
                var originCell = OriginCellOf(buildingCells.Count > 0 ? buildingCells
                                            : taken.Count > 0 ? taken : land);
                extra.Sort((a, b2) => Dist(CenterCellOf(a), originCell)
                                     .CompareTo(Dist(CenterCellOf(b2), originCell)));

                // Chia deu = phai PHU KIN: o nao khong thuoc lo nao se thanh dat tu do
                // (IsCellUnlocked tra true) => khong duoc cat bot lo.
                int room = evenGridWholeMap ? int.MaxValue : Mathf.Max(0, maxLots - lots.Count);
                for (int i = 0; i < extra.Count && added < room; i++) { lots.Add(extra[i]); added++; }

                reportB = $"B. Phu kin: {land.Count} o dat trong -> {extra.Count} manh, " +
                          $"lay {added} lo (block {blockSize}x{blockSize}, bo {dropped} manh vun" +
                          (extra.Count > added ? $", cat bot {extra.Count - added} lo do cham tran {maxLots}" : "") +
                          ").\n";
            }
        }

        // ── Sap xep: lo gan tam ban do dung truoc ────────────────────────
        // Lo VE TAY luon giu thu tu uu tien truoc lo luoi (index < fencedCount).
        var fenced = lots.GetRange(0, fencedCount);
        var grids  = lots.GetRange(fencedCount, lots.Count - fencedCount);
        var origin = buildingCells.Count > 0 ? OriginCellOf(buildingCells) : OriginCellOf(AllCellsOf(lots));
        fenced.Sort((a, b) => Dist(CenterCellOf(a), origin).CompareTo(Dist(CenterCellOf(b), origin)));
        grids .Sort((a, b) => Dist(CenterCellOf(a), origin).CompareTo(Dist(CenterCellOf(b), origin)));
        lots.Clear(); lots.AddRange(fenced); lots.AddRange(grids);

        // ── Lo nao dang co nha/ruong cua nguoi choi -> MO SAN, khong bat mua lai ──
        int openLots = 0;
        foreach (var l in lots)
        {
            bool has = false;
            foreach (var c in l) if (buildingCells.Contains(c)) { has = true; break; }
            lotHasBuilding.Add(has);
            if (has) openLots++;
        }

        // ── Chon cac o DUONG KE se bi an khi vao game ────────────────────
        // Chi an o nao GIAP MOT LO (8 huong) — nho vay neu tren cung lop con ve
        // cau tau that o xa thi cau tau khong bi dung toi.
        var lotCells = new HashSet<Vector2Int>();
        foreach (var l in lots) foreach (var c in l) lotCells.Add(c);

        foreach (var w in dividerWalls)
        {
            if (hideAllDividerCells) { dividerHideCells.Add(w); continue; }

            bool touches = false;
            for (int dx = -1; dx <= 1 && !touches; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (lotCells.Contains(new Vector2Int(w.x + dx, w.y + dy))) { touches = true; break; }
                }
            if (touches) dividerHideCells.Add(w);
        }

        string reportC = fenceTilemap == null ? "" :
            $"C. Vao game se an {dividerHideCells.Count}/{dividerWalls.Count} o tren " +
            $"'{fenceTilemap.name}'" +
            (hideAllDividerCells ? " (an het)." : " (chi o giap lo).") +
            " Thoat Play la hien lai nguyen ven.\n";

        string reportD = buildingCellCount > 0
            ? $"D. Doc save: {buildingCellCount} o dang co nha/ruong -> {openLots} lo se MO SAN.\n"
            : "D. Khong co save cong trinh -> chi lo 0 (gan tam) mo san.\n";

        status = reportA + reportB + reportC + reportD + $"=> TONG CONG {lots.Count} LO.\n";
        if (lots.Count == 0)
            status += "\nKhong ra lo nao. Thu bat 'Cat not phan dat chua co lo' hoac giam 'O toi thieu moi lo'.";
        else
            status += "\nBam nut 2 ben duoi de sinh du lieu + cam bang.";

        Debug.Log($"[LoDat] {status}");
        Repaint();
    }

    // ── Doc save cong trinh (PlayerPrefs FARM_PLACED_BUILDINGS) ─────────
    // Cong trinh nguoi choi dat KHONG nam trong scene luc Edit — chung duoc
    // Instantiate luc Play tu save nay. Nen phai doc thang JSON de biet o nao
    // dang co nha/ruong, roi mo san nhung lo do (khong thi nha nam tren dat khoa).
    [System.Serializable] private class SaveEntry { public string itemId; public float x, y; public int rot; }
    [System.Serializable] private class SaveRoot  { public int saveVersion; public List<SaveEntry> list; }

    private static HashSet<Vector2Int> LoadPlacedBuildingCells()
    {
        var cells = new HashSet<Vector2Int>();
        string json = PlayerPrefs.GetString(PlacementManager.BuildingsSaveKey, "");
        if (string.IsNullOrEmpty(json)) return cells;

        SaveRoot save = null;
        try { save = JsonUtility.FromJson<SaveRoot>(json); } catch { }
        if (save == null || save.list == null) return cells;

        // tra gridSize theo itemId
        var sizeById = new Dictionary<string, Vector2Int>();
        foreach (var guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d != null && !string.IsNullOrEmpty(d.itemID) && !sizeById.ContainsKey(d.itemID))
                sizeById[d.itemID] = d.gridSize;
        }

        foreach (var e in save.list)
        {
            if (e == null || string.IsNullOrEmpty(e.itemId)) continue;
            Vector2Int size = sizeById.TryGetValue(e.itemId, out var s) ? s : Vector2Int.one;
            size = IsoGrid.RotateSize(size, e.rot);
            RectInt r = IsoGrid.RectFromAnchor(new Vector3(e.x, e.y, 0f), size);
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    cells.Add(new Vector2Int(x, y));
        }
        return cells;
    }

    /// <summary>
    /// DAT XANH — o co tile tren lop CO / DAT / DA (khong tinh cat, cau tau, nuoc).
    /// Dung de loai cac vung nam ngoai bien / bai cat ra khoi danh sach lo ban.
    /// </summary>
    private HashSet<Vector2Int> BuildGreenLandMask()
    {
        var green = new HashSet<Vector2Int>();
        var grid = GameObject.Find(IsoGrid.IsoGridObjectName);
        if (grid == null) return green;

        foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            string n = tm.name.ToLowerInvariant();
            bool laDatXanh = n.Contains("grass") || n.Contains("dirt") || n.Contains("stone")
                             || n.Contains("co_") || n.Contains("dat_nen") || n.Contains("mong");
            bool laBien = n.Contains("sand") || n.Contains("dock") || n.Contains("water")
                          || n.Contains("fence") || n.Contains("overlay") || n.Contains("locked");

            // VONG 4 — lop Sep khai bao THEM: nhan thang, bo qua ca bo loc laBien
            // (mac dinh danh sach RONG => khong doi hanh vi cu).
            bool laLopThem = false;
            if (tenLopDatXanhThem != null)
            {
                foreach (var them in tenLopDatXanhThem)
                {
                    if (string.IsNullOrEmpty(them)) continue;
                    if (n.Contains(them.Trim().ToLowerInvariant())) { laLopThem = true; break; }
                }
            }

            if (!laLopThem && (!laDatXanh || laBien)) continue;

            foreach (var p in tm.cellBounds.allPositionsWithin)
                if (tm.GetTile(p) != null) green.Add(new Vector2Int(p.x, p.y));
        }
        return green;
    }

    /// <summary>Tap o duoc coi la DAT: o nao co tile tren cac lop nen da tick.</summary>
    private HashSet<Vector2Int> BuildLandMask()
    {
        var land = new HashSet<Vector2Int>();
        if (groundMaps.Count == 0) CollectGroundMaps();

        for (int i = 0; i < groundMaps.Count; i++)
        {
            if (i >= groundUse.Count || !groundUse[i]) continue;
            var tm = groundMaps[i];
            if (tm == null) continue;
            foreach (var p in tm.cellBounds.allPositionsWithin)
                if (tm.GetTile(p) != null) land.Add(new Vector2Int(p.x, p.y));
        }
        return land;
    }

    /// <summary>Tach mot tap o thanh cac manh LIEN NHAU (4 huong).</summary>
    private static List<List<Vector2Int>> SplitConnected(List<Vector2Int> cells)
    {
        var set = new HashSet<Vector2Int>(cells);
        var seen = new HashSet<Vector2Int>();
        var outList = new List<List<Vector2Int>>();

        foreach (var c0 in cells)
        {
            if (seen.Contains(c0)) continue;
            var piece = new List<Vector2Int>();
            var st = new Stack<Vector2Int>();
            st.Push(c0); seen.Add(c0);
            while (st.Count > 0)
            {
                var c = st.Pop();
                piece.Add(c);
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (!set.Contains(n) || seen.Contains(n)) continue;
                    seen.Add(n); st.Push(n);
                }
            }
            outList.Add(piece);
        }
        return outList;
    }

    private static int FloorDiv(int a, int b) => (int)Mathf.Floor(a / (float)b);

    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    private static int Dist(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private static Vector2Int CenterCellOf(List<Vector2Int> cells)
    {
        long sx = 0, sy = 0;
        foreach (var c in cells) { sx += c.x; sy += c.y; }
        return new Vector2Int((int)(sx / cells.Count), (int)(sy / cells.Count));
    }

    private static IEnumerable<Vector2Int> AllCellsOf(List<List<Vector2Int>> ls)
    {
        foreach (var l in ls) foreach (var c in l) yield return c;
    }

    private static Vector2Int OriginCellOf(IEnumerable<Vector2Int> cells)
    {
        long sx = 0, sy = 0; int n = 0;
        foreach (var c in cells) { sx += c.x; sy += c.y; n++; }
        return n == 0 ? Vector2Int.zero : new Vector2Int((int)(sx / n), (int)(sy / n));
    }

    // ─────────────────────────────────────────────────────────────────────
    // KINH TE
    // ─────────────────────────────────────────────────────────────────────
    private int PriceOf(int i, int total)
    {
        if (i == 0) return 0;
        float raw = priceCurve == PriceCurve.LuyThua_MuotHon
            ? basePrice * Mathf.Pow(i, priceExponent)
            : basePrice * Mathf.Pow(priceGrowth, i - 1);
        raw = Mathf.Min(raw, 50_000_000f);
        return Mathf.RoundToInt(raw / 10f) * 10;
    }

    private int LevelOf(int i, int total)
    {
        if (i == 0) return 0;
        if (total <= 2) return baseUnlockLevel;
        float t = (i - 1) / (float)(total - 2);          // 0 .. 1
        return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(baseUnlockLevel, maxUnlockLevel, t)));
    }

    // ─────────────────────────────────────────────────────────────────────
    // SINH DU LIEU
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// VONG 4 (DevAI) — CANH BAO CHAN TRUOC KHI GHI DE.
    /// Ham nay XOA + DANH SO LAI toan bo Lot_*.asset. Ban luu cua nguoi choi la
    /// PlayerPrefs["FARM_UNLOCKED_REGIONS"] = danh sach regionId ngan cach '|',
    /// nen danh so lai bien lo_07 da mua thanh MOT MANH DAT KHAC.
    /// silent = true chi dung tu GhiThatChiaLoToanMap(), noi DA co hop thoai xac
    /// nhan rieng + sao luu + anh xa lai tien do, nen khong hoi lan hai.
    /// </summary>
    private bool XacNhanGhiDeLot()
    {
        int soLotHienCo = 0;
        for (int i = 0; i < 600; i++)
            if (AssetDatabase.LoadAssetAtPath<LandRegionData>($"{AssetFolder}/Lot_{i:00}.asset") != null)
                soLotHienCo++;

        return EditorUtility.DisplayDialog(
            "⚠ GHI ĐÈ TOÀN BỘ LÔ ĐẤT",
            "Thao tác này sẽ XOÁ và ĐÁNH SỐ LẠI toàn bộ asset Lot_*.asset trong\n" +
            $"{AssetFolder}\n\n" +
            $"Hiện có {soLotHienCo} asset Lot_*. Sau khi chạy, số lô có thể đổi và\n" +
            "mã lô (regionId lot_00, lot_01…) sẽ TRỎ SANG MẢNH ĐẤT KHÁC.\n\n" +
            "Bản lưu của người chơi là PlayerPrefs[\"FARM_UNLOCKED_REGIONS\"] — một\n" +
            "danh sách regionId ngăn cách bằng '|'. Vì vậy LÔ ĐÃ MUA CỦA NGƯỜI CHƠI\n" +
            "CÓ THỂ BIẾN THÀNH MẢNH ĐẤT KHÁC, hoặc mất hẳn.\n\n" +
            "• Asset North_* và Land_* KHÔNG bị đụng tới.\n" +
            "• Muốn thêm lô mới mà KHÔNG ghi đè: dùng\n" +
            "  Tools ▸ Farm Game ▸ Land Region Author.\n\n" +
            "Vẫn ghi đè?",
            "Tôi hiểu — vẫn ghi đè", "Huỷ");
    }

    private void GenerateRegions(bool silent = false)
    {
        if (lots.Count == 0) return;

        // VONG 4 — chan truoc khi ghi de (xem XacNhanGhiDeLot).
        if (!silent && !XacNhanGhiDeLot())
        {
            status = "Da huy — khong ghi de Lot_*.asset.";
            Debug.Log($"[LoDat] {status}");
            return;
        }
        if (silent)
            Debug.LogWarning("[LoDat] Dang GHI DE toan bo Lot_*.asset (da xac nhan o buoc truoc). " +
                             "Tien do mua dat cua nguoi choi se duoc anh xa lai.");
        if (!Directory.Exists(AssetFolder)) Directory.CreateDirectory(AssetFolder);

        var signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SignPrefabPath);
        if (signPrefab == null)
            Debug.LogWarning($"[LoDat] Khong tim thay bang chi duong tai {SignPrefabPath} " +
                             "— bien se duoc ve bang code.");

        // tap o cua tung lo, de tinh lo KE BEN
        var cellSets = new List<HashSet<Vector2Int>>();
        foreach (var l in lots) cellSets.Add(new HashSet<Vector2Int>(l));

        var created = new List<LandRegionData>();
        for (int i = 0; i < lots.Count; i++)
        {
            var cells = lots[i];
            string id   = $"lot_{i:00}";
            string path = $"{AssetFolder}/Lot_{i:00}.asset";

            var data = AssetDatabase.LoadAssetAtPath<LandRegionData>(path);
            if (data == null)
            {
                data = CreateInstance<LandRegionData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.regionId    = id;
            bool open = i == 0 || (i < lotHasBuilding.Count && lotHasBuilding[i]);
            data.displayName = i == 0 ? "Đất khởi đầu" : $"Lô {i}";
            data.cellRects   = ToRects(cells);
            data.unlockedByDefault = open;      // lo dang co nha/ruong -> mo san
            data.unlockLevel = LevelOf(i, lots.Count);
            data.goldPrice   = PriceOf(i, lots.Count);
            data.gemPrice    = 0;
            data.clearSeconds = i == 0 ? 0 : clearSeconds;
            data.workerCount = 4;

            // ── Dieu kien: phai mo mot lo KE BEN (chi so nho hon) truoc ──
            // Nho vay dat mo rong lan toa tu nha ra, khong nhay coc sang goc map.
            data.requiredRegionIds = new List<string>();
            if (i > 0)
            {
                int neighbour = FindAdjacentLower(cellSets, i);
                if (neighbour >= 0) data.requiredRegionIds.Add($"lot_{neighbour:00}");
            }

            // ── Nguyen lieu tang dan ────────────────────────────────────
            data.materialCosts = new List<BuildMaterialCost>();
            if (i > 0)
            {
                int k = Mathf.Max(1, i);
                data.materialCosts.Add(new BuildMaterialCost("go", Mathf.Min(240, 6 + k * 3)));
                data.materialCosts.Add(new BuildMaterialCost("da", Mathf.Min(200, 4 + k * 2)));
                if (i >= 2) data.materialCosts.Add(new BuildMaterialCost("dinh", Mathf.Min(180, 2 + k * 2)));
                if (i >= 3) data.materialCosts.Add(new BuildMaterialCost("kinh", Mathf.Min(160, 2 + k)));
            }

            EditorUtility.SetDirty(data);
            created.Add(data);
        }

        // ── Xoa asset lo thua tu lan chay truoc ─────────────────────────
        int removed = 0;
        for (int i = lots.Count; i < lots.Count + 400; i++)
        {
            string path = $"{AssetFolder}/Lot_{i:00}.asset";
            var thua = AssetDatabase.LoadAssetAtPath<LandRegionData>(path);
            if (thua == null) continue;

            // VONG 4 — CHI duoc xoa asset ten bat dau bang "Lot_". Asset North_*
            // (do Land Region Author sinh) va Land_* (ho cu) phai song sot.
            string tenFile = Path.GetFileNameWithoutExtension(path);
            if (!tenFile.StartsWith("Lot_", System.StringComparison.Ordinal))
            {
                Debug.LogWarning($"[LoDat] Bo qua (khong phai Lot_): {path}");
                continue;
            }

            AssetDatabase.DeleteAsset(path); removed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Nap vao manager ─────────────────────────────────────────────
        var mgr = Object.FindFirstObjectByType<LandExpansionManager>();
        if (mgr == null)
        {
            var root = new GameObject("LandExpansion");
            mgr = root.AddComponent<LandExpansionManager>();
        }
        mgr.regions.Clear();
        mgr.regions.AddRange(created);
        if (signPrefab != null) mgr.signPrefab = signPrefab;
        mgr.hideFenceInPlayMode = true;
        mgr.hideWholeFenceLayer = false;      // giu vien dao, chi an duong ke chia lo

        // AN THEO O — khong tat ca lop, nen cau tau that (neu ve chung lop) van con
        string fenceNote;
        if (fenceTilemap != null && dividerHideCells.Count > 0)
        {
            mgr.dividerTilemap = fenceTilemap;
            mgr.dividerCells   = new List<Vector2Int>(dividerHideCells);
            fenceNote = $"Vao game se an {dividerHideCells.Count} o duong ke tren " +
                        $"'{fenceTilemap.name}'; vien ban do va cac tile khac giu nguyen.";
        }
        else
        {
            mgr.dividerTilemap = null;
            mgr.dividerCells   = new List<Vector2Int>();
            fenceNote = "Khong co o duong ke nao de an.";
        }

        // field cu: chi giu neu that su la lop hang rao rieng
        if (mgr.designerFenceTilemap != null && !IsFence(mgr.designerFenceTilemap))
            mgr.designerFenceTilemap = null;

        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);

        status = $"Da sinh {created.Count} lo vao {AssetFolder}" +
                 (removed > 0 ? $" (xoa {removed} lo thua cu)" : "") + ".\n" +
                 fenceNote + "\n" +
                 "DA danh dau scene thay doi — nho Ctrl+S de luu lai.";
        Debug.Log($"[LoDat] {status}");

        if (silent) return;                    // RunAll tu luu scene + tu bao cao
        if (EditorUtility.DisplayDialog("Chia Lo Dat", status + "\n\nLuu scene ngay bay gio?",
                                        "Luu ngay", "De sau"))
            EditorSceneManager.SaveOpenScenes();
    }

    // ─────────────────────────────────────────────────────────────────────
    // MENU 12 — MOT NUT LAM HET
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Chay tron bo: doan lop -> chia DEU toan map -> sinh du lieu + cam bang ->
    /// ghi danh sach o duong ke de an -> xoa tien do mua dat cu -> LUU SCENE.
    /// Sep chi can bam mot lan roi Play.
    /// </summary>
    [MenuItem("Tools/Map45/12. LAM HET - chia deu full map + cam bang + an rao + luu", false, 12)]
    public static void RunAll()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Chia Lo Dat", "Dang Play. Bam Stop roi chay lai.", "OK");
            return;
        }

        var w = GetWindow<IsoFenceLotScanner>("Chia Lo Dat");
        w.fenceTilemap = null; w.borderTilemap = null;
        w.AutoFindTilemaps();
        w.CollectGroundMaps();

        // VONG 18 — SEP DA DUYET: chia lo CA BAN DO.
        // Vong 17 phai quay ve khung rao ve tay vi ban chia deu cu de len khu giua
        // lang. Nay bo loc tu dong da ne: duong ray, khu dang co cong trinh, cau tau
        // + bien + bai cat. Muon quay lai cach cu -> bat cheDoCu_KhungRaoVeTay
        // trong cua so Tools/Map45/11.
        w.ApDungCheDoChiaLo();             // VONG 18 — mac dinh CHIA DEU TOAN MAP
        w.hideAllDividerCells = true;      // vào game ẩn sạch lớp vạch rào
        w.useBorderAsWall    = true;       // viền đá bờ biển khép kín bản đồ
        w.minLotCells        = 6;
        w.maxLotCells        = 200000;     // giữ luôn cả vùng giữa làng làm ĐẤT NHÀ
        w.maxLots            = 400;

        // VONG 18 — di qua luong AN TOAN: quet -> hoi lai -> sao luu -> sinh lai
        // -> ANH XA lai tien do mua dat sang ma lo moi -> luu scene.
        // (Ban cu xoa thang FARM_UNLOCKED_REGIONS => Sep mat sach dat da mua.)
        w.GhiThatChiaLoToanMap(luuScene: true);
    }


    // ═════════════════════════════════════════════════════════════════════
    // VONG 18 — CHIA LO TOAN BAN DO (them moi, khong dung toi ham cu)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chon che do chia lo cho nut "LAM HET". Mac dinh = CHIA DEU TOAN MAP.
    /// Bat cheDoCu_KhungRaoVeTay de quay lai cach cu (chi 28 khung rao ve tay).
    /// </summary>
    private void ApDungCheDoChiaLo()
    {
        if (cheDoCu_KhungRaoVeTay)
        {
            evenGridWholeMap = false;
            fillWholeMap     = false;
        }
        else
        {
            evenGridWholeMap = true;
            fillWholeMap     = true;
            if (blockSize < 3) blockSize = 10;      // ~100 o / lo, bang co lo cu
        }
    }

    // ── Loc ten ─────────────────────────────────────────────────────────

    /// <summary>Tach ten thanh tu (cat theo ky tu la va theo chu HOA giua ten).</summary>
    private static List<string> TachTu(string ten)
    {
        var outList = new List<string>();
        if (string.IsNullOrEmpty(ten)) return outList;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < ten.Length; i++)
        {
            char ch = ten[i];
            bool laChuSo = (ch >= '0' && ch <= '9');
            bool laChu   = (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
            if (!laChu && !laChuSo)
            {
                if (sb.Length > 0) { outList.Add(sb.ToString().ToLowerInvariant()); sb.Length = 0; }
                continue;
            }
            // ranh gioi camelCase: 'a' roi 'B'
            if (i > 0 && ch >= 'A' && ch <= 'Z' && ten[i - 1] >= 'a' && ten[i - 1] <= 'z')
            {
                if (sb.Length > 0) { outList.Add(sb.ToString().ToLowerInvariant()); sb.Length = 0; }
            }
            sb.Append(ch);
        }
        if (sb.Length > 0) outList.Add(sb.ToString().ToLowerInvariant());
        return outList;
    }

    /// <summary>
    /// Ten co dinh toi DUONG RAY / TAU LUA khong.
    /// Tu khoa manh (tim ca trong ten): rail / train / duongray / taulua.
    /// Tu khoa ngan (chi khop khi la MOT TU rieng): ray / tau  — de "Tray",
    /// "Gray", "Restaurant" khong bi nhan nham.
    /// </summary>
    private static bool TenLaDuongRay(string ten)
    {
        if (string.IsNullOrEmpty(ten)) return false;
        string n = ten.ToLowerInvariant();
        if (n.Contains("rail") || n.Contains("train") || n.Contains("duongray")
            || n.Contains("duong_ray") || n.Contains("taulua") || n.Contains("tau_lua")) return true;

        foreach (var tu in TachTu(n))
            if (tu == "ray" || tu == "tau" || tu.StartsWith("ray") || tu.StartsWith("taul")) return true;
        return false;
    }

    // ── Mat na DUONG RAY ────────────────────────────────────────────────

    /// <summary>
    /// O thuoc khu duong ray. HAI NGUON, vi du an nay KHONG co tilemap ray rieng:
    ///   1. Tilemap co ten dinh toi ray/train/tau  (neu sau nay designer them lop).
    ///   2. GameObject trong scene co ten dinh toi ray/train/tau (Taulua,
    ///      TrainVisualRoot, TrainPath, ShippingTrainPath, gataulua...) — lay
    ///      hop bao renderer, khong co renderer thi lay vi tri cac node con.
    /// UI (nam duoi Canvas / RectTransform) bi bo qua.
    /// </summary>
    private HashSet<Vector2Int> BuildDuongRayMask()
    {
        var mask = new HashSet<Vector2Int>();
        tkLoaiTru.soTilemapRay = 0;
        tkLoaiTru.soObjectRay  = 0;

        // 1. tilemap
        try
        {
            var grid = GameObject.Find(IsoGrid.IsoGridObjectName);
            if (grid != null)
            {
                foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tm == null || !TenLaDuongRay(tm.name)) continue;
                    tkLoaiTru.soTilemapRay++;
                    foreach (var p in tm.cellBounds.allPositionsWithin)
                        if (tm.GetTile(p) != null) mask.Add(new Vector2Int(p.x, p.y));
                }
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[LoDat] Doc tilemap ray loi: {e.Message}"); }

        // 2. GameObject trong scene
        try
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t is RectTransform) continue;                    // UI
                if (!TenLaDuongRay(t.gameObject.name)) continue;
                if (t.GetComponentInParent<Canvas>() != null) continue;
                // cha da duoc nhan roi thi bo qua con
                if (t.parent != null && TenLaDuongRay(t.parent.name)) continue;

                var oCua = dungOThatCuaRay ? CacOThatCuaObject(t) : CacOCuaObject(t);
                if (oCua.Count == 0) continue;
                tkLoaiTru.soObjectRay++;
                mask.UnionWith(oCua);
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[LoDat] Quet object ray loi: {e.Message}"); }

        // VONG 4 — khung gioi han (mac dinh TAT => khong doi hanh vi cu)
        if (gioiHanVungCamRay)
        {
            var gh = rectGioiHanVungCamRay;
            mask.RemoveWhere(c => c.x < gh.xMin || c.x >= gh.xMax
                                  || c.y < gh.yMin || c.y >= gh.yMax);
        }

        return mask;
    }

    /// <summary>
    /// VONG 4 — O THAT cua mot object: lay TUNG Collider2D (hoac tung Renderer)
    /// RIENG LE roi hop lai, thay vi gop thanh MOT hop bao duy nhat. Voi duong
    /// tau cheo dai, hop bao gop lai om ca mot dai rong va nuot luon vung bac;
    /// cach nay chi cam dung vet duong ray. Khong tim thay collider/renderer nao
    /// thi quay ve cach cu de khong bo sot.
    /// </summary>
    private static HashSet<Vector2Int> CacOThatCuaObject(Transform t)
    {
        var cells = new HashSet<Vector2Int>();
        if (t == null) return cells;

        try
        {
            int nguon = 0;

            foreach (var col in t.GetComponentsInChildren<Collider2D>(true))
            {
                if (col == null) continue;
                nguon++;
                ThemOTrongBounds(col.bounds, cells);
            }

            if (nguon == 0)
            {
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null) continue;
                    if (r is TilemapRenderer) continue;
                    nguon++;
                    ThemOTrongBounds(r.bounds, cells);
                }
            }

            if (nguon == 0) return CacOCuaObject(t);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LoDat] Doc o that cua '{t.name}' loi: {e.Message}");
            return CacOCuaObject(t);
        }

        return cells;
    }

    /// <summary>Them cac o luoi co TAM nam trong mot Bounds world.</summary>
    private static void ThemOTrongBounds(Bounds b, HashSet<Vector2Int> cells)
    {
        var goc = new[]
        {
            new Vector3(b.min.x, b.min.y, b.center.z),
            new Vector3(b.max.x, b.min.y, b.center.z),
            new Vector3(b.min.x, b.max.y, b.center.z),
            new Vector3(b.max.x, b.max.y, b.center.z),
        };

        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var g in goc)
        {
            var c = IsoGrid.WorldToCell(g);
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }

        long dienTich = (long)(maxX - minX + 1) * (maxY - minY + 1);
        if (dienTich > 20000) return;      // vat to bat thuong -> bo qua, khoi nuot map

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                var w = IsoGrid.CellCenterToWorld(new Vector2Int(x, y));
                if (b.Contains(new Vector3(w.x, w.y, b.center.z)))
                    cells.Add(new Vector2Int(x, y));
            }
    }

    // ── Mat na VUNG DA DAT DO (giua lang) ───────────────────────────────

    /// <summary>
    /// O dang bi cong trinh / do da dat chiem. BA NGUON:
    ///   1. Save FARM_PLACED_BUILDINGS (do nguoi choi dat luc Play) — dung lai
    ///      ham LoadPlacedBuildingCells() da co san.
    ///   2. Object trong scene co script gameplay: PlotController (o dat),
    ///      PenMiniPanelUI (chuong), EditableBuilding (nha sua duoc).
    ///   3. Object trong scene TRUNG TEN prefabToBuild cua mot PlaceableItemData
    ///      (designer keo tay vao scene). Bo "(Clone)" khi so ten.
    /// Bat coiMoiSpriteLaVatDaDat de quet them MOI SpriteRenderer (manh tay).
    /// </summary>
    private HashSet<Vector2Int> BuildVungDaDatMask()
    {
        var mask = new HashSet<Vector2Int>();
        tkLoaiTru.soObjectDaDat = 0;

        // 1. save
        try { mask.UnionWith(LoadPlacedBuildingCells()); }
        catch (System.Exception e) { Debug.LogWarning($"[LoDat] Doc save cong trinh loi: {e.Message}"); }

        // 2 + 3. object trong scene
        var tenPrefabDat = new HashSet<string>();
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
            {
                var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.prefabToBuild != null)
                    tenPrefabDat.Add(d.prefabToBuild.name.ToLowerInvariant());
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[LoDat] Doc PlaceableItemData loi: {e.Message}"); }

        try
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t is RectTransform) continue;
                var go = t.gameObject;
                if (t.GetComponentInParent<Canvas>() != null) continue;
                if (go.GetComponent<Tilemap>() != null) continue;
                if (TenLaDuongRay(go.name)) continue;              // ray tinh o mat na khac

                bool laVatDaDat = false;
                try
                {
                    if (go.GetComponent<PlotController>()   != null) laVatDaDat = true;
                    if (go.GetComponent<PenMiniPanelUI>()   != null) laVatDaDat = true;
                    if (go.GetComponent<EditableBuilding>() != null) laVatDaDat = true;
                }
                catch { }

                if (!laVatDaDat && tenPrefabDat.Count > 0)
                {
                    string ten = go.name.ToLowerInvariant().Replace("(clone)", "").Trim();
                    if (tenPrefabDat.Contains(ten)) laVatDaDat = true;
                }

                if (!laVatDaDat && coiMoiSpriteLaVatDaDat &&
                    go.GetComponent<SpriteRenderer>() != null) laVatDaDat = true;

                if (!laVatDaDat) continue;

                var oCua = CacOCuaObject(t);
                if (oCua.Count == 0) continue;
                tkLoaiTru.soObjectDaDat++;
                mask.UnionWith(oCua);
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[LoDat] Quet vat da dat loi: {e.Message}"); }

        return mask;
    }

    /// <summary>
    /// Vung O LUOI ma mot object dang phu. Uu tien hop bao renderer; khong co
    /// renderer thi lay vi tri cac node con (dung cho TrainPath chi la waypoint).
    /// Chan vat qua to (hop bao om ca ban do) de khong loai tru nham het map.
    /// </summary>
    private static HashSet<Vector2Int> CacOCuaObject(Transform t)
    {
        var cells = new HashSet<Vector2Int>();
        if (t == null) return cells;

        try
        {
            bool coBounds = false;
            Bounds b = default;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r is TilemapRenderer) continue;
                if (!coBounds) { b = r.bounds; coBounds = true; }
                else b.Encapsulate(r.bounds);
            }

            if (coBounds)
            {
                var goc = new Vector3[]
                {
                    new Vector3(b.min.x, b.min.y, b.center.z),
                    new Vector3(b.max.x, b.min.y, b.center.z),
                    new Vector3(b.min.x, b.max.y, b.center.z),
                    new Vector3(b.max.x, b.max.y, b.center.z),
                };
                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                foreach (var g in goc)
                {
                    var c = IsoGrid.WorldToCell(g);
                    minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                    minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
                }

                long dienTich = (long)(maxX - minX + 1) * (maxY - minY + 1);
                if (dienTich > 20000)
                {
                    // vat nay to bat thuong (thuong la root rong om ca map) -> chi lay tam
                    cells.Add(IsoGrid.WorldToCell(t.position));
                    return cells;
                }

                for (int x = minX; x <= maxX; x++)
                    for (int y = minY; y <= maxY; y++)
                    {
                        var w = IsoGrid.CellCenterToWorld(new Vector2Int(x, y));
                        if (b.Contains(new Vector3(w.x, w.y, b.center.z)))
                            cells.Add(new Vector2Int(x, y));
                    }

                if (cells.Count == 0) cells.Add(IsoGrid.WorldToCell(b.center));
                return cells;
            }

            // khong co renderer -> lay chinh no + cac node con (waypoint path)
            cells.Add(IsoGrid.WorldToCell(t.position));
            foreach (var con in t.GetComponentsInChildren<Transform>(true))
            {
                if (con == null || con is RectTransform) continue;
                cells.Add(IsoGrid.WorldToCell(con.position));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LoDat] Do vung o cua '{t.name}' loi: {e.Message}");
        }
        return cells;
    }

    /// <summary>No rong mot tap o them r vong (8 huong). Co tran chong no bo nho.</summary>
    private static HashSet<Vector2Int> NoRong(HashSet<Vector2Int> src, int r)
    {
        var cur = new HashSet<Vector2Int>(src);
        if (r <= 0 || cur.Count == 0) return cur;
        for (int step = 0; step < r; step++)
        {
            if (cur.Count > 400000) { Debug.LogWarning("[LoDat] No rong qua lon -> dung som."); break; }
            var next = new HashSet<Vector2Int>(cur);
            foreach (var c in cur)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        next.Add(new Vector2Int(c.x + dx, c.y + dy));
            cur = next;
        }
        return cur;
    }

    /// <summary>
    /// MAT NA DAT DUNG DE CAT LO = dat tho (cac lop da tick) sau khi tru:
    /// bien/cat/cau tau, duong ray, vung da co cong trinh, vung Sep khoanh tay.
    /// Moi buoc boc try/catch: hong mot buoc thi bo qua buoc do, khong dung ca tool.
    /// </summary>
    private HashSet<Vector2Int> BuildLandMaskDaLoc()
    {
        tkLoaiTru = new ThongKeLoaiTru();

        var land = BuildLandMask();
        tkLoaiTru.oDatThoTruocLoc = land.Count;

        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var c in land)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }
        tkLoaiTru.bienBanDo = land.Count == 0
            ? new RectInt(0, 0, 0, 0)
            : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);

        // 1. BIEN / BAI CAT / CAU TAU — chi giu o nam tren lop DAT XANH
        if (loaiTruBienCatCauTau)
        {
            try
            {
                var green = BuildGreenLandMask();
                tkLoaiTru.oXanh = green.Count;
                int truoc = land.Count;
                land.IntersectWith(green);
                tkLoaiTru.boBienCatDock = truoc - land.Count;
            }
            catch (System.Exception e)
            { Debug.LogWarning($"[LoDat] Loc bien/cat/dock loi: {e.Message}"); }
        }
        else
        {
            try { tkLoaiTru.oXanh = BuildGreenLandMask().Count; } catch { }
        }

        // 2. DUONG RAY
        if (loaiTruDuongRay)
        {
            try
            {
                var ray = NoRong(BuildDuongRayMask(), Mathf.Max(0, banKinhNoRongDuongRay));
                int truoc = land.Count;
                land.ExceptWith(ray);
                tkLoaiTru.boDuongRay = truoc - land.Count;
                if (tkLoaiTru.soTilemapRay == 0 && tkLoaiTru.soObjectRay == 0)
                    tkLoaiTru.ghiChu += "Khong tim thay lop/object duong ray nao trong scene. ";
            }
            catch (System.Exception e)
            { Debug.LogWarning($"[LoDat] Loc duong ray loi: {e.Message}"); }
        }

        // 3. VUNG GIUA DANG CO CONG TRINH
        if (loaiTruVungDaDatDo)
        {
            try
            {
                var vat = NoRong(BuildVungDaDatMask(), Mathf.Max(0, banKinhNoRongVungDaDat));
                int truoc = land.Count;
                land.ExceptWith(vat);
                tkLoaiTru.boVungDaDat = truoc - land.Count;
            }
            catch (System.Exception e)
            { Debug.LogWarning($"[LoDat] Loc vung da dat loi: {e.Message}"); }
        }

        // 4. VUNG SEP KHOANH TAY
        if (vungLoaiTruThem != null && vungLoaiTruThem.Count > 0)
        {
            try
            {
                int truoc = land.Count;
                foreach (var r in vungLoaiTruThem)
                    for (int x = r.xMin; x < r.xMax; x++)
                        for (int y = r.yMin; y < r.yMax; y++)
                            land.Remove(new Vector2Int(x, y));
                tkLoaiTru.boThuCong = truoc - land.Count;
            }
            catch (System.Exception e)
            { Debug.LogWarning($"[LoDat] Loc vung thu cong loi: {e.Message}"); }
        }

        tkLoaiTru.oConLai = land.Count;
        return land;
    }

    // ═════════════════════════════════════════════════════════════════════
    // GIU DAT DA MUA
    // ═════════════════════════════════════════════════════════════════════

    private const string KhoaLuuDatDaMua = "FARM_UNLOCKED_REGIONS";

    /// <summary>Danh sach regionId dang duoc ghi trong PlayerPrefs.</summary>
    private static List<string> DocIdDaMua()
    {
        var ids = new List<string>();
        string raw = PlayerPrefs.GetString(KhoaLuuDatDaMua, "");
        if (string.IsNullOrEmpty(raw)) return ids;
        foreach (var s in raw.Split('|'))
            if (!string.IsNullOrWhiteSpace(s)) ids.Add(s.Trim());
        return ids;
    }

    /// <summary>
    /// TAP O NGUOI CHOI DANG SO HUU truoc khi sinh lai lo.
    /// = o cua moi khu co regionId nam trong FARM_UNLOCKED_REGIONS, cong o cua
    /// moi khu unlockedByDefault. Doc tu LandExpansionManager trong scene; neu
    /// khong co thi doc thang cac asset LandRegionData trong thu muc Land.
    /// </summary>
    private static HashSet<Vector2Int> DocODaSoHuu(out int soKhuDaMua)
    {
        var cells = new HashSet<Vector2Int>();
        soKhuDaMua = 0;
        var ids = new HashSet<string>(DocIdDaMua());

        var khuList = new List<LandRegionData>();
        try
        {
            var mgr = Object.FindFirstObjectByType<LandExpansionManager>();
            if (mgr != null && mgr.regions != null)
                foreach (var r in mgr.regions) if (r != null) khuList.Add(r);
        }
        catch (System.Exception e) { Debug.LogWarning($"[LoDat] Doc manager loi: {e.Message}"); }

        if (khuList.Count == 0)
        {
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:LandRegionData", new[] { AssetFolder }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.Contains("_backup_")) continue;
                    var r = AssetDatabase.LoadAssetAtPath<LandRegionData>(p);
                    if (r != null) khuList.Add(r);
                }
            }
            catch (System.Exception e) { Debug.LogWarning($"[LoDat] Doc asset khu loi: {e.Message}"); }
        }

        foreach (var r in khuList)
        {
            if (r == null) continue;
            bool coCua = r.unlockedByDefault || ids.Contains(r.regionId);
            if (!coCua) continue;
            soKhuDaMua++;
            try { foreach (var c in r.AllCells()) cells.Add(c); }
            catch (System.Exception e) { Debug.LogWarning($"[LoDat] Doc o cua '{r.regionId}' loi: {e.Message}"); }
        }
        return cells;
    }

    /// <summary>
    /// Sau khi sinh lai lo: lo MOI nao de len o Sep da so huu thi danh dau da mua,
    /// roi ghi lai FARM_UNLOCKED_REGIONS theo id MOI. Tra ve bao cao dang chu.
    /// </summary>
    private string GiuLaiDatDaMua(HashSet<Vector2Int> oSoHuuTruoc, bool ghiThat)
    {
        if (oSoHuuTruoc == null) oSoHuuTruoc = new HashSet<Vector2Int>();
        var idMoi = new List<string>();
        var oSauKhi = new HashSet<Vector2Int>();

        for (int i = 0; i < lots.Count; i++)
        {
            bool trung = false;
            foreach (var c in lots[i]) if (oSoHuuTruoc.Contains(c)) { trung = true; break; }
            bool moSan = i == 0 || (i < lotHasBuilding.Count && lotHasBuilding[i]);
            if (!trung && !moSan) continue;
            idMoi.Add($"lot_{i:00}");
            foreach (var c in lots[i]) oSauKhi.Add(c);
        }

        int thieu = 0;
        foreach (var c in oSoHuuTruoc) if (!oSauKhi.Contains(c)) thieu++;

        if (ghiThat)
        {
            try
            {
                PlayerPrefs.SetString(KhoaLuuDatDaMua, string.Join("|", idMoi));
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            { Debug.LogError($"[LoDat] GHI LAI tien do mua dat THAT BAI: {e.Message}"); }
        }

        string bc =
            $"DAT DA MUA: truoc {oSoHuuTruoc.Count} o -> sau {oSauKhi.Count} o " +
            $"({idMoi.Count} lo moi duoc danh dau da mo).\n";
        if (thieu > 0)
            bc += $"  *** CANH BAO: {thieu} o Sep da so huu KHONG nam trong lo moi nao " +
                  "(thuong do o do bi loai tru: ray / cong trinh / bien). " +
                  "O do se thanh dat tu do hoac dat khong thuoc lo nao — KIEM TRA LAI. ***\n";
        if (oSauKhi.Count > oSoHuuTruoc.Count)
            bc += $"  Luu y: sau khi chia lai, Sep so huu NHIEU hon {oSauKhi.Count - oSoHuuTruoc.Count} o " +
                  "vi lo moi to hon phan dat cu (khong mat gi).\n";
        return bc;
    }

    // ═════════════════════════════════════════════════════════════════════
    // SAO LUU
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chep moi asset LandRegionData hien co sang
    /// Assets/_Game/Farm/Land/_backup_&lt;ngay_gio&gt;~/  (CHU Y dau ~ o cuoi ten).
    ///
    /// VI SAO CO DAU ~ :
    ///   Unity BO QUA moi thu muc co ten ket thuc bang '~' — khong import, khong
    ///   cap GUID, khong hien trong Project window, va KHONG bao gio lot vao
    ///   AssetDatabase.FindAssets("t:LandRegionData"). Neu de thu muc sao luu la
    ///   thu muc asset binh thuong thi mot tool khac quet theo thu muc se vo tinh
    ///   nhat ca ban sao vao LandExpansionManager.regions => map co lo ma.
    ///
    ///   Hau qua: KHONG duoc dung AssetDatabase.CopyAsset (dich nam ngoai asset
    ///   database) va KHONG duoc goi AssetDatabase.Refresh() len duong dan nay.
    ///   Chep bang System.IO.File.Copy, chep CA file .meta de GUID cu con nguyen
    ///   khi Sep keo nguoc file ra.
    ///
    /// Tra ve duong dan thu muc, hoac "" neu that bai.
    /// </summary>
    private static string SaoLuuLandAssets()
    {
        try
        {
            if (!Directory.Exists(AssetFolder))
            {
                Directory.CreateDirectory(AssetFolder);
                AssetDatabase.Refresh();
            }

            // Day moi thay doi con nam trong bo nho xuong dia truoc da — File.Copy
            // doc file THAT tren dia, khong doc duoc ban nhap trong AssetDatabase.
            AssetDatabase.SaveAssets();

            // Lay danh sach TRUOC khi tao thu muc — nguon van la asset that.
            var nguon = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:LandRegionData", new[] { AssetFolder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(p) || p.Contains("_backup_")) continue;
                nguon.Add(p);
            }

            // dau '~' cuoi ten = Unity khong nhin thay thu muc nay
            string tenThuMuc = "_backup_" + System.DateTime.Now.ToString("yyyy-MM-dd_HHmm") + "~";
            string duongDan  = $"{AssetFolder}/{tenThuMuc}";

            if (!Directory.Exists(duongDan)) Directory.CreateDirectory(duongDan);

            int chep = 0, chepMeta = 0;
            foreach (var p in nguon)
            {
                string ten = Path.GetFileName(p);
                try
                {
                    File.Copy(p, $"{duongDan}/{ten}", true);
                    chep++;
                    // .meta di kem => giu nguyen GUID khi khoi phuc tay
                    if (File.Exists(p + ".meta"))
                    {
                        File.Copy(p + ".meta", $"{duongDan}/{ten}.meta", true);
                        chepMeta++;
                    }
                }
                catch (System.Exception ex)
                { Debug.LogWarning($"[LoDat] Khong chep duoc {p}: {ex.Message}"); }
            }

            // KHONG goi AssetDatabase.Refresh() cho duong dan nay — thu muc '~'
            // nam ngoai asset database, refresh chi vo ich.
            Debug.Log($"[LoDat] DA SAO LUU {chep} asset lo dat (+{chepMeta} file .meta) vao: {duongDan}\n" +
                      "Thu muc nay co dau '~' o cuoi nen KHONG hien trong Unity. Mo bang " +
                      "Windows Explorer. Muon khoi phuc: chep cac file trong do LEN MOT CAP " +
                      $"({AssetFolder}) roi quay lai Unity de no tu import.");
            return duongDan;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoDat] Sao luu that bai: {e.Message}");
            return "";
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // QUET THU — KHONG GHI GI
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tinh truoc ket qua chia lo toan map va in bao cao. TUYET DOI khong ghi
    /// asset, khong dong vao PlayerPrefs, khong sua scene.
    /// </summary>
    private void QuetThuChiaLoToanMap()
    {
        try
        {
            tkLoaiTru = new ThongKeLoaiTru();   // xoa so lieu lan truoc
            ApDungCheDoChiaLo();
            AutoFindTilemaps();
            CollectGroundMaps();

            int soKhuCu;
            var oSoHuuTruoc = DocODaSoHuu(out soKhuCu);
            string rawCu = PlayerPrefs.GetString(KhoaLuuDatDaMua, "");

            ScanLots();     // chi tinh trong bo nho

            if (cheDoCu_KhungRaoVeTay)
                tkLoaiTru.ghiChu += "Dang o CHE DO CU (khung rao ve tay) — bo loc loai tru " +
                                    "khong chay, cac so loai tru ben duoi deu = 0. ";
            if (!apDungLoaiTruTuDong)
                tkLoaiTru.ghiChu += "Bo loc loai tru tu dong DANG TAT. ";

            var kichThuoc = new List<int>();
            foreach (var l in lots) kichThuoc.Add(l.Count);
            kichThuoc.Sort();
            int nho = kichThuoc.Count > 0 ? kichThuoc[0] : 0;
            int to  = kichThuoc.Count > 0 ? kichThuoc[kichThuoc.Count - 1] : 0;
            int giua = kichThuoc.Count > 0 ? kichThuoc[kichThuoc.Count / 2] : 0;

            string bcDatMua = GiuLaiDatDaMua(oSoHuuTruoc, ghiThat: false);

            var b = tkLoaiTru.bienBanDo;
            string bc =
                "===== QUET THU — KHONG GHI GI =====\n" +
                $"Bien ban do (o luoi): x {b.xMin}..{b.xMax - 1}, y {b.yMin}..{b.yMax - 1} " +
                $"({b.width} x {b.height})\n" +
                $"O dat tho (cac lop da tick): {tkLoaiTru.oDatThoTruocLoc}\n" +
                $"O DAT XANH (co/dat/da): {tkLoaiTru.oXanh}\n" +
                "LOAI TRU:\n" +
                $"   - bien / bai cat / cau tau : {tkLoaiTru.boBienCatDock} o\n" +
                $"   - duong ray (no rong {banKinhNoRongDuongRay}) : {tkLoaiTru.boDuongRay} o " +
                $"({tkLoaiTru.soTilemapRay} tilemap + {tkLoaiTru.soObjectRay} object)\n" +
                $"   - vung da co cong trinh (no rong {banKinhNoRongVungDaDat}) : {tkLoaiTru.boVungDaDat} o " +
                $"({tkLoaiTru.soObjectDaDat} object)\n" +
                $"   - vung khoanh tay : {tkLoaiTru.boThuCong} o ({(vungLoaiTruThem == null ? 0 : vungLoaiTruThem.Count)} vung)\n" +
                $"=> Con {tkLoaiTru.oConLai} o de chia lo.\n" +
                $"SE SINH {lots.Count} LO (canh block {blockSize}x{blockSize}).\n" +
                $"   Kich thuoc lo: nho nhat {nho} o · trung vi {giua} o · to nhat {to} o\n" +
                bcDatMua +
                $"FARM_UNLOCKED_REGIONS hien tai = \"{rawCu}\" ({soKhuCu} khu dang thuoc ve Sep)\n" +
                (string.IsNullOrEmpty(tkLoaiTru.ghiChu) ? "" : "GHI CHU: " + tkLoaiTru.ghiChu + "\n") +
                "===== CHUA GHI GI CA — bam nut GHI THAT neu thay dung =====";

            status = bc;
            Debug.Log($"[LoDat] {bc}");
            Repaint();
        }
        catch (System.Exception e)
        {
            status = "Quet thu loi: " + e.Message;
            Debug.LogError($"[LoDat] {status}\n{e}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // GHI THAT
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sao luu -> hoi lai -> sinh lai toan bo lo -> giu dat da mua -> luu scene.
    /// Day la ham DUY NHAT duoc phep ghi trong luong chia lo toan map.
    /// </summary>
    private void GhiThatChiaLoToanMap(bool luuScene)
    {
        try
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Chia Lo Dat", "Dang Play. Bam Stop roi chay lai.", "OK");
                return;
            }

            ApDungCheDoChiaLo();
            AutoFindTilemaps();
            CollectGroundMaps();

            int soKhuCu;
            var oSoHuuTruoc = DocODaSoHuu(out soKhuCu);
            string rawCu = PlayerPrefs.GetString(KhoaLuuDatDaMua, "");
            Debug.Log($"[LoDat] TRUOC KHI SUA — FARM_UNLOCKED_REGIONS = \"{rawCu}\"  " +
                      $"({soKhuCu} khu, {oSoHuuTruoc.Count} o). Chep dong nay lai neu can khoi phuc tay.");

            ScanLots();
            if (lots.Count == 0)
            {
                EditorUtility.DisplayDialog("Chia Lo Dat",
                    "Khong tim ra o dat nao.\n\n" + status +
                    "\nKiem tra: Grid_Iso45 co trong scene? Cac lop nen (Grass/Dirt...) co tile?", "OK");
                return;
            }

            bool dongY = EditorUtility.DisplayDialog(
                "Chia Lo Dat — GHI THAT",
                $"Sap ghi de {lots.Count} lo dat.\n\n" +
                "MA LO SE DUOC SINH LAI TU DAU (lot_00, lot_01, ...).\n" +
                "Moi asset LandRegionData cu se duoc chep vao thu muc _backup_...~ truoc\n" +
                "(thu muc an voi Unity, chi thay bang Windows Explorer).\n" +
                "Tien do mua dat se duoc ANH XA sang ma lo moi (khong xoa).\n\n" +
                $"Hien Sep dang so huu {oSoHuuTruoc.Count} o dat.\n\nTiep tuc?",
                "Ghi that", "Huy");
            if (!dongY) { status = "Da huy — khong ghi gi."; Debug.Log($"[LoDat] {status}"); return; }

            string thuMucBackup = SaoLuuLandAssets();

            GenerateRegions(silent: true);

            string bcDatMua = GiuLaiDatDaMua(oSoHuuTruoc, ghiThat: true);

            // cong truong dang don do cua ban cu khong con y nghia -> don sach
            try
            {
                for (int i = 0; i < 600; i++) PlayerPrefs.DeleteKey($"LAND_CLEAR_lot_{i:00}");
                PlayerPrefs.Save();
            }
            catch (System.Exception e) { Debug.LogWarning($"[LoDat] Xoa LAND_CLEAR loi: {e.Message}"); }

            if (luuScene)
            {
                try { EditorSceneManager.SaveOpenScenes(); }
                catch (System.Exception e) { Debug.LogWarning($"[LoDat] Luu scene loi: {e.Message}"); }
            }

            string msg = "XONG.\n\n" + status + "\n" + bcDatMua +
                         (string.IsNullOrEmpty(thuMucBackup)
                            ? "*** KHONG SAO LUU DUOC — xem Console. ***\n"
                            : $"Ban sao lo cu: {thuMucBackup}\n" +
                              "   Thu muc nay ket thuc bang '~' nen Unity KHONG nhin thay — " +
                              "mo bang Windows Explorer. Khoi phuc: chep cac file trong do len " +
                              $"mot cap ({AssetFolder}) roi quay lai Unity cho no tu import.\n") +
                         $"FARM_UNLOCKED_REGIONS cu (chep lai neu can): \"{rawCu}\"\n";
            status = msg;
            Debug.Log($"[LoDat] {msg}");
            EditorUtility.DisplayDialog("Chia Lo Dat — GHI THAT", msg, "OK");
            Repaint();
        }
        catch (System.Exception e)
        {
            status = "Ghi that loi: " + e.Message;
            Debug.LogError($"[LoDat] {status}\n{e}");
        }
    }

    /// <summary>Tim lo co chi so NHO HON va ke sat lo i (chung canh o).</summary>
    private static int FindAdjacentLower(List<HashSet<Vector2Int>> sets, int i)
    {
        var mine = sets[i];
        int best = -1, bestTouch = 0;
        for (int j = 0; j < i; j++)
        {
            int touch = 0;
            foreach (var c in mine)
            {
                foreach (var d in Dirs)
                    if (sets[j].Contains(c + d)) { touch++; break; }
                if (touch > 2) break;                 // du de ket luan la ke ben
            }
            if (touch > bestTouch) { bestTouch = touch; best = j; }
            if (bestTouch > 2) break;
        }
        return best;
    }

    /// <summary>Gom cac o roi thanh cac RectInt lien nhau (it rect hon => nhe hon).</summary>
    private static List<RectInt> ToRects(List<Vector2Int> cells)
    {
        var set = new HashSet<Vector2Int>(cells);
        var used = new HashSet<Vector2Int>();
        var rects = new List<RectInt>();

        var sorted = new List<Vector2Int>(cells);
        sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        foreach (var c in sorted)
        {
            if (used.Contains(c)) continue;

            int w = 1;
            while (set.Contains(new Vector2Int(c.x + w, c.y)) &&
                   !used.Contains(new Vector2Int(c.x + w, c.y))) w++;

            int h = 1;
            bool grow = true;
            while (grow)
            {
                for (int x = c.x; x < c.x + w; x++)
                {
                    var probe = new Vector2Int(x, c.y + h);
                    if (!set.Contains(probe) || used.Contains(probe)) { grow = false; break; }
                }
                if (grow) h++;
            }

            for (int x = c.x; x < c.x + w; x++)
                for (int y = c.y; y < c.y + h; y++)
                    used.Add(new Vector2Int(x, y));

            rects.Add(new RectInt(c.x, c.y, w, h));
        }
        return rects;
    }
}
#endif
