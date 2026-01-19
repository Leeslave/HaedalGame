using UnityEngine;
[CreateAssetMenu(menuName = "Game/Economy/CurrencyConfig")]
public class CurrencyConfig : ScriptableObject
{
    public CurrencyId id;
    public CurrencyType type;
    public string displayName;
    public Sprite icon;
    public int defaultValue;
    public int maxValue;        // -1이면 제한 없음
    public bool canBeNegative;  // 빚 같은 것 허용 여부
}
