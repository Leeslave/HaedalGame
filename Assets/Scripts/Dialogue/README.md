# 대사(Dialogue) 시스템

JSON으로 대사를 적고, 배경·초상화·선택지까지 붙일 수 있는 대화 시스템이다.
튜토리얼 시스템과 같은 구조(매니저 + 뷰 + 에디터 생성 도구)로 만들어져 있다.

## 1. 설치

Hierarchy에서 마우스 오른쪽 → **UI > Dialogue System** 을 누르면
`DialogueSystem`(매니저)과 `DialogueCanvas`(화면)가 연결된 채로 만들어진다.
색이나 스프라이트만 취향대로 바꾸고 프리팹으로 저장해서 쓰면 된다.

## 2. 재생 방법

```csharp
// Resources 아래 JSON 경로로 (확장자 없이)
DialogueManager.Instance.PlayFromResources("Dialogue/sample_dialogue");

// TextAsset을 직접
DialogueManager.Instance.Play(myJsonAsset);

// DialogueManager의 Scripts 목록에 등록해뒀다면 스크립트 id로
DialogueManager.Instance.Play("sample_dialogue");

// 끝난 뒤에 할 일을 넘길 수도 있다
DialogueManager.Instance.PlayFromResources("Dialogue/intro", () => 게임시작());
```

코드를 건드리기 싫으면 아무 오브젝트에 **DialogueTrigger**를 붙이고
JSON 파일이나 Resources 경로만 적어두면 된다.

## 3. JSON 형식

전부 **선택 사항**이다. 안 적은 값은 기본값이 쓰이고, 모르는 필드는 조용히 무시된다.
그래서 나중에 필드를 늘려도 예전 파일이 그대로 돌아간다.

가장 짧게 쓰면 이 정도로 끝난다.

```json
{ "nodes": [ { "text": "안녕!" }, { "text": "잘 가!" } ] }
```

### 3-1. 스크립트 (최상단)

| 필드 | 기본값 | 설명 |
|---|---|---|
| `id` | `""` | 스크립트 구분용. `playOnce` 기록의 열쇠로 쓰인다 |
| `background` | `""` | 배경 **이미지 키** |
| `backgroundStyle` | `""` | 이미지가 없을 때 쓸 **기본 배경 스타일** |
| `canSkip` | `false` | **건너뛰기 허용 여부. 기본은 꺼져 있어 버튼이 아예 안 뜬다** |
| `pauseGameTime` | `true` | 대사 중 게임 시간을 멈출지 |
| `playOnce` | `false` | 한 번 본 대사는 다시 재생하지 않는다 |
| `typeSpeed` | `30` | 초당 글자 수. `0`이면 즉시 표시 |
| `allowFastForward` | `true` | 글자 나오는 중 클릭하면 즉시 다 채울지 |
| `characters` | `[]` | 등장인물 목록 |
| `nodes` | `[]` | 대사 줄 목록 |

### 3-2. 등장인물 `characters[]`

매 줄마다 이름과 색을 적지 않아도 되게 묶어두는 것뿐이라, 안 쓰면 생략해도 된다.

| 필드 | 설명 |
|---|---|
| `id` | 노드의 `speaker`에서 가리킬 이름 |
| `name` | 화면에 보여줄 이름 (비우면 `id`) |
| `color` | 이름표 색. `#7FD4E8` 형식 |
| `portrait` | 기본 초상화 키 |
| `side` | 기본 위치. `Left` / `Right` / `Center` / `None` |

### 3-3. 대사 줄 `nodes[]`

| 필드 | 기본값 | 설명 |
|---|---|---|
| `id` | `""` | `next`나 선택지에서 가리킬 때만 필요 |
| `speaker` | `""` | `characters`의 `id`. 없으면 적은 글자를 그대로 이름으로 쓴다 |
| `name` | `""` | 이 줄에서만 이름을 다르게 (예: `"???"`) |
| `text` | `""` | 실제 대사. TMP 리치 텍스트 태그 사용 가능 |
| `portrait` | `""` | 초상화 키. `"none"`이면 지운다 |
| `side` | `Left` | 초상화 위치 |
| `background` | `""` | 배경 이미지 키 (비우면 이전 배경 유지) |
| `backgroundStyle` | `""` | 이미지가 없을 때 쓸 스타일 |
| `transition` | `Fade` | `Fade` / `Instant` |
| `typeSpeed` | `-1` | 음수면 스크립트 값을 따름. `0`이면 즉시 |
| `autoAdvance` | `0` | `0`보다 크면 클릭 없이 그 초 뒤 자동 진행 |
| `blockSkip` | `false` | **건너뛰기가 이 줄에서 멈춘다** |
| `eventKey` | `""` | 이 줄에서 게임 쪽으로 보낼 신호 |
| `setFlag` | `""` | 이 줄에서 켜둘 플래그 |
| `next` | `""` | 다음 노드 `id`. 비우면 바로 아래 줄, `"end"`면 종료 |

> `next`를 찾는 순서는 **① 같은 `id`의 노드 → ② 종료 예약어(`end`/`exit`/`finish`) → ③ 바로 아래 줄**이다.
> 노드 `id`가 예약어보다 우선하므로 `finish`라는 이름의 노드를 만들어도 대사가 엉뚱하게 끝나지 않는다.
> 다만 읽는 사람이 헷갈리니 그런 이름은 피하는 편이 낫다. (F9 점검이 알려준다)
| `choices` | `[]` | 선택지 |

### 3-4. 선택지 `choices[]`

| 필드 | 설명 |
|---|---|
| `text` | 버튼에 보여줄 글자 |
| `next` | 고르면 이동할 노드 `id` |
| `eventKey` | 고른 순간 보낼 신호 |
| `setFlag` | 고른 순간 켤 플래그 |
| `requireFlag` | 이 플래그가 켜져 있을 때만 표시. `!이름`이면 반대 |

## 4. 배경

배경은 **사진(스프라이트)** 을 쓸 수 있고, 없으면 **코드로 그린 기본 배경**이 대신 나온다.

### 사진을 쓰려면

`background`에 키를 적고, 그 키를 실제 스프라이트에 이어주면 된다. 찾는 순서는

1. `DialogueManager`의 **Sprite Table**에 등록한 키 (인스펙터에서 직접 연결)
2. `Resources` 아래 경로 — 기본 검색 폴더는 `Dialogue/`, 그다음 최상단
3. 스프라이트 시트 안의 낱장 이름

`Assets/Sprites/...`에 있는 그림은 `Resources` 폴더가 아니라서 자동으로는 못 찾는다.
**Sprite Table**에 키와 함께 끌어다 놓는 쪽이 제일 간단하다.

화면에 맞추는 방식은 `DialogueView`의 **Background Fit**에서 고른다.

- `Cover` (기본): 비율을 지키며 화면을 덮는다. 넘치는 부분은 잘린다 — 사진 배경용
- `Stretch`: 화면에 꽉 차게 늘린다
- `Contain`: 전부 보이게 넣는다 (여백 생김)

### 사진이 없으면

`backgroundStyle`에 아래 중 하나를 적으면 해달 식당의 바닷가 분위기에 맞춘
그러데이션 배경이 그려진다. **이미지 키를 못 찾았을 때도 자동으로 여기로 넘어온다.**

| 스타일 | 분위기 |
|---|---|
| `SeasideDay` | 한낮 바다 (하늘빛 → 모래빛) |
| `SeasideDusk` | 노을 진 바다 (주황 → 남색) |
| `SeasideNight` | 밤바다 (짙은 남색 → 보랏빛) |
| `WarmInterior` | 식당 실내 (따뜻한 나무빛) |
| `DeepSea` | 깊은 바닷속 (청록 → 짙은 파랑) |
| `Dim` | 게임 화면을 어둡게만 덮는다 |
| `None` | 배경을 안 그린다 (뒤 게임 화면이 그대로 보인다) |

아무것도 안 적으면 `DialogueManager`의 **Default Background Style**
(기본 `SeasideDusk`)이 쓰인다.

## 5. 건너뛰기

**기본은 꺼져 있다.** `canSkip`을 켜지 않으면 건너뛰기 버튼이 아예 뜨지 않고,
`Skip()`을 호출해도 무시된다.

```json
{ "canSkip": true, "nodes": [ ... ] }
```

켜두면 오른쪽 위에 `건너뛰기 ▶▶` 버튼이 생긴다. 누르면 기다리지 않고 줄을 넘기다가

- **선택지가 있는 줄**
- **`blockSkip: true`인 줄**

을 만나면 거기서 멈추고 다시 보통 속도로 돌아온다. 꼭 봐야 하는 대사는 `blockSkip`으로 지킨다.

> 글자가 나오는 중에 클릭해서 즉시 채우는 건 건너뛰기와 별개다.
> 그건 `allowFastForward`(기본 켜짐)로 따로 조절한다.

버튼 말고 코드로 부르려면:

```csharp
DialogueManager.Instance.Skip();   // canSkip이 켜진 대사에서만 동작
DialogueManager.Instance.Stop();   // canSkip과 무관하게 즉시 종료 (씬 전환 등)
```

## 6. 게임 로직 붙이기

```csharp
DialogueManager.Instance.OnEvent += key =>
{
    if (key == "sample.goto_shop")
        상점열기();
};

DialogueManager.Instance.OnDialogueFinished += script => 저장하기();
DialogueManager.Instance.OnChoiceMade += (node, choice) => Debug.Log(choice.text);
```

플래그는 선택지 조건에 쓰인다. 밖에서도 넣어줄 수 있다.

```csharp
DialogueManager.Instance.SetFlag("met_before", true);
```

## 7. 테스트

빈 오브젝트에 **DialogueTestRunner**를 붙이고 Play를 누르면 단축키로 확인할 수 있다.

| 키 | 하는 일 |
|---|---|
| `F5` | 진행 기록·플래그를 지우고 처음부터 재생 |
| `F6` | 진행 기록만 삭제 (`playOnce` 대사 다시 보기) |
| `F7` | 건너뛰기 (`canSkip`이 꺼져 있으면 이유를 알려준다) |
| `F8` | 강제 종료 |
| `F9` | **JSON 점검** |
| `F10` | 지금 플래그 상태 출력 |

재생 중에는 줄마다 `번호 / id / 화자 / 선택지 수`가, 선택·이벤트·종료가 콘솔에 찍힌다.

### F9 점검이 잡아주는 것

`JsonUtility`는 모르는 필드를 조용히 무시해서 **오타가 나도 에러가 안 난다.**
그래서 재생 전에 미리 걸러준다.

- **오류** — 없는 `next` 대상, `id` 중복, 잘못된 열거형 값(`side`/`transition`/`backgroundStyle`), 빈 선택지
- **경고** — 못 찾는 이미지 키, 빈 `text`, 도달할 수 없는 줄
- **참고** — `characters`에 없는 `speaker`, `choices`와 `next`를 함께 쓴 줄,
  `canSkip`이 꺼져 있는데 `blockSkip`을 쓴 경우

이미지 키 검사는 매니저의 Sprite Table을 봐야 해서 **Play 중에만** 정확하다.
(에디터에서 돌리면 그 항목만 건너뛴다고 알려준다)

### 테스트용 JSON

`Assets/Resources/Dialogue/test_dialogue.json` — 허브에서 항목을 골라
원하는 것만 확인하는 구조다. 처음부터 끝까지 볼 필요가 없다.

1. 배경 스타일 7종 + `Fade`/`Instant` 전환
2. 화자 표시 — 이름·색·`???` 정체 숨김·미등록 화자·초상화 지우기
3. 글자 속도 (느림/즉시) 와 `autoAdvance`
4. 건너뛰기와 `blockSkip`이 실제로 멈추는지
5. `setFlag` / `requireFlag` 분기 (숨겨진 선택지가 나타나고 사라지는지)
6. **일부러 틀린 이미지 키** — 경고가 뜨고 기본 배경으로 대체되는지

`_presetFlags`에 플래그 이름을 적어두면 그 분기부터 바로 확인할 수 있다.

## 8. 참고

- `playOnce` 기록은 `PlayerPrefs`에 `Dialogue_Completed_<id>`로 저장된다.
  테스트 중엔 매니저 인스펙터의 **Ignore Saved Progress**를 켜거나,
  톱니 메뉴의 **모든 대사 진행도 초기화**를 쓰면 된다.
- 예시 파일: `Assets/Resources/Dialogue/sample_dialogue.json` (그림 없이 바로 실행됨),
  `sample_dialogue_full.json` (모든 필드 사용 예)
