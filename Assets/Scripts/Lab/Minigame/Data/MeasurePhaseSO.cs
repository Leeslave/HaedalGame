using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 계량할 재료 하나. "간장 2/3컵"처럼 목표량까지 따르는 대상.
/// 계량 대상은 고정이 아니므로(간장/녹차/물 등) 이름·색·스프라이트를 데이터로 지정한다.
/// </summary>
[Serializable]
public class MeasureTarget
{
    [SerializeField] private string _displayName;         // "간장", "녹차", "물"
    [SerializeField] private string _targetLabel;         // "2/3 컵" (화면 표시용)
    [SerializeField, Range(0f, 1f)] private float _targetAmount = 0.667f; // 목표 채움 비율 0~1
    [SerializeField, Range(0.01f, 0.5f)] private float _tolerance = 0.05f; // 완벽 판정 허용오차

    [Header("비주얼")]
    [SerializeField] private Color _liquidColor = new Color(0.4f, 0.25f, 0.1f, 1f); // 채워지는 액체 색
    [SerializeField] private Sprite _liquidSprite;        // 있으면 색 대신 이 스프라이트로 채움
    [SerializeField] private Sprite _sourceSprite;        // 따르는 병/주전자 (선택)

    [SerializeField] private int _ingredientId = -1;      // 있으면 DB에서 아이콘/이름 보조

    public string DisplayName => _displayName;
    public string TargetLabel => _targetLabel;
    public float TargetAmount => _targetAmount;
    public float Tolerance => _tolerance;
    public Color LiquidColor => _liquidColor;
    public Sprite LiquidSprite => _liquidSprite;
    public Sprite SourceSprite => _sourceSprite;
    public int IngredientId => _ingredientId;
}

/// <summary>
/// 계량 페이즈. 계량컵에 액체를 꾹 눌러 따르다가 목표량에서 손을 뗀다.
/// 여러 재료(간장 2/3컵, 물 1/2컵...)를 순서대로 계량하고 평균 점수로 완료한다.
/// </summary>
[CreateAssetMenu(fileName = "MeasurePhaseSO", menuName = "Game Data/Lab/Phase/Measure")]
public class MeasurePhaseSO : PhaseSO
{
    [Header("Measure")]
    [SerializeField] private List<MeasureTarget> _measures = new List<MeasureTarget>();

    // 채우는 속도 (초당 정규화 진행량. 예: 0.4 = 2.5초에 컵 가득)
    [SerializeField] private float _fillSpeed = 0.4f;

    public IReadOnlyList<MeasureTarget> Measures => _measures;
    public float FillSpeed => _fillSpeed;

    public override CookingActionType ActionType => CookingActionType.Measure;
}
