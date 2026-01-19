using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class DialogueEntry
{
    public int day;          // 일자 -> 캐릭터 Json파일에 일자별로 대사를 넣어둘 예정
    public string[] lines;   // 일자의 대사들을 배열로 불러옴
}

[System.Serializable]
public class DialogueData
{
    public DialogueEntry[] dialogues;
}


public class DialogTrigger : MonoBehaviour
{
    [Header("Speaker Setting")]
    [SerializeField] protected string character; // 캐릭터 명
    [SerializeField] protected string characterRank; // 캐릭터 직책
    [SerializeField] protected TextAsset dialogueJson; // 불러올 Json 파일
    protected DialogueEntry currentDialogue;        // 오늘 진행할 대사 
    private int currentDay;


    protected virtual void Awake()
    {
        LoadDialogForToday();
    }

    protected void LoadDialogForToday()
    {
        if (dialogueJson == null) { Debug.LogWarning("오늘의 dialogJson이 비어있습니다."); return; } // 대사 파일 자체가 없는 파일이면 예외처리

        DialogueData data = JsonUtility.FromJson<DialogueData>(dialogueJson.text);

        if (data == null || data.dialogues == null || data.dialogues.Length == 0)
        {
            Debug.LogWarning($"{name} : JSON 파싱 실패 또는 dialogues 가 비어있습니다.");
            return;
        }

        // 데이터 파싱이 전부 성공적으로 완료하였다면,
        currentDay = 1;
        if (GameManager.Instance != null) { currentDay = GameManager.Instance.currentDay; }
        else { Debug.LogWarning("GameManager.Instance가 없습니다. 값은 1이 들어갑니다."); }

        currentDialogue = null;


        foreach (var entry in data.dialogues)
        {
            if (entry.day == currentDay) { currentDialogue = entry; return; }
        }

    }
}
