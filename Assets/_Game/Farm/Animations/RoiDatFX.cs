// ============================================================================
//  RoiDatFX — chuyen dong "ROI RAI RAC XUONG DAT roi moi bay ve HUD" (V2 2026-09-24)
//  Dung chung cho HarvestFlyItemFX (icon nong san -> kho) va ExpFlyToAvatarFX (sao EXP -> thanh EXP).
//    1. Tung len theo vong cung, roi xuong 1 diem ngau nhien quanh luong dat (elip det = mat dat)
//    2. Nay 1 lan nho + nen dep (squash) khi cham dat, bong mo duoi chan
//    3. Nam tren dat mot nhip (tho nhe) cho nguoi choi kip nhin
//    4. Nhun lay da -> bay vong cung ve dich, nho dan
//  Chay theo gio thuc (unscaled) nhu cac FX thuong khac.
// ============================================================================
using System;
using System.Collections;
using UnityEngine;

public static class RoiDatFX
{
    [Serializable]
    public class CauHinh
    {
        [Tooltip("Ban kinh rai tren mat dat (don vi world, nhan zoom).")]
        public float banKinhRai = 150f;
        [Tooltip("Mat dat det: ti le truc Y so voi truc X cua vung rai.")]
        public float doDetMatDat = 0.45f;
        [Tooltip("Ha thap diem roi so voi diem thu hoach.")]
        public float haThap = 30f;
        [Tooltip("Do cao tung len (world, nhan zoom).")]
        public float doCaoTung = 150f;
        public float thoiGianRoi = 0.5f;
        [Tooltip("Do cao nay lai = doCaoTung x ti le nay.")]
        public float tiLeNay = 0.28f;
        public float thoiGianNay = 0.22f;
        public float namDat = 0.45f;
        public float namDatNgauNhien = 0.2f;
        public float thoiGianBay = 0.6f;
        public float doCongBay = 90f;
        public float scaleDich = 0.7f;
        [Tooltip("Bong mo duoi chan icon khi nam dat.")]
        public bool coBong = true;
        public float doDamBong = 0.28f;

        public float TongThoiGian => thoiGianRoi + thoiGianNay + namDat + namDatNgauNhien + 0.12f + thoiGianBay;
    }

    public static IEnumerator Chay(Transform tf, Transform visual, SpriteRenderer sr, Vector3 spawn, Vector3 dich,
                                   Vector3 scaleDau, Vector3 scaleThuong, float zoom, CauHinh c, Action khiToi)
    {
        if (visual == null) visual = tf;
        Vector2 r = UnityEngine.Random.insideUnitCircle * c.banKinhRai;
        Vector3 dat = spawn + new Vector3(r.x, r.y * c.doDetMatDat - c.haThap, 0f) * zoom;
        float quay = UnityEngine.Random.Range(-25f, 25f);

        // Bong mo
        SpriteRenderer bong = null;
        if (c.coBong && sr != null)
        {
            var go = new GameObject("Fx_Bong");
            bong = go.AddComponent<SpriteRenderer>();
            bong.sprite = SoftFxSprites.GlowSprite;
            bong.color = new Color(0f, 0f, 0f, 0f);
            bong.sortingLayerID = sr.sortingLayerID;
            bong.sortingOrder = sr.sortingOrder - 1;
            go.transform.SetParent(tf, false);          // con cua icon -> huy cung icon, khong ro ri
            go.transform.position = dat;
        }

        void GiuBong()
        {
            if (bong == null) return;
            bong.transform.position = dat;
            bong.transform.rotation = Quaternion.identity;
        }

        void DatBong(float caoTuongDoi, float alphaMul)
        {
            if (bong == null || sr == null) return;
            float w = Mathf.Max(1f, sr.bounds.size.x) * 0.95f;
            float kichSprite = bong.sprite.bounds.size.x;
            float s = w / Mathf.Max(0.0001f, kichSprite) * Mathf.Lerp(1f, 0.55f, caoTuongDoi);
            Vector3 ls = tf.lossyScale;
            bong.transform.localScale = new Vector3(s / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), s * 0.32f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);
            GiuBong();
            bong.color = new Color(0f, 0f, 0f, c.doDamBong * alphaMul * Mathf.Lerp(1f, 0.3f, caoTuongDoi));
        }

        try
        {
            // 1) Tung + roi
            float t = 0f, d = Mathf.Max(0.05f, c.thoiGianRoi);
            float h = c.doCaoTung * zoom;
            while (t < d)
            {
                if (tf == null) yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / d);
                // cung khong doi xung: len nhanh, roi nhanh dan (nhu co trong luc)
                float cao = 4f * k * (1f - k) * h;
                tf.position = Vector3.LerpUnclamped(spawn, dat, k) + Vector3.up * cao;
                visual.localScale = Vector3.LerpUnclamped(scaleDau, scaleThuong, Mathf.Min(1f, k * 1.6f));
                visual.localRotation = Quaternion.Euler(0f, 0f, quay * Mathf.Sin(k * Mathf.PI * 2f) * (1f - k));
                DatBong(Mathf.Clamp01(cao / Mathf.Max(1f, h)), Mathf.Clamp01(k * 2f));
                yield return null;
            }

            // 2) Nay nho + nen dep khi cham dat
            t = 0f; d = Mathf.Max(0.05f, c.thoiGianNay);
            float hn = h * c.tiLeNay;
            while (t < d)
            {
                if (tf == null) yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / d);
                float cao = 4f * k * (1f - k) * hn;
                tf.position = dat + Vector3.up * cao;
                float nen = k < 0.25f ? Mathf.Sin(k / 0.25f * Mathf.PI) : 0f;          // bep o khoanh dau
                float nen2 = k > 0.8f ? Mathf.Sin((k - 0.8f) / 0.2f * Mathf.PI) * 0.5f : 0f; // bep nhe luc dap lai
                float sq = (nen + nen2) * 0.18f;
                visual.localScale = new Vector3(scaleThuong.x * (1f + sq), scaleThuong.y * (1f - sq), scaleThuong.z);
                visual.localRotation = Quaternion.identity;
                DatBong(Mathf.Clamp01(cao / Mathf.Max(1f, h)), 1f);
                yield return null;
            }
            tf.position = dat;

            // 3) Nam dat, tho nhe
            t = 0f; d = c.namDat + UnityEngine.Random.Range(0f, c.namDatNgauNhien);
            float pha = UnityEngine.Random.value * 6f;
            while (t < d)
            {
                if (tf == null) yield break;
                t += Time.unscaledDeltaTime;
                float b = 1f + 0.04f * Mathf.Sin((t * 7f) + pha);
                visual.localScale = new Vector3(scaleThuong.x * b, scaleThuong.y * (2f - b), scaleThuong.z);
                DatBong(0f, 1f);
                yield return null;
            }

            // 4) Nhun lay da
            t = 0f; d = 0.12f;
            while (t < d)
            {
                if (tf == null) yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / d);
                visual.localScale = new Vector3(scaleThuong.x * (1f + 0.14f * k), scaleThuong.y * (1f - 0.12f * k), scaleThuong.z);
                yield return null;
            }

            // 5) Bay vong cung ve dich
            Vector3 dau = tf.position;
            Vector3 huong = dich - dau;
            Vector3 vuong = new Vector3(-huong.y, huong.x, 0f).normalized;
            Vector3 dk = (dau + dich) * 0.5f + vuong * (c.doCongBay * zoom * (UnityEngine.Random.value < 0.5f ? -1f : 1f))
                         + Vector3.up * c.doCongBay * 0.6f * zoom;
            t = 0f; d = Mathf.Max(0.05f, c.thoiGianBay);
            while (t < d)
            {
                if (tf == null) yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / d);
                float e = k * k * (3f - 2f * k);
                e = Mathf.Lerp(e, k * k, 0.35f);          // tang toc dan ve cuoi
                float u = 1f - e;
                tf.position = u * u * dau + 2f * u * e * dk + e * e * dich;
                float s = Mathf.Lerp(1.12f, c.scaleDich, k);
                visual.localScale = new Vector3(scaleThuong.x * s, scaleThuong.y * s, scaleThuong.z);
                GiuBong();
                if (bong != null) { var bc = bong.color; bc.a = Mathf.Max(0f, bc.a - Time.unscaledDeltaTime * 3f); bong.color = bc; }
                yield return null;
            }
            if (tf != null) tf.position = dich;
        }
        finally
        {
            if (bong != null) UnityEngine.Object.Destroy(bong.gameObject);
        }

        if (tf == null) yield break;
        try { khiToi?.Invoke(); }
        catch (Exception e) { Debug.LogWarning("[RoiDatFX] khiToi loi: " + e); }
    }
}
