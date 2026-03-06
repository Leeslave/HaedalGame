public class OrderFoodData
{
    public string foodName;
    public CookingDiff cookingDiff;
    public string imageName;

    public OrderFoodData(string name, CookingDiff diff, string image)
    {
        foodName = name;
        cookingDiff = diff;
        imageName = image;
    }
}