using UnityEngine;

[CreateAssetMenu(fileName = "food Data", menuName ="Game/Food Data")]
public class FoodData : ScriptableObject
{
    public int id; // 음식의 넘버링
    public string foodName; // 음식의 이름
    public float cookTime; // 요리하는데 걸리는 시간
    public int price; // 음식 가격

    public CookingDiff cookingDiff;
    public CookingType cookingType;
    public string imageName;
}