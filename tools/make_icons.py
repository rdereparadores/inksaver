"""Generates the InkSaver .ico files (normal and paused) with Pillow.

Usage: python tools/make_icons.py
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / "src" / "InkSaver" / "Resources"
SIZE = 1024  # supersampled canvas
SIZES = [(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)]


def drop_polygon(cx, cy, w, h, steps=400):
    pts = []
    for i in range(steps):
        t = 2 * math.pi * i / steps
        x = math.sin(t) * math.sin(t / 2) ** 1.3
        y = math.cos(t)
        pts.append((cx + x * w, cy - y * h))
    return pts


def gradient(size, top, bottom):
    img = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(img)
    for y in range(size):
        f = y / (size - 1)
        c = tuple(round(top[i] + (bottom[i] - top[i]) * f) for i in range(3)) + (255,)
        d.line([(0, y), (size, y)], fill=c)
    return img


def make(top, bottom, highlight_alpha):
    mask = Image.new("L", (SIZE, SIZE), 0)
    poly = drop_polygon(SIZE / 2, SIZE * 0.53, SIZE * 0.44, SIZE * 0.45)
    ImageDraw.Draw(mask).polygon(poly, fill=255)

    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    img.paste(gradient(SIZE, top, bottom), (0, 0), mask)

    # Soft highlight to give the drop some volume.
    hl = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(hl).ellipse(
        [SIZE * 0.30, SIZE * 0.50, SIZE * 0.42, SIZE * 0.74], fill=highlight_alpha
    )
    hl = hl.filter(ImageFilter.GaussianBlur(SIZE * 0.02))
    white = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 255))
    img = Image.composite(white, img, hl)
    img.putalpha(mask)
    return img


def save(img, name):
    base = img.resize((256, 256), Image.LANCZOS)
    base.save(OUT / name, format="ICO", sizes=SIZES)
    print("wrote", OUT / name)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    save(make((0, 188, 230), (200, 0, 150), 150), "inksaver.ico")
    save(make((170, 170, 170), (95, 95, 95), 110), "inksaver-paused.ico")
