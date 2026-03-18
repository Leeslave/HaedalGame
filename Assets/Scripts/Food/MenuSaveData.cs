using System;
using System.Collections.Generic;

[Serializable]
public class MenuSaveData
{
    public List<int> unlockedFoodIds = new List<int>(); // 해금된 음식 들
    public List<int> dailyFoodIds = new List<int>();
}