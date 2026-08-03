using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 집 UI "알바 스카우트" 패널.
/// 등급별 스카우트 카드를 요약 표시하고, 클릭하면 기존 가챠(스카우트) 화면으로 이동한다.
/// </summary>
public class HouseScoutPanelUI : MonoBehaviour
{
    [SerializeField] private List<ScoutData> _scoutDatas = new List<ScoutData>();

    [Header("List")]
    [SerializeField] private Transform _cardRoot;
    [SerializeField] private HouseScoutCardUI _cardPrefab;

    [Header("가챠 팝업")]
    [SerializeField] private GachaPopupUI _gachaPopup;

    private readonly List<HouseScoutCardUI> _spawnedCards = new List<HouseScoutCardUI>();

    public void Refresh()
    {
        ClearCards();

        if (_cardPrefab == null || _cardRoot == null)
            return;

        for (int i = 0; i < _scoutDatas.Count; i++)
        {
            if (_scoutDatas[i] == null)
                continue;

            HouseScoutCardUI card = Instantiate(_cardPrefab, _cardRoot);
            card.Bind(_scoutDatas[i], OpenGachaPopup);
            _spawnedCards.Add(card);
        }
    }

    /// <summary>기존 가챠(스카우트) UI를 연다.</summary>
    private void OpenGachaPopup()
    {
        if (_gachaPopup != null)
            _gachaPopup.Open();
        else
            Debug.LogWarning("[House] GachaPopupUI 미할당 - 스카우트 화면을 열 수 없습니다", this);
    }

    private void ClearCards()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            if (_spawnedCards[i] != null)
                Destroy(_spawnedCards[i].gameObject);
        }

        _spawnedCards.Clear();
    }
}
