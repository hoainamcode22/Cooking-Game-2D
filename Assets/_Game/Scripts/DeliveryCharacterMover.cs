using System.Collections;
using UnityEngine;

public class DeliveryCharacterMover : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject deliveryRoot; // object delivery

    [Header("UI References")]
    [SerializeField] private RectTransform character;      // man_delivery
    [SerializeField] private RectTransform cookingPoint;   // cooking
    [SerializeField] private RectTransform warehousePoint; // warehouse

    [Header("Move Settings")]
    [SerializeField] private float moveDuration = 5f;
    [SerializeField] private bool hideAfterMove = false;

    private Coroutine moveRoutine;

    public void ShowDeliveryOnly()
    {
        if (deliveryRoot != null)
        {
            deliveryRoot.SetActive(true);
        }
    }

    public void HideDelivery()
    {
        if (deliveryRoot != null)
        {
            deliveryRoot.SetActive(false);
        }
    }

    public void MoveFromCookingToWarehouse()
    {

        if (deliveryRoot != null)
        {
            deliveryRoot.SetActive(true);
        }

        if (character == null || cookingPoint == null || warehousePoint == null)
        {
            Debug.LogWarning("[DeliveryCharacterMover] Chưa kéo đủ character / cookingPoint / warehousePoint.");
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {

        character.gameObject.SetActive(true);

        Vector3 startPos = cookingPoint.position;
        Vector3 endPos = warehousePoint.position;

        character.position = startPos;

        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = timer / moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            character.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        character.position = endPos;

        if (hideAfterMove && deliveryRoot != null)
        {
            deliveryRoot.SetActive(false);
        }

        moveRoutine = null;

    }
}
