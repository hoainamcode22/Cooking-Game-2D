using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// [PERF P0 — F3] DOI TilemapRenderer NEN TU Individual SANG Chunk LUC CHAY.
///
/// ══ SO DO DUOC TU SCN_Farm.unity (16 TilemapRenderer, tat ca m_Enabled: 1) ══
///   ten                     m_Mode                sortingLayerID   sortingOrder
///   Underwater_Tilemap      0 Chunk               0 (Default)      -9
///   Water_Tilemap           2 SRPBatch            0 (Default)      -8
///   Tilemap_IsoSand         1 Individual          0 (Default)       0
///   Tilemap_IsoGrass        1 Individual          0 (Default)       1
///   Tilemap_IsoDirt         1 Individual          0 (Default)       2
///   Tilemap_IsoStone        1 Individual          0 (Default)       3
///   Tilemap_IsoDirtPatch    1 Individual          0 (Default)       4
///   Tilemap_IsoDock         1 Individual          0 (Default)       4
///   Tilemap_IsoRock         1 Individual          0 (Default)       5
///   Tilemap_IsoFence        1 Individual          0 (Default)       6
///   Tilemap_LockedOverlay   1 Individual          0 (Default)       7
///   Tilemap_IsoDecor        1 Individual          0 (Default)       7
///   Dat_Nen                 1 Individual          0 (Default)      11
///   Co_Grass                1 Individual          0 (Default)      12
///   Mong                    1 Individual          1161173501       -2
///   Dat_Nen (1)             1 Individual          1161173501       -1
///
/// ══ VI SAO Individual DAT ══
/// Individual nop TUNG O GACH nhu mot quad rieng theo thu tu sort, thay vi gop ca chunk thanh
/// MOT mesh. Do dung la hinh dang chi phi hien ra LUC KEO MAP (moi o vao/ra khung hinh la mot
/// lan sort + mot draw). Chunk gop lai => it draw call hon han.
///
/// ══ VI SAO KHONG DOI TAT ══
/// Game iso thuong CHON Individual co chu dich: o gach phai xen ke voi SpriteRenderer theo truc Y
/// (hang rao, da, decor dung TRUOC hay SAU nhan vat). Doi mot lop can dieu do la VO SORT NHIN THAY NGAY.
/// Chi doi lop NEN PHANG — thu ma moi thu khac ve DE LEN.
///
/// ══ LY DO CHON / LOAI TUNG LOP (da doi chieu voi scene that) ══
/// DOI  Tilemap_IsoSand (0), Tilemap_IsoGrass (1), Tilemap_IsoDirt (2), Dat_Nen (11)
///      → nen phang, MOI LOP MOT sortingOrder RIENG trong Default, khong chia order voi ai
///        => khong co gi phai xen ke theo tung o.
/// DOI  Dat_Nen (1) (layer 1161173501, order -1) → order rieng trong layer do (chi co Mong o -2).
/// KHONG Water_Tilemap → DA o m_Mode 2 (SRP Batch), doi ve Chunk la DI LUI. Bo han.
/// KHONG Underwater_Tilemap → DA o m_Mode 0 (Chunk). Khong con gi de lam.
/// KHONG Tilemap_LockedOverlay → CHIA CHUNG sortingOrder 7 voi Tilemap_IsoDecor (decor BUOC PHAI
///        giu Individual). Doi mot ben sang Chunk lam quy tac phan giai hoa giua hai lop doi
///        => rui ro sort nhin thay. De Sep tu them vao mang duoi neu da xac nhan an toan.
/// KHONG IsoStone / IsoDirtPatch / IsoDock / IsoRock / IsoFence / IsoDecor / Co_Grass / Mong
///        → decor / hang rao / da / mong nha: chinh la nhung lop CAN xen ke theo Y. KHONG dong.
///
/// ══ SUA DANH SACH ══
/// Component chu (<see cref="PerformanceGuardian"/>) duoc TAO LUC CHAY nen khong co Inspector de
/// go [SerializeField]. Vi vay danh sach nam o <see cref="TEN_LOP_NEN_AN_TOAN"/> ngay duoi —
/// sua mang do roi build lai. Tat ca bang <see cref="Bat"/> = false.
/// </summary>
public static class TilemapRenderModeOptimizer
{
    /// <summary>Cong tac tong. Mac dinh BAT.</summary>
    // [VONG 13 — TAT] Chunk mode lam LO DUONG LUOI giua cac o tile (texture bleeding
    // o canh UV khi loc bilinear). Individual ve tung o rieng nen khong bi. Loi hien thi
    // nay an dut moi loi ich draw call => MAC DINH TAT. Chi bat lai khi da them
    // padding/extrude cho sprite tile va da kiem chung tren may that.
    public static bool Bat = false;

    /// <summary>
    /// TEN GameObject cua cac Tilemap NEN PHANG duoc phep gop thanh Chunk.
    /// So sanh KHOP CHINH XAC ten GameObject. Xem phan "LY DO CHON / LOAI" o doc tren truoc khi them.
    /// </summary>
    public static readonly string[] TEN_LOP_NEN_AN_TOAN =
    {
        "Tilemap_IsoSand",
        "Tilemap_IsoGrass",
        "Tilemap_IsoDirt",
        "Dat_Nen",
        "Dat_Nen (1)",
        // "Tilemap_LockedOverlay",  // chia order 7 voi Tilemap_IsoDecor — bo mo neu da kiem chung.
    };

    /// <summary>Quet scene vua nap va doi mode. Goi tu <see cref="PerformanceGuardian"/>.</summary>
    public static void ApDung(string tenScene)
    {
        if (!Bat) return;

        TilemapRenderer[] tatCa = UnityEngine.Object.FindObjectsByType<TilemapRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int doi = 0;
        string daDoi = string.Empty;

        for (int i = 0; i < tatCa.Length; i++)
        {
            TilemapRenderer r = tatCa[i];
            if (r == null) continue;
            if (r.mode != TilemapRenderer.Mode.Individual) continue;   // da Chunk / SRP Batch roi.
            if (!LaLopNenAnToan(r.gameObject.name)) continue;

            r.mode = TilemapRenderer.Mode.Chunk;
            doi++;
            daDoi += (daDoi.Length == 0 ? "" : ", ") + r.gameObject.name;
        }

        Debug.Log("[Perf] TilemapRenderModeOptimizer (" + tenScene + "): tim thay " + tatCa.Length +
                  " TilemapRenderer, doi " + doi + " lop nen sang Chunk" +
                  (doi > 0 ? (" -> " + daDoi) : "") + ". Cac lop decor/hang rao/da GIU NGUYEN Individual.");
    }

    private static bool LaLopNenAnToan(string ten)
    {
        for (int i = 0; i < TEN_LOP_NEN_AN_TOAN.Length; i++)
            if (string.Equals(ten, TEN_LOP_NEN_AN_TOAN[i], System.StringComparison.Ordinal))
                return true;
        return false;
    }
}
