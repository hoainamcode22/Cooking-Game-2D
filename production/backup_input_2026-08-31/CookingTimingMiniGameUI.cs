using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
public class CookingTimingMiniGameUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject miniGameRoot;
    [SerializeField] private RectTransform barBackground;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private RectTransform movingMarker;

    [Header("Easy Settings")]
    [SerializeField] private float easyMarkerSpeed = 350f;
    [SerializeField] private float easyZoneSpeed = 150f;

    [Header("Normal Settings")]
    [SerializeField] private float normalMarkerSpeed = 500f;
    [SerializeField] private float normalZoneSpeed = 250f;

    [Header("Hard Settings")]
    [SerializeField] private float hardMarkerSpeed = 700f;
    [SerializeField] private float hardZoneSpeed = 400f;

    [Header("Input")]
    [SerializeField] private KeyCode stopKey = KeyCode.Space;

    [Header("Time Limit")]
    [SerializeField] private float miniGameDuration = 5f;

    private float remainingTime;

    private bool isPlaying;
    private bool hasStopped;

    private float markerDirection = 1f;
    private float zoneDirection = -1f;

    private float currentMarkerSpeed;
    private float currentZoneSpeed;

    private Action<bool> onMiniGameFinished;
    
    [Header("Interaction Blocker")]
    [SerializeField] private GameObject interactionBlocker;
    [SerializeField] private TMP_Text txtTimeRemaining;

    private void Start()
    {
        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (hasStopped) return;

        MoveMarker();
        MoveSuccessZone();

        if (IsStopKeyPressed())
        {
            StopMiniGame();
            return;
        }

        remainingTime -= Time.deltaTime;
        UpdateTimeRemainingUI();

        if (remainingTime <= 0f)
        {
            TimeoutMiniGame();
            return;
        }
    }
    public void StartMiniGame(DishDifficulty difficulty, Action<bool> callback)
    {

        onMiniGameFinished = callback;

        ApplyDifficulty(difficulty);

        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(true);
            interactionBlocker.transform.SetAsLastSibling();
        }

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(true);
            miniGameRoot.transform.SetAsLastSibling();
        }

        isPlaying = true;
        hasStopped = false;

        remainingTime = miniGameDuration;
        UpdateTimeRemainingUI();

        markerDirection = 1f;
        zoneDirection = -1f;

        ResetPositions();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

    }

    private void ApplyDifficulty(DishDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DishDifficulty.Easy:
                currentMarkerSpeed = easyMarkerSpeed;
                currentZoneSpeed = easyZoneSpeed;
                break;

            case DishDifficulty.Normal:
                currentMarkerSpeed = normalMarkerSpeed;
                currentZoneSpeed = normalZoneSpeed;
                break;

            case DishDifficulty.Hard:
                currentMarkerSpeed = hardMarkerSpeed;
                currentZoneSpeed = hardZoneSpeed;
                break;

            default:
                currentMarkerSpeed = normalMarkerSpeed;
                currentZoneSpeed = normalZoneSpeed;
                break;
        }
    }

    private void MoveMarker()
    {
        MoveRectInsideBar(movingMarker, ref markerDirection, currentMarkerSpeed);
        if (movingMarker != null)
        {
            float scaleX = 1f + 0.15f * Mathf.Sin(Time.time * 25f);
            float scaleY = 1f - 0.15f * Mathf.Sin(Time.time * 25f);
            movingMarker.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }

    private void MoveSuccessZone()
    {
        MoveRectInsideBar(successZone, ref zoneDirection, currentZoneSpeed);
        if (successZone != null)
        {
            var img = successZone.GetComponent<UnityEngine.UI.Image>();
            if(img != null) img.color = Color.Lerp(Color.white, Color.green, (Mathf.Sin(Time.time * 10f) + 1f) / 2f);
        }
    }

    private void MoveRectInsideBar(RectTransform target, ref float direction, float speed)
    {
        if (barBackground == null || target == null) return;

        float barHalfWidth = barBackground.rect.width / 2f;
        float targetHalfWidth = target.rect.width / 2f;

        float leftLimit = -barHalfWidth + targetHalfWidth;
        float rightLimit = barHalfWidth - targetHalfWidth;

        Vector2 pos = target.anchoredPosition;
        pos.x += direction * speed * Time.deltaTime;

        if (pos.x >= rightLimit)
        {
            pos.x = rightLimit;
            direction = -1f;
        }
        else if (pos.x <= leftLimit)
        {
            pos.x = leftLimit;
            direction = 1f;
        }

        target.anchoredPosition = pos;
    }

    private void ResetPositions()
    {
        if (barBackground == null || movingMarker == null || successZone == null) return;

        float barHalfWidth = barBackground.rect.width / 2f;

        float markerHalfWidth = movingMarker.rect.width / 2f;
        float zoneHalfWidth = successZone.rect.width / 2f;

        Vector2 markerPos = movingMarker.anchoredPosition;
        markerPos.x = -barHalfWidth + markerHalfWidth;
        movingMarker.anchoredPosition = markerPos;

        Vector2 zonePos = successZone.anchoredPosition;
        zonePos.x = barHalfWidth - zoneHalfWidth;
        successZone.anchoredPosition = zonePos;
    }

    private void StopMiniGame()
    {
        bool isSuccess = IsMarkerInsideSuccessZone();


        FinishMiniGame(isSuccess);
    }

    private bool IsMarkerInsideSuccessZone()
    {
        if (movingMarker == null || successZone == null) return false;

        float markerX = movingMarker.anchoredPosition.x;

        float zoneCenterX = successZone.anchoredPosition.x;
        float zoneHalfWidth = successZone.rect.width / 2f;

        float zoneMinX = zoneCenterX - zoneHalfWidth;
        float zoneMaxX = zoneCenterX + zoneHalfWidth;


        return markerX >= zoneMinX && markerX <= zoneMaxX;
    }
    private bool IsStopKeyPressed()
    {
        bool pressed = false;

    #if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(stopKey))
        {
            pressed = true;
        }
    #endif

    #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            pressed = true;
        }
    #endif

        return pressed;
    }
    private void TimeoutMiniGame()
    {
        FinishMiniGame(false);
    }
    private void FinishMiniGame(bool isSuccess)
    {
        if (hasStopped) return;

        hasStopped = true;
        isPlaying = false;

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(false);
        }

        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(false);
        }
        if (txtTimeRemaining != null)
        {
            txtTimeRemaining.text = "";
        }

        Action<bool> callback = onMiniGameFinished;
        onMiniGameFinished = null;

        callback?.Invoke(isSuccess);
    }
    private void UpdateTimeRemainingUI()
    {
        if (txtTimeRemaining == null) return;

        float time = Mathf.Max(0f, remainingTime);
        txtTimeRemaining.text = Mathf.CeilToInt(time)+"s";
    }
}
