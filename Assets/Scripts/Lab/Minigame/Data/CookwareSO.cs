using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조리 도구 데이터 (예: 도마, 후라이팬, 냄비, 웍, 튀김솥, 접시).
/// 페이즈 하나당 도구 1개만 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "CookwareSO", menuName = "Game Data/Lab/Cookware")]
public class CookwareSO : ScriptableObject
{
    [SerializeField] private string _cookwareName;
    [SerializeField] private GameObject _cookwarePrefab;
    [SerializeField] private List<CookingActionType> _supportedActions = new List<CookingActionType>();

    public string CookwareName => _cookwareName;
    public GameObject CookwarePrefab => _cookwarePrefab;
    public IReadOnlyList<CookingActionType> SupportedActions => _supportedActions;

    public bool Supports(CookingActionType actionType)
    {
        return _supportedActions != null && _supportedActions.Contains(actionType);
    }
}
