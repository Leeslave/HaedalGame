using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>초상화를 화면 어느 쪽에 세울지.</summary>
public enum DialoguePortraitSide
{
    None,   // 초상화 없음
    Left,   // 왼쪽
    Right,  // 오른쪽
    Center  // 가운데
}

/// <summary>배경이 바뀔 때의 전환 방식.</summary>
public enum DialogueTransition
{
    Instant, // 즉시 교체
    Fade     // 서서히 교체
}

/// <summary>
/// 배경 이미지가 없을 때 대신 그려줄 기본 배경.
/// 스프라이트를 아직 준비하지 못한 대사도 그림 없이 바로 돌려볼 수 있게 해준다.
/// </summary>
public enum DialogueBackgroundStyle
{
    None,        // 배경을 그리지 않는다 (뒤에 있는 게임 화면이 그대로 보인다)
    Dim,         // 게임 화면을 어둡게만 덮는다
    SeasideDay,  // 한낮 바다 (하늘빛 → 모래빛)
    SeasideDusk, // 노을 진 바다 (주황 → 남색)
    SeasideNight,// 밤바다 (짙은 남색 → 보랏빛)
    WarmInterior,// 식당 실내 (따뜻한 나무빛)
    DeepSea      // 깊은 바닷속 (청록 → 짙은 파랑)
}

/// <summary>
/// 대사 한 줄(노드)의 데이터.
/// 모든 필드는 선택 사항이라 JSON에서 빼면 기본값이 그대로 쓰인다.
/// 가장 단순한 노드는 { "text": "안녕" } 하나로 끝난다.
/// </summary>
[Serializable]
public class DialogueNodeData
{
    [Tooltip("이 노드의 이름표. next나 choices에서 가리킬 때만 필요하다. 순서대로만 진행할 거면 비워도 된다.")]
    public string id = "";

    [Tooltip("말하는 사람. characters에 등록한 id를 쓰면 이름과 색을 가져오고, 없으면 적은 글자를 그대로 이름으로 쓴다.")]
    public string speaker = "";

    [Tooltip("이 줄에서만 이름표를 다르게 보여주고 싶을 때. (예: '???')")]
    public string name = "";

    [TextArea(2, 5)]
    [Tooltip("실제로 출력할 대사. TMP 리치 텍스트 태그를 그대로 쓸 수 있다.")]
    public string text = "";

    [Tooltip("이 줄에서 띄울 초상화 키. 비우면 캐릭터의 기본 초상화를 쓴다. 'none'이라고 적으면 초상화를 지운다.")]
    public string portrait = "";

    [Tooltip("초상화를 세울 위치. None / Left / Right / Center")]
    public string side = "";

    [Tooltip("이 줄에서 바꿀 배경 이미지 키. 비우면 이전 배경을 유지한다.")]
    public string background = "";

    [Tooltip("배경 이미지가 없을 때 쓸 기본 배경 스타일.")]
    public string backgroundStyle = "";

    [Tooltip("배경 전환 방식. Instant / Fade")]
    public string transition = "";

    [Tooltip("초당 출력할 글자 수. 0이면 즉시 전부 표시, 음수면 스크립트 기본값을 따른다.")]
    public float typeSpeed = -1f;

    [Tooltip("0보다 크면 클릭 없이 이 시간(초) 뒤에 자동으로 다음 줄로 넘어간다.")]
    public float autoAdvance = 0f;

    [Tooltip("켜면 전체 건너뛰기가 이 줄에서 멈춘다. 놓치면 안 되는 대사에 쓴다.")]
    public bool blockSkip = false;

    [Tooltip("이 줄이 시작될 때 게임 쪽으로 흘려보낼 신호. DialogueManager.OnEvent로 받는다.")]
    public string eventKey = "";

    [Tooltip("이 줄이 시작될 때 켜둘 플래그 이름. 뒤쪽 선택지의 requireFlag에서 조건으로 쓴다.")]
    public string setFlag = "";

    [Tooltip("다음에 재생할 노드 id. 비우면 배열의 바로 다음 노드로 넘어가고, 'end'라고 적으면 대사를 끝낸다.")]
    public string next = "";

    [Tooltip("선택지. 비워두면 그냥 다음 줄로 넘어가는 평범한 대사가 된다.")]
    public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();

    public bool HasChoices => choices != null && choices.Count > 0;
}

/// <summary>선택지 하나. 고르면 next로 적힌 노드로 건너뛴다.</summary>
[Serializable]
public class DialogueChoiceData
{
    [Tooltip("버튼에 보여줄 글자.")]
    public string text = "";

    [Tooltip("고르면 이동할 노드 id. 비우면 그냥 다음 노드로 넘어간다.")]
    public string next = "";

    [Tooltip("고른 순간 게임 쪽으로 흘려보낼 신호.")]
    public string eventKey = "";

    [Tooltip("고른 순간 켜둘 플래그 이름.")]
    public string setFlag = "";

    [Tooltip("이 플래그가 켜져 있을 때만 선택지를 보여준다. 앞에 !를 붙이면 반대로 꺼져 있을 때만 보여준다.")]
    public string requireFlag = "";
}

/// <summary>등장인물 한 명. 매번 이름과 색을 적지 않아도 되게 미리 묶어둔다.</summary>
[Serializable]
public class DialogueCharacterData
{
    [Tooltip("노드의 speaker에서 가리킬 id.")]
    public string id = "";

    [Tooltip("화면에 보여줄 이름. 비우면 id를 그대로 쓴다.")]
    public string name = "";

    [Tooltip("이름표 색. #RRGGBB 형식.")]
    public string color = "";

    [Tooltip("기본 초상화 키. 노드에서 portrait를 비우면 이게 쓰인다.")]
    public string portrait = "";

    [Tooltip("기본으로 설 위치. None / Left / Right / Center")]
    public string side = "";
}

/// <summary>
/// 대사 스크립트 한 편. JSON 파일 하나가 이 구조에 그대로 대응된다.
/// JsonUtility로 읽기 때문에 모르는 필드는 조용히 무시되고, 빠진 필드는 기본값이 쓰인다.
/// 그래서 필요한 것만 적어도 되고, 나중에 필드를 늘려도 예전 파일이 그대로 돌아간다.
/// </summary>
[Serializable]
public class DialogueScriptData
{
    [Tooltip("스크립트 구분용 아이디. playOnce로 '한 번만 재생'을 기록할 때 열쇠로 쓴다.")]
    public string id = "";

    [Tooltip("스크립트 전체의 기본 배경 이미지 키.")]
    public string background = "";

    [Tooltip("배경 이미지를 못 찾았을 때 쓸 기본 배경 스타일.")]
    public string backgroundStyle = "";

    [Tooltip("건너뛰기를 허용할지. 기본은 꺼져 있어서 건너뛰기 버튼이 아예 뜨지 않는다.")]
    public bool canSkip = false;

    [Tooltip("대사 중에 게임 시간을 멈출지.")]
    public bool pauseGameTime = true;

    [Tooltip("켜면 한 번 본 대사는 다시 재생하지 않는다. (id 기준으로 기록)")]
    public bool playOnce = false;

    [Tooltip("초당 출력할 글자 수. 0이면 타자 효과 없이 즉시 표시한다.")]
    public float typeSpeed = 30f;

    [Tooltip("글자가 다 나오기 전에 클릭하면 남은 글자를 즉시 채울지. (건너뛰기와는 별개다)")]
    public bool allowFastForward = true;

    [Tooltip("등장인물 목록. 안 쓰면 비워도 된다.")]
    public List<DialogueCharacterData> characters = new List<DialogueCharacterData>();

    [Tooltip("대사 줄 목록. 위에서부터 순서대로 재생된다.")]
    public List<DialogueNodeData> nodes = new List<DialogueNodeData>();

    public int NodeCount => nodes != null ? nodes.Count : 0;

    /// <summary>id로 노드 번호를 찾는다. 못 찾으면 -1.</summary>
    public int IndexOfNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || nodes == null)
            return -1;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].id == nodeId)
                return i;
        }

        return -1;
    }

    /// <summary>id로 등장인물을 찾는다. 못 찾으면 null.</summary>
    public DialogueCharacterData FindCharacter(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || characters == null)
            return null;

        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] != null && characters[i].id == characterId)
                return characters[i];
        }

        return null;
    }
}

/// <summary>
/// JSON에 적힌 글자를 실제 값으로 바꿔주는 도우미.
/// 오타나 대소문자 차이로 대사가 통째로 안 나오는 일이 없도록 전부 관대하게 읽고,
/// 못 알아들으면 기본값으로 되돌린다.
/// </summary>
public static class DialogueParse
{
    /// <summary>"left", "Left", "LEFT"를 모두 같은 값으로 읽는다. 비었거나 모르는 값이면 fallback.</summary>
    public static T ParseEnum<T>(string raw, T fallback) where T : struct
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        if (System.Enum.TryParse(raw.Trim(), true, out T parsed))
            return parsed;

        Debug.LogWarning($"[Dialogue] '{raw}'는 {typeof(T).Name}에 없는 값이라 {fallback}으로 대신한다.");
        return fallback;
    }

    /// <summary>
    /// 열거형으로 읽을 수 있는 글자인지 조용히 확인한다. 비어 있으면 기본값이 쓰이므로 정상으로 본다.
    /// 검사 도구에서 오타를 잡을 때 쓴다.
    /// </summary>
    public static bool IsValidEnum<T>(string raw) where T : struct
    {
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        return System.Enum.TryParse(raw.Trim(), true, out T _);
    }

    /// <summary>"#RRGGBB" 같은 글자를 색으로 바꾼다. 비었거나 이상하면 fallback.</summary>
    public static Color ParseColor(string raw, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        string trimmed = raw.Trim();

        if (!trimmed.StartsWith("#"))
            trimmed = "#" + trimmed;

        if (ColorUtility.TryParseHtmlString(trimmed, out Color parsed))
            return parsed;

        Debug.LogWarning($"[Dialogue] '{raw}'는 색으로 읽을 수 없어 기본색을 쓴다.");
        return fallback;
    }

    /// <summary>초상화를 일부러 지우라는 표시인지. ("none", "off", "-"를 지우기로 본다)</summary>
    public static bool IsClearKeyword(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string trimmed = raw.Trim().ToLowerInvariant();
        return trimmed == "none" || trimmed == "off" || trimmed == "-";
    }

    /// <summary>대사를 여기서 끝내라는 표시인지.</summary>
    public static bool IsEndKeyword(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string trimmed = raw.Trim().ToLowerInvariant();
        return trimmed == "end" || trimmed == "exit" || trimmed == "finish";
    }
}
