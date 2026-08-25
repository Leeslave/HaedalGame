using UnityEngine;

public class RatingSystem : MonoBehaviour
{
    public float PersonalRating(int recipeGrade, int expectation)
    {
        float result = 0;
        int diff = Mathf.Clamp(recipeGrade - expectation, -3, 2);
        switch(diff)
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

    // 요리 등급 문자(F~S)를 정수 스케일로 변환 (F=0, E=1, D=2, C=3, B=4, A=5, S=6)
    public static int GradeToInt(string grade)
    {
        switch (grade)
        {
            case "F": return 0;
            case "E": return 1;
            case "D": return 2;
            case "C": return 3;
            case "B": return 4;
            case "A": return 5;
            case "S": return 6;
            default:
                Debug.LogWarning($"GradeToInt: 알 수 없는 등급 문자 '{grade}'입니다. 0으로 처리합니다.");
                return 0;
        }
    }
}
