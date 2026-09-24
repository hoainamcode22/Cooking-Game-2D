using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv3
{
    /// <summary>
    /// HIEU UNG NAU AN cho Kitchen_UI_v3 — chi NGHE su kien, khong dong vao logic game.
    ///
    /// Nghe CookingChallengeManager (static events da co san tu 2026-08-26):
    ///   OnCookStarted   -> noi soi + dong ho chay + thanh thoi gian chay + lua lo (V2 lo)
    ///   OnDishCooked    -> icon mon BAY tu noi sang dia, roi noi/dong ho dung
    ///   OnDishFailed    -> dung het
    ///   OnDishCollected -> reset thanh thoi gian
    ///
    /// Thoi gian nau lay tu CookingChallengeManager.cookSubmitDelay (private, doc bang
    /// reflection; khong doc duoc thi dung thoiGianMacDinh). Muon noi soi lau hon thi Sep
    /// tang "Cook Submit Delay" tren component CookingChallengeManager trong Inspector.
    ///
    /// Moi tham chieu deu keo tay trong Inspector (hoac tool tu gan). Thieu cai nao thi bo
    /// qua cai do, KHONG nem loi.
    /// </summary>
    public class KitchenV3CookingFX : MonoBehaviour
    {
        [Header("Animation")]
        public UISpriteFrameAnimator noi;        // Pot_Anim
        public UISpriteFrameAnimator dongHo;     // Clock_Anim

        [Header("Thanh thoi gian (tuy chon)")]
        public Image  thanhFill;                 // Cook_Timer_Bar/Fill  (Image type Filled)
        public TMP_Text txtThoiGian;             // Cook_Timer_Bar/Txt_Time
        public float thoiGianMacDinh = 0.8f;

        [Header("Mon bay tu noi sang dia")]
        public RectTransform diemXuatPhat;       // Pot_Anim (hoac 1 object rong tren noi)
        public RectTransform diemDen;            // Plating_Table/Dish_Visual (V2 tu tao luc chay -> co the bo trong)
        public RectTransform diaTrinhBay;        // Plating_Table — dung de tu tim Dish_Visual luc chay
        public float thoiGianBay = 0.6f;
        public float doCaoVongCung = 90f;
        public Vector2 kichThuocIconBay = new Vector2(80f, 80f);

        private CookingChallengeManager _challenge;
        private float _tongThoiGian;
        private float _batDau;
        private bool  _dangNau;
        private Coroutine _coBay;
        private Canvas _canvas;

        private void OnEnable()
        {
            CookingChallengeManager.OnCookStarted   += KhiBatDauNau;
            CookingChallengeManager.OnDishCooked    += KhiNauXong;
            CookingChallengeManager.OnDishFailed    += KhiNauHong;
            CookingChallengeManager.OnDishCollected += KhiCatVaoKho;
            DatLaiThanh();
        }

        private void OnDisable()
        {
            CookingChallengeManager.OnCookStarted   -= KhiBatDauNau;
            CookingChallengeManager.OnDishCooked    -= KhiNauXong;
            CookingChallengeManager.OnDishFailed    -= KhiNauHong;
            CookingChallengeManager.OnDishCollected -= KhiCatVaoKho;
        }

        private void Start()
        {
            _challenge = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
            _canvas    = GetComponentInParent<Canvas>();
            _tongThoiGian = DocThoiGianNau();
        }

        private float DocThoiGianNau()
        {
            if (_challenge == null) return thoiGianMacDinh;
            try
            {
                var f = typeof(CookingChallengeManager).GetField("cookSubmitDelay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return Mathf.Max(0.05f, (float)f.GetValue(_challenge));
            }
            catch { }
            return thoiGianMacDinh;
        }

        // ── su kien ────────────────────────────────────────────────────────────
        private void KhiBatDauNau(DishData d)
        {
            // [2026-09-23] Thoi gian THAT cua mon (De/Vua/Kho), khong con 0.8s co dinh.
            if (_challenge == null) _challenge = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
            _tongThoiGian = _challenge != null && _challenge.CookDuration > 0f ? _challenge.CookDuration : CookingChallengeManager.CookTimeOf(d);
            _batDau  = Time.time;   // cung dong ho voi WaitForSeconds cua CookingChallengeManager
            _dangNau = true;
            if (noi    != null) noi.Chay();
            if (dongHo != null) dongHo.Chay();
        }

        private void KhiNauXong(DishData d, int diem)
        {
            _dangNau = false;
            if (noi    != null) noi.Dung();
            if (dongHo != null) dongHo.Dung();
            if (thanhFill != null) thanhFill.fillAmount = 1f;
            if (txtThoiGian != null) txtThoiGian.text = "Done!";
            if (diemDen == null && diaTrinhBay != null)
            {
                var dv = diaTrinhBay.Find("Dish_Visual") as RectTransform;   // V2 EnsurePlatingDishVisual tao san
                diemDen = dv != null ? dv : diaTrinhBay;
            }
            if (d != null && d.dishSprite != null && diemXuatPhat != null && diemDen != null)
            {
                if (_coBay != null) StopCoroutine(_coBay);
                _coBay = StartCoroutine(CoBayMon(d.dishSprite));
            }
        }

        private void KhiNauHong(DishData d, int diem)
        {
            _dangNau = false;
            if (noi    != null) noi.Dung();
            if (dongHo != null) dongHo.Dung();
            DatLaiThanh();
        }

        private void KhiCatVaoKho(DishData d) => DatLaiThanh();

        private void DatLaiThanh()
        {
            if (thanhFill != null) thanhFill.fillAmount = 0f;
            if (txtThoiGian != null) txtThoiGian.text = "";
        }

        private void Update()
        {
            if (!_dangNau || _tongThoiGian <= 0f) return;
            float troi = Time.time - _batDau;
            float t = Mathf.Clamp01(troi / _tongThoiGian);
            if (thanhFill != null) thanhFill.fillAmount = t;
            if (txtThoiGian != null)
            {
                float con = Mathf.Max(0f, _tongThoiGian - troi);
                int m = (int)(con / 60f); int s = Mathf.CeilToInt(con - m * 60);
                txtThoiGian.text = m > 0 ? $"{m}m {s:00}s" : $"{s}s";
            }
        }

        // ── mon bay noi -> dia ─────────────────────────────────────────────────
        private IEnumerator CoBayMon(Sprite sp)
        {
            // an Dish_Visual trong luc bay de khong thay 2 mon cung luc (V2 se bat lai khi pop)
            var imgDia = diemDen.GetComponent<Image>();
            bool daBatDia = imgDia != null && imgDia.enabled;
            if (imgDia != null) imgDia.enabled = false;

            var go = new GameObject("FX_DishFly", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform.parent != null ? transform.parent : transform, false); // cung canvas voi FX
            rt.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = sp; img.preserveAspect = true; img.raycastTarget = false;
            rt.sizeDelta = kichThuocIconBay;
            // [2026-09-24] Ha canh DUNG kich thuoc + vi tri Dish_Visual Sep can trong Edit mode
            Vector2 sizeDen = diemDen.rect.size.sqrMagnitude > 1f ? diemDen.rect.size : kichThuocIconBay;
            float scaleDen = rt.parent != null && rt.parent.lossyScale.x != 0f ? diemDen.lossyScale.x / rt.parent.lossyScale.x : 1f;

            Vector3 a = diemXuatPhat.position;
            Vector3 b = diemDen.position;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.05f, thoiGianBay);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                Vector3 p = Vector3.Lerp(a, b, k);
                p.y += Mathf.Sin(k * Mathf.PI) * doCaoVongCung * (_canvas != null ? _canvas.scaleFactor : 1f);
                rt.position = p;
                rt.sizeDelta = Vector2.Lerp(kichThuocIconBay, sizeDen, k);
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, scaleDen, k);
                yield return null;
            }
            Destroy(go);
            if (imgDia != null) imgDia.enabled = true;
            _coBay = null;
        }
    }
}
