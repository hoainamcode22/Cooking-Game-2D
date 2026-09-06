using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [VONG 7 - 06/09] Tach ra file rieng.
// LY DO: Unity chi sinh DUNG MOT script asset cho moi file .cs, ung voi class trung ten file.
// Class MonoBehaviour thu hai nam chung file thi KHONG co asset rieng, nen AddComponent<>() cua
// Editor tool (TrainPackageBuildTool.cs:376) bi Unity ghi thanh fileID 11500000 = class chinh.
// Hau qua da xay ra: 4 toa tau mang component TrainStationMasterPopupUI, moi cai tu dung mot
// popup rieng => 5 popup de len nhau. Giu class nay o file rieng de bug do khong tai phat.
// Luat du an: MOI FILE MOT CHU (memory/MEMORY.md).

namespace ExportTrainUIPackage
{
    public class StationWagonSlotUI : MonoBehaviour
    {
        private const string SpritesDir = "Assets/Export_Train_UI_Package/Sprites";
        private const string ShopSvgDir = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";

        public Image imgWagon;
        public GameObject bubbleReq;
        public Image imgBubble;
        public Image imgDisc;
        public Image imgIcon;
        public TextMeshProUGUI txtAmount;
        public GameObject checkBadge;
        public Image imgCheckBadge;
        public Button btnSlot;

        private System.Action onClickCallback;
        private Coroutine bobbingRoutine;

        public void AutoBindComponents() => BuildWagonHierarchy();

        public void BuildWagonHierarchy()
        {
            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();

            // Raycast target trên chính root slot để Button bắt được click trên toàn bộ diện tích toa
            var slotImg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            slotImg.color = Color.clear;
            slotImg.raycastTarget = true;

            // 1. Wagon Image
            Transform wTr = transform.Find("Img_Wagon");
            bool isNewWagon = wTr == null;
            if (isNewWagon)
            {
                GameObject wGo = new GameObject("Img_Wagon", typeof(RectTransform));
                wGo.transform.SetParent(transform, false);
                wTr = wGo.transform;
            }
            wTr.gameObject.SetActive(true);
            imgWagon = wTr.GetComponent<Image>() ?? wTr.gameObject.AddComponent<Image>();
            RectTransform wiRt = wTr.GetComponent<RectTransform>();
            wiRt.anchorMin = new Vector2(0.5f, 0f);
            wiRt.anchorMax = new Vector2(0.5f, 0f);
            wiRt.pivot = new Vector2(0.5f, 0.5f);
            wiRt.anchoredPosition = new Vector2(0f, 55f);
            wiRt.sizeDelta = new Vector2(170f, 110f);
            TrainSpriteLoader.Assign(imgWagon, $"{SpritesDir}/flat_wagon_horizontal.png");
            imgWagon.preserveAspect = true;
            imgWagon.color = Color.white;
            imgWagon.raycastTarget = false;
            imgWagon.enabled = true;

            // 2. Bubble Req
            Transform bTr = transform.Find("Bubble_Req");
            if (bTr == null)
            {
                GameObject bGo = new GameObject("Bubble_Req", typeof(RectTransform));
                bGo.transform.SetParent(transform, false);
                bTr = bGo.transform;
            }
            bubbleReq = bTr.gameObject;
            imgBubble = bTr.GetComponent<Image>() ?? bTr.gameObject.AddComponent<Image>();
            TrainSpriteLoader.Assign(imgBubble, $"{ShopSvgDir}/shop_card_outer.png", $"{SpritesDir}/bubble_cargo_req.png");
            imgBubble.type = Image.Type.Sliced;
            imgBubble.color = Color.white;
            imgBubble.raycastTarget = false;

            RectTransform bRt = bTr.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0f, 175f);
            bRt.sizeDelta = new Vector2(130f, 62f);

            // Icon Disc
            Transform dTr = bTr.Find("Icon_Disc");
            if (dTr == null)
            {
                GameObject dGo = new GameObject("Icon_Disc", typeof(RectTransform));
                dGo.transform.SetParent(bTr, false);
                dTr = dGo.transform;
            }
            imgDisc = dTr.GetComponent<Image>() ?? dTr.gameObject.AddComponent<Image>();
            TrainSpriteLoader.Assign(imgDisc, $"{SpritesDir}/icon_disc_large.png");
            imgDisc.preserveAspect = true;
            imgDisc.color = Color.white;
            imgDisc.raycastTarget = false;

            RectTransform dRt = dTr.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = new Vector2(32f, 0f);
            dRt.sizeDelta = new Vector2(40f, 40f);

            // Icon
            Transform icTr = dTr.Find("Img_Icon");
            if (icTr == null)
            {
                GameObject icGo = new GameObject("Img_Icon", typeof(RectTransform));
                icGo.transform.SetParent(dTr, false);
                icTr = icGo.transform;
            }
            imgIcon = icTr.GetComponent<Image>() ?? icTr.gameObject.AddComponent<Image>();
            imgIcon.preserveAspect = true;
            imgIcon.raycastTarget = false;
            RectTransform icRt = icTr.GetComponent<RectTransform>();
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.offsetMin = new Vector2(4f, 4f);
            icRt.offsetMax = new Vector2(-4f, -4f);

            // Amount Text
            Transform amTr = bTr.Find("Txt_Amount");
            bool isNewAm = amTr == null;
            if (isNewAm)
            {
                GameObject amGo = new GameObject("Txt_Amount", typeof(RectTransform));
                amGo.transform.SetParent(bTr, false);
                amTr = amGo.transform;
            }
            txtAmount = amTr.GetComponent<TextMeshProUGUI>() ?? amTr.gameObject.AddComponent<TextMeshProUGUI>();
            txtAmount.raycastTarget = false;
            RectTransform amRt = amTr.GetComponent<RectTransform>();
            if (isNewAm)
            {
                amRt.anchorMin = new Vector2(0.45f, 0f);
                amRt.anchorMax = new Vector2(1f, 1f);
                amRt.offsetMin = Vector2.zero;
                amRt.offsetMax = new Vector2(-4f, 0f);
                txtAmount.alignment = TextAlignmentOptions.Center;
                txtAmount.fontSize = 22;
                txtAmount.fontStyle = FontStyles.Bold;
                txtAmount.color = new Color(0.36f, 0.20f, 0.09f);
            }

            // 3. Check Badge
            Transform chkTr = transform.Find("Check_Badge");
            bool isNewChk = chkTr == null;
            if (isNewChk)
            {
                GameObject chkGo = new GameObject("Check_Badge", typeof(RectTransform));
                chkGo.transform.SetParent(transform, false);
                chkTr = chkGo.transform;
            }
            checkBadge = chkTr.gameObject;
            imgCheckBadge = chkTr.GetComponent<Image>() ?? chkTr.gameObject.AddComponent<Image>();
            TrainSpriteLoader.Assign(imgCheckBadge, $"{SpritesDir}/check_badge_green.png");
            imgCheckBadge.preserveAspect = true;
            imgCheckBadge.color = Color.white;
            imgCheckBadge.raycastTarget = false;

            RectTransform chkRt = chkTr.GetComponent<RectTransform>();
            if (isNewChk)
            {
                chkRt.anchorMin = new Vector2(1f, 0f);
                chkRt.anchorMax = new Vector2(1f, 0f);
                chkRt.pivot = new Vector2(0.5f, 0.5f);
                chkRt.anchoredPosition = new Vector2(-25f, 55f);
                chkRt.sizeDelta = new Vector2(38f, 38f);
            }
            checkBadge.SetActive(false);

            // Button
            btnSlot = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            btnSlot.targetGraphic = slotImg;
            btnSlot.onClick.RemoveAllListeners();
            btnSlot.onClick.AddListener(() => onClickCallback?.Invoke());
        }

        /// <summary>Toa trống — không yêu cầu hàng, không click được.</summary>
        public void SetupEmptyMode()
        {
            BuildWagonHierarchy();
            onClickCallback = null;
            StopBobbingAnimation();
            if (bubbleReq != null) bubbleReq.SetActive(false);
            if (checkBadge != null) checkBadge.SetActive(false);
        }

        /// <summary>Bản Sprite thật (từ TrainCargoData asset) — chạy được cả trong build.</summary>
        public void SetupCargoMode(string name, Sprite iconSprite, int cur, int target, System.Action onClick)
        {
            BuildWagonHierarchy();
            onClickCallback = onClick;

            if (bubbleReq != null) bubbleReq.SetActive(true);

            if (txtAmount != null)
            {
                txtAmount.text = $"{cur}/{target}";
                txtAmount.color = (cur >= target) ? new Color(0.30f, 0.56f, 0.11f) : new Color(0.36f, 0.20f, 0.09f);
            }

            if (checkBadge != null)
                checkBadge.SetActive(cur >= target);

            if (imgIcon != null)
            {
                if (iconSprite != null) imgIcon.sprite = iconSprite;
                imgIcon.enabled = imgIcon.sprite != null;
                imgIcon.color = Color.white;
            }

            StartBobbingAnimation();
        }

        /// <summary>Bản Sprite thật (từ TrainRewardData asset) — chạy được cả trong build.</summary>
        public void SetupRewardMode(string name, Sprite iconSprite, int count, bool isCollected, System.Action onClick)
        {
            BuildWagonHierarchy();
            onClickCallback = onClick;

            if (checkBadge != null) checkBadge.SetActive(false);

            if (bubbleReq != null) bubbleReq.SetActive(!isCollected);

            if (txtAmount != null)
            {
                txtAmount.text = $"x{count}";
                txtAmount.color = new Color(0.48f, 0.29f, 0.06f);
            }

            if (imgIcon != null)
            {
                if (iconSprite != null) imgIcon.sprite = iconSprite;
                imgIcon.enabled = imgIcon.sprite != null;
                imgIcon.color = Color.white;
            }

            if (!isCollected) StartBobbingAnimation();
            else StopBobbingAnimation();
        }

        public void SetupCargoMode(string name, string iconPath, int cur, int target, System.Action onClick)
        {
            BuildWagonHierarchy();
            onClickCallback = onClick;

            if (bubbleReq != null) bubbleReq.SetActive(true);

            if (txtAmount != null)
            {
                txtAmount.text = $"{cur}/{target}";
                txtAmount.color = (cur >= target) ? new Color(0.30f, 0.56f, 0.11f) : new Color(0.36f, 0.20f, 0.09f);
            }

            if (checkBadge != null)
                checkBadge.SetActive(cur >= target);

            if (imgIcon != null)
            {
                TrainSpriteLoader.Assign(imgIcon, iconPath);
                imgIcon.color = Color.white;
                imgIcon.enabled = true;
            }

            StartBobbingAnimation();
        }

        public void SetupRewardMode(string name, string iconPath, int count, bool isCollected, System.Action onClick)
        {
            BuildWagonHierarchy();
            onClickCallback = onClick;

            if (checkBadge != null) checkBadge.SetActive(false);

            if (bubbleReq != null)
            {
                bubbleReq.SetActive(!isCollected);
            }

            if (txtAmount != null)
            {
                txtAmount.text = $"x{count}";
                txtAmount.color = new Color(0.48f, 0.29f, 0.06f);
            }

            if (imgIcon != null)
            {
                TrainSpriteLoader.Assign(imgIcon, iconPath);
                imgIcon.color = Color.white;
                imgIcon.enabled = true;
            }

            if (!isCollected) StartBobbingAnimation();
            else StopBobbingAnimation();
        }

        private void OnEnable()
        {
            if (bubbleReq != null && bubbleReq.activeSelf)
                StartBobbingAnimation();
        }

        private void OnDisable()
        {
            StopBobbingAnimation();
        }

        public void PlayClaimRewardEffect()
        {
            if (bubbleReq != null && gameObject.activeInHierarchy)
                StartCoroutine(RoutineClaimBounce());
        }

        private IEnumerator RoutineClaimBounce()
        {
            if (bubbleReq == null) yield break;
            RectTransform rt = bubbleReq.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            float el = 0f;
            while (el < 0.4f)
            {
                el += Time.deltaTime;
                float scale = 1f + Mathf.Sin(el / 0.4f * Mathf.PI) * 0.4f;
                rt.localScale = Vector3.one * scale;
                rt.anchoredPosition = startPos + new Vector2(0f, Mathf.Sin(el / 0.4f * Mathf.PI) * 25f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            bubbleReq.SetActive(false);
        }

        private void StartBobbingAnimation()
        {
            StopBobbingAnimation();
            if (gameObject.activeInHierarchy)
                bobbingRoutine = StartCoroutine(RoutineBobbing());
        }

        private void StopBobbingAnimation()
        {
            if (bobbingRoutine != null)
            {
                StopCoroutine(bobbingRoutine);
                bobbingRoutine = null;
            }
        }

        private IEnumerator RoutineBobbing()
        {
            if (bubbleReq == null) yield break;
            RectTransform rt = bubbleReq.GetComponent<RectTransform>();
            Vector2 basePos = rt.anchoredPosition;
            float seed = Random.Range(0f, 10f);
            while (true)
            {
                float dy = Mathf.Sin((Time.time + seed) * 3.5f) * 6f;
                rt.anchoredPosition = basePos + new Vector2(0f, dy);
                yield return null;
            }
        }
    }
}
