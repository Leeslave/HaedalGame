# 해달_웨이터정면.png(우리 웨이터 해달 정면)에서 서 있는 발을 지우고, 앞으로 뻗은 발바닥을 그려 앉은 자세로 만든다.
import glob, os, sys, unicodedata
from PIL import Image, ImageDraw
CHAR, out = sys.argv[1], sys.argv[2]
def find(name):
    return [p for p in glob.glob(os.path.join(CHAR,'*')) if unicodedata.normalize('NFC',os.path.basename(p))==name][0]
src = Image.open(find('해달_웨이터정면.png')).convert('RGBA')
W, H = src.size
LINE = (60, 21, 7, 255)
FOOT = (113, 73, 56, 255)
PAD = (201, 160, 138, 255)
BODY_END = 963   # 배 아래 테두리까지만 남긴다 (그 아래는 서 있는 발)

body = src.copy()
ImageDraw.Draw(body).rectangle((0, BODY_END, W, H), fill=(0, 0, 0, 0))

# 발바닥: 4배 크기로 그린 뒤 줄여 가장자리를 부드럽게
K = 4
feet = Image.new('RGBA', (W * K, H * K), (0, 0, 0, 0))
d = ImageDraw.Draw(feet)
def foot(cx, cy, tilt):
    layer = Image.new('RGBA', (300 * K, 300 * K), (0, 0, 0, 0))
    g = ImageDraw.Draw(layer)
    c = 150 * K
    rx, ry = 70 * K, 56 * K
    g.ellipse((c - rx, c - ry, c + rx, c + ry), fill=LINE)
    o = 13 * K
    g.ellipse((c - rx + o, c - ry + o, c + rx - o, c + ry - o), fill=FOOT)
    # 큰 발바닥 젤리 + 발가락 젤리 셋
    g.ellipse((c - 26 * K, c - 2 * K, c + 26 * K, c + 30 * K), fill=PAD)
    for dx, dy in ((-30, -22), (0, -32), (30, -22)):
        g.ellipse((c + (dx - 11) * K, c + (dy - 10) * K, c + (dx + 11) * K, c + (dy + 10) * K), fill=PAD)
    layer = layer.rotate(tilt, resample=Image.BICUBIC)
    feet.alpha_composite(layer, (int((cx - 150) * K), int((cy - 150) * K)))
foot(392, 955, 12)
foot(632, 955, -12)
feet = feet.resize((W, H), Image.LANCZOS)

sit = body.copy()
sit.alpha_composite(feet)
# 전체를 조금 아래로 내려 다른 손님 스프라이트처럼 캔버스 가운데 근처에 몸이 오게 한다
bbox = sit.getbbox()
canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0))
canvas.alpha_composite(sit.crop(bbox), (bbox[0], H - 24 - (bbox[3] - bbox[1])))
canvas = canvas.crop(canvas.getbbox())  # 프로젝트에는 여백을 잘라 넣는다 (가운데 피벗)
canvas.save(out)
print(bbox, canvas.getbbox())
