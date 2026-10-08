#!/usr/bin/env python3
"""Draws the app icon (Dodgy the capsule between two walls) for every Android density.

    python3 tools/make_icons.py
"""
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
RES = os.path.join(HERE, "..", "ObstacleDodge.Android", "Resources")
S = 1024  # draw big, then downscale for smooth edges


def draw_icon():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # background: rounded square with a vertical sky gradient
    bg = Image.new("RGBA", (S, S))
    bd = ImageDraw.Draw(bg)
    for y in range(S):
        t = y / S
        bd.line([(0, y), (S, y)], fill=(int(70 + 40 * t), int(170 - 30 * t), int(240 - 60 * t), 255))
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=220, fill=255)
    img.paste(bg, (0, 0), mask)
    # ground
    d.rounded_rectangle([0, 700, S - 1, S - 1], radius=220, fill=(76, 175, 80, 255))
    d.rectangle([0, 700, S - 1, 860], fill=(76, 175, 80, 255))
    # two orange walls
    d.rounded_rectangle([90, 380, 300, 760], radius=30, fill=(255, 140, 0, 255), outline=(190, 90, 0, 255), width=14)
    d.rounded_rectangle([724, 300, 934, 760], radius=30, fill=(255, 140, 0, 255), outline=(190, 90, 0, 255), width=14)
    # shadow under Dodgy
    d.ellipse([370, 760, 654, 830], fill=(0, 0, 0, 70))
    # Dodgy: a blue capsule
    d.rounded_rectangle([382, 250, 642, 800], radius=130, fill=(33, 150, 243, 255), outline=(13, 71, 161, 255), width=16)
    # eyes
    for ex in (455, 570):
        d.ellipse([ex - 38, 380, ex + 38, 470], fill=(255, 255, 255, 255))
        d.ellipse([ex - 18, 405, ex + 18, 460], fill=(20, 20, 20, 255))
    # smile
    d.arc([450, 450, 575, 560], start=20, end=160, fill=(13, 71, 161, 255), width=14)
    return img


def main():
    icon = draw_icon()
    sizes = {"mipmap-mdpi": 48, "mipmap-hdpi": 72, "mipmap-xhdpi": 96, "mipmap-xxhdpi": 144, "mipmap-xxxhdpi": 192}
    for folder, px in sizes.items():
        path = os.path.join(RES, folder)
        os.makedirs(path, exist_ok=True)
        icon.resize((px, px), Image.LANCZOS).save(os.path.join(path, "appicon.png"))
    store = os.path.join(HERE, "..", "store")
    os.makedirs(store, exist_ok=True)
    icon.resize((512, 512), Image.LANCZOS).save(os.path.join(store, "icon-512.png"))
    print("icons written")


if __name__ == "__main__":
    main()
