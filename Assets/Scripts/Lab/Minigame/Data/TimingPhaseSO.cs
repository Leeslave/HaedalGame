using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타이밍 게이지 기반 페이즈 공통 데이터 (Grill/Boil/StirFry/Fry).
/// 게이지는 좌→우 단방향 진행, 5단계 판정(설익음1/약간익음3/익음5/살짝오버쿡3/오버쿡1).
/// 게이지가 여러 번(gaugeCount) 등장하면 각 게이지 점수의 평균을 사용한다.
/// </summary>
public abstract class TimingPhaseSO : PhaseSO
{
    [Header("Timing Gauge")]
    // 게이지 진행 속도 (초당 정규화 진행량. 예: 0.5 = 2초에 끝까지)
    [SerializeField] private float _gaugeSpeed = 0.5f;

    // 한 페이즈에서 게이지가 등장하는 횟수 (예: 굽기 앞면/뒷면 = 2)
    [SerializeField] private int _gaugeCount = 1;

    [Header("Cook Visual")]
    // 게이지 진행에 따라 교체되는 익힘 단계 이미지 (예: 생연어 → 노릇 → 탄 연어).
    // 비워두면 이미지 교체 없이 게이지만 표시된다.
    [SerializeField] private List<Sprite> _cookStageSprites = new List<Sprite>();

    public float GaugeSpeed => _gaugeSpeed;
    public int GaugeCount => _gaugeCount;
    public IReadOnlyList<Sprite> CookStageSprites => _cookStageSprites;
}
