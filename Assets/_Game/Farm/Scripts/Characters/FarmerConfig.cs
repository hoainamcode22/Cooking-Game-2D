// ============================================================================
//  FarmerConfig — cau hinh ONG NONG DAN (2026-09-24)
//  Asset: Assets/_Game/Resources/FarmerConfig.asset (tao bang Tools > Farm Game > Nong Dan > 1).
//  Frame lay tu 3 sheet trong Assets/Art/Characters/Farmer:
//    walk  1350x1800 = 3 cot x 4 hang (o 450): hang 0 DOWN, 1 LEFT, 2 RIGHT, 3 UP  -> walk_01..12
//    hoe   2000x1000 = 4 cot x 2 hang (o 500): 8 frame cuoc dat                     -> hoe_01..08
//    water 2000x1000 = 4 cot x 2 hang (o 500): 8 frame tuoi nuoc                    -> water_01..08
//  PPU walk 100, hoe/water 111.1 -> nhan vat 3 sheet CUNG CO khi cung 1 scale.
// ============================================================================
using UnityEngine;

[CreateAssetMenu(fileName = "FarmerConfig", menuName = "Farm/Farmer Config")]
public class FarmerConfig : ScriptableObject
{
    [Header("Frame (tool tu gan)")]
    [Tooltip("12 frame: 0-2 DOWN, 3-5 LEFT, 6-8 RIGHT, 9-11 UP.")]
    public Sprite[] walkFrames = new Sprite[0];
    public Sprite[] hoeFrames = new Sprite[0];
    public Sprite[] waterFrames = new Sprite[0];

    [Header("Kich thuoc & toc do (cham rai nhu video)")]
    [Tooltip("Chieu cao NHIN THAY cua ong (mu -> chan), don vi world. 1 o dat = 300 x 150.")]
    public float chieuCaoNhinThay = 120f;
    [Tooltip("Ti le phan nhin thay / ca o sprite walk (275/450).")]
    public float tiLeNhinThay = 0.611f;
    [Tooltip("Toc do di bo (world/giay). Co giao hang ~420; ong nong dan thong tha ~55.")]
    public float tocDoDi = 55f;
    public float walkFps = 6f;
    public float hoeFps = 9f;
    public float waterFps = 7f;
    [Tooltip("So vong 8 frame cho 1 lan cuoc / tuoi.")]
    public int soVongCuoc = 3;
    public int soVongTuoi = 2;
    [Tooltip("Nghi giua 2 viec (giay).")]
    public Vector2 nghiGiuaViec = new Vector2(0.4f, 1.0f);
    [Tooltip("Luc cay da lon (stage 4-5): dung nghi giua 2 lan di qua di lai (giay).")]
    public Vector2 nghiKhiDiDao = new Vector2(1.2f, 2.8f);

    [Header("Phan cong")]
    [Tooltip("Moi ong phu trach toi da bao nhieu o dat (4 o = 1 ong, 8 o = 2 ong).")]
    public int soODatMoiNguoi = 4;
    [Tooltip("Diem den xa hon muc nay thi ong mo di roi hien ra o cho moi (khoi di bo ca ban do).")]
    public float xaQuaThiHienLai = 1400f;
    [Tooltip("Phan trong hinh thoi o dat ma ong duoc dung (1 = sat mep).")]
    [Range(0.2f, 1f)] public float vungDung = 0.6f;
    [Tooltip("Chu ky quet ruong (giay).")]
    public float chuKyQuet = 1.5f;

    [Header("Ong chau hoa")]
    // [2026-09-25 v3] Do tu sheet water (o 500, nguoi cao ~307 px): voi binh cach chan ~96 px (31% chieu cao),
    // dong nuoc cat mieng chau o ~120 px phia truoc chan. Muon nuoc roi VAO chau thi voi phai cao hon mieng chau
    // -> ong phai cao >= 3.2 lan THAN chau (ngoai doi: chau hoa ngang goi nguoi).
    [Tooltip("Ong tuoi chau cao gap bao nhieu lan THAN CHAU (khong tinh hoa). 1.9 = ngang chau + hoa.")]
    public float tiLeCaoSoVoiChau = 1.9f;
    [Tooltip("[NEO TUOI] Diem dong nuoc roi xuong trong sheet water, tinh tu CHAN ong (px o 500, x ve phia binh, y len tren). " +
             "Do tu frame 5-6: vung nuoc x 289..406, cham dat y 458 -> tam ~ (105, 14). Ong dung sao cho diem nay TRUNG mat dat trong chau.")]
    public Vector2 diemNuocPx = new Vector2(124f, 14f);   // [2026-09-26] do lai tren sheet tuoi: vung nuoc bat toe tu x 96..152 px -> tam 124
    [Tooltip("Luc tuoi: tat = ong ve SAU chau (chau che chan ong, nuoc roi vao mieng chau - dung phoi canh vi chan ong ngang mat dat trong chau). " +
             "Bat = ong ve TRUOC chau (chan ong de len than chau).")]
    public bool veTruocChauKhiTuoi = false;
    [Tooltip("Tam chau cach chan ong bao nhieu px (o 500 cua sheet water). Nuoc cat mieng chau o ~120 px.")]
    public float tamChauPxNuoc = 140f;
    [Tooltip("Chan ong lech so voi day chau (x chieu cao chau). Am = dung phia truoc day chau. Ong chau LUON ve TREN chau.")]
    public float dungSauDayChau = -0.06f;

    [Header("Vet mo khi di (giong co giao hang nhung cham, diu)")]
    public bool vetMo = true;
    [Tooltip("Moi bao nhieu giay nha 1 bong (co giao hang 0.07; ong di cham -> thua hon).")]
    public float nhipBong = 0.28f;
    public float doiBong = 0.6f;
    [Range(0f, 1f)] public float alphaBong = 0.26f;
    public Color mauBong = new Color(1f, 0.95f, 0.85f, 1f);

    [Header("Hien / an")]
    public float thoiGianHien = 0.6f;
}
