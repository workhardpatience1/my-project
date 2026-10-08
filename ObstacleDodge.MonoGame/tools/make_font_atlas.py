#!/usr/bin/env python3
"""Generates the bitmap font atlas used by the game (Shared/Content/font.png + font.txt).

Run once after changing the font or the character set:
    python3 tools/make_font_atlas.py
No MonoGame content pipeline is needed: the game loads the PNG with Texture2D.FromStream.
"""
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Shared", "Content")
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
SIZE = 56          # glyph pixel size in the atlas (the game scales it down)
PAD = 3            # empty pixels around each glyph, so linear filtering does not bleed
ATLAS_W = 1024

# ASCII + the letters Uzbek (Latin) and Russian texts may need.
chars = [chr(c) for c in range(32, 127)]
chars += list("ʻʼ‘’“”«»—–…•×÷©")
chars += list("ЁЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮёйцукенгшщзхъфывапролджэячсмитьбюЎўҚқҒғҲҳ")

font = ImageFont.truetype(FONT, SIZE)
ascent, descent = font.getmetrics()
line_h = ascent + descent

glyphs = []
x, y, row_h = PAD, PAD, 0
for ch in chars:
    l, t, r, b = font.getbbox(ch)
    w, h = max(r - l, 1), max(b - t, 1)
    if x + w + PAD > ATLAS_W:
        x, y = PAD, y + row_h + PAD
        row_h = 0
    glyphs.append((ch, x, y, w, h, l, t, font.getlength(ch)))
    x += w + PAD
    row_h = max(row_h, h)

atlas_h = 1
while atlas_h < y + row_h + PAD:
    atlas_h *= 2

img = Image.new("RGBA", (ATLAS_W, atlas_h), (255, 255, 255, 0))
mask = Image.new("L", (ATLAS_W, atlas_h), 0)
draw = ImageDraw.Draw(mask)
for ch, gx, gy, w, h, l, t, adv in glyphs:
    draw.text((gx - l, gy - t), ch, font=font, fill=255)
img.putalpha(mask)
os.makedirs(OUT, exist_ok=True)
img.save(os.path.join(OUT, "font.png"), optimize=True)

with open(os.path.join(OUT, "font.txt"), "w", encoding="utf-8") as f:
    f.write(f"lineHeight {line_h} base {ascent}\n")
    for ch, gx, gy, w, h, l, t, adv in glyphs:
        # code x y width height xoffset yoffset xadvance
        f.write(f"{ord(ch)} {gx} {gy} {w} {h} {l} {t} {adv:.2f}\n")
print("atlas", ATLAS_W, "x", atlas_h, "glyphs", len(glyphs))
