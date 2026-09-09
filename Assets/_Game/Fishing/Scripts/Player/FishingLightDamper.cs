using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FarmGame.Fishing
{
    /// <summary>
    /// VONG 14b — HA SANG RIENG CHO SCENE CAU CA.
    ///
    /// TRIEU CHUNG SEP BAO: "sang qua khong thay nhan vat, lau lau trang xoa man hinh,
    /// ban ngay bac mau nuoc; ban dem thi dep".
    ///
    /// GOC RE (do that tu file): SCN_Fishing khong co Light2D nao rieng — toan bo anh sang
    /// den tu 1 instance prefab DayNightWeatherSetup, va scene KHONG override gia tri den nao.
    /// URP 2D cong don moi Light2D cung blend style roi NHAN vao mau sprite. Blend style 0
    /// trong Renderer2D.asset la "Multiply". Cong lai:
    ///     Ambient(1.15..1.45) + DayLight(0.25..0.85) + PlayerLantern(0.8) ≈ 2.5 .. 2.8
    /// Moi pixel sang tu 0.36 tro len la bao hoa trang. DayDurationInSeconds = 300 nen
    /// ratio quet qua dinh sang 2 lan moi 5 phut -> dung hien tuong "lau lau trang xoa".
    /// Ban dem ambient nhan voi mau xanh dam nen van dep — khop mo ta cua Sep.
    ///
    /// VI SAO KHONG SUA THANG DayNightWeatherSetup.prefab: prefab do DUNG CHUNG voi
    /// SCN_Farm. Ha curve la farm toi theo, ma Sep khong che farm. Nen ha rieng o day,
    /// chi trong scene cau ca, bang code — khong dung scene, khong dung prefab chung.
    ///
    /// CACH LAM: chay o LateUpdate (sau khi DayNightCycleController da ghi intensity moi
    /// frame) va nhan lai he so. Giu nguyen PlayerLantern — vong tron sang theo nhan vat
    /// la thu Sep khen dep, khong dong vao (no da duoc ha rieng trong FishingConfig).
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class FishingLightDamper : MonoBehaviour
    {
        /// <summary>He so nhan cho den TOAN CUC (Global). 1 = giu nguyen.</summary>
        [Range(0.2f, 1f)] public float heSoGlobal = 0.62f;

        /// <summary>He so nhan cho den DIEM cua chu ky ngay/dem (DayLight, SunRim...).</summary>
        [Range(0.1f, 1f)] public float heSoDiem = 0.40f;

        /// <summary>Tran cung cho tong he so nhan, chan chay sang du curve co doi.</summary>
        [Range(0.5f, 2f)] public float tranSang = 1.15f;

        private Light2D[] _dsDen;
        private float _hetHanQuet;

        public static FishingLightDamper GanVao(GameObject chu)
        {
            if (chu == null) { return null; }
            FishingLightDamper cu = chu.GetComponent<FishingLightDamper>();
            if (cu != null) { return cu; }
            return chu.AddComponent<FishingLightDamper>();
        }

        private void QuetDen()
        {
            _dsDen = FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            _hetHanQuet = Time.time + 3f;
        }

        private void OnEnable() { QuetDen(); }

        private void LateUpdate()
        {
            // Quet lai dinh ky: den cua chu ky ngay/dem co the duoc bat/tat giua chung.
            if (_dsDen == null || Time.time >= _hetHanQuet) { QuetDen(); }
            if (_dsDen == null) { return; }

            float tong = 0f;
            for (int i = 0; i < _dsDen.Length; i++)
            {
                Light2D d = _dsDen[i];
                if (d == null || !d.isActiveAndEnabled) { continue; }
                if (d.gameObject.name == "PlayerLantern") { continue; }   // vong tron sang: khong dong

                float k = d.lightType == Light2D.LightType.Global ? heSoGlobal : heSoDiem;
                d.intensity *= k;

                if (d.blendStyleIndex == 0) { tong += d.intensity; }      // style 0 = Multiply
            }

            // Chan tran: neu tong van vuot, ha deu tat ca den style 0 xuong cho vua tran.
            if (tong > tranSang && tong > 0.0001f)
            {
                float ep = tranSang / tong;
                for (int i = 0; i < _dsDen.Length; i++)
                {
                    Light2D d = _dsDen[i];
                    if (d == null || !d.isActiveAndEnabled) { continue; }
                    if (d.gameObject.name == "PlayerLantern") { continue; }
                    if (d.blendStyleIndex != 0) { continue; }
                    d.intensity *= ep;
                }
            }
        }
    }
}
