
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;//mới

public class CookingEffectController : MonoBehaviour
{
    [Header("Cooked Dish Display")]
    [SerializeField] private Image cookedDishDisplayImage; // Image component để hiển thị món ăn đã nấu

    private DishData currentDishData; // Dữ liệu món ăn hiện tại


    public void ShowCookedDishOnPlate(DishData dishData)
    {
        if (cookedDishDisplayImage == null)
        {
            Debug.LogWarning("Cooked Dish Display Image chưa được gán!");
            return;
        }

        if (dishData == null || dishData.dishSprite == null)
        {
            Debug.LogWarning("Món hiện tại chưa có sprite!");
            return;
        }

        cookedDishDisplayImage.sprite = dishData.dishSprite;
        cookedDishDisplayImage.gameObject.SetActive(true);



        // StartCoroutine(PlayCookSmoke());
    }
    public IEnumerator HideCookedDishCoroutine()
    {
        // ví dụ: đợi 2 giây trước khi ẩn
            yield return new WaitForSeconds(5f); // Giữ hình ảnh món ăn trên đĩa trong 5 giây trước khi ẩn); 
            if (cookedDishDisplayImage != null)
            {
                cookedDishDisplayImage.sprite = null;
                cookedDishDisplayImage.gameObject.SetActive(false);
            }
    }
    public  void HideCookedDish()
    {
        StartCoroutine(HideCookedDishCoroutine());
    }
}