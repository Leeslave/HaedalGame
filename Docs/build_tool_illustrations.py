"""첫 조리도구 선택 창의 도구 그림 4종(프라이팬·도마·튀김기·냄비)을 코드로 그린다.
굵은 진갈색 외곽선 + 단순 명암의 카툰 스타일. 1024px로 그린 뒤 256px로 줄여 Assets/Resources/UIDesign/Art/Tool*.png로 저장한다.
실행: python Docs/build_tool_illustrations.py
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import distance_transform_edt

N = 1024
OUT = Path(__file__).resolve().parents[1] / 'Assets/Resources/UIDesign/Art'
INK = (0x2B, 0x1D, 0x14)
STROKE = 20
yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))


class Canvas:
    def __init__(self):
        self.rgb = np.zeros((N, N, 3), np.float32)
        self.a = np.zeros((N, N), np.float32)

    def paint(self, mask, color, alpha=1.0):
        m = mask.astype(np.float32) * alpha
        c = np.asarray(color, np.float32)
        if c.ndim == 1:
            c = np.broadcast_to(c, (N, N, 3))
        self.rgb = self.rgb * (1 - m[..., None]) + c * m[..., None]
        self.a = self.a + m * (1 - self.a)

    def part(self, mask, fill, stroke=STROKE):
        """외곽선(둥글게 부풀린 모양)을 먼저 깔고 그 위에 칠한다."""
        if stroke > 0:
            self.paint(distance_transform_edt(~mask) <= stroke, INK)
        self.paint(mask, fill)

    def save(self, name):
        img = Image.fromarray(np.dstack([self.rgb, self.a * 255]).clip(0, 255).astype(np.uint8), 'RGBA')
        img = img.convert('RGBa').resize((256, 256), Image.LANCZOS).convert('RGBA')
        img.save(OUT / f'Tool{name}.png')


def _mask(draw_fn):
    img = Image.new('L', (N, N), 0)
    draw_fn(ImageDraw.Draw(img))
    return np.asarray(img) > 127


def poly(points):
    return _mask(lambda d: d.polygon([tuple(map(float, p)) for p in points], fill=255))


def ellipse(cx, cy, rx, ry):
    return _mask(lambda d: d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=255))


def rrect(x0, y0, x1, y1, r):
    return _mask(lambda d: d.rounded_rectangle((x0, y0, x1, y1), r, fill=255))


def capsule(p0, p1, w):
    def draw(d):
        d.line([tuple(p0), tuple(p1)], fill=255, width=int(w))
        for p in (p0, p1):
            d.ellipse((p[0] - w / 2, p[1] - w / 2, p[0] + w / 2, p[1] + w / 2), fill=255)
    return _mask(draw)


def lines(segments, w):
    def draw(d):
        for a, b in segments:
            d.line([tuple(a), tuple(b)], fill=255, width=int(w))
    return _mask(draw)


def arc(cx, cy, rx, ry, start, end, w):
    return _mask(lambda d: d.arc((cx - rx, cy - ry, cx + rx, cy + ry), start, end, fill=255, width=int(w)))


def rotate(mask, angle, center):
    img = Image.fromarray(mask.astype(np.uint8) * 255).rotate(angle, Image.BICUBIC, center=center)
    return np.asarray(img) > 127


def grad(c0, c1, p0, p1):
    """p0에서 c0, p1에서 c1인 직선 그러데이션."""
    c0, c1 = np.array(hexc(c0), np.float32), np.array(hexc(c1), np.float32)
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    t = ((xx - p0[0]) * dx + (yy - p0[1]) * dy) / (dx * dx + dy * dy)
    t = t.clip(0, 1)[..., None]
    return c0 * (1 - t) + c1 * t


def along(a, b):
    """a→b 축 기준 (u: 0~1, v: 픽셀) 좌표를 화면 좌표로 바꾸는 함수."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a
    n = np.array([-d[1], d[0]]) / np.linalg.norm(d)
    return lambda u, v: tuple(a + d * u + n * v)


def pan():
    c = Canvas()
    # 손잡이(뒤)
    c.part(capsule((700, 470), (950, 270), 100), grad('#D98A48', '#A55A28', (820, 330), (870, 420)))
    c.paint(capsule((735, 445), (930, 290), 26), hexc('#E9A56A'), .8)
    c.part(ellipse(905, 305, 22, 22), hexc('#5A3420'), stroke=10)
    c.part(capsule((680, 490), (775, 420), 78), grad('#B4B9C1', '#6F757F', (720, 430), (740, 480)))
    # 몸통 옆면 + 테두리 + 안쪽
    body = ellipse(430, 650, 340, 235) | rrect(90, 560, 770, 650, 0)
    c.part(body, grad('#3B3C43', '#1C1D21', (430, 560), (430, 880)))
    c.part(ellipse(430, 560, 345, 235), grad('#4A4C55', '#2A2B31', (200, 380), (650, 780)))
    inner = ellipse(430, 568, 290, 190)
    c.part(inner, grad('#24252B', '#34363E', (430, 380), (430, 760)), stroke=12)
    c.paint(arc(430, 568, 245, 155, 200, 265, 26) & inner, hexc('#7C808B'), .85)
    c.paint(arc(430, 568, 245, 155, 275, 290, 26) & inner, hexc('#7C808B'), .6)
    c.save('Pan')


def board():
    c = Canvas()
    centre = (500, 590)
    top = rotate(rrect(140, 380, 860, 760, 70), 12, centre)
    side = rotate(rrect(140, 425, 860, 805, 70), 12, centre)
    c.part(side | top, hexc('#A85C28'))
    c.part(top, grad('#F0B06C', '#D18342', (300, 380), (700, 800)), stroke=14)
    for i, (y, s, e) in enumerate([(520, 200, 330), (600, 190, 340), (690, 205, 320)]):
        c.paint(rotate(arc(440 + i * 40, y, 300, 60, s, e, 9), 12, centre) & top, hexc('#BE6F33'), .7)
    c.part(rotate(ellipse(790, 470, 30, 30), 12, centre), hexc('#7A4220'), stroke=10)
    # 칼: 칼끝(왼쪽 위) → 손잡이(오른쪽 아래)
    k = along((250, 300), (830, 650))
    blade = poly([k(0, 0), k(.07, -48), k(.6, -52), k(.6, 66), k(.3, 70), k(.12, 46)])
    c.part(blade, grad('#F2F5F8', '#A6AEB8', k(.3, -50), k(.3, 70)), stroke=16)
    c.paint(poly([k(.1, -40), k(.58, -42), k(.58, -18), k(.12, -20)]) & blade, hexc('#FFFFFF'), .55)
    c.part(poly([k(.6, -60), k(.65, -60), k(.65, 74), k(.6, 74)]), hexc('#8C939D'), stroke=14)
    handle = capsule(k(.68, 7), k(.98, 7), 108)
    c.part(handle, grad('#5B4033', '#2F211B', k(.8, -50), k(.8, 60)), stroke=16)
    for u in (.75, .9):
        c.part(ellipse(*k(u, 7), 15, 15), hexc('#E7D3A6'), stroke=7)
    c.save('Board')


def fryer():
    c = Canvas()
    # 손잡이
    c.part(capsule((720, 380), (835, 285), 34), hexc('#8C939D'))
    c.part(capsule((820, 300), (965, 175), 90), grad('#3F3F46', '#1C1C20', (880, 210), (910, 260)))
    c.paint(capsule((850, 270), (950, 185), 20), hexc('#6C6D76'), .8)
    front = poly([(170, 440), (640, 440), (610, 830), (200, 830)]) | rrect(200, 700, 610, 840, 60)
    right = poly([(640, 440), (800, 330), (775, 700), (610, 830)])
    rim = poly([(170, 440), (640, 440), (800, 330), (340, 330)])
    c.part(rim, hexc('#23262C'))
    # 안쪽 뒷벽 그물
    c.paint(lines([((x, 335), (x + 10, 440)) for x in range(380, 800, 58)], 10) & rim, hexc('#6C737E'))
    c.part(right, hexc('#3C4149'))
    c.paint(lines([((640 + i * 40, 440 - i * 27), (610 + i * 41, 830 - i * 32)) for i in range(1, 5)]
                  + [((640, 440 + j * 78), (800, 330 + j * 74)) for j in range(1, 6)], 11) & right, hexc('#7F8893'))
    c.part(front, hexc('#4A5059'))
    c.paint(lines([((170 + i * 67, 440), (200 + i * 59, 840)) for i in range(1, 7)]
                  + [((150, 440 + j * 72), (660, 440 + j * 72)) for j in range(1, 6)], 12) & front, hexc('#9EA6B1'))
    # 테두리 철사
    c.part(lines([((170, 440), (640, 440)), ((640, 440), (800, 330))], 34) | capsule((170, 440), (640, 440), 34)
           | capsule((640, 440), (800, 330), 34), hexc('#A9B0BA'), stroke=12)
    c.part(capsule((340, 330), (800, 330), 22) | capsule((170, 440), (340, 330), 22), hexc('#8C939D'), stroke=10)
    c.save('Fryer')


def pot():
    c = Canvas()
    # 옆 손잡이
    for x0, x1 in ((120, 250), (780, 910)):
        c.part(rrect(x0, 520, x1, 600, 34), hexc('#34353B'))
    body = rrect(220, 460, 810, 830, 90) | rrect(220, 460, 810, 700, 0)
    # 왼쪽 가장자리는 살짝 어둡게, 가운데에서 밝다가 오른쪽으로 어두워지는 금속 명암
    c.part(body, np.minimum(grad('#E2E6EB', '#858C96', (380, 0), (810, 0)), grad('#A3AAB4', '#E2E6EB', (220, 0), (360, 0))))
    c.paint(rrect(300, 520, 345, 780, 22) & body, hexc('#FFFFFF'), .55)
    c.paint(rrect(220, 640, 810, 668, 0) & body, hexc('#8A919B'), .5)
    # 뚜껑
    c.part(ellipse(515, 455, 315, 72), grad('#C4CAD2', '#7E858F', (515, 400), (515, 530)))
    dome = ellipse(515, 450, 270, 175) & (yy < 455)
    c.part(dome | ellipse(515, 450, 270, 50), grad('#F1F4F7', '#A3AAB4', (360, 300), (680, 470)))
    c.paint(arc(515, 452, 215, 128, 205, 250, 24) & dome, hexc('#FFFFFF'), .75)
    c.part(rrect(492, 250, 538, 300, 10), hexc('#5A5B62'), stroke=14)
    c.part(rrect(440, 215, 590, 268, 26), grad('#4A4B53', '#24252A', (515, 215), (515, 268)))
    c.save('Pot')


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    for draw in (pan, board, fryer, pot):
        draw()
    print('saved', ', '.join(f'Tool{n}.png' for n in ('Pan', 'Board', 'Fryer', 'Pot')))
