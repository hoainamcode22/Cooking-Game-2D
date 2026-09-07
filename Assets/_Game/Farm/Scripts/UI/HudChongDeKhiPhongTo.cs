using UnityEngine;

namespace FarmGame.UI
{
    /// <summary>
    /// CHẶN TRẦN PHÓNG TO CHO MỘT PHẦN TỬ HUD — để cú "nảy mẩy mẩy" không đè sang phần tử bên cạnh.
    /// ══════════════════════════════════════════════════════════════════════════════
    ///
    /// VÌ SAO CẦN (số đo thật trên SCN_Farm, hệ quy chiếu 1920×1080; cụm HUD localScale 1.2):
    ///   • `JuicyPulseFX.Play(...)` phóng localScale của phần tử HUD lên 1.20–1.25 lần rồi trả
    ///     về gốc. Nó được gọi từ NĂM chỗ, không chỗ nào là file của Dev UI:
    ///     RewardFlyFX.cs:384 · HarvestFeedbackSpawner.cs:269 · CoinFlyFX.cs:182 ·
    ///     GemFlyFX.cs:192 · UnifiedTaskPopupUI.cs:2220.
    ///   • Cụm HUD lại xếp sát đến mức KHÔNG còn chỗ cho cú phóng to đó:
    ///       – Mép phải khung Vàng (x 1468) ↔ mép trái icon Kim Cương (x 1484.6): hở 16.6 px.
    ///       – Mép phải khung avatar (x 180) ↔ mép trái thanh EXP (x 223.3): hở 43.3 px,
    ///         mà thanh EXP phóng 1.22 lần cần tới 51.5 px.
    ///     ⇒ MỖI lần nhận thưởng là một lần chồng lấn. Nhận dồn dập thì các cú phóng nối
    ///       nhau, mắt thấy chúng "tự đè vào nhau" gần như liên tục — đúng như Sếp mô tả.
    ///
    /// CÁCH LÀM — chỉ CHẶN TRẦN. Không dựng lại layout, không dời vị trí, không huỷ con:
    ///   1. Ở nhịp LateUpdate đầu tiên (lúc HUD còn ĐỨNG YÊN) đo khe hở THẬT bằng
    ///      `GetWorldCorners` + `InverseTransformPoint`, quét cả các con đang bật — vì icon
    ///      vàng/kim cương thò 16.8 px ra ngoài mép capsule, bỏ sót là tính sai khe hở.
    ///   2. Suy ra scale lớn nhất phần tử được phép đạt: mỗi bên chỉ được ăn
    ///      `phanKheHoDuocDung` (mặc định 45%) của khe hở ⇒ hai bên cùng phóng vẫn còn hở 10%.
    ///   3. Các nhịp sau chỉ làm MỘT việc: localScale vượt trần thì kéo về trần. KHÔNG vượt
    ///      thì KHÔNG ghi gì cả — chỉnh tay của Sếp trong scene không hề bị đụng tới.
    ///
    /// TỰ THÍCH NGHI: khe hở do Sếp kéo trong scene quyết định trần. Kéo hai khung xa nhau ra
    /// là trần tự nới, cú nảy tự đầy đặn lại — không phải sửa lại code.
    ///
    /// KHÔNG dùng cho: phần tử mà chính nó cần phóng to tự do (popup, card thưởng).
    /// </summary>
    [DisallowMultipleComponent]
    public class HudChongDeKhiPhongTo : MonoBehaviour
    {
        [Tooltip("Phần tử được canh giữ (thường chính là object mang script này).")]
        [SerializeField] private RectTransform khungCanhGiu;

        [Tooltip("Phần tử bên TRÁI không được đè vào. Để trống nếu bên trái trống.")]
        [SerializeField] private RectTransform tuongTrai;

        [Tooltip("Phần tử bên PHẢI không được đè vào. Để trống nếu bên phải trống.")]
        [SerializeField] private RectTransform tuongPhai;

        [Tooltip("Phần khe hở mỗi bên được phép dùng cho cú phóng to. 0.45 = 45%; hai bên cùng phóng vẫn còn hở 10%.")]
        [SerializeField, Range(0.1f, 0.9f)] private float phanKheHoDuocDung = 0.45f;

        [Tooltip("Trần cứng — chặn trường hợp khe hở đo được quá rộng làm cú nảy thành phi lý.")]
        [SerializeField] private float tranCung = 1.30f;

        // ══ [VÒNG 10] Ba núm MỚI. Tên khác hẳn mọi field cũ nên giá trị đã lưu trong
        //    scene KHÔNG đè được mặc định ở đây.
        [Tooltip("[VÒNG 10] Phần khe hở được dùng khi TƯỜNG bên đó KHÔNG BAO GIỜ nảy (Avatar_Button, Btn_Settings — đã soát cả dự án: không chỗ nào gọi JuicyPulseFX vào hai cái đó). Tường đứng yên thì chỉ MỘT mình phần tử này ăn khe hở, nên được ăn tới 85% mà vẫn còn hở 15%.")]
        [SerializeField, Range(0.1f, 0.95f)] private float phanKheHoKhiTuongDungYen = 0.85f;

        [Tooltip("[VÒNG 10] Con có localScale khác 1 (Icon_Gold, Icon_Diamond đang 1.2 và thò ra ngoài capsule) CŨNG bị RewardFlyFX phóng riêng 1.25 lần. Đo tầm với phải tính luôn cỡ phình đó, không thì trần tính ra quá rộng và hai khung vẫn đè nhau. Đặt 1 = tắt.")]
        [SerializeField] private float duPhongChoIconConTuNay = 1.25f;

        [Tooltip("[VÒNG 10] TRUE: đo lại khe hở khi kích thước màn hình đổi (xoay máy, đổi cỡ cửa sổ, chia đôi màn hình). BẮT BUỘC khi tường nằm ở CỤM KHÁC — khe hở giữa 2 cụm đổi theo tỉ lệ màn hình.")]
        [SerializeField] private bool doLaiKhiDoiKichThuocManHinh = true;

        // ══ [VÒNG 11] hai núm MỚI ═══════════════════════════════════════════════
        [Tooltip("[VÒNG 11] Kê dư BẮT BUỘC còn lại giữa 2 mép khi cả hai bên đã nảy hết trần, tính bằng px hệ 1920×1080. Luật % cũ để lại kê dư TỈ LỆ với khe hở, nên khe hở hẹp lại là kê dư teo theo. Số tuyệt đối này bảo đảm kê dư KHÔNG teo, và đó là cái cho phép kéo hai khung sát nhau hơn mà vẫn an toàn.")]
        [SerializeField] private float keDuAnToanToiThieu = 7.5f;

        [Tooltip("[VÒNG 11] TRUE: chỉ cộng dự phòng cho icon con SAU KHI thật sự thấy nó phình (so localScale với lúc đứng yên). Trước vòng 11 code cộng dự phòng cho MỌI con có localScale khác 1, kể cả con chẳng FX nào đụng tới — mất oan 9.3 đơn vị bề rộng. Bỏ tick là về cách cũ (luôn cộng).")]
        [SerializeField] private bool tuPhatHienIconConNay = true;

        private float _scaleToiDa = -1f;   // < 0 = chưa đo được
        private float _scaleLucGan = 1f;   // scale lúc cài đặt — dùng để biết phần tử đang đứng yên
        private int   _soNhipDaThu;
        private bool  _daBoQua;

        // [VÒNG 10] Tường bên nào đứng yên — quyết định dùng 45% hay 85% khe hở.
        private bool  _traiDungYen;
        private bool  _phaiDungYen;

        // [VÒNG 10] Cỡ màn hình lúc đo được — đổi cỡ thì đo lại (tường ở cụm khác sẽ xê dịch).
        private int   _manHinhWLucDo = -1;
        private int   _manHinhHLucDo = -1;

        // [VÒNG 10] Scale của 2 tường lúc đo LẦN ĐẦU. Đo lại mà tường đang phình thì khe hở
        // đo ra nhỏ giả và trần bị khoá chặt vĩnh viễn — phải đợi tường về mốc cũ.
        private float _scaleTuongTraiLucDo = -1f;
        private float _scaleTuongPhaiLucDo = -1f;

        // [VÒNG 11] Danh sách con có localScale riêng (Icon_Gold, Icon_Diamond) của phần tử
        // được canh giữ VÀ của 2 tường, kèm scale lúc đứng yên. Gom đúng một lần.
        private RectTransform[] _conCoScaleRieng;
        private float[]         _scaleNghiCuaCon;
        private bool            _daTungThayConNay;

        // [VÒNG 11] Kê dư đã đổi từ px hệ 1920×1080 sang đơn vị local của cha. Tính lúc đo.
        private float _keDuTheoDonViCha;

        private const int SO_NHIP_THU_TOI_DA = 300;   // ~5 giây ở 60fps rồi thôi, không quay vòng vô ích

        // Đệm 4 góc dùng chung — phép đo chỉ chạy vài nhịp đầu nên không sinh rác về sau.
        private static readonly Vector3[] GocBuf = new Vector3[4];

        /// <summary>
        /// Cài đặt từ code — bản CŨ, giữ y nguyên cách hiểu trước vòng 10: coi CẢ HAI tường
        /// đều có thể nảy, nên mỗi bên chỉ ăn <c>phanKheHoDuocDung</c>.
        /// </summary>
        public void Nap(RectTransform khung, RectTransform trai, RectTransform phai, float phanKheHo)
        {
            Nap(khung, trai, phai, phanKheHo, false, false);
        }

        /// <summary>
        /// [VÒNG 10] Cài đặt có khai báo TƯỜNG NÀO ĐỨNG YÊN. Gọi lại được — sẽ đo lại từ đầu.
        ///
        /// VÌ SAO CẦN: khe hở giữa thanh EXP và khung avatar chỉ có MỘT bên nảy (không FX nào
        /// phóng khung avatar). Bắt nó chỉ ăn 45% là phải chừa khe hở gấp đôi mức cần — đúng
        /// chỗ đó là cái đẩy cả cụm HUD vào giữa màn hình, việc Sếp không muốn.
        /// </summary>
        public void Nap(RectTransform khung, RectTransform trai, RectTransform phai, float phanKheHo,
                        bool traiDungYen, bool phaiDungYen)
        {
            Nap(khung, trai, phai, phanKheHo, traiDungYen, phaiDungYen, keDuAnToanToiThieu);
        }

        /// <summary>
        /// [VÒNG 11] Bản đầy đủ: thêm <paramref name="keDuPx"/> là kê dư bắt buộc còn lại khi cả
        /// hai bên nảy hết trần (px hệ 1920×1080).
        /// </summary>
        public void Nap(RectTransform khung, RectTransform trai, RectTransform phai, float phanKheHo,
                        bool traiDungYen, bool phaiDungYen, float keDuPx)
        {
            if (keDuPx >= 0f) keDuAnToanToiThieu = keDuPx;
            _traiDungYen = traiDungYen;
            _phaiDungYen = phaiDungYen;
            khungCanhGiu = khung;
            tuongTrai = trai;
            tuongPhai = phai;
            if (phanKheHo > 0f) phanKheHoDuocDung = Mathf.Clamp(phanKheHo, 0.1f, 0.9f);

            _scaleLucGan = khungCanhGiu != null ? khungCanhGiu.localScale.x : 1f;
            _scaleToiDa = -1f;
            _soNhipDaThu = 0;
            _daBoQua = false;
            _manHinhWLucDo = -1;
            _manHinhHLucDo = -1;
            _scaleTuongTraiLucDo = -1f;
            _scaleTuongPhaiLucDo = -1f;
            _conCoScaleRieng = null;
            _scaleNghiCuaCon = null;
            _daTungThayConNay = false;
            enabled = true;
        }

        private void Awake()
        {
            if (khungCanhGiu == null) khungCanhGiu = transform as RectTransform;
            _scaleLucGan = khungCanhGiu != null ? khungCanhGiu.localScale.x : 1f;
        }

        private void LateUpdate()
        {
            if (_daBoQua || khungCanhGiu == null) return;

            // [VÒNG 10] Đổi cỡ màn hình (xoay máy / đổi cỡ cửa sổ / chia đôi màn hình) làm đổi
            // khe hở giữa hai CỤM (CanvasScaler match 0.5 nên cỡ canvas logic đổi theo tỉ lệ),
            // nên trần đo lúc trước không còn đúng. Đo lại từ đầu.
            if (doLaiKhiDoiKichThuocManHinh && _scaleToiDa >= 0f &&
                (Screen.width != _manHinhWLucDo || Screen.height != _manHinhHLucDo))
            {
                _scaleToiDa = -1f;
                _soNhipDaThu = 0;
            }

            // [VÒNG 11] Thấy con có scale riêng bắt đầu phình ⇒ từ nay phải cộng dự phòng cho
            // nó. Đo lại ngay (phép đo tự đợi tới lúc mọi thứ đứng yên trở lại).
            if (tuPhatHienIconConNay && !_daTungThayConNay && _conCoScaleRieng != null)
            {
                for (int i = 0; i < _conCoScaleRieng.Length; i++)
                {
                    RectTransform con = _conCoScaleRieng[i];
                    if (con == null) continue;
                    if (Mathf.Abs(con.localScale.x - _scaleNghiCuaCon[i]) > 0.001f)
                    {
                        _daTungThayConNay = true;
                        _scaleToiDa = -1f;
                        _soNhipDaThu = 0;
                        break;
                    }
                }
            }

            if (_scaleToiDa < 0f)
            {
                if (!Do()) return;
            }

            Vector3 s = khungCanhGiu.localScale;
            if (s.x <= _scaleToiDa && s.y <= _scaleToiDa) return;   // đứng yên / còn trong trần ⇒ KHÔNG ghi gì

            khungCanhGiu.localScale = new Vector3(
                Mathf.Min(s.x, _scaleToiDa),
                Mathf.Min(s.y, _scaleToiDa),
                s.z);
        }

        /// <summary>
        /// Đo khe hở thật rồi suy ra trần. Chỉ nhận kết quả khi phần tử ĐANG ĐỨNG YÊN
        /// (scale bằng lúc cài đặt): đo đúng lúc đang phình sẽ ra khe hở nhỏ giả và trần
        /// quá chặt, nên gặp trường hợp đó thì đợi nhịp sau.
        /// </summary>
        private bool Do()
        {
            _soNhipDaThu++;
            if (_soNhipDaThu > SO_NHIP_THU_TOI_DA)
            {
                _daBoQua = true;
                {
                    Debug.LogWarning($"[HudChongDeKhiPhongTo] Không đo được khe hở cho '{name}' sau {SO_NHIP_THU_TOI_DA} nhịp — bỏ qua, HUD chạy như cũ.", this);
                }
                return false;
            }

            Transform cha = khungCanhGiu.parent;
            if (cha == null) return false;
            if (!Mathf.Approximately(khungCanhGiu.localScale.x, _scaleLucGan)) return false;

            // [VÒNG 10] Tường đang phình thì đợi — đo lúc đó sẽ ra khe hở nhỏ giả.
            if (!TuongDangDungYen(tuongTrai, ref _scaleTuongTraiLucDo)) return false;
            if (!TuongDangDungYen(tuongPhai, ref _scaleTuongPhaiLucDo)) return false;

            _keDuTheoDonViCha = TinhKeDuTheoDonViCha(cha);
            float duPhong = DuPhongDangDung();

            float taMin, taMax;
            if (!DoKhoangX(cha, khungCanhGiu, duPhong, out taMin, out taMax)) return false;

            float truc = khungCanhGiu.localPosition.x;   // cú phóng to lấy đúng điểm này làm tâm
            float he = float.MaxValue;

            // [VÒNG 10] Tường KHÁC CHA vẫn dùng được: DoKhoangX đổi 4 góc THẬT (world) về hệ
            // toạ độ của `cha`, nên hai bên luôn cùng một thước đo. Nhờ vậy thanh EXP (cụm
            // trái) lấy được khung Vàng (cụm phải) làm tường — cần cho màn hình hẹp (4:3 /
            // cửa sổ chia đôi) lúc hai cụm tới sát nhau.
            if (tuongPhai != null)
            {
                float pMin, pMax;
                if (DoKhoangX(cha, tuongPhai, duPhong, out pMin, out pMax))
                {
                    float voi = taMax - truc;                     // tầm với sang phải, đã gồm con thò ra
                    float kheHo = pMin - taMax;
                    if (voi > 1f) he = Mathf.Min(he, 1f + PhanDuocPhongRa(kheHo, _phaiDungYen) / voi);
                }
            }

            if (tuongTrai != null)
            {
                float tMin, tMax;
                if (DoKhoangX(cha, tuongTrai, duPhong, out tMin, out tMax))
                {
                    float voi = truc - taMin;                     // tầm với sang trái
                    float kheHo = taMin - tMax;
                    if (voi > 1f) he = Mathf.Min(he, 1f + PhanDuocPhongRa(kheHo, _traiDungYen) / voi);
                }
            }

            if (he == float.MaxValue) return false;               // chưa đo được tường nào ⇒ thử lại nhịp sau
            he = Mathf.Clamp(he, 1f, Mathf.Max(1f, tranCung));

            _scaleToiDa = Mathf.Max(0.01f, _scaleLucGan) * he;
            _manHinhWLucDo = Screen.width;
            _manHinhHLucDo = Screen.height;
            GomConCoScaleRieng();
            return true;
        }

        /// <summary>
        /// [VÒNG 11] Số đơn vị mà mép phần tử được phép phình thêm về một phía.
        ///
        /// Lấy số NHỎ HƠN của hai luật:
        ///   · luật % cũ  : khe hở × phần được dùng (45% khi tường cũng nảy, 85% khi tường đứng yên);
        ///   · luật kê dư : phần còn lại sau khi chừa <c>keDuAnToanToiThieu</c>, chia đôi nếu
        ///     tường bên đó CŨNG nảy (hai bên cùng ăn thì mỗi bên chỉ được nửa).
        ///
        /// VÌ SAO THÊM LUẬT KÊ DƯ: luật % để lại kê dư TỈ LỆ với khe hở, nên khe hở càng hẹp thì
        /// kê dư càng teo — kéo hai khung sát nhau là kê dư mỏng dần tới 0. Số tuyệt đối bảo đảm
        /// kê dư đứng yên ở mức đã hẹn, nhờ đó hai khung được phép sát nhau hơn mà vẫn không đè.
        /// Lấy MIN nên luật mới chỉ có thể chặt hơn hoặc bằng, không bao giờ lỏng hơn luật cũ ở
        /// khe hở rộng.
        /// </summary>
        private float PhanDuocPhongRa(float kheHo, bool tuongDungYen)
        {
            if (kheHo <= 0f) return 0f;

            float phan = tuongDungYen ? phanKheHoKhiTuongDungYen : phanKheHoDuocDung;
            float theoTiLe = kheHo * phan;

            float conLai = kheHo - Mathf.Max(0f, _keDuTheoDonViCha);
            float theoKeDu = tuongDungYen ? conLai : conLai * 0.5f;

            return Mathf.Max(0f, Mathf.Min(theoTiLe, theoKeDu));
        }

        /// <summary>
        /// [VÒNG 11] Hệ số dự phòng đang dùng cho con có scale riêng. Bật tự phát hiện thì chỉ
        /// cộng dự phòng sau khi ĐÃ thấy con đó phình — con nào không FX nào đụng tới thì không
        /// bắt cả layout trả giá cho nó.
        /// </summary>
        private float DuPhongDangDung()
        {
            if (!tuPhatHienIconConNay) return duPhongChoIconConTuNay;
            return _daTungThayConNay ? duPhongChoIconConTuNay : 1f;
        }

        /// <summary>
        /// Đổi <c>keDuAnToanToiThieu</c> (px hệ 1920×1080) sang đơn vị local của <paramref name="cha"/>.
        /// Cụm HUD đang localScale 1.2 nên 7.5 px thành 6.25 đơn vị. Không tìm được Canvas gốc
        /// thì dùng thẳng số px — chặt hơn thực tế, vẫn an toàn.
        /// </summary>
        private float TinhKeDuTheoDonViCha(Transform cha)
        {
            if (cha == null) return keDuAnToanToiThieu;

            Canvas goc = khungCanhGiu.GetComponentInParent<Canvas>();
            if (goc != null && goc.rootCanvas != null) goc = goc.rootCanvas;
            if (goc == null) return keDuAnToanToiThieu;

            float sCha = cha.lossyScale.x;
            float sGoc = goc.transform.lossyScale.x;
            if (sCha <= 0.0001f || sGoc <= 0.0001f) return keDuAnToanToiThieu;

            return keDuAnToanToiThieu * sGoc / sCha;
        }

        /// <summary>
        /// [VÒNG 11] Gom con có localScale khác 1 của phần tử được canh giữ và của 2 tường, ghi
        /// nhớ scale lúc đứng yên để về sau biết con nào thật sự bị FX phóng.
        /// </summary>
        private void GomConCoScaleRieng()
        {
            if (_conCoScaleRieng != null) return;

            var ds = new System.Collections.Generic.List<RectTransform>(4);
            ThemConCoScaleRieng(khungCanhGiu, ds);
            ThemConCoScaleRieng(tuongTrai, ds);
            ThemConCoScaleRieng(tuongPhai, ds);

            _conCoScaleRieng = ds.ToArray();
            _scaleNghiCuaCon = new float[_conCoScaleRieng.Length];
            for (int i = 0; i < _conCoScaleRieng.Length; i++)
            {
                _scaleNghiCuaCon[i] = _conCoScaleRieng[i].localScale.x;
            }
        }

        private static void ThemConCoScaleRieng(RectTransform goc, System.Collections.Generic.List<RectTransform> ds)
        {
            if (goc == null) return;

            RectTransform[] con = goc.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < con.Length; i++)
            {
                if (con[i] == null || con[i] == goc) continue;
                if (Mathf.Approximately(con[i].localScale.x, 1f)) continue;
                if (!ds.Contains(con[i])) ds.Add(con[i]);
            }
        }

        /// <summary>
        /// [VÒNG 10] Tường phải đứng yên mới đo được. Lần đầu thấy tường thì ghi nhớ scale của
        /// nó làm mốc; các lần đo lại (đổi cỡ màn hình) chỉ nhận khi tường đã về đúng mốc đó.
        /// </summary>
        private static bool TuongDangDungYen(RectTransform tuong, ref float scaleMoc)
        {
            if (tuong == null) return true;
            float s = tuong.localScale.x;
            if (scaleMoc < 0f)
            {
                scaleMoc = s;
                return true;
            }
            return Mathf.Approximately(s, scaleMoc);
        }

        /// <summary>
        /// Khoảng X (min, max) của <paramref name="rt"/> và mọi con ĐANG BẬT, đo trong hệ toạ độ
        /// local của <paramref name="hqc"/>. Con đang tắt bị bỏ qua — đúng ý: 2 nút + đã ẩn thì
        /// không được tính vào bề rộng khung nữa.
        ///
        /// [VÒNG 10] duPhongCon: con có localScale khác 1 là con mà FX phóng RIÊNG (Icon_Gold /
        /// Icon_Diamond đang 1.2, RewardFlyFX đẩy lên 1.25 lần nữa). Nới bề rộng đo được của nó
        /// lên đúng cỡ phình cực đại — không nới thì tầm với đo ra nhỏ hơn thực tế, trần tính ra
        /// quá rộng, và hai khung vẫn đè nhau đúng như vòng 8.
        /// </summary>
        private static bool DoKhoangX(Transform hqc, RectTransform rt, float duPhongCon, out float minX, out float maxX)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            if (hqc == null || rt == null) return false;

            RectTransform[] ds = rt.GetComponentsInChildren<RectTransform>(false);
            for (int i = 0; i < ds.Length; i++)
            {
                if (ds[i] == null) continue;
                ds[i].GetWorldCorners(GocBuf);

                float cMin = float.MaxValue;
                float cMax = float.MinValue;
                for (int g = 0; g < 4; g++)
                {
                    float x = hqc.InverseTransformPoint(GocBuf[g]).x;
                    if (x < cMin) cMin = x;
                    if (x > cMax) cMax = x;
                }

                if (ds[i] != rt && duPhongCon > 1f && !Mathf.Approximately(ds[i].localScale.x, 1f))
                {
                    float giua = (cMin + cMax) * 0.5f;
                    float nua  = (cMax - cMin) * 0.5f * duPhongCon;
                    cMin = giua - nua;
                    cMax = giua + nua;
                }

                if (cMin < minX) minX = cMin;
                if (cMax > maxX) maxX = cMax;
            }

            return maxX - minX > 1f;
        }
    }
}
