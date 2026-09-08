using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Thẻ kết quả giữa màn hình scene câu: icon cá, tên, "x.x kg", viền màu theo độ hiếm; nảy JuicyPulseFX; tự ẩn sau cfg.resultShowSeconds.
    /// Con "Toast_Result" của Canvas_FishingHUD (FishingHudUI.BuildIfEmpty tạo). Cũng dùng hiện chữ ngắn (ShowText).
    /// </summary>
    public class FishingResultToastUI : MonoBehaviour
    {
        public static FishingResultToastUI Instance { get; private set; }

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private Image imgBorder;
        [SerializeField] private Image imgFrame;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtName;
        [SerializeField] private TextMeshProUGUI txtWeight;

        private Coroutine _hideRoutine;

        private static readonly Vector2 CardSize = new Vector2(560f, 200f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning(FishingIds.LogTag + " FishingResultToastUI trùng tại '" + name + "' — tự ẩn."); gameObject.SetActive(false); return; }
            Instance = this;
            BuildIfEmpty();
        }

        private void OnDestroy() { if (Instance == this) { Instance = null; } }

        /// <summary>Dựng con nếu thiếu (tool + runtime), không huỷ con có sẵn.</summary>
        public void BuildIfEmpty()
        {
            var rt = transform as RectTransform;
            if (rt != null && rt.sizeDelta == Vector2.zero) { rt.sizeDelta = CardSize; }

            Image border = FishingUiKit.Panel(transform, "Img_Border", CardSize, FishingUiKit.Rounded(26f), Vector2.zero, FishingUiKit.RarityColor(FishRarity.Common));
            if (imgBorder == null) { imgBorder = border; }
            imgBorder.raycastTarget = false;

            Image frame = FishingUiKit.Panel(transform, "Img_Frame", CardSize - new Vector2(14f, 14f), UIStandardSprites.PanelPaper);
            if (imgFrame == null) { imgFrame = frame; }
            imgFrame.raycastTarget = false;

            Image icon = FishingUiKit.Icon(imgFrame.transform, "Img_Icon", null, new Vector2(130f, 130f), new Vector2(-190f, 0f), FishingUiKit.RarityColor(FishRarity.Common));
            if (imgIcon == null) { imgIcon = icon; }

            TextMeshProUGUI title = FishingUiKit.Label(imgFrame.transform, "Txt_Title", Loc.T("BẮT ĐƯỢC!"), 26f, TextAlignmentOptions.Left, new Vector2(330f, 36f), new Vector2(55f, 60f), FishingUiKit.TextMuted, true);
            if (txtTitle == null) { txtTitle = title; }
            TextMeshProUGUI nm = FishingUiKit.Label(imgFrame.transform, "Txt_Name", string.Empty, 40f, TextAlignmentOptions.Left, new Vector2(330f, 56f), new Vector2(55f, 8f), FishingUiKit.TextDark, true);
            if (txtName == null) { txtName = nm; }
            TextMeshProUGUI kg = FishingUiKit.Label(imgFrame.transform, "Txt_Weight", string.Empty, 30f, TextAlignmentOptions.Left, new Vector2(330f, 40f), new Vector2(55f, -48f), FishingUiKit.TextMuted);
            if (txtWeight == null) { txtWeight = kg; }
        }

        /// <summary>Hiện thẻ cá bắt được.</summary>
        public static void Show(FishData f, float kg)
        {
            FishingResultToastUI inst = Resolve();
            if (inst == null) { return; }
            inst.ShowFish(f, kg);
        }

        /// <summary>Hiện chữ ngắn (cá thoát, thu không...). Không icon.</summary>
        public static void ShowText(string vi)
        {
            FishingResultToastUI inst = Resolve();
            if (inst == null) { return; }
            inst.ShowPlain(vi);
        }

        private static FishingResultToastUI Resolve()
        {
            if (Instance != null) { return Instance; }
            var found = FindFirstObjectByType<FishingResultToastUI>(FindObjectsInactive.Include);
            if (found != null) { Instance = found; return found; }
            // Chưa có trong scene: tạo dưới HUD nếu HUD tồn tại.
            FishingHudUI hud = FishingHudUI.Instance;
            if (hud == null) { hud = FindFirstObjectByType<FishingHudUI>(FindObjectsInactive.Include); }
            if (hud == null) { Debug.Log(FishingIds.LogTag + " Không có Canvas_FishingHUD để hiện toast kết quả."); return null; }
            RectTransform rt = FishingUiKit.Child(hud.transform, "Toast_Result", CardSize, new Vector2(0f, 140f));
            var inst = FishingUiKit.GetOrAdd<FishingResultToastUI>(rt.gameObject);
            Instance = inst;
            inst.BuildIfEmpty();
            return inst;
        }

        private void ShowFish(FishData f, float kg)
        {
            BuildIfEmpty();
            FishRarity rarity = f != null ? f.rarity : FishRarity.Common;
            Color rc = FishingUiKit.RarityColor(rarity);
            if (imgBorder != null) { imgBorder.color = rc; }
            if (imgIcon != null) { imgIcon.gameObject.SetActive(true); FishingUiKit.SetIcon(imgIcon, f != null ? f.icon : null, rc); }
            if (txtTitle != null) { txtTitle.text = Loc.T("BẮT ĐƯỢC!"); }
            if (txtName != null) { txtName.text = f != null ? f.displayName : Loc.T("Cá lạ"); }
            if (txtWeight != null) { txtWeight.text = FishingUiKit.Kg(kg); }
            Present();
        }

        private void ShowPlain(string vi)
        {
            BuildIfEmpty();
            if (imgBorder != null) { imgBorder.color = FishingUiKit.RarityColor(FishRarity.Common); }
            if (imgIcon != null) { imgIcon.gameObject.SetActive(false); }
            if (txtTitle != null) { txtTitle.text = string.Empty; }
            if (txtName != null) { txtName.text = vi ?? string.Empty; }
            if (txtWeight != null) { txtWeight.text = string.Empty; }
            Present();
        }

        private void Present()
        {
            FishingUiKit.ActivateUpToCanvas(transform);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            JuicyPulseFX.Play(transform);
            if (_hideRoutine != null) { StopCoroutine(_hideRoutine); }
            _hideRoutine = StartCoroutine(HideAfter(FishingDatabase.ConfigOrDefault.resultShowSeconds));
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0.2f, seconds));
            _hideRoutine = null;
            gameObject.SetActive(false);
        }
    }
}
