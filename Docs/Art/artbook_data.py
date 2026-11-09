# 해달 식당 아트북 내용. 표기·프롬프트·요청 목록은 여기만 고치고 build_artbook.py로 다시 만든다.
# 근거 표시: 관찰(현재 파일에서 확인) / 규칙(앞으로 지킬 기준) / 제안(이번 문서의 제안, 팀 확정 전) / 미정(기획이 없음)

GAME_DESC_KO = (
    "해달 식당은 작은 열대 섬에서 해달 직원들과 해산물 식당을 운영하는 2D 경영 게임이다. "
    "주방은 나무 뗏목이고, 손님은 헤엄쳐 와서 바다 위 돌 의자에 앉는다."
)
GAME_DESC_EN = (
    "Haedal Restaurant (해달 식당) is a cozy 2D restaurant-management game set on a small tropical island. "
    "Sea otter staff run a seafood restaurant whose kitchen is a wooden raft and whose guests swim up and sit on stone seats in the sea."
)

STYLE_CORE = (
    "cozy Korean casual mobile game art, cute sticker-like illustration, clean thick dark-brown outline, "
    "soft flat cel shading with one highlight and one shadow tone, warm seaside palette"
)
NEGATIVE_COMMON = (
    "photorealistic, 3D render, pixel art, thin sketchy lines, black pure outline, neon colors, gradient mesh, "
    "text, letters, watermark, logo, signature, UI, frame, busy background, cast shadow on background, blurry, low detail, deformed"
)

STYLE_FAMILIES = [
    # 이름, 근거, 대상, 특징, 프롬프트 키
    ("A. 스티커 캐릭터", "관찰", "해달 직원·손님 스프라이트, 초상", "2등신 안팎. 굵고 일정한 진갈색 선. 면은 거의 평면이고 그림자는 한 단계. 투명 배경."),
    ("B. 진한 음식 일러스트", "관찰", "Food Sprite 4장, UIDesign Food 3장", "A보다 묘사가 많다. 재료 질감, 김, 그릇 나무결까지 그린다. 진갈색 선 유지."),
    ("C. 납작한 재료 아이콘", "관찰", "재료 아이콘 44장", "한 사물을 정면 또는 약간 비스듬히. 면 2~3단계. 라벨 글자는 한글. 투명 배경."),
    ("D. 도구 아이콘", "관찰", "프라이팬·도마·튀김기·냄비", "C와 같은 문법에 금속 반사 1~2줄. 해상도가 낮아(256px) 재제작 후보."),
    ("E. 손그림 쿼터뷰 배경", "관찰", "섬 지도, 식당 준비 배경, 뗏목", "거친 질감의 채색. 나무 판자 선이 보인다. 위에서 비스듬히 내려다본다."),
    ("F. 위에서 본 바다", "관찰", "영업 화면 바닥", "파랑 면 위에 흰 물결무늬(카우스틱). 테두리 선 없음. 캐릭터가 잘 보이는 중간 명도."),
    ("G. 광고풍 타이틀 일러스트", "관찰", "타이틀 배경", "밝고 채도 높은 정면 풍경. 게임 안 화면과 질감이 달라 타이틀에만 쓴다."),
    ("H. 픽셀 돌 가구 (예외)", "관찰", "돌 테이블·돌 의자", "이 프로젝트에서 유일한 픽셀 표현. 현재 게임에 쓰이지만 다른 군과 문법이 달라 학습에서 분리한다."),
]

UI_COLORS = [
    ("Sail", "#FBF3E2", "패널 바탕"), ("Sand", "#F2E2C2", "보조 버튼·칸"), ("Wood", "#8E5A34", "상단 바·탭"),
    ("WoodLight", "#9A6238", "메뉴 띠 버튼"), ("Line", "#4A2E1B", "외곽선·그림자"), ("Awning", "#C8443A", "화면당 핵심 행동 1개"),
    ("Sea", "#1F7F8A", "확인·선택됨"), ("SeaDeep", "#155A63", "깊은 바다색"), ("Coin", "#F2B632", "골드·별"),
    ("Kelp", "#3B8A5A", "성공·호평"), ("Alert", "#E0574A", "위험 게이지"), ("Night", "#2D3E66", "잠듦·취침"),
    ("Ink", "#3A2718", "본문 글자"), ("InkSoft", "#7D6654", "보조 글자"),
]
OTTER_COLORS = [
    ("털", "#C9A08A", "게임 스프라이트 면적 1위"), ("외곽선", "#3C1507", "가장 어두운 면이 아니라 선 색"),
    ("배·입 주변", "#FCEFE5", "크림색"), ("꼬리·발", "#6D4434", "털보다 어두운 갈색"), ("볼터치", "#E6A08D", "분홍 타원"),
    ("눈동자", "#29150C", "거의 검은 갈색"), ("초상용 털", "#E0A083", "OtterServer·타이틀용. 스프라이트보다 따뜻하다"),
    ("나비넥타이", "#FA8C3D", "웨이터 포인트"),
]
TIME_COLORS = [
    ("SeasideDay", "#7EC8E3", "#F6E2B3", "낮 바닷가"), ("SeasideDusk", "#F39C6B", "#2E4A7A", "노을"),
    ("SeasideNight", "#1B244A", "#4A3F6B", "밤바다"), ("WarmInterior", "#6B4735", "#D9A36A", "등불 켠 실내"),
    ("DeepSea", "#3CA0A8", "#0E2B4A", "깊은 물속"),
]

OTTER_RULES = [
    ("비율", "약 2등신. 머리가 키의 절반 정도를 차지한다. (1024px 스프라이트 측정: 몸 전체 약 750×950px)"),
    ("외곽선", "1024px 기준 약 20~23px의 일정한 굵기. 색은 #3C1507. 순수 검정을 쓰지 않는다."),
    ("머리", "둥근 곰형 귀 2개, 귀 안 흰 털뭉치. 볼 양옆 뾰족한 털. 이마로 내려오는 앞머리 한 다발."),
    ("얼굴", "눈은 처진 반달에 위쪽 직선 눈썹(졸리고 무심한 표정). 볼터치 분홍 타원. 입은 작은 ‘ω’."),
    ("몸", "크림색 배, 짧은 팔다리, 어두운 갈색 발, 뒤로 뻗은 납작한 꼬리."),
    ("셰프", "주름 잡힌 흰 셰프 모자와 단추 2줄 흰 조리복. 정면·측면·후면과 걷기 2프레임이 있다."),
    ("웨이터", "진갈색 조끼, 주황 나비넥타이, 흰 셔츠 소매. 정면·측면·후면과 수영 2프레임이 있다."),
    ("손님", "현재 전용 그림이 없다. 웨이터 스프라이트를 재사용하며 앉은 자세(Sit_Front)와 물 위 대기(Idle)가 추가돼 있다."),
    ("색 틴트", "직원은 실행 중 코드로 색을 입힌다(PartTimerAgent). 그래서 화면에서 조금 붉게 보여도 원본 색이 기준이다."),
]

# 재료: DB 이름, 영어, 아이콘 파일 (없으면 None)
INGREDIENTS = [
    ("새우", "shrimp", "ing_ shrimp.png"), ("가리비", "scallop", "재료_가리비.png"), ("오징어", "squid", "재료_오징어.png"),
    ("조개", "clam", "ing_seashell.png"), ("광어", "flounder", "재료_광어.png"), ("문어", "octopus", "ing_octo.png"),
    ("고등어", "mackerel", "재료_고등어.png"), ("연어", "salmon", "재료_연어.png"), ("바지락", "short-neck clam", "재료_바지락.png"),
    ("흰살생선", "white fish fillet", "재료_흰살생선.png"), ("튀김가루", "tempura batter mix", None), ("소금", "salt", "ing_salt.png"),
    ("설탕", "sugar", "ing_sugar.png"), ("식용유", "cooking oil", "ing_oil.png"), ("전분", "starch", "ing_starch.png"),
    ("후추", "pepper", "ing_pepper.png"), ("밀가루", "flour", "ing_flour.png"), ("식초", "vinegar", "ing_ vinegar.png"),
    ("와사비", "wasabi", "ing_wasabi.png"), ("간장", "soy sauce", "ing_soy.png"), ("미림", "mirin", "ing_Mirin.png"),
    ("레몬", "lemon", "ing_lemon.png"), ("마늘", "garlic", "ing_garlic.png"), ("된장", "doenjang soybean paste", "ing_Doenjang.png"),
    ("생강", "ginger", "ing_ginger.png"), ("계란", "egg", "ing_egg.png"), ("꿀", "honey", "ing_honey.png"), ("겨자", "korean mustard", "ing_gyeoja.png"),
]
EXTRA_ICONS = {
    "ing_apple.png": "apple", "ing_banana.png": "banana", "ing_blueBerry.png": "blueberry", "ing_carrot.png": "carrot",
    "ing_chestnut.png": "chestnut", "ing_corn.png": "corn", "ing_herb.png": "herb sprig", "ing_Mayo.png": "mayonnaise",
    "ing_mapleSyrup.png": "maple syrup", "ing_milk.png": "milk bottle", "ing_mushroom.png": "mushroom", "ing_mustard.png": "yellow mustard",
    "ing_oyster.png": "oyster", "ing_seeweed.png": "seaweed", "ing_sesame.png": "sesame seeds", "ing_sesameOil.png": "sesame oil", "ing_tuna.png": "tuna",
}

TOOL_NAMES = {201: "프라이팬", 202: "도마", 203: "튀김기", 204: "냄비", 205: "복합(도마+기타)"}
VESSELS = [
    ("찜·탕 (냄비 204)", "관찰", "깊은 갈색 질그릇 + 둥근 나무 받침 + 금속 숟가락 + 김 두 줄기",
     "deep brown earthenware pot on a round wooden trivet with a metal spoon and two curls of steam"),
    ("회·무침 (도마 202)", "관찰", "검은 잎 모양 도자기 접시(파도 무늬) + 나무 젓가락 + 나무 받침",
     "black leaf-shaped ceramic plate with a wave pattern, wooden chopsticks, round wooden trivet"),
    ("구이 (프라이팬 201)", "제안", "얕은 갈색 질그릇 접시 + 둥근 나무 받침 + 레몬 조각 (고등어 구이 그림의 형식을 얕게)",
     "shallow brown earthenware dish on a round wooden trivet with a lemon wedge"),
    ("튀김 (튀김기 203)", "제안", "대나무 바구니 + 기름종이 + 나무 받침",
     "shallow bamboo basket lined with parchment paper on a round wooden trivet"),
    ("플래터 (복합 205)", "제안", "길쭉한 나무 배 모양 접시 + 얼음 + 작은 간장 종지",
     "long wooden boat-shaped serving platter with crushed ice and a tiny soy sauce dish"),
]
VESSEL_EN = {201: VESSELS[2][3], 202: VESSELS[1][3], 203: VESSELS[3][3], 204: VESSELS[0][3], 205: VESSELS[4][3]}

# id, 이름, 도구, 등급, 가격, 재료 요약, 영어 묘사, 현재 상태
RECIPES = [
    (1, "가리비 구이", 201, 0, 50, "가리비", "grilled scallops on the half shell, browned edges", "신규"),
    (2, "새우 구이", 201, 0, 50, "새우·소금", "salt-grilled whole shrimp, curled, orange shells", "신규"),
    (3, "조개 찜", 204, 0, 50, "조개", "steamed clams, open shells in clear broth", "재제작: 지금은 미역국 그림을 쓴다"),
    (4, "광어 회", 202, 0, 50, "광어", "thin slices of white flounder sashimi fanned in a circle", "재제작: 지금은 새우 초밥 그림을 쓴다"),
    (5, "문어 숙회", 204, 1, 80, "문어·소금", "boiled octopus slices, purple-pink skin, white flesh", "신규"),
    (6, "새우 튀김", 203, 1, 80, "새우·튀김가루", "golden crispy fried shrimp tempura with tails", "신규"),
    (7, "고등어 구이", 201, 1, 80, "고등어·소금", "whole salt-grilled mackerel, crisp silver-brown skin", "기존 유지 (FoodGrilledFish)"),
    (8, "오징어 숙회", 204, 2, 130, "오징어·소금", "blanched squid rings and tentacles, glossy white", "신규"),
    (9, "바지락 술찜", 204, 2, 130, "바지락·미림·마늘", "short-neck clams steamed in rice wine with sliced garlic", "신규"),
    (10, "연어 간장구이", 201, 2, 130, "연어·간장·미림·설탕", "soy-glazed grilled salmon fillet, shiny caramelized surface", "신규"),
    (11, "가리비 간장구이", 201, 2, 130, "가리비·간장·설탕", "soy-glazed scallops on the shell, glossy sauce", "신규"),
    (12, "생선가스", 203, 3, 200, "흰살생선·튀김옷", "crispy breaded white fish cutlet, sliced, with tartar sauce", "신규"),
    (13, "새우 완자탕", 204, 3, 200, "새우·전분·간장", "shrimp dumpling balls in light soy broth", "신규"),
    (14, "고등어 된장조림", 204, 3, 200, "고등어·된장·생강", "mackerel braised in thick doenjang sauce with ginger slivers", "신규"),
    (15, "문어 초무침", 202, 3, 200, "문어·식초·겨자", "spicy-sour octopus salad, red dressing, cut vegetables", "신규"),
    (16, "해물 크림스튜", 204, 4, 300, "새우·가리비·오징어·밀가루", "creamy white seafood stew with shrimp, scallop and squid", "신규"),
    (17, "연어 스테이크 레몬소스", 201, 4, 300, "연어·레몬·꿀", "thick seared salmon steak with honey-lemon sauce and lemon slice", "신규"),
    (18, "해물 튀김 모둠", 203, 4, 300, "새우·오징어·조개·튀김옷", "assorted seafood tempura: shrimp, squid rings and clams", "신규"),
    (19, "광어 간장찜", 204, 4, 300, "광어·간장·마늘·생강", "whole steamed flounder in soy sauce with garlic and ginger", "신규"),
    (20, "해산물 전골", 204, 5, 450, "새우·가리비·조개·문어·된장", "generous seafood hotpot with shrimp, scallop, clams, octopus, bubbling", "신규"),
    (21, "해물 된장구이 모둠", 201, 5, 450, "새우·가리비·연어·된장·꿀", "assorted doenjang-glazed grilled seafood: shrimp, scallop, salmon", "신규"),
    (22, "바다의 진미 플래터", 205, 5, 450, "새우·가리비·광어·문어·와사비·레몬", "luxurious sashimi and seafood platter with shrimp, scallop, flounder, octopus, wasabi, lemon", "신규"),
]

# (폴더, 원본 상대경로, 내보낼 이름, 설명 영어, 용도)
# 용도: train=학습 후보, ref=참고 전용
CHAR = "Sprites/Character"
ART = "Resources/UIDesign/Art"
REF_SETS = {
    "01_character_otter": {
        "title": "해달 캐릭터", "use": "train",
        "note": "스타일 A. 캐릭터 모델 학습 후보.",
        "items": [
            (f"{CHAR}/해달_셰프정면.png", "chef_front_idle", "chef otter, front view, standing, white chef hat and double-breasted jacket"),
            (f"{CHAR}/해달_셰프정면_걷기1.png", "chef_front_walk1", "chef otter, front view, walking pose"),
            (f"{CHAR}/해달_셰프정면_걷기2.png", "chef_front_walk2", "chef otter, front view, walking pose, other foot"),
            (f"{CHAR}/해달_셰프측면.png", "chef_side_idle", "chef otter, side view, standing"),
            (f"{CHAR}/해달_셰프측면_걷기1.png", "chef_side_walk1", "chef otter, side view, walking pose"),
            (f"{CHAR}/해달_셰프측면_걷기2.png", "chef_side_walk2", "chef otter, side view, walking pose, other foot"),
            (f"{CHAR}/해달_셰프후면.png", "chef_back_idle", "chef otter, back view, standing, tail visible"),
            (f"{CHAR}/해달_셰프후면_걷기1.png", "chef_back_walk1", "chef otter, back view, walking pose"),
            (f"{CHAR}/해달_셰프후면_걷기2.png", "chef_back_walk2", "chef otter, back view, walking pose, other foot"),
            (f"{CHAR}/해달_웨이터정면.png", "waiter_front_idle", "waiter otter, front view, standing, dark brown vest and orange bow tie"),
            (f"{CHAR}/해달_웨이터정면_수영1.png", "waiter_front_swim1", "waiter otter, front view, swimming pose"),
            (f"{CHAR}/해달_웨이터정면_수영2.png", "waiter_front_swim2", "waiter otter, front view, swimming pose, other frame"),
            (f"{CHAR}/해달_웨이터측면_수영1.png", "waiter_side_swim1", "waiter otter, side view, swimming pose, tail stretched back"),
            (f"{CHAR}/해달_웨이터측면_수영2.png", "waiter_side_swim2", "waiter otter, side view, swimming pose, other frame"),
            (f"{CHAR}/해달_웨이터후면_수영1.png", "waiter_back_swim1", "waiter otter, back view, swimming pose"),
            (f"{CHAR}/해달_웨이터후면_수영2.png", "waiter_back_swim2", "waiter otter, back view, swimming pose, other frame"),
            (f"{CHAR}/Sit_Front.png", "guest_sit_front", "otter in vest, front view, sitting with foot pads showing"),
            (f"{CHAR}/Front.png", "plain_front", "plain otter without clothes, front view"),
            (f"{CHAR}/Back.png", "plain_back", "plain otter without clothes, back view"),
            (f"{CHAR}/RightSide1.png", "plain_side_swim1", "plain otter without clothes, side view, swimming pose"),
            (f"{CHAR}/RightSide2.png", "plain_side_swim2", "plain otter without clothes, side view, swimming pose, other frame"),
            (f"{ART}/OtterChef.png", "portrait_chef", "chef otter character portrait"),
            (f"{ART}/OtterGuest.png", "portrait_plain", "plain otter character portrait"),
        ],
    },
    "01b_character_warm": {
        "title": "해달 (색이 다른 그림)", "use": "ref", "note": "털색이 더 붉거나 인물이 작아 게임 스프라이트 학습에서 뺐다. 타이틀·초상 그림의 참고용이다.",
        "items": [
            (f"{ART}/OtterServer.png", "portrait_waiter_warm", "waiter otter holding steaming omelette plate and white towel, warm fur tone"),
            (f"{ART}/ClosedOtter.png", "portrait_closed_warm", "sad chef otter sitting with a closed sign and an empty plate, warm fur tone"),
            (f"{CHAR}/Idle_Right.png", "guest_water_idle", "otter floating in water with ripple ring, waiting, reddish fur"),
        ],
    },
    "02_food": {
        "title": "음식", "use": "ref", "note": "스타일 B. 4장뿐이라 학습보다 이미지 참조로 쓴다. 원본 배경은 흰색이다.",
        "items": [
            ("Sprites/Food Sprite/Grilled Mackerel.png", "food_grilled_mackerel", "grilled mackerel in brown earthenware pot on wooden trivet, steam, spoon"),
            ("Sprites/Food Sprite/Seaweed_Soup.png", "food_seaweed_soup", "seaweed soup in brown earthenware pot on wooden trivet, steam, spoon"),
            ("Sprites/Food Sprite/abalone porridge.png", "food_abalone_porridge", "abalone porridge in brown earthenware pot on wooden trivet, steam, spoon"),
            ("Sprites/Food Sprite/Shrimp Sushi.png", "food_shrimp_sushi", "shrimp sushi on black wave-patterned leaf plate with chopsticks"),
        ],
    },
    "04_prop": {
        "title": "조리도구·소품", "use": "ref", "note": "스타일 D·E. 도구는 256px라 해상도가 낮다. 돌 가구(H)는 학습에 넣지 않는다.",
        "items": [
            (f"{ART}/ToolPan.png", "tool_pan", "black frying pan with wooden handle"),
            (f"{ART}/ToolBoard.png", "tool_board", "wooden cutting board with kitchen knife"),
            (f"{ART}/ToolFryer.png", "tool_fryer", "metal wire fryer basket"),
            (f"{ART}/ToolPot.png", "tool_pot", "silver cooking pot with lid"),
            (f"{ART}/RopePost.png", "rope_post", "wooden post with green rope standing in water"),
            (f"{ART}/KitchenRaft.png", "kitchen_raft", "wooden raft kitchen with red awning, rope fence on both sides"),
            (f"{ART}/StoneSeat.png", "stone_seat_pixel", "pixel-art round stone seat"),
            (f"{ART}/SeaTable1.png", "stone_table_pixel", "pixel-art square stone table"),
            (f"{ART}/SeaTable2.png", "stone_table_set2_pixel", "pixel-art stone table with two round stone seats"),
            (f"{ART}/SeaTable4.png", "stone_table_set4_pixel", "pixel-art stone table with four round stone seats"),
        ],
    },
    "05_environment": {
        "title": "배경", "use": "ref", "note": "스타일 E·F·G. 군이 섞여 있어 한 모델로 학습하지 않는다.",
        "items": [
            (f"{ART}/IslandBackground.png", "island_map", "tropical island map, quarter view, wooden buildings, palm trees, dock and sailboat"),
            (f"{ART}/RestaurantPrepBackground.jpg", "restaurant_prep", "wooden raft restaurant on the sea beside a beach, quarter view"),
            (f"{ART}/RestaurantBackground.png", "restaurant_wide", "wooden raft restaurant with red awning, wide view"),
            ("Sprites/바다.png", "sea_topdown", "top-down sea with white caustic pattern and sandy shore at top"),
            (f"{ART}/TitleBackground.png", "title_scene", "bright seaside restaurant terrace, ocean, lighthouse, clouds, title screen art"),
        ],
    },
    "06_ui": {
        "title": "로고·UI", "use": "ref", "note": "로고 글자는 AI로 다시 만들지 않는다. 나무 버튼은 이전 UI이며 질감 참고용이다.",
        "items": [
            (f"{ART}/Logo.png", "logo", "Haedal Restaurant game logo, Korean wooden sign with waves, fish and starfish"),
            ("Sprites/UI/MaxButton.png", "wood_button_max", "wooden button with MAX text"),
            ("Sprites/UI/PlusButton.png", "wood_button_plus", "round wooden plus button"),
            ("Sprites/UI/MinusButton.png", "wood_button_minus", "round wooden minus button"),
            ("Sprites/UI/InputImage.png", "scroll_frame", "parchment scroll frame"),
            ("Sprites/UI/TotalField.png", "rope_wood_frame", "wooden frame with rope corners"),
        ],
    },
}

EXCLUDED = [
    ("Sprites/Icons/Recipes/*.png (10장)", "5.png에 shutterstock 표기와 이미지 번호, 4.png에 크라우드픽 표기가 보인다. 1.png도 옅은 워터마크가 보인다. 나머지도 출처를 알 수 없고 화풍이 제각각이다."),
    ("Sprites/Character/Feeling_*.png (4장)", "상용 이모지와 비슷한 그림이다. 현재 Customer.prefab이 쓰고 있어 교체 대상이다(U-01)."),
    ("Sprites/Character/Haedal_Idle·Haedal_Run_*, Icons/tmpOtter·그림1", "다른 화풍의 임시 그림이다."),
    ("Resources/Haedal_Idle/frame_*.png", "연속 프레임이라 학습에 넣으면 같은 이미지가 과하게 반복된다."),
    ("Sprites/TMPShopBackground, Icons/Test*, UI/Temp/*, TmpSelectBorder", "테스트·임시 리소스다."),
    ("Sprites/Character/ChatGPT Image 2026년 7월…, Sprites/Gemini_Generated_Image_…", "이전 콘셉트 또는 다른 AI 생성물이다. 학습 재료로 쓰려면 권리 확인이 먼저다."),
    ("Externel Asset/ 전체", "외부 에셋이다. 라이선스를 확인하지 않은 채 학습 데이터로 올리지 않는다."),
]

# id, 우선순위, 분류, 이름, 근거, 규격, 영어 주제
_ch = "1024×1024 투명"
_bg = "1920×1080 이상 불투명"
REQUESTS = [
    ("U-01", "P1", "ui", "감정 아이콘 4종 (기쁨·보통·나쁨·경고)", "현재 이모지형 이미지를 교체", "512×512 투명",
     "sheet of four small hand-drawn badges showing a guest mood: a pink heart, a calm wave, a storm cloud, an exclamation mark"),
    ("U-02", "P1", "ui", "골드 동전 아이콘", "HUD가 단색 원을 쓴다", "512×512 투명",
     "single round gold coin icon stamped with a small sea shell, warm yellow #F2B632"),
    ("I-01", "P1", "icon", "튀김가루 재료 아이콘", "DB 28종 중 유일하게 아이콘이 없다", "1024×1024 투명",
     "paper bag of tempura batter mix ingredient icon, Korean label area left blank"),
    ("C-01", "P1", "character", "손님: 요정 종족", "CustomerPreferences 2번째 종족. 전용 외형 없음. 외형 미정", _ch,
     "small forest fairy restaurant guest, front view, standing, leaf-like wings, same head-to-body ratio as the otter"),
    ("C-02", "P1", "character", "손님: 돌고래 종족", "3번째 종족. 외형 미정", _ch,
     "cute chibi dolphin restaurant guest, front view, upright, little fins as arms, smooth grey-blue skin"),
    ("C-03", "P2", "character", "손님: 해달 왕국 종족", "4번째 종족. 외형 미정", _ch,
     "noble otter guest from an otter kingdom, muted cocoa fur #C9A08A, front view, small golden crown and short blue cape"),
    ("N-01", "P2", "character", "엘프 상인", "대사와 상점은 있으나 그림이 없다. 외형 미정", _ch,
     "cute chibi elf merchant, front view, pointed ears, leaf-green tunic, big travel backpack with goods"),
    ("C-10", "P2", "character", "해달 대화 표정 4종", "대화창용. 기본·기쁨·걱정·놀람", _ch,
     "sheet of four bust portraits of the same sea otter with muted cocoa fur #C9A08A: neutral, happy, worried, surprised"),
    ("E-01", "P2", "bg", "컷신: 낮 바닷가", "대화 배경이 그라데이션 임시다 (SeasideDay)", _bg,
     "empty sunny tropical beach with gentle waves, wooden restaurant raft far away, no characters"),
    ("E-02", "P2", "bg", "컷신: 노을 바닷가", "SeasideDusk", _bg,
     "same tropical beach at sunset, orange sky fading into deep blue sea, no characters"),
    ("E-03", "P2", "bg", "컷신: 밤바다", "SeasideNight. 첫 대화 장면의 기본값", _bg,
     "same tropical beach at night, starry sky, moonlight on the waves, small lantern on a wooden dock, no characters"),
    ("E-04", "P2", "bg", "컷신: 식당 실내", "WarmInterior. ‘오래 닫힌 식당의 주방’", _bg,
     "inside a small old wooden restaurant kitchen lit by warm lanterns, dusty but cozy, no characters"),
    ("E-05", "P3", "bg", "컷신: 깊은 바다", "DeepSea", _bg,
     "underwater view looking up, light rays through teal water, deep navy below, kelp silhouettes, no characters"),
    ("E-06", "P2", "bg", "탐험: 앞바다", "탐험 지역. 그림 없음", _bg,
     "open shallow sea near an island with a small wooden boat, floating buoys, calm water, quarter view"),
    ("E-07", "P2", "bg", "탐험: 갯벌", "탐험 지역. 그림 없음", _bg,
     "wet tidal flat at low tide with small crabs, shells and buckets, quarter view"),
    ("E-08", "P3", "bg", "탐험: 산호 숲", "식당 Lv.3에 열린다. 그림 없음", _bg,
     "colorful coral forest in clear water, schools of tiny fish, quarter view"),
    ("E-09", "P2", "bg", "연구 실습 작업대", "지금은 단색 면이다. 해달 셰프 뒤 벽과 작업대", "1920×1080 이상 불투명",
     "cozy island research kitchen, wooden workbench across the lower third, shelves with jars on a plain cream wall, centre kept empty, no characters"),
    ("P-01", "P3", "prop", "6인석 테이블", "메뉴에 잠금 표시만 있다. 현재 돌 가구가 픽셀이라 화풍 결정이 먼저다", _ch,
     "long stone table with six round stone seats, viewed from above at a slight angle"),
]

STORY = [
    ("시작", "주인공은 바다에서 표류하다 작은 해달에게 구조된다. 주인공은 한때 셰프였다."),
    ("거래", "섬을 떠나려면 배가 필요하다. 해달은 배를 만들 줄 알지만 식당 운영에는 소질이 없다. 식당을 도와주면 배를 만들어 주기로 한다."),
    ("하루", "아침 준비 → 영업 → 결산 → 자유시간(상점·연구·대장간·탐험) → 집에서 취침."),
    ("성장", "메뉴 연구, 식당 증축, 조리도구 강화, 직원 고용. 첫 도구는 프라이팬이다."),
]
PLACES = [
    ("식당", "바다 위. 맨 위에 빨간 차양의 주방 뗏목, 가운데 돌 테이블 두 줄, 아래에 나무 기둥과 초록 밧줄 경계, 그 아래 돌 의자 대기석."),
    ("집", "섬 가운데 풀 지붕의 큰 나무집. 밤에 잠들고 저장하는 곳."),
    ("상점", "줄무늬 차양의 노점. 재료를 산다."),
    ("연구소", "빨간 지붕에 나무 벽. 새 요리를 실습해 배운다."),
    ("대장간", "회색 지붕의 작업장. 식당 증축과 도구 강화."),
    ("선착장·탐험", "나무 부두와 돛단배. 앞바다·갯벌·산호 숲으로 직원을 보낸다."),
    ("엘프 상점", "엘프 상인이 설계도와 재료를 판다. 장소 그림은 아직 없다."),
]
UNDECIDED = [
    "주인공의 외형, 종족, 성별, 이름 (대사에 ‘나’로만 나온다)",
    "구해 준 해달의 이름과 다른 해달과의 구분법",
    "요정·돌고래·해달 왕국·곡물·목장 종족의 외형 (곡물과 목장은 이름도 미정)",
    "엘프 상인의 외형과 성격 (샘플 대사만 있다)",
    "엔딩과 섬 주민의 설정",
    "돌 가구를 픽셀로 유지할지, 다른 그림으로 다시 그릴지",
    "식당 레벨이 오를 때 외형이 어떻게 달라질지",
]
TONE = ["밝고 느긋하다. 위기보다 일상의 소소함이 중심이다.", "해달은 무심한 표정이지만 말은 다정하다. 예: “일단 앉아. 따뜻한 거라도 내올게.”",
        "무서운 표현, 피, 술에 취한 묘사, 성적인 표현을 넣지 않는다.", "음식은 맛있어 보여야 한다. 해달이 잡히거나 해산물이 의인화되어 고통받는 그림은 금지다."]
