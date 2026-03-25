using TMPro;
using UnityEngine;
using UnityEngine.UI; // TextMeshPro를 쓰신다면 TMPro 네임스페이스를 추가하세요!

public enum CandidateSlotState
{
    Hidden,     // 슬롯 자체 안 보임
    Mystery,    // 미스테리 이미지
    Revealed,   // 정보 공개됨
    Hired       // 이미 고용 완료(선택사항)
}

public class CondinateSlot : MonoBehaviour
{
    public GameObject emptyImageObj;
    public GameObject condinateInfoObj;

    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI servingText;
    public TextMeshProUGUI cookingText;
    public TextMeshProUGUI handyText;
    public TextMeshProUGUI hpText;

    [SerializeField] private Button _button;

    private CandidateSlotState _state = CandidateSlotState.Hidden;
    private PartTimerData _data;
    private int _slotIndex;

    public int SlotIndex => _slotIndex;
    public PartTimerData Data => _data;
    public CandidateSlotState State => _state;

    private void Awake()
    {
        _button.onClick.AddListener(HandleClick);

    }

    public void SetEmpty()
    {
        _data = null;
        _state = CandidateSlotState.Hidden;
        gameObject.SetActive(false);
        _button.interactable = true;
    }

    public void BindMystery(PartTimerData data, int slotIndex)
    {
        _data = data;
        _slotIndex = slotIndex;
        _state = CandidateSlotState.Mystery;

        gameObject.SetActive(true);

        emptyImageObj.SetActive(true);
        condinateInfoObj.SetActive(false);
    }

    private void HandleClick()
    {
        switch (_state)
        {
            case CandidateSlotState.Mystery:
                Reveal();
                break;

            case CandidateSlotState.Revealed:
                ScoutManager.Instance.RequestHire(this);
                break;
        }
    }

    private void Reveal()
    {
        if (_data == null)
            return;

        _state = CandidateSlotState.Revealed;

        emptyImageObj.SetActive(false);
        condinateInfoObj.SetActive(true);

        gradeText.text = _data.level;
        nameText.text = _data.serverName;
        servingText.text = _data.status.serving.ToString();
        cookingText.text = _data.status.cooking.ToString();
        handyText.text = _data.status.handy.ToString();
        hpText.text = _data.status.hp.ToString();
    }

    public void MarkHired()
    {
        _state = CandidateSlotState.Hired;
        _button.interactable = false;
    }
}