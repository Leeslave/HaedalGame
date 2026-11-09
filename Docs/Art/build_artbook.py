# 해달 식당 아트북을 만든다. 게임 파일은 읽기만 하고 결과는 Docs/Art/ArtBook/ 아래에만 쓴다.
# 사용: python Docs/Art/build_artbook.py [--captures 캡처폴더] [--preview 미리보기폴더]
#  --captures  before-*.png 실제 게임 화면을 references/07_ingame 으로 가져온다. 생략하면 이미 있는 것을 쓴다.
#  --preview   PDF 대신 쪽별 PNG와 넘침 검사 결과를 지정 폴더에 쓴다(검수용).
import argparse, base64, csv, html, io, os, re, shutil, subprocess, sys, tempfile, unicodedata
from pathlib import Path
from PIL import Image
sys.dont_write_bytecode = True
import artbook_data as D

Image.MAX_IMAGE_PIXELS = None
ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets"
OUT = ROOT / "Docs/Art/ArtBook"
REFS = OUT / "references"
BOOK = "HaedalRestaurant_ArtBook"
VERSION = "v1 · 2026-11-06"
nf = lambda s: unicodedata.normalize("NFC", s)


def asset(rel):
    """파일 이름의 한글 정규화(NFC/NFD)가 섞여 있어 정규화해서 찾는다."""
    rel = rel.replace("\\", "/")
    folder, name = rel.rsplit("/", 1)
    base = ASSETS
    for part in folder.split("/"):
        base = next(p for p in base.iterdir() if nf(p.name) == nf(part))
    return next(p for p in base.iterdir() if nf(p.name) == nf(name))


def fit(im, size):
    im = im.copy()
    im.thumbnail((size, size), Image.LANCZOS)
    return im


_thumbs = {}
def uri(path, size=600, crop=True):
    key = (str(path), size, crop)
    if key not in _thumbs:
        im = Image.open(path).convert("RGBA")
        box = im.getchannel("A").getbbox()
        if box and crop:
            im = im.crop(box)
        im = fit(im, size)
        buf = io.BytesIO()
        im.save(buf, "WEBP", quality=88, method=4)
        _thumbs[key] = "data:image/webp;base64," + base64.b64encode(buf.getvalue()).decode()
    return _thumbs[key]


def font_uri(name):
    return "data:font/ttf;base64," + base64.b64encode((ASSETS / "Resources/UIDesign/Fonts" / name).read_bytes()).decode()


e = html.escape


# ───── 프롬프트 ─────
CHAR_STYLE = (D.STYLE_CORE + ", chibi proportions about two heads tall, big round head, uniform outline #3C1507, "
              "full body, centered, plain transparent background")
FOOD_STYLE = ("cozy Korean casual mobile game art, rich appetizing food illustration, clean dark-brown outline, painted cel shading, "
              "detailed ingredient textures, glossy warm highlights, three-quarter top view, single dish, centered, isolated on plain white background")
ICON_STYLE = (D.STYLE_CORE + ", single game item icon, simple flat shapes, readable at 64 pixels, centered, plain transparent background")
BG_STYLE = ("cozy Korean casual game background, hand-painted textured illustration, visible brush grain and wood plank lines, "
            "sunny tropical island palette with turquoise sea, warm sand and reddish wood, soft dark-brown linework, high-angle quarter view unless stated")
PROP_STYLE = D.STYLE_CORE + ", single game prop, hand-painted wood and stone textures, centered, plain transparent background"
NEG_BG = ("photorealistic, 3D render, pixel art, text, letters, watermark, logo, signature, UI, frame, characters, people, "
          "neon colors, blurry, low detail, deformed")
STYLE_OF = {"character": CHAR_STYLE, "icon": ICON_STYLE, "ui": ICON_STYLE, "bg": BG_STYLE, "prop": PROP_STYLE}


def all_prompts():
    rows = []
    for rid, name, tool, grade, price, parts, desc, state in D.RECIPES:
        keep = state.startswith("기존")
        pri = "유지" if keep else ("P1" if grade <= 2 else "P2")
        prompt = f"{desc}, served in a {D.VESSEL_EN[tool]}, {FOOD_STYLE}"
        rows.append((f"F-{rid:02d}", pri, "food", name, f"{state} · 레시피 {rid} · ★{grade} · {price}G", "1024×1024 흰 배경 → 누끼", prompt, D.NEGATIVE_COMMON))
    for rid, pri, cat, name, why, size, subject in D.REQUESTS:
        neg = NEG_BG if cat == "bg" else D.NEGATIVE_COMMON
        rows.append((rid, pri, cat, name, why, size, f"{subject}, {STYLE_OF[cat]}", neg))
    return rows


# ───── 참조 이미지 내보내기 ─────
def ingredient_set():
    items = [(f"Sprites/Icons/Ingredients/{f}", "icon_" + en.replace(" ", "_").replace("-", "_"), f"{en} ingredient icon")
             for _, en, f in D.INGREDIENTS if f]
    items += [(f"Sprites/Icons/Ingredients/{f}", "icon_" + en.replace(" ", "_"), f"{en} ingredient icon") for f, en in D.EXTRA_ICONS.items()]
    return {"title": "재료 아이콘", "use": "train", "note": "스타일 C. 아이콘 모델 학습 후보.", "items": items}


def export_refs(captures):
    sets = dict(D.REF_SETS)
    sets["03_icon_ingredient"] = ingredient_set()
    rows = []
    for folder, meta in sorted(sets.items()):
        target = REFS / folder
        if target.exists():
            shutil.rmtree(target)
        target.mkdir(parents=True)
        limit = 2048 if folder == "05_environment" else 1024
        for rel, name, caption in meta["items"]:
            src = asset(rel)
            im = Image.open(src)
            im = im.convert("RGBA") if im.mode in ("RGBA", "LA", "P") else im.convert("RGB")
            fit(im, limit).save(target / f"{name}.png", optimize=True)
            rows.append((f"{folder}/{name}.png", meta["use"], "Assets/" + nf(rel), f"{caption}, " + (
                "cute otter sticker game art" if folder.startswith("01") else "game asset")))
    ingame = REFS / "07_ingame"
    if captures:
        if ingame.exists():
            shutil.rmtree(ingame)
        ingame.mkdir(parents=True)
        for p in sorted(Path(captures).glob("before-*.png")):
            fit(Image.open(p).convert("RGB"), 1600).save(ingame / (p.stem.replace("before-", "") + ".jpg"), quality=88)
    for p in sorted(ingame.glob("*.jpg")):
        rows.append((f"07_ingame/{p.name}", "context", "Unity Play 캡처(복사본)", SCREEN_NOTES.get(p.stem, "in-game screen")))
    with open(REFS / "manifest.csv", "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["file", "use(train/ref/context)", "source", "caption_en"])
        w.writerows(rows)
    with open(OUT / "prompts.csv", "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["id", "priority", "category", "name_ko", "why_ko", "size", "prompt_en", "negative_en"])
        w.writerows(all_prompts())
    return sets, rows


SCREEN_NOTES = {
    "title": "타이틀 화면", "island": "섬 지도. 건물 표식이 지도 위에 올라간다", "house": "밤의 집. 카드 UI (extra0은 캡처용 시험 데이터)",
    "restaurantPrep": "영업 준비. 왼쪽 띠와 준비 카드", "menu": "관리 > 메뉴. 전용 그림이 없는 요리는 글자 원으로 나온다",
    "shop": "관리 > 상점. 재료 아이콘이 연결되지 않아 글자 원으로 나온다", "staff": "관리 > 직원 (extra0·1은 캡처용 시험 데이터)",
    "furniture": "관리 > 가구", "research": "연구 실습. 뒤 배경이 단색이다", "operation": "영업 중. 뗏목 주방, 돌 테이블, 밧줄, 대기석, 네 귀퉁이 HUD",
}


# ───── HTML 조각 ─────
_page = 0
def page(chapter, title, body, cls=""):
    global _page
    _page += 1
    return (f'<section class="page {cls}"><header><span class="ch">{e(chapter)}</span><h1>{e(title)}</h1></header>'
            f'<div class="body">{body}</div><footer><span>해달 식당 아트북 {VERSION}</span><span>{_page}</span></footer></section>')


def tag(kind):
    return f'<span class="tag t-{ {"관찰":"a","규칙":"b","제안":"c","미정":"d"}.get(kind,"a") }">{e(kind)}</span>'


def img(path, size=600, cap=None, cls=""):
    c = f"<figcaption>{cap}</figcaption>" if cap else ""
    return f'<figure class="{cls}"><div class="frame"><img src="{uri(path, size)}"></div>{c}</figure>'


def swatch(name, hexv, note):
    return (f'<div class="sw"><i style="background:{hexv}"></i><b>{e(name)}</b><code>{hexv}</code><small>{e(note)}</small></div>')


def table(head, rows, cls=""):
    th = "".join(f"<th>{e(h)}</th>" for h in head)
    tr = "".join("<tr>" + "".join(f"<td>{c}</td>" for c in r) + "</tr>" for r in rows)
    head_html = f"<thead><tr>{th}</tr></thead>" if any(head) else ""
    return f'<table class="{cls}">{head_html}<tbody>{tr}</tbody></table>'


def pill(text):
    return f"<pre class=prompt>{e(text)}</pre>"


CSS = """
@font-face{font-family:Jua;src:url(%JUA%)}
@page{size:1600px 900px;margin:0}
*{box-sizing:border-box}
html,body{margin:0;padding:0;background:#FBF3E2;-webkit-print-color-adjust:exact;print-color-adjust:exact}
body{font-family:'Malgun Gothic',sans-serif;color:#3A2718;font-size:20px;line-height:1.45}
.page{width:1600px;height:900px;position:relative;overflow:hidden;background:#FBF3E2;padding:34px 56px 44px;page-break-after:always;break-after:page;display:flex;flex-direction:column}
header{display:flex;align-items:baseline;gap:18px;border-bottom:4px solid #4A2E1B;padding-bottom:8px;margin-bottom:18px;flex:none}
h1{font-family:Jua;font-weight:400;font-size:44px;margin:0;line-height:1.1}
h2{font-family:Jua;font-weight:400;font-size:27px;margin:0 0 8px;color:#155A63;line-height:1.2}
.ch{font-family:Jua;background:#C8443A;color:#fff;border-radius:10px;padding:2px 14px;font-size:23px}
.body{flex:1;min-height:0;display:flex;flex-direction:column;gap:16px}
footer{position:absolute;left:56px;right:56px;bottom:12px;display:flex;justify-content:space-between;font-size:15px;color:#7D6654}
p{margin:0 0 8px} ul{margin:0;padding-left:24px} li{margin-bottom:5px}
.row{display:flex;gap:22px;min-height:0}.grow{flex:1;min-width:0}
.card{background:#fff;border:3px solid #4A2E1B;border-radius:18px;box-shadow:0 5px 0 #4A2E1B;padding:14px 20px;min-width:0}
.tag{font-family:'Malgun Gothic',sans-serif;vertical-align:middle;font-size:14px;border-radius:7px;padding:1px 8px;color:#fff;white-space:nowrap;font-weight:700}.t-a{background:#3B8A5A}.t-b{background:#1F7F8A}.t-c{background:#B57F12}.t-d{background:#7D6654}
figure{margin:0;display:flex;flex-direction:column;min-width:0;min-height:0}
.frame{flex:1;min-height:0;display:flex;align-items:center;justify-content:center;background:#E9DCC4;border-radius:12px;overflow:hidden;background-image:linear-gradient(45deg,#E1D2B6 25%,transparent 25%,transparent 75%,#E1D2B6 75%),linear-gradient(45deg,#E1D2B6 25%,transparent 25%,transparent 75%,#E1D2B6 75%);background-size:22px 22px;background-position:0 0,11px 11px}
.frame img{max-width:100%;max-height:100%;display:block;object-fit:contain}
figcaption{font-size:16px;line-height:1.3;padding-top:4px;text-align:center}
.grid{display:grid;gap:12px;min-height:0;flex:1}
.sws{display:grid;gap:8px}.sw{display:grid;grid-template-columns:44px 1fr;column-gap:10px;align-items:center;line-height:1.2}
.sw i{grid-row:span 3;width:44px;height:58px;border-radius:9px;border:2px solid #4A2E1B}.sw b{font-size:17px}.sw code{font-size:15px;color:#7D6654}.sw small{font-size:14px;color:#7D6654}
table{border-collapse:collapse;width:100%;font-size:16.5px;line-height:1.28}th{background:#8E5A34;color:#fff;text-align:left;padding:4px 8px;font-weight:700;white-space:nowrap}td{padding:3.5px 8px;border-bottom:1px solid #E6D2AE;vertical-align:top}tr:nth-child(even) td{background:#FFFBF2}
pre.prompt{white-space:pre-wrap;word-break:break-word;background:#155A63;color:#F4FBFB;border-radius:12px;padding:10px 14px;margin:4px 0 8px;font:15.5px/1.38 Consolas,'Malgun Gothic',monospace}
.cover{padding:0}.cover header,.cover footer{display:none}.cover .body{position:relative;display:block}
.cover .bg{position:absolute;inset:0;width:100%;height:100%;object-fit:cover}
.cover .box{position:absolute;left:56px;bottom:52px;width:800px;background:#FBF3E2;border:4px solid #4A2E1B;border-radius:24px;box-shadow:0 8px 0 #4A2E1B;padding:24px 32px}
.cover .logo{position:absolute;left:56px;top:40px;width:520px}
.cover h1{font-size:60px}.cover .sub{font-size:24px;margin:8px 0 0}
.bad{color:#A3392F;font-weight:700}.good{color:#2F7048;font-weight:700}
.small{font-size:17px;line-height:1.35}
.big table,table.big{font-size:20px;line-height:1.35}.big td,table.big td{padding:9px 12px}.big th,table.big th{padding:7px 12px}
.k td:first-child{white-space:nowrap}.nw td:nth-child(1),.nw td:nth-child(3),.nw td:nth-child(4){white-space:nowrap}
.quote{border-left:6px solid #1F7F8A;padding:4px 0 4px 14px;margin:6px 0;font-size:18px}
"""


def build_pages(sets):
    global _page
    _page = 0
    art = lambda n: asset("Resources/UIDesign/Art/" + n)
    ch = lambda n: asset("Sprites/Character/" + n)
    food = lambda n: asset("Sprites/Food Sprite/" + n)
    shot = lambda n: REFS / "07_ingame" / f"{n}.jpg"
    P = []

    # 1 표지
    _page += 1
    P.append(f'''<section class="page cover"><div class="body"><img class="bg" src="{uri(art("TitleBackground.png"), 1600)}">
      <img class="logo" src="{uri(art("Logo.png"), 640)}"><div class="box"><h1>해달 식당 아트북</h1>
      <p class="sub">시나리오(Scenario) AI에 전달할 화풍 기준과 제작 요청서</p>
      <p class="small" style="margin-top:10px">{VERSION} · develop 작업 트리 기준. 그림은 모두 프로젝트의 실제 파일이고, 화면은 실제 게임을 실행해 캡처했다.</p></div></div></section>''')

    # 2 사용법
    P.append(page("시작", "이 책을 쓰는 법", f'''
    <div class="row"><div class="grow card"><h2>AI에게 먼저 줄 요약 (그대로 붙여 넣기)</h2>{pill(D.GAME_DESC_EN + " Match the supplied references exactly: thick dark-brown outlines, flat cel shading, warm seaside colors, cute and calm mood. Keep the sea otter design unchanged. Do not add text or watermarks. Output one subject per image unless the request asks for a sheet.")}
      <h2>순서</h2><ul>
      <li><b>5~13쪽</b>을 보고 화풍을 맞춘다.</li><li><b>참조 이미지</b>는 <code>references/</code>의 세트 단위로 올린다. <code>manifest.csv</code>에 설명이 있다.</li>
      <li><b>프롬프트</b>는 <code>prompts.csv</code>의 id별 문장을 쓴다. 틀과 예시는 19~20쪽에 있다.</li><li><b>결과물</b>은 22쪽 체크리스트로 거른 뒤 게임에 넣는다.</li></ul></div>
      <div class="card" style="width:560px"><h2>근거 표시</h2><p>{tag("관찰")} 현재 파일에서 확인했다.</p><p>{tag("규칙")} 앞으로 지킬 기준이다.</p><p>{tag("제안")} 이 문서의 제안이다. 팀 확정 전이다.</p><p>{tag("미정")} 기획이 없다. AI가 임의로 정하게 두지 않는다.</p>
      <h2 style="margin-top:12px">한 줄 원칙</h2><p>새 그림은 <b>기존 해달 옆에 놓였을 때 같은 게임처럼</b> 보여야 한다.</p></div></div>
    <div class="row" style="flex:1"><div class="grid" style="grid-template-columns:repeat(5,1fr)">
      {img(art("OtterServer.png"),300,"캐릭터")}{img(food("Grilled Mackerel.png"),300,"음식")}{img(asset("Sprites/Icons/Ingredients/재료_고등어.png"),300,"재료 아이콘")}{img(art("KitchenRaft.png"),400,"소품")}{img(art("IslandBackground.png"),400,"배경")}
    </div></div>'''))

    # 3 세계
    P.append(page("세계", "무슨 게임인가", f'''
    <p>{e(D.GAME_DESC_KO)}</p>
    <div class="row" style="flex:1"><div class="card" style="width:640px"><h2>이야기 {tag("관찰")}</h2>{table(["", ""], [(f"<b>{a}</b>", e(b)) for a, b in D.STORY], "k")}
      <h2 style="margin-top:12px">첫 대사 {tag("관찰")}</h2><div class="quote">“식당 운영을 도와주면 나가는 배를 만들어 줄게. 어때?”</div>
      <h2 style="margin-top:12px">톤 {tag("제안")}</h2><ul class="small">{"".join(f"<li>{e(t)}</li>" for t in D.TONE)}</ul></div>
      <div class="grow" style="display:flex">{img(shot("island"),1000,"섬 지도 (실제 게임 화면)",cls="grow")}</div></div>'''))

    P.append(page("세계", "장소와 빈칸", f'''
    <div class="row" style="flex:1"><div class="grow card"><h2>장소 {tag("관찰")}</h2>{table(["곳","모습과 역할"], [(f"<b>{a}</b>", e(b)) for a, b in D.PLACES], "big k")}</div>
      <div class="card" style="width:600px"><h2>아직 정하지 않은 것 {tag("미정")}</h2><ul>{"".join(f"<li>{e(t)}</li>" for t in D.UNDECIDED)}</ul>
      <p style="margin-top:12px">AI가 만든 시안을 설정으로 굳히기 전에 기획자가 정한다. 요청 목록의 손님·엘프 프롬프트는 외형을 잡기 위한 시안용이다.</p></div></div>'''))

    # 5 화풍
    samples = [art("OtterChef.png"), food("Seaweed_Soup.png"), asset("Sprites/Icons/Ingredients/ing_octo.png"), art("ToolPot.png"), art("RestaurantPrepBackground.jpg"), asset("Sprites/바다.png"), art("TitleBackground.png"), art("SeaTable2.png")]
    cells = "".join(f'<div class="card" style="display:flex;gap:12px;padding:10px 12px"><div style="width:132px;height:132px;flex:none;display:flex">{img(s,300,cls="grow")}</div><div class="small" style="min-width:0"><h2 style="font-size:22px">{e(n)}</h2><b>{e(t)}</b><br>{e(d)}</div></div>'
                    for s, (n, _, t, d) in zip(samples, D.STYLE_FAMILIES))
    P.append(page("화풍", "한 게임 안의 8가지 그림 문법", f'''
    <p>아래 8개 군은 현재 파일에서 확인했다. {tag("관찰")} 한 모델에 모두 넣으면 특징이 섞이기 쉬우므로 <b>군별로 나눠</b> 쓴다. {tag("제안")}</p>
    <div class="grid" style="grid-template-columns:1fr 1fr;grid-auto-rows:minmax(0,1fr)">{cells}</div>'''))

    # 6 색
    grads = "".join(f'<div style="flex:1;text-align:center"><div style="height:104px;border-radius:12px;border:2px solid #4A2E1B;background:linear-gradient({t},{b})"></div><div class="small"><b>{n}</b><br>{e(k)}<br><code>{t} → {b}</code></div></div>' for n, t, b, k in D.TIME_COLORS)
    P.append(page("색", "팔레트", f'''
    <div class="row"><div class="grow card"><h2>UI 키트 {tag("관찰")} <small style="font:16px Malgun Gothic">DayLoopUI.Kit의 실제 값</small></h2><div class="sws" style="grid-template-columns:repeat(4,1fr)">{"".join(swatch(*c) for c in D.UI_COLORS)}</div></div>
    <div class="card" style="width:520px"><h2>해달 {tag("관찰")} <small style="font:16px Malgun Gothic">이미지 측정값</small></h2><div class="sws" style="grid-template-columns:1fr 1fr">{"".join(swatch(*c) for c in D.OTTER_COLORS)}</div></div></div>
    <div class="card"><h2>시간대 {tag("관찰")} <small style="font:16px Malgun Gothic">DialogueBackgroundFactory의 값. 컷신 배경의 기준으로 쓴다</small></h2><div class="row">{grads}</div></div>
    <p class="small"><b>색 규칙 {tag("규칙")}</b> 선은 순수 검정 대신 진갈색. 빨간 차양색 #C8443A는 화면마다 가장 중요한 행동 하나에만 쓴다. 넓은 면은 바다 파랑·모래·나무색이 맡는다.</p>'''))

    # 7 캐릭터 시트
    def strip(names):
        return "".join(img(ch(f"{n}.png"), 380, c) for n, c in names)
    P.append(page("캐릭터", "해달 직원 스프라이트 — 기준 원본", f'''
    <h2>셰프 (주방): 걷기 2프레임 포함 {tag("관찰")}</h2>
    <div class="grid" style="grid-template-columns:repeat(6,1fr)">{strip([("해달_셰프정면","정면"),("해달_셰프정면_걷기1","정면 걷기"),("해달_셰프측면","측면"),("해달_셰프측면_걷기1","측면 걷기"),("해달_셰프후면","후면"),("해달_셰프후면_걷기1","후면 걷기")])}</div>
    <h2>웨이터 (홀): 물 위를 수영으로 이동 {tag("관찰")}</h2>
    <div class="grid" style="grid-template-columns:repeat(6,1fr)">{strip([("해달_웨이터정면","정면"),("해달_웨이터정면_수영1","정면 수영"),("해달_웨이터측면_수영1","측면 수영"),("해달_웨이터후면_수영1","후면 수영"),("Sit_Front","앉기 (손님)"),("Idle_Right","물 위 대기 (손님)")])}</div>'''))

    # 8 규칙
    P.append(page("캐릭터", "해달을 그리는 규칙", f'''
    <div class="row" style="flex:1"><div class="grow card"><h2>생김새 {tag("관찰")}</h2>{table(["부분","기준"], [(f"<b>{a}</b>", e(b)) for a, b in D.OTTER_RULES], "big k")}</div>
    <div style="width:520px;display:flex;flex-direction:column;gap:12px">
      <div class="grow" style="display:flex;gap:10px;min-height:0">{img(ch("해달_웨이터정면.png"),360,"스프라이트: 차분한 털색",cls="grow")}{img(art("OtterServer.png"),360,"초상: 따뜻한 털색",cls="grow")}</div>
      <div class="card small"><b>털색이 두 가지다 {tag("관찰")}</b><br>게임 안 스프라이트는 #C9A08A, 타이틀·직원 초상은 #E0A083이다. 게임 안 캐릭터를 새로 만들 때는 스프라이트 색을 따른다. {tag("제안")}<br>
      <span class="bad">금지</span> 앞머리·볼 털·직선 눈썹을 빼기, 큰 반짝이 눈, 사실적인 수달, 흰 테두리 스티커 효과, 해달을 4등신 이상으로 늘이기.</div></div></div>'''))

    # 9 음식
    fig = "".join(img(food(f), 360, c) for f, c in [("Grilled Mackerel.png","고등어 구이 (사용 중)"),("Seaweed_Soup.png","미역국 (조개 찜 자리에 사용 중)"),("abalone porridge.png","전복죽 (이전 메뉴)"),("Shrimp Sushi.png","새우 초밥 (광어 회 자리에 사용 중)")])
    P.append(page("음식", "음식 그림의 형식", f'''
    <div class="grid" style="grid-template-columns:repeat(4,1fr);flex:1.1">{fig}</div>
    <div class="row"><div class="grow card">{table(["조리법","근거","그릇과 소품"], [(f"<b>{a}</b>", tag(b), e(c)) for a, b, c, _ in D.VESSELS])}</div>
    <div class="card small" style="width:470px"><b>공통 {tag("규칙")}</b><ul><li>약간 위에서 본 시점, 그릇 하나, 나무 받침.</li><li>화면에서 약 30~110px로 보인다. <b>주재료 하나가 크게</b> 읽혀야 한다.</li><li>등급이 높을수록 재료와 고명을 늘린다. 그릇 크기는 그대로 둔다. {tag("제안")}</li></ul></div></div>'''))

    # 10 음식 목록
    def recipe_rows(items):
        return [(f"F-{r[0]:02d}", f"<b>{e(r[1])}</b>", e(D.TOOL_NAMES[r[2]]), f"★{r[3]}", e(r[5]), f'<span class="{"good" if r[7].startswith("기존") else "bad" if r[7].startswith("재") else ""}">{e(r[7])}</span>') for r in items]
    head = ["id", "요리", "도구", "등급", "재료", "그림 상태"]
    P.append(page("음식", "만들어야 할 요리 22종", f'''
    <p class="small">게임의 22개 레시피 중 전용 그림을 쓰는 것은 3개뿐이고, 그중 2개는 다른 음식 그림이다. 나머지는 첫 글자 원으로 표시된다. {tag("관찰")} 출처: <code>RecipeDatabaseSO.asset</code></p>
    <div class="row" style="flex:1"><div class="grow">{table(head, recipe_rows(D.RECIPES[:11]), "nw")}</div><div class="grow">{table(head, recipe_rows(D.RECIPES[11:]), "nw")}</div></div>
    <p class="small">그림 상태가 ‘신규’면 지금 글자 원으로 나온다. 그릇 형식은 9쪽, 프롬프트는 <code>prompts.csv</code>의 같은 id를 쓴다.</p>'''))

    # 11 재료
    cells = []
    for ko, en, f in D.INGREDIENTS:
        if f:
            cells.append(img(asset(f"Sprites/Icons/Ingredients/{f}"), 170, e(ko)))
        else:
            cells.append(f'<figure><div class="frame" style="background:#FFF1E8;border:3px dashed #A3392F;color:#A3392F;font-weight:700">없음</div><figcaption class="bad">{e(ko)} (I-01)</figcaption></figure>')
    P.append(page("재료", "재료 아이콘 — 게임 DB 28종", f'''
    <p class="small">그림은 있지만 <b>DB에 연결되어 있지 않아</b>(<code>_icon</code>이 모두 비어 있다) 상점에는 글자 원으로 나온다. 27종은 <b>새로 만들 필요 없이 연결만 하면 된다</b>. {tag("관찰")} 이 외에 DB에 없는 아이콘 17장이 더 있어 학습 세트에 넣었다.</p>
    <div class="grid" style="grid-template-columns:repeat(10,1fr);grid-auto-rows:minmax(0,1fr)">{"".join(cells)}</div>'''))

    # 12 도구·소품
    P.append(page("소품", "조리도구와 식당 소품", f'''
    <div class="row" style="flex:1"><div class="card" style="width:430px;display:flex;flex-direction:column"><h2>조리도구 {tag("관찰")}</h2><div class="grid" style="grid-template-columns:1fr 1fr">{img(art("ToolPan.png"),256,"프라이팬")}{img(art("ToolBoard.png"),256,"도마")}{img(art("ToolFryer.png"),256,"튀김기")}{img(art("ToolPot.png"),256,"냄비")}</div></div>
    <div class="grow card" style="display:flex;flex-direction:column"><h2>주방 뗏목·경계 {tag("관찰")}</h2><div class="grid" style="grid-template-rows:2fr 1fr">{img(art("KitchenRaft.png"),900,"빨간 차양, 나무 판자, 난간과 조리대")}{img(art("RopePost.png"),260,"나무 기둥과 초록 밧줄")}</div></div>
    <div class="card" style="width:330px;display:flex;flex-direction:column"><h2>돌 가구 (예외) {tag("관찰")}</h2><div class="grid" style="grid-template-rows:1fr 1fr">{img(art("SeaTable4.png"),300,"4인석")}{img(art("StoneSeat.png"),200,"돌 의자")}</div><p class="small" style="margin-top:8px">유일한 픽셀 그림이다. 학습에 넣지 않는다. 다시 그릴지는 <b>미정</b>이다.</p></div></div>'''))

    # 13 배경
    P.append(page("배경", "배경과 시점", f'''
    <div class="grid" style="grid-template-columns:1fr 1fr 1fr;flex:1">{img(art("IslandBackground.png"),760,"E. 섬 지도: 위에서 비스듬히. 손그림 질감")}{img(art("RestaurantPrepBackground.jpg"),760,"E. 식당 준비: 쿼터뷰 근경")}{img(asset("Sprites/바다.png"),760,"F. 영업 바닥: 바로 위에서. 흰 물결 무늬")}</div>
    <div class="row"><div class="grow card small"><b>시점과 구도 {tag("규칙")}</b><ul><li>지역·컷신 배경은 <b>섬 지도와 같은 쿼터뷰</b>와 손그림 질감으로 맞춘다.</li><li>대화·UI가 왼쪽 420px와 아래쪽을 덮으므로 <b>핵심 사물을 가운데·오른쪽 위</b>에 둔다.</li><li>사람·캐릭터·글자를 그려 넣지 않는다. 캐릭터는 게임에서 위에 올린다.</li></ul></div>
    <div class="grow card small"><b>재료 {tag("관찰")}</b><ul><li>나무: 붉은 갈색 판자에 선이 보인다. 지붕: 빨강, 풀색, 회색.</li><li>바다: 청록~파랑. 흰 물결은 굵고 부드럽다.</li><li>모래: 크림~노랑. 초목은 채도 높은 초록 점묘.</li></ul></div></div>'''))

    # 14 영업 화면
    P.append(page("게임 화면", "영업 화면 — 그림이 실제로 쓰이는 크기", f'''
    <div class="row" style="flex:1"><div style="width:1080px;display:flex">{img(shot("operation"),1200,"실제 게임 1920×1080 캡처 (영업 중)",cls="grow")}</div>
    <div class="grow card"><h2>읽어 낼 점 {tag("관찰")}</h2><ul>
    <li>위 가운데 <b>주방 뗏목</b>, 가운데 돌 테이블 두 줄, 아래 밧줄, 그 아래 대기석.</li>
    <li>캐릭터 키는 화면에서 <b>약 80~90px</b>다. 작은 크기에서 윤곽과 옷 색으로 구분돼야 한다.</li>
    <li>말풍선 안 음식은 <b>약 30px</b>다. 단순한 실루엣이 필요하다.</li>
    <li>손님 전부가 웨이터와 같은 그림이다. 조끼를 입은 해달이 손님과 직원 양쪽에 보인다.</li>
    <li>글자 원(‘가’, ‘새’)은 음식 그림이 없다는 표시다.</li></ul></div></div>'''))

    # 15 다른 화면
    P.append(page("게임 화면", "다른 화면들", f'''
    <div class="grid" style="grid-template-columns:repeat(3,1fr);grid-auto-rows:minmax(0,1fr)">
    {img(shot("title"),640,"타이틀")}{img(shot("restaurantPrep"),640,"영업 준비")}{img(shot("menu"),640,"메뉴: 그림 없는 요리는 글자 원")}
    {img(shot("shop"),640,"상점: 재료 아이콘 미연결")}{img(shot("research"),640,"연구 실습: 배경이 단색")}{img(shot("furniture"),640,"가구")}</div>
    <p class="small">캡처용 저장 데이터를 썼으므로 금액·이름 등 수치는 예시다. 게임 코드는 바꾸지 않았다.</p>'''))

    # 16 UI
    icons = sorted(p for p in (ASSETS / "Resources/UIDesign/Art").glob("Icon*.png"))
    icon_cells = "".join(f'<div style="background:#1F7F8A;border-radius:8px;padding:5px;display:flex"><img src="{uri(p, 88, False)}" style="width:62%;margin:auto"></div>' for p in icons)
    P.append(page("UI", "UI 키트와 로고", f'''
    <div class="row" style="flex:1"><div class="grow card"><h2>지금의 UI {tag("관찰")}</h2><ul><li>크림색 패널 + 진갈색 3px 외곽선 + <b>아래쪽 단단한 그림자</b>. 그러데이션이 없다.</li><li>제목은 <b>Jua</b>, 본문은 <b>NanumGothic Bold</b>. 둘 다 OFL 폰트로 프로젝트에 포함돼 있다.</li><li>버튼은 코드로 그린다. <b>AI로 버튼·패널을 만들지 않는다.</b> 필요한 것은 아이콘과 동전 등 그림 요소다. {tag("규칙")}</li><li>선 아이콘은 88px, 흰 선 2.2 규격이다. 스크립트(<code>Docs/build_reference_icons.cjs</code>)로 만들어 AI 대상이 아니다.</li></ul>
    <div style="display:grid;grid-template-columns:repeat(9,1fr);gap:8px;margin-top:14px">{icon_cells}</div></div>
    <div class="card" style="width:560px;display:flex;flex-direction:column;gap:10px"><h2>로고 {tag("규칙")}</h2><div style="flex:1;display:flex;min-height:0">{img(art("Logo.png"),520,cls="grow")}</div><p class="small">글자가 든 그림은 AI에 맡기지 않는다. 한글이 깨지기 쉽다. 재료 라벨(‘소금’, ‘꿀’)도 빈 라벨로 받아 게임 폰트로 얹는다. 나무 간판, 물결, 생선, 불가사리, 뒤집개가 로고의 구성 요소다.</p></div></div>'''))

    # 17-18 요청
    pr = D.REQUESTS
    rows = [("F-01~22", "P1·P2", "요리 22종 (F-07 제외 21종)", "10쪽 표 참고", "1024×1024 흰 배경 → 누끼")]
    rows += [(r[0], r[1], f"<b>{e(r[3])}</b>", e(r[4]), e(r[5])) for r in pr if r[1] == "P1"]
    P.append(page("요청", "제작 요청 목록 ① 먼저 할 것", f'''
    <p class="small">게임에서 지금 비어 있거나 임시로 때운 곳만 골랐다. 우선순위는 <b>제안</b>이다. {tag("제안")} 전체 문장은 <code>prompts.csv</code>에 있다.</p>
    {table(["id","우선","대상","왜 필요한가","규격"], rows, "big k")}
    <div class="card"><h2>AI가 아니라 코드 작업이 필요한 것</h2><ul><li>재료 아이콘 27종을 DB에 연결한다. <code>DesignArt.Food()</code>는 요리 3종만 이름으로 찾으므로 새 음식을 연결하려면 수정이 필요하다.</li><li>손님 종족은 주문 선호에만 쓰이고 외형을 바꾸는 코드가 없다. 종족 그림을 넣으려면 스폰 코드도 바꿔야 한다.</li></ul></div>'''))
    rows = [(r[0], r[1], f"<b>{e(r[3])}</b>", e(r[4]), e(r[5])) for r in pr if r[1] != "P1"]
    P.append(page("요청", "제작 요청 목록 ② 다음에 할 것", table(["id","우선","대상","왜 필요한가","규격"], rows, "big k")))

    # 19 프롬프트 원칙
    P.append(page("프롬프트", "군별 프롬프트 틀", f'''
    <p class="small">{{주제}} 자리에 <code>prompts.csv</code>의 묘사가 들어간다. 이미지 생성용이라 영어로 적었다. 결과를 보며 조정한다. 투명 배경이 나오지 않으면 단색 배경으로 받아 배경을 지운다. {tag("제안")}</p>
    <div class="row"><div class="grow"><h2>캐릭터</h2>{pill("{subject}, " + CHAR_STYLE)}<h2>음식</h2>{pill("{dish}, served in a {vessel}, " + FOOD_STYLE)}<h2>재료·아이콘</h2>{pill("{item}, " + ICON_STYLE)}</div>
    <div class="grow"><h2>배경</h2>{pill("{scene}, " + BG_STYLE)}<h2>소품</h2>{pill("{prop}, " + PROP_STYLE)}<h2>제외 (공통)</h2>{pill(D.NEGATIVE_COMMON)}<h2>제외 (배경)</h2>{pill(NEG_BG)}</div></div>'''))

    # 20 프롬프트 예
    samp = {r[0]: r for r in all_prompts()}
    P.append(page("프롬프트", "완성 문장 예시", "".join(f'<div><h2>{e(k)} · {e(samp[k][3])}</h2>{pill(samp[k][6])}</div>' for k in ["F-03", "F-22", "C-01", "E-07"])))

    # 21 학습 세트
    def setrow(name):
        m = sets[name]
        return (f"<code>{name}</code>", e(m["title"]), str(len(m["items"])), {"train": '<span class="good">학습 후보</span>', "ref": "이미지 참조"}[m["use"]], e(m["note"]))
    rows = [setrow(n) for n in sorted(sets)]
    rows.append(("<code>07_ingame</code>", "실제 게임 화면", str(len(list((REFS / "07_ingame").glob("*.jpg")))), "맥락 설명", "학습에 넣지 않는다. UI와 글자가 들어 있다."))
    P.append(page("자료", "시나리오 AI에 올릴 묶음", f'''
    {table(["폴더","내용","장수","용도","메모"], rows)}
    <div class="row"><div class="grow card"><h2>진행 순서 {tag("제안")}</h2><ul>
    <li><b>모델을 군별로 따로</b> 만든다. 먼저 해달 캐릭터, 그다음 재료 아이콘.</li>
    <li>음식은 4장뿐이라 학습 대신 이미지 참조로 시작한다. 합격 그림이 쌓이면 음식 모델을 학습한다.</li>
    <li>작은 묶음으로 시험한다. F-03 조개 찜 1장을 먼저 합격시킨 뒤 나머지를 만든다.</li></ul>
    <p style="margin-top:8px">외부 접속 없이 작성해서 <b>시나리오의 현재 메뉴 이름, 장수 제한, 크레딧은 확인하지 못했다.</b> 업로드 전에 화면에서 확인한다.</p></div>
    <div class="grow card"><h2>권리 확인 {tag("규칙")}</h2><p>일부 파일은 이름으로 보아 ChatGPT·Gemini로 만든 것이다. 외부 서비스에 올리기 전에 팀 그림의 권리와 해당 서비스의 약관을 확인한다. 다음 쪽의 제외 목록은 올리지 않는다.</p>
    <h2 style="margin-top:10px">이야기를 쓰는 AI에 줄 때</h2><p>3~4쪽과 <code>{BOOK}.md</code>의 ‘세계’ 절이면 충분하다. 미정 항목은 임의로 확정하지 말고 제안으로 받는다.</p></div></div>'''))

    # 22 제외 + 체크리스트
    P.append(page("자료", "올리지 말 것, 받기 전 볼 것", f'''
    <div class="row" style="flex:1"><div class="grow card"><h2>제외 목록 {tag("관찰")}</h2>{table(["파일","이유"], [(f'<b>{e(a)}</b>', e(b)) for a, b in D.EXCLUDED])}</div>
    <div class="card" style="width:520px"><h2>합격 체크리스트 {tag("규칙")}</h2><ul>
    <li>기존 해달 옆에 놓아도 선 굵기와 색이 어긋나지 않는다.</li><li>선은 진갈색이고 순수 검정이 아니다.</li><li>64px로 줄여도 무엇인지 보인다.</li><li>글자, 워터마크, 서명이 없다.</li><li>손가락, 다리, 꼬리 수가 맞다.</li><li>요리의 주재료가 레시피와 맞다.</li><li>투명 배경의 가장자리에 흰 테두리가 남지 않는다.</li></ul>
    <h2 style="margin-top:10px">납품 규격 {tag("제안")}</h2><ul><li>PNG. 파일명은 <code>{{id}}_{{english}}_v01.png</code>.</li><li>캐릭터: 1024px, 발이 아래 여백 2% 이내.</li><li>음식·아이콘: 1024px, 여백 약 5%.</li><li>배경: 1920×1080 이상.</li><li>합격본은 <code>prompts.csv</code>의 문장과 함께 보관한다.</li></ul></div></div>'''))
    return P


def write_html(pages, path):
    css = CSS.replace("%JUA%", font_uri("Jua-Regular.ttf"))
    script = ("<script>const o=[...document.querySelectorAll('.page')].map((p,i)=>{const b=p.querySelector('.body');"
              "let over=b.scrollHeight>b.clientHeight+1||b.scrollWidth>b.clientWidth+1;let items=[];"
              "const r=b.getBoundingClientRect();b.querySelectorAll('.card,table,pre,figure,p,ul').forEach(x=>{const q=x.getBoundingClientRect();"
              "if(q.bottom>r.bottom+1||q.right>r.right+1)items.push(x.tagName)});"
              "b.querySelectorAll('.card').forEach(c=>{if(c.scrollHeight>c.clientHeight+2)items.push('CARD-CLIP')});"
              "return (over||items.length)?(i+1)+':'+items.slice(0,6).join(','):''}).filter(Boolean);"
              "document.documentElement.setAttribute('data-overflow',o.join(' | ')||'none');"
              "document.documentElement.setAttribute('data-pages',document.querySelectorAll('.page').length);</script>")
    path.write_text(f'<!doctype html><html lang="ko"><head><meta charset="utf-8"><title>해달 식당 아트북</title><style>{css}</style></head><body>{"".join(pages)}{script}</body></html>', encoding="utf-8")


def browser():
    for c in [r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", r"C:\Program Files\Google\Chrome\Application\chrome.exe"]:
        if os.path.exists(c):
            return c
    sys.exit("PDF를 만들 Edge나 Chrome을 찾지 못했습니다.")


def headless(*args):
    # 로컬 파일만 연다. 백그라운드 네트워크 기능을 끈다.
    with tempfile.TemporaryDirectory() as profile:
        cmd = [browser(), "--headless=new", "--disable-gpu", "--no-first-run", "--disable-background-networking", "--disable-component-update",
               "--disable-sync", "--disable-extensions", "--hide-scrollbars", "--allow-file-access-from-files", f"--user-data-dir={profile}", *args]
        return subprocess.run(cmd, capture_output=True, timeout=300)


def write_markdown(sets):
    L = [f"# 해달 식당 아트북 ({VERSION})", "", "시나리오(Scenario) AI에 전달할 화풍 기준과 제작 요청서의 글 버전이다. 그림이 든 전체본은 `" + BOOK + ".pdf`다.",
         "", "근거 표시: **[관찰]** 현재 파일에서 확인 / **[규칙]** 앞으로 지킬 기준 / **[제안]** 팀 확정 전 / **[미정]** 기획 없음", "",
         "## AI에게 줄 요약", "", "```", D.GAME_DESC_EN + " Match the supplied references exactly: thick dark-brown outlines, flat cel shading, warm seaside colors, cute and calm mood. Keep the sea otter design unchanged. Do not add text or watermarks. Output one subject per image unless the request asks for a sheet.", "```", "",
         "## 세계 [관찰]", ""]
    L += [f"- **{a}**: {b}" for a, b in D.STORY]
    L += ["", "### 장소", ""] + [f"- **{a}**: {b}" for a, b in D.PLACES]
    L += ["", "### 톤 [제안]", ""] + [f"- {t}" for t in D.TONE]
    L += ["", "### 아직 정하지 않은 것 [미정]", ""] + [f"- {t}" for t in D.UNDECIDED]
    L += ["", "## 화풍 군 [관찰]", "", "| 군 | 대상 | 특징 |", "|---|---|---|"] + [f"| {a} | {c} | {d} |" for a, _, c, d in D.STYLE_FAMILIES]
    L += ["", "## 색", "", "| 이름 | 값 | 용도 |", "|---|---|---|"] + [f"| {a} | `{b}` | {c} |" for a, b, c in D.UI_COLORS + D.OTTER_COLORS]
    L += ["", "시간대 그라데이션(위→아래): " + ", ".join(f"{n} `{t}`→`{b}`" for n, t, b, _ in D.TIME_COLORS)]
    L += ["", "## 해달 규칙 [관찰]", ""] + [f"- **{a}**: {b}" for a, b in D.OTTER_RULES]
    L += ["", "## 음식 그릇", "", "| 조리법 | 근거 | 그릇 |", "|---|---|---|"] + [f"| {a} | {b} | {c} |" for a, b, c, _ in D.VESSELS]
    L += ["", "## 프롬프트 틀", "", "캐릭터:", "```", "{subject}, " + CHAR_STYLE, "```", "음식:", "```", "{dish}, served in a {vessel}, " + FOOD_STYLE, "```", "아이콘:", "```", "{item}, " + ICON_STYLE, "```", "배경:", "```", "{scene}, " + BG_STYLE, "```", "소품:", "```", "{prop}, " + PROP_STYLE, "```", "제외(공통):", "```", D.NEGATIVE_COMMON, "```", "제외(배경):", "```", NEG_BG, "```"]
    L += ["", "## 제작 요청", "", "`prompts.csv`와 같은 내용이다.", "", "| id | 우선 | 대상 | 이유 | 규격 |", "|---|---|---|---|---|"]
    L += [f"| {r[0]} | {r[1]} | {r[3]} | {r[4]} | {r[5]} |" for r in all_prompts()]
    L += ["", "## 참조 이미지", "", "`references/manifest.csv`에 파일별 원본 경로·용도·설명이 있다.", "", "| 폴더 | 내용 | 장수 | 용도 |", "|---|---|---|---|"]
    L += [f"| `{n}` | {sets[n]['title']} | {len(sets[n]['items'])} | {'학습 후보' if sets[n]['use']=='train' else '이미지 참조'} |" for n in sorted(sets)]
    L += [f"| `07_ingame` | 실제 게임 화면 | {len(list((REFS / '07_ingame').glob('*.jpg')))} | 맥락 설명(학습 제외) |"]
    L += ["", "## 올리지 말 것", ""] + [f"- **{a}**: {b}" for a, b in D.EXCLUDED]
    L += ["", "시나리오의 현재 메뉴 이름, 장수 제한, 크레딧은 외부 접속 없이 작성해 확인하지 못했다.", ""]
    (OUT / f"{BOOK}.md").write_text("\n".join(L), encoding="utf-8")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--captures")
    ap.add_argument("--preview")
    a = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    sets, rows = export_refs(a.captures)
    write_markdown(sets)
    pages = build_pages(sets)
    html_path = OUT / f"{BOOK}.html"
    write_html(pages, html_path)
    if a.preview:
        pv = Path(a.preview)
        pv.mkdir(parents=True, exist_ok=True)
        r = headless("--virtual-time-budget=15000", "--dump-dom", html_path.as_uri())
        dom = r.stdout.decode("utf-8", "ignore")[:800]
        print("overflow:", re.search(r'data-overflow="([^"]*)"', dom).group(1) if 'data-overflow' in dom else "unknown")
        n = len(pages)
        shot = pv / "all.png"
        headless("--virtual-time-budget=15000", f"--window-size=1600,{900 * n}", f"--screenshot={shot}", html_path.as_uri())
        im = Image.open(shot).convert("RGB")
        for i in range(n):
            im.crop((0, 900 * i, 1600, 900 * (i + 1))).save(pv / f"p{i + 1:02d}.jpg", quality=85)
        print("pages", n, im.size)
    else:
        pdf = OUT / f"{BOOK}.pdf"
        r = headless("--no-pdf-header-footer", "--virtual-time-budget=15000", f"--print-to-pdf={pdf}", html_path.as_uri())
        if not pdf.exists():
            sys.exit(r.stderr.decode("utf-8", "ignore")[-800:])
        data = pdf.read_bytes()
        print("pdf", pdf.stat().st_size // 1024, "KB", "pages", len(re.findall(rb"/Type\s*/Page[^s]", data)))
    print("refs", len(rows), "prompts", len(all_prompts()))


if __name__ == "__main__":
    main()
