using UnityEngine;

[CreateAssetMenu(fileName = "FoodData", menuName = "Game Data/Food/Food Data")]
public class FoodData : ScriptableObject
{
    public int id;                  // 음식의 넘버링
    public string foodName;         // 음식의 이름
    public float cookTime;          // 요리하는데 걸리는 시간
    public int price;               // 음식 가격
    public CookingType cookingType; // 어떤 방식으로 요리하는 지?
    public Sprite image;        // 어떤 이미지?
}