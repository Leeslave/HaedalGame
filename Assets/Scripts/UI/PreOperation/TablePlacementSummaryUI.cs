using TMPro;
using UnityEngine;

// 준비 화면의 "식당 배치" 박스에 배치된 테이블 개수를 요약해서 보여준다.
// 실제 배치 모습은 별도 미리보기 카메라(RenderTexture)가 담당한다.
public class TablePlacementSummaryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _countText;

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_countText == null)
            return;

        int count = TableManager.Instance != null ? TableManager.Instance.GetPlacedTableCount() : 0;
        _countText.text = $"테이블 {count}개 배치됨";
    }
}
