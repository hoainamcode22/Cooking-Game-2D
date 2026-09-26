// ============================================================================
//  SeedPanelSkin — khay hat giong / khay hoa (2026-09-25 v2)
//  - CHI 1 khung ngoai bo goc: nau ca cao dam (sang nhe o tren), vien vang am.
//    Nen toi + am -> icon hat/hoa (xanh, vang, do, cam, tim) noi bat nhat, ca ngay lan dem.
//  - KHONG boc the rieng tung o nua (v1 co the be -> bo). O cu con "Tile_Be" thi tu xoa.
//  - Chu tren nen toi: ten kem sang, so luong xanh non (con) / do cam (het).
//  Goi tu PopupPillApplier (khung) va SeedPopupController.SpawnAllItems (tung o).
// ============================================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SeedPanelSkin
{
    // [v3 2026-09-25] Sep chot nen KEM sang -> chu nau dam, so xanh la dam / do (khong trung nen)
    public static readonly Color MauChuTen = new Color(0.36f, 0.22f, 0.10f, 1f);   // nau dam
    public static readonly Color MauSoCon  = new Color(0.18f, 0.46f, 0.10f, 1f);   // xanh la dam
    public static readonly Color MauSoHet  = new Color(0.80f, 0.18f, 0.12f, 1f);   // do

    private static readonly Color NenTren = new Color(1.00f, 0.975f, 0.92f, 0.97f);  // kem sang
    private static readonly Color NenDuoi = new Color(0.97f, 0.91f, 0.79f, 0.97f);  // kem dam hon chut
    private static readonly Color MauVien = new Color(0.78f, 0.56f, 0.30f, 1f);     // vien nau vang

    private static Sprite _khung;

    public static Sprite SpriteKhung => _khung != null ? _khung : (_khung = TaoBoGoc(160, 36, 4));

    /// <summary>Khung khay: ca cao dam bo goc, vien vang.</summary>
    public static void ApDungKhung(Image img)
    {
        if (img == null) return;
        img.sprite = SpriteKhung;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        img.color = Color.white;
    }

    /// <summary>1 o hat: bo the cu, nen o trong suot, chu doi mau hop nen toi (idempotent).</summary>
    public static void ApDungThe(Transform o)
    {
        if (o == null) return;
        var cu = o.Find("Tile_Be");
        if (cu != null)
        {
            if (Application.isPlaying) Object.Destroy(cu.gameObject);
            else Object.DestroyImmediate(cu.gameObject);
        }

        // Nen goc cua o (neu co) trong suot de khong lo vuong
        var goc = o.GetComponent<Image>();
        if (goc != null) { var c = goc.color; c.a = 0f; goc.color = c; }

        foreach (var tx in o.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tx == null) continue;
            string s = tx.text ?? "";
            if (s.StartsWith("x")) continue;             // so luong: SeedDragItem to mau theo ton kho
            tx.color = MauChuTen;
            tx.fontStyle |= FontStyles.Bold;
        }
    }

    /// <summary>Sprite bo goc khu rang cua: ruot gradient doc, vien trong mau vang. 9-slice theo ban kinh.</summary>
    private static Sprite TaoBoGoc(int n, int r, int vien)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            Color nen = Color.Lerp(NenDuoi, NenTren, y / (float)(n - 1));
            for (int x = 0; x < n; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, n - r), cy = Mathf.Clamp(y + 0.5f, r, n - r);
                float d = r - Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));   // >0 = ben trong
                float phu = Mathf.Clamp01(d + 0.5f);
                float kVien = Mathf.Clamp01(vien - d + 0.5f);                                                       // 1 = trong vung vien
                Color c = Color.Lerp(nen, MauVien, kVien);
                c.a *= phu;
                px[y * n + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(r + 1, r + 1, r + 1, r + 1));
    }
}
