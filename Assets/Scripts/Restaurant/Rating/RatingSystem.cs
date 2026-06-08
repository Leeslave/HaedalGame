using UnityEngine;

public class RatingSystem : MonoBehaviour
{
    public static RatingSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public float PersonalRating(int recipeGrade, int expectation)
    {
        float result = 0;
        switch(recipeGrade - expectation)
        {
            case 2: result = 5.0f; break;
            case 1: result = 4.5f; break;
            case 0: result = 4.0f; break;
            case -1: result = 3.0f; break;
            case -2: result = 2.0f; break;
            case -3: result = 1.0f; break;
        }
        return result;

    }
}
