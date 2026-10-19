# HaedalGame | 해달 식당

Unity 기반 2D 식당 경영 시뮬레이션 팀 프로젝트입니다. 플레이어가 메뉴와 테이블을 준비하면 손님이 입장하고, 직원들이 주문·조리·서빙 작업을 처리하는 하루 단위의 식당 운영 흐름을 구현했습니다.

## 프로젝트 정보

| 구분 | 내용 |
| --- | --- |
| 개발 형태 | 팀 프로젝트 (프로그래머 2명, 아트 2명, 기획 1명) |
| 주요 담당 | 식당 운영 흐름, 손님·직원 상태 제어, 작업 배정, 길찾기, 테이블 배치 시스템 |
| Engine | Unity 6.3 (`6000.3.8f1`), Universal Render Pipeline 2D |
| Language | C# |
| 주요 기술 | State 기반 NPC 제어, Coroutine, Event, A\*, BFS, Tilemap |

## 주요 코드 바로가기

아래는 제가 직접 구현하거나 개선한 핵심 코드입니다.

| 시스템 | 확인할 내용 | 코드 |
| --- | --- | --- |
| 하루 운영 흐름 | 영업 시작, 마지막 손님 퇴장 감지, 결산 데이터 수집 및 일일 상태 초기화 | [RestaurantGameManager](./Assets/Scripts/Restaurant/Core/RestaurantGameManager.cs#L39-L114) |
| 중앙 작업 배정 | 유휴 직원 FIFO 관리, 새 작업/유휴 전환 시 즉시 배정 | [HallSystem](./Assets/Scripts/Restaurant/Hall/Server/HallSystem.cs#L23-L45) · [KitchenSystem](./Assets/Scripts/Restaurant/Kitchen/KitchenSystem.cs#L24-L46) |
| 작업 우선순위·선점 | 부스트된 서빙 작업 우선 선택, 작업 중복 선점 방지 | [ServingTaskQueue](./Assets/Scripts/Restaurant/Hall/Server/ServingTaskQueue.cs#L17-L54) |
| 직원 상태 실행 | 상태별 Coroutine 분리, 외부 중단과 내부 전환 규칙 구분 | [ServerAgent](./Assets/Scripts/Restaurant/Hall/Server/ServerAgent.cs#L54-L93) · [상태별 행동](./Assets/Scripts/Restaurant/Hall/Server/ServerAgent.cs#L100-L188) |
| 이동 실패 처리 | A\* 이동 결과 전달, 제한 횟수 재탐색, 대상 인접 타일 탐색 | [PartTimerAgent](./Assets/Scripts/Restaurant/PartTimer/PartTimerAgent.cs#L26-L90) |
| A\* 길찾기 | 4방향 Grid 탐색, Manhattan 휴리스틱, 경로 역추적 | [Pathfinder](./Assets/Scripts/Pathfinding/Pathfinder.cs#L12-L100) |
| BFS 배치 검증 | 후보 영역을 가상 차단한 전후의 도달 가능 영역 비교 | [PathfindingGrid](./Assets/Scripts/Pathfinding/PathfindingGrid.cs#L113-L176) |
| 테이블 배치 | Ghost 기반 타일 검사, 접근 공간·연결성 검증 | [TableGhostController](./Assets/Scripts/Restaurant/Placement/TableGhostController.cs#L55-L120) · [TablePlacementManager](./Assets/Scripts/Restaurant/Placement/TablePlacementManager.cs#L57-L105) |

## 핵심 구현

### 1. 이벤트 기반 중앙 작업 배정

초기에는 각 직원 NPC가 수행 가능한 작업을 직접 탐색하고 선점했습니다. 이 방식은 어떤 작업이 대기 중인지 한곳에서 확인하기 어렵고, 직원마다 탐색 코드가 필요해 작업 배정 결과를 예측하기 어려웠습니다.

이를 작업 관리 객체가 대기 작업과 유휴 직원을 함께 관리하는 중앙 배정 방식으로 변경했습니다.

- 손님의 주문 결정과 조리 완료 이벤트를 각각 서빙 작업으로 변환합니다.
- 작업이 추가되거나 직원이 유휴 상태가 되면 `DispatchPending()`을 호출합니다.
- 유휴 직원은 `Queue`로 관리해 먼저 대기한 직원부터 배정합니다.
- 서빙 작업은 부스트된 손님을 우선하고, 우선순위가 같으면 먼저 등록된 작업을 선택합니다.
- `TryClaim()`을 모든 배정 경로가 통과하게 해 이미 제거된 작업의 중복 선점을 막습니다.

관련 코드: [서빙 작업 생성과 배정](./Assets/Scripts/Restaurant/Hall/Server/HallSystem.cs#L34-L84), [서빙 작업 선택과 선점](./Assets/Scripts/Restaurant/Hall/Server/ServingTaskQueue.cs#L17-L53)

### 2. 상태별 NPC 행동 분리

직원의 이동·대기·상호작용을 하나의 `ExecuteTask` Coroutine에서 처리하던 구조는 행동이 추가될수록 분기와 예외 처리가 집중되는 문제가 있었습니다. 이를 직원의 현재 상태와 상태별 Coroutine으로 분리했습니다.

- `ServerAgent`: 손님 접근 → 주문 접수 → 주방 이동 → 음식 픽업 → 배달
- `ChefAgent`: 조리도구 대기 → 조리대 이동 → 조리 → 배달대 이동
- `Interrupt()`: 복귀·대기처럼 외부 요청으로 실행 중인 Coroutine을 끊어야 하는 전환
- `Advance()`: 현재 Coroutine이 다음 상태로 자연스럽게 이어지는 내부 전환

이동 결과도 `MoveResult`로 호출부에 전달합니다. 일시적인 점유로 경로를 찾지 못하면 제한된 횟수만 재탐색하고, 끝내 실패하면 도착을 전제로 하는 주문·조리·배달 단계로 진행하지 않습니다.

관련 코드: [Server 상태 전환](./Assets/Scripts/Restaurant/Hall/Server/ServerAgent.cs#L74-L93), [Chef 상태 전환](./Assets/Scripts/Restaurant/Kitchen/ChefAgent.cs#L47-L95), [공통 이동과 재시도](./Assets/Scripts/Restaurant/PartTimer/PartTimerAgent.cs#L26-L90)

### 3. 동적 장애물을 반영하는 A\* 길찾기

Tilemap으로부터 이동 가능한 `PathNode` Grid를 만들고, 직원과 손님의 이동 경로를 A\*로 탐색합니다.

- 상하좌우 4방향 탐색과 Manhattan Distance 휴리스틱을 사용합니다.
- 가구 배치·이동·삭제 및 좌석 점유 상태를 `walkable`에 반영합니다.
- 손님이나 조리대처럼 목적지 타일을 직접 점유할 수 없는 대상은 인접한 이동 가능 타일로 접근합니다.
- 탐색 실패를 성공으로 간주하지 않고 호출부가 재시도 또는 작업 종료를 선택하도록 했습니다.

관련 코드: [A\* 탐색](./Assets/Scripts/Pathfinding/Pathfinder.cs#L12-L100), [Grid와 동적 장애물](./Assets/Scripts/Pathfinding/PathfindingGrid.cs#L19-L109), [목적지 인접 타일 탐색](./Assets/Scripts/Restaurant/PartTimer/PartTimerAgent.cs#L78-L90)

### 4. BFS 기반 테이블 배치 검증

각 타일이 비어 있는지만 검사하면 테이블 하나가 통로 전체를 가로막아 NPC가 이동할 수 없는 배치도 허용될 수 있습니다. Ghost가 새로운 Grid 칸으로 이동했을 때 다음 순서로 배치 가능 여부를 검사합니다.

1. 테이블 본체·의자 칸의 바닥 존재 여부와 기존 장애물 충돌 여부를 확인합니다.
2. 의자 아래에 직원이 접근할 수 있는 이동 가능 칸이 있는지 확인합니다.
3. 테이블 본체 영역을 `walkable = false`로 가상 적용합니다.
4. 적용 전후의 BFS 도달 가능 영역을 비교합니다.
5. 후보 칸을 제외한 기존 영역이 분리되지 않은 경우에만 배치를 허용합니다.
6. 검사가 끝나면 Grid를 원래 상태로 복원합니다.

BFS를 두 번 수행하는 비용이 추가되지만, Ghost가 같은 칸에 머무는 동안에는 재검사하지 않고 식당의 제한된 Grid에서만 실행됩니다. 잘못된 배치로 하루 운영 전체가 중단되는 상황을 사전에 막는 편이 더 중요하다고 판단했습니다.

관련 코드: [Ghost 유효성 검사](./Assets/Scripts/Restaurant/Placement/TableGhostController.cs#L37-L120), [가상 차단과 BFS 비교](./Assets/Scripts/Pathfinding/PathfindingGrid.cs#L113-L176)

## 트러블슈팅

### 도달 불가능한 좌석의 무한 재할당

하루 전체 운영 통합 테스트에서 동일한 경고가 반복된 뒤 메모리 사용량이 계속 증가하는 문제를 확인했습니다. 원인은 서로 연결된 두 흐름이었습니다.

- 손님이 좌석까지의 경로를 찾지 못하면 좌석을 반납했지만, 다음 탐색에서 같은 좌석을 다시 받아 `TrySeat()`와 `MoveToSeat()`가 반복됐습니다.
- 대기 손님이 퇴장하거나 실내 좌석으로 이동할 때 대기 벤치 타일의 `walkable`을 복원하지 않아 이동 가능한 영역이 계속 줄었습니다.

손님별 `failedSeats`에 경로 탐색에 실패한 좌석을 기록하고, 다음 배정에서 해당 좌석을 제외했습니다. 또한 대기열 이탈과 실내 좌석 승격 양쪽에서 기존 벤치의 `Vacate()`를 호출해 점유 상태와 Grid 상태를 함께 복원했습니다.

관련 코드: [실패 좌석 기록](./Assets/Scripts/Restaurant/Customer/CustomerAgent.cs#L47-L49), [좌석 재배정 흐름](./Assets/Scripts/Restaurant/Customer/CustomerAgent.cs#L132-L176), [좌석 제외와 대기 벤치 복원](./Assets/Scripts/Restaurant/Seat/SeatManager.cs#L40-L103)

## 실행 환경

- Unity Editor: `6000.3.8f1`
- 주요 Scene: `Assets/Scenes/MainIsland.unity`, `Assets/Scenes/Restaurant.unity`

> 이 저장소에는 팀원이 제작한 코드와 외부 에셋이 함께 포함되어 있습니다. 위 `주요 코드 바로가기`와 `핵심 구현`에서 소개한 범위는 제가 직접 구현하거나 개선한 코드입니다.
