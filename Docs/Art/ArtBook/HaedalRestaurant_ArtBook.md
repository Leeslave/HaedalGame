# 해달 식당 아트북 (v1 · 2026-11-06)

시나리오(Scenario) AI에 전달할 화풍 기준과 제작 요청서의 글 버전이다. 그림이 든 전체본은 `HaedalRestaurant_ArtBook.pdf`다.

근거 표시: **[관찰]** 현재 파일에서 확인 / **[규칙]** 앞으로 지킬 기준 / **[제안]** 팀 확정 전 / **[미정]** 기획 없음

## AI에게 줄 요약

```
Haedal Restaurant (해달 식당) is a cozy 2D restaurant-management game set on a small tropical island. Sea otter staff run a seafood restaurant whose kitchen is a wooden raft and whose guests swim up and sit on stone seats in the sea. Match the supplied references exactly: thick dark-brown outlines, flat cel shading, warm seaside colors, cute and calm mood. Keep the sea otter design unchanged. Do not add text or watermarks. Output one subject per image unless the request asks for a sheet.
```

## 세계 [관찰]

- **시작**: 주인공은 바다에서 표류하다 작은 해달에게 구조된다. 주인공은 한때 셰프였다.
- **거래**: 섬을 떠나려면 배가 필요하다. 해달은 배를 만들 줄 알지만 식당 운영에는 소질이 없다. 식당을 도와주면 배를 만들어 주기로 한다.
- **하루**: 아침 준비 → 영업 → 결산 → 자유시간(상점·연구·대장간·탐험) → 집에서 취침.
- **성장**: 메뉴 연구, 식당 증축, 조리도구 강화, 직원 고용. 첫 도구는 프라이팬이다.

### 장소

- **식당**: 바다 위. 맨 위에 빨간 차양의 주방 뗏목, 가운데 돌 테이블 두 줄, 아래에 나무 기둥과 초록 밧줄 경계, 그 아래 돌 의자 대기석.
- **집**: 섬 가운데 풀 지붕의 큰 나무집. 밤에 잠들고 저장하는 곳.
- **상점**: 줄무늬 차양의 노점. 재료를 산다.
- **연구소**: 빨간 지붕에 나무 벽. 새 요리를 실습해 배운다.
- **대장간**: 회색 지붕의 작업장. 식당 증축과 도구 강화.
- **선착장·탐험**: 나무 부두와 돛단배. 앞바다·갯벌·산호 숲으로 직원을 보낸다.
- **엘프 상점**: 엘프 상인이 설계도와 재료를 판다. 장소 그림은 아직 없다.

### 톤 [제안]

- 밝고 느긋하다. 위기보다 일상의 소소함이 중심이다.
- 해달은 무심한 표정이지만 말은 다정하다. 예: “일단 앉아. 따뜻한 거라도 내올게.”
- 무서운 표현, 피, 술에 취한 묘사, 성적인 표현을 넣지 않는다.
- 음식은 맛있어 보여야 한다. 해달이 잡히거나 해산물이 의인화되어 고통받는 그림은 금지다.

### 아직 정하지 않은 것 [미정]

- 주인공의 외형, 종족, 성별, 이름 (대사에 ‘나’로만 나온다)
- 구해 준 해달의 이름과 다른 해달과의 구분법
- 요정·돌고래·해달 왕국·곡물·목장 종족의 외형 (곡물과 목장은 이름도 미정)
- 엘프 상인의 외형과 성격 (샘플 대사만 있다)
- 엔딩과 섬 주민의 설정
- 돌 가구를 픽셀로 유지할지, 다른 그림으로 다시 그릴지
- 식당 레벨이 오를 때 외형이 어떻게 달라질지

## 화풍 군 [관찰]

| 군 | 대상 | 특징 |
|---|---|---|
| A. 스티커 캐릭터 | 해달 직원·손님 스프라이트, 초상 | 2등신 안팎. 굵고 일정한 진갈색 선. 면은 거의 평면이고 그림자는 한 단계. 투명 배경. |
| B. 진한 음식 일러스트 | Food Sprite 4장, UIDesign Food 3장 | A보다 묘사가 많다. 재료 질감, 김, 그릇 나무결까지 그린다. 진갈색 선 유지. |
| C. 납작한 재료 아이콘 | 재료 아이콘 44장 | 한 사물을 정면 또는 약간 비스듬히. 면 2~3단계. 라벨 글자는 한글. 투명 배경. |
| D. 도구 아이콘 | 프라이팬·도마·튀김기·냄비 | C와 같은 문법에 금속 반사 1~2줄. 해상도가 낮아(256px) 재제작 후보. |
| E. 손그림 쿼터뷰 배경 | 섬 지도, 식당 준비 배경, 뗏목 | 거친 질감의 채색. 나무 판자 선이 보인다. 위에서 비스듬히 내려다본다. |
| F. 위에서 본 바다 | 영업 화면 바닥 | 파랑 면 위에 흰 물결무늬(카우스틱). 테두리 선 없음. 캐릭터가 잘 보이는 중간 명도. |
| G. 광고풍 타이틀 일러스트 | 타이틀 배경 | 밝고 채도 높은 정면 풍경. 게임 안 화면과 질감이 달라 타이틀에만 쓴다. |
| H. 픽셀 돌 가구 (예외) | 돌 테이블·돌 의자 | 이 프로젝트에서 유일한 픽셀 표현. 현재 게임에 쓰이지만 다른 군과 문법이 달라 학습에서 분리한다. |

## 색

| 이름 | 값 | 용도 |
|---|---|---|
| Sail | `#FBF3E2` | 패널 바탕 |
| Sand | `#F2E2C2` | 보조 버튼·칸 |
| Wood | `#8E5A34` | 상단 바·탭 |
| WoodLight | `#9A6238` | 메뉴 띠 버튼 |
| Line | `#4A2E1B` | 외곽선·그림자 |
| Awning | `#C8443A` | 화면당 핵심 행동 1개 |
| Sea | `#1F7F8A` | 확인·선택됨 |
| SeaDeep | `#155A63` | 깊은 바다색 |
| Coin | `#F2B632` | 골드·별 |
| Kelp | `#3B8A5A` | 성공·호평 |
| Alert | `#E0574A` | 위험 게이지 |
| Night | `#2D3E66` | 잠듦·취침 |
| Ink | `#3A2718` | 본문 글자 |
| InkSoft | `#7D6654` | 보조 글자 |
| 털 | `#C9A08A` | 게임 스프라이트 면적 1위 |
| 외곽선 | `#3C1507` | 가장 어두운 면이 아니라 선 색 |
| 배·입 주변 | `#FCEFE5` | 크림색 |
| 꼬리·발 | `#6D4434` | 털보다 어두운 갈색 |
| 볼터치 | `#E6A08D` | 분홍 타원 |
| 눈동자 | `#29150C` | 거의 검은 갈색 |
| 초상용 털 | `#E0A083` | OtterServer·타이틀용. 스프라이트보다 따뜻하다 |
| 나비넥타이 | `#FA8C3D` | 웨이터 포인트 |

시간대 그라데이션(위→아래): SeasideDay `#7EC8E3`→`#F6E2B3`, SeasideDusk `#F39C6B`→`#2E4A7A`, SeasideNight `#1B244A`→`#4A3F6B`, WarmInterior `#6B4735`→`#D9A36A`, DeepSea `#3CA0A8`→`#0E2B4A`

## 해달 규칙 [관찰]

- **비율**: 약 2등신. 머리가 키의 절반 정도를 차지한다. (1024px 스프라이트 측정: 몸 전체 약 750×950px)
- **외곽선**: 1024px 기준 약 20~23px의 일정한 굵기. 색은 #3C1507. 순수 검정을 쓰지 않는다.
- **머리**: 둥근 곰형 귀 2개, 귀 안 흰 털뭉치. 볼 양옆 뾰족한 털. 이마로 내려오는 앞머리 한 다발.
- **얼굴**: 눈은 처진 반달에 위쪽 직선 눈썹(졸리고 무심한 표정). 볼터치 분홍 타원. 입은 작은 ‘ω’.
- **몸**: 크림색 배, 짧은 팔다리, 어두운 갈색 발, 뒤로 뻗은 납작한 꼬리.
- **셰프**: 주름 잡힌 흰 셰프 모자와 단추 2줄 흰 조리복. 정면·측면·후면과 걷기 2프레임이 있다.
- **웨이터**: 진갈색 조끼, 주황 나비넥타이, 흰 셔츠 소매. 정면·측면·후면과 수영 2프레임이 있다.
- **손님**: 현재 전용 그림이 없다. 웨이터 스프라이트를 재사용하며 앉은 자세(Sit_Front)와 물 위 대기(Idle)가 추가돼 있다.
- **색 틴트**: 직원은 실행 중 코드로 색을 입힌다(PartTimerAgent). 그래서 화면에서 조금 붉게 보여도 원본 색이 기준이다.

## 음식 그릇

| 조리법 | 근거 | 그릇 |
|---|---|---|
| 찜·탕 (냄비 204) | 관찰 | 깊은 갈색 질그릇 + 둥근 나무 받침 + 금속 숟가락 + 김 두 줄기 |
| 회·무침 (도마 202) | 관찰 | 검은 잎 모양 도자기 접시(파도 무늬) + 나무 젓가락 + 나무 받침 |
| 구이 (프라이팬 201) | 제안 | 얕은 갈색 질그릇 접시 + 둥근 나무 받침 + 레몬 조각 (고등어 구이 그림의 형식을 얕게) |
| 튀김 (튀김기 203) | 제안 | 대나무 바구니 + 기름종이 + 나무 받침 |
| 플래터 (복합 205) | 제안 | 길쭉한 나무 배 모양 접시 + 얼음 + 작은 간장 종지 |

## 프롬프트 틀

캐릭터:
```
{subject}, cozy Korean casual mobile game art, cute sticker-like illustration, clean thick dark-brown outline, soft flat cel shading with one highlight and one shadow tone, warm seaside palette, chibi proportions about two heads tall, big round head, uniform outline #3C1507, full body, centered, plain transparent background
```
음식:
```
{dish}, served in a {vessel}, cozy Korean casual mobile game art, rich appetizing food illustration, clean dark-brown outline, painted cel shading, detailed ingredient textures, glossy warm highlights, three-quarter top view, single dish, centered, isolated on plain white background
```
아이콘:
```
{item}, cozy Korean casual mobile game art, cute sticker-like illustration, clean thick dark-brown outline, soft flat cel shading with one highlight and one shadow tone, warm seaside palette, single game item icon, simple flat shapes, readable at 64 pixels, centered, plain transparent background
```
배경:
```
{scene}, cozy Korean casual game background, hand-painted textured illustration, visible brush grain and wood plank lines, sunny tropical island palette with turquoise sea, warm sand and reddish wood, soft dark-brown linework, high-angle quarter view unless stated
```
소품:
```
{prop}, cozy Korean casual mobile game art, cute sticker-like illustration, clean thick dark-brown outline, soft flat cel shading with one highlight and one shadow tone, warm seaside palette, single game prop, hand-painted wood and stone textures, centered, plain transparent background
```
제외(공통):
```
photorealistic, 3D render, pixel art, thin sketchy lines, black pure outline, neon colors, gradient mesh, text, letters, watermark, logo, signature, UI, frame, busy background, cast shadow on background, blurry, low detail, deformed
```
제외(배경):
```
photorealistic, 3D render, pixel art, text, letters, watermark, logo, signature, UI, frame, characters, people, neon colors, blurry, low detail, deformed
```

## 제작 요청

`prompts.csv`와 같은 내용이다.

| id | 우선 | 대상 | 이유 | 규격 |
|---|---|---|---|---|
| F-01 | P1 | 가리비 구이 | 신규 · 레시피 1 · ★0 · 50G | 1024×1024 흰 배경 → 누끼 |
| F-02 | P1 | 새우 구이 | 신규 · 레시피 2 · ★0 · 50G | 1024×1024 흰 배경 → 누끼 |
| F-03 | P1 | 조개 찜 | 재제작: 지금은 미역국 그림을 쓴다 · 레시피 3 · ★0 · 50G | 1024×1024 흰 배경 → 누끼 |
| F-04 | P1 | 광어 회 | 재제작: 지금은 새우 초밥 그림을 쓴다 · 레시피 4 · ★0 · 50G | 1024×1024 흰 배경 → 누끼 |
| F-05 | P1 | 문어 숙회 | 신규 · 레시피 5 · ★1 · 80G | 1024×1024 흰 배경 → 누끼 |
| F-06 | P1 | 새우 튀김 | 신규 · 레시피 6 · ★1 · 80G | 1024×1024 흰 배경 → 누끼 |
| F-07 | 유지 | 고등어 구이 | 기존 유지 (FoodGrilledFish) · 레시피 7 · ★1 · 80G | 1024×1024 흰 배경 → 누끼 |
| F-08 | P1 | 오징어 숙회 | 신규 · 레시피 8 · ★2 · 130G | 1024×1024 흰 배경 → 누끼 |
| F-09 | P1 | 바지락 술찜 | 신규 · 레시피 9 · ★2 · 130G | 1024×1024 흰 배경 → 누끼 |
| F-10 | P1 | 연어 간장구이 | 신규 · 레시피 10 · ★2 · 130G | 1024×1024 흰 배경 → 누끼 |
| F-11 | P1 | 가리비 간장구이 | 신규 · 레시피 11 · ★2 · 130G | 1024×1024 흰 배경 → 누끼 |
| F-12 | P2 | 생선가스 | 신규 · 레시피 12 · ★3 · 200G | 1024×1024 흰 배경 → 누끼 |
| F-13 | P2 | 새우 완자탕 | 신규 · 레시피 13 · ★3 · 200G | 1024×1024 흰 배경 → 누끼 |
| F-14 | P2 | 고등어 된장조림 | 신규 · 레시피 14 · ★3 · 200G | 1024×1024 흰 배경 → 누끼 |
| F-15 | P2 | 문어 초무침 | 신규 · 레시피 15 · ★3 · 200G | 1024×1024 흰 배경 → 누끼 |
| F-16 | P2 | 해물 크림스튜 | 신규 · 레시피 16 · ★4 · 300G | 1024×1024 흰 배경 → 누끼 |
| F-17 | P2 | 연어 스테이크 레몬소스 | 신규 · 레시피 17 · ★4 · 300G | 1024×1024 흰 배경 → 누끼 |
| F-18 | P2 | 해물 튀김 모둠 | 신규 · 레시피 18 · ★4 · 300G | 1024×1024 흰 배경 → 누끼 |
| F-19 | P2 | 광어 간장찜 | 신규 · 레시피 19 · ★4 · 300G | 1024×1024 흰 배경 → 누끼 |
| F-20 | P2 | 해산물 전골 | 신규 · 레시피 20 · ★5 · 450G | 1024×1024 흰 배경 → 누끼 |
| F-21 | P2 | 해물 된장구이 모둠 | 신규 · 레시피 21 · ★5 · 450G | 1024×1024 흰 배경 → 누끼 |
| F-22 | P2 | 바다의 진미 플래터 | 신규 · 레시피 22 · ★5 · 450G | 1024×1024 흰 배경 → 누끼 |
| U-01 | P1 | 감정 아이콘 4종 (기쁨·보통·나쁨·경고) | 현재 이모지형 이미지를 교체 | 512×512 투명 |
| U-02 | P1 | 골드 동전 아이콘 | HUD가 단색 원을 쓴다 | 512×512 투명 |
| I-01 | P1 | 튀김가루 재료 아이콘 | DB 28종 중 유일하게 아이콘이 없다 | 1024×1024 투명 |
| C-01 | P1 | 손님: 요정 종족 | CustomerPreferences 2번째 종족. 전용 외형 없음. 외형 미정 | 1024×1024 투명 |
| C-02 | P1 | 손님: 돌고래 종족 | 3번째 종족. 외형 미정 | 1024×1024 투명 |
| C-03 | P2 | 손님: 해달 왕국 종족 | 4번째 종족. 외형 미정 | 1024×1024 투명 |
| N-01 | P2 | 엘프 상인 | 대사와 상점은 있으나 그림이 없다. 외형 미정 | 1024×1024 투명 |
| C-10 | P2 | 해달 대화 표정 4종 | 대화창용. 기본·기쁨·걱정·놀람 | 1024×1024 투명 |
| E-01 | P2 | 컷신: 낮 바닷가 | 대화 배경이 그라데이션 임시다 (SeasideDay) | 1920×1080 이상 불투명 |
| E-02 | P2 | 컷신: 노을 바닷가 | SeasideDusk | 1920×1080 이상 불투명 |
| E-03 | P2 | 컷신: 밤바다 | SeasideNight. 첫 대화 장면의 기본값 | 1920×1080 이상 불투명 |
| E-04 | P2 | 컷신: 식당 실내 | WarmInterior. ‘오래 닫힌 식당의 주방’ | 1920×1080 이상 불투명 |
| E-05 | P3 | 컷신: 깊은 바다 | DeepSea | 1920×1080 이상 불투명 |
| E-06 | P2 | 탐험: 앞바다 | 탐험 지역. 그림 없음 | 1920×1080 이상 불투명 |
| E-07 | P2 | 탐험: 갯벌 | 탐험 지역. 그림 없음 | 1920×1080 이상 불투명 |
| E-08 | P3 | 탐험: 산호 숲 | 식당 Lv.3에 열린다. 그림 없음 | 1920×1080 이상 불투명 |
| E-09 | P2 | 연구 실습 작업대 | 지금은 단색 면이다. 해달 셰프 뒤 벽과 작업대 | 1920×1080 이상 불투명 |
| P-01 | P3 | 6인석 테이블 | 메뉴에 잠금 표시만 있다. 현재 돌 가구가 픽셀이라 화풍 결정이 먼저다 | 1024×1024 투명 |

## 참조 이미지

`references/manifest.csv`에 파일별 원본 경로·용도·설명이 있다.

| 폴더 | 내용 | 장수 | 용도 |
|---|---|---|---|
| `01_character_otter` | 해달 캐릭터 | 23 | 학습 후보 |
| `01b_character_warm` | 해달 (색이 다른 그림) | 3 | 이미지 참조 |
| `02_food` | 음식 | 4 | 이미지 참조 |
| `03_icon_ingredient` | 재료 아이콘 | 44 | 학습 후보 |
| `04_prop` | 조리도구·소품 | 10 | 이미지 참조 |
| `05_environment` | 배경 | 5 | 이미지 참조 |
| `06_ui` | 로고·UI | 6 | 이미지 참조 |
| `07_ingame` | 실제 게임 화면 | 10 | 맥락 설명(학습 제외) |

## 올리지 말 것

- **Sprites/Icons/Recipes/*.png (10장)**: 5.png에 shutterstock 표기와 이미지 번호, 4.png에 크라우드픽 표기가 보인다. 1.png도 옅은 워터마크가 보인다. 나머지도 출처를 알 수 없고 화풍이 제각각이다.
- **Sprites/Character/Feeling_*.png (4장)**: 상용 이모지와 비슷한 그림이다. 현재 Customer.prefab이 쓰고 있어 교체 대상이다(U-01).
- **Sprites/Character/Haedal_Idle·Haedal_Run_*, Icons/tmpOtter·그림1**: 다른 화풍의 임시 그림이다.
- **Resources/Haedal_Idle/frame_*.png**: 연속 프레임이라 학습에 넣으면 같은 이미지가 과하게 반복된다.
- **Sprites/TMPShopBackground, Icons/Test*, UI/Temp/*, TmpSelectBorder**: 테스트·임시 리소스다.
- **Sprites/Character/ChatGPT Image 2026년 7월…, Sprites/Gemini_Generated_Image_…**: 이전 콘셉트 또는 다른 AI 생성물이다. 학습 재료로 쓰려면 권리 확인이 먼저다.
- **Externel Asset/ 전체**: 외부 에셋이다. 라이선스를 확인하지 않은 채 학습 데이터로 올리지 않는다.

시나리오의 현재 메뉴 이름, 장수 제한, 크레딧은 외부 접속 없이 작성해 확인하지 못했다.
