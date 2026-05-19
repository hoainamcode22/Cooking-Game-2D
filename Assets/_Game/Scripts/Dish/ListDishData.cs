using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cooking/Data/List Dish Data", fileName = "ListDishData")]
public class ListDishData : ScriptableObject
{
    [Header("All Dishes")]
    public List<DishData> allDishes = new List<DishData>();
}