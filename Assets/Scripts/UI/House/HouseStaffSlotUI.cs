using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 집 UI "식당 알바 현황"의 슬롯 1칸.
/// 세 가지 상태를 스위칭한다: 배치된 알바 / 빈칸(+) / 잠금(자물쇠 + 요구 식당 레벨).
/// 빈칸이나 알바 칸을 누르면 알바 관리 화면(PartTimePopup)으로 이동한다.
/// </summary>
public class HouseStaffSlotUI : MonoBehaviour
{
    [Header("상태 루트")]
    [SerializeField] private GameObject _filledRoot; // 알바 배치됨
    [SerializeField] private GameObject _emptyRoot;  // 빈칸 (+)
    [SerializeField] private GameObject _lockRoot;   // 잠금

    [Header("Filled")]
    [SerializeField] private Image _portraitImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText; // "Lv.3"

    [Header("Lock")]
    [SerializeField] private TMP_Text _lockText;  // "식당 레벨 7"

    [SerializeField] private Button _button;

    private Action _onClick;

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    /// <summary>알바가 배치된 상태로 표시한다.</summary>
    public void BindFilled(PartTimerData data, Sprite portrait, Action onClick)
    {
        SetState(filled: true, empty: false, locked: false);
        _onClick = onClick;

        if (_portraitImage != null)
        {
            _portraitImage.sprite = portrait;
            _portraitImage.enabled = portrait != null;
        }

        if (_nameText != null)
            _nameText.text = data != null ? data.serverName : string.Empty;

        // PartTimerData.level은 등급 문자열("A", "B"...). 목업의 "Lv.3" 자리에 그대로 표시한다.
        if (_levelText != null)
            _levelText.text = data != null ? data.level : string.Empty;

        if (_button != null)
            _button.interactable = true;
    }

    /// <summary>빈 슬롯(+) 상태로 표시한다.</summary>
    public void BindEmpty(Action onClick)
    {
        SetState(filled: false, empty: true, locked: false);
        _onClick = onClick;

        if (_button != null)
            _button.interactable = true;
    }

    /// <summary>잠금 상태로 표시한다. 요구 식당 레벨을 함께 보여준다.</summary>
    public void BindLocked(int requiredLevel)
    {
        SetState(filled: false, empty: false, locked: true);
        _onClick = null;

        if (_lockText != null)
            _lockText.text = $"식당 레벨 {requiredLevel}";

        if (_button != null)
            _button.interactable = false;
    }

    private void SetState(bool filled, bool empty, bool locked)
    {
        if (_filledRoot != null)
            _filledRoot.SetActive(filled);

        if (_emptyRoot != null)
            _emptyRoot.SetActive(empty);

        if (_lockRoot != null)
            _lockRoot.SetActive(locked);
    }

    private void HandleClick()
    {
        _onClick?.Invoke();
    }
}
