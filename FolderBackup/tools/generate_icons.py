"""Draws the FolderBackup icons (a gradient folder with a circular "sync" arrow).

Each size is rendered separately at 8x and scaled down, with stroke widths clamped so the
16 px tray icon stays legible. Requires Pillow:  pip install pillow
Usage:  python tools/generate_icons.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw

SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
SUPERSAMPLE = 8
OUTPUT = Path(__file__).resolve().parent.parent / "src" / "FolderBackup.App" / "Resources"

BACK = (30, 64, 175)            # folder back / tab
FRONT_TOP = (56, 189, 248)      # gradient start
FRONT_BOTTOM = (37, 99, 235)    # gradient end
OUTLINE = (23, 37, 84)
ARROW = (255, 255, 255)
BADGE = (245, 158, 11)          # amber "busy" dot


def gradient(size, top, bottom):
    image = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(image)
    for y in range(size):
        t = y / max(size - 1, 1)
        draw.line([(0, y), (size, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    return image


def render(size, busy):
    r = size * SUPERSAMPLE
    px = SUPERSAMPLE  # one target pixel at render scale
    image = Image.new("RGBA", (r, r), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    outline = max(round(0.035 * r), px)
    radius = max(round(0.09 * r), px)

    # Folder back with tab
    draw.rounded_rectangle([0.04 * r, 0.10 * r, 0.50 * r, 0.40 * r], radius, fill=BACK, outline=OUTLINE, width=outline)
    draw.rounded_rectangle([0.04 * r, 0.20 * r, 0.96 * r, 0.90 * r], radius, fill=BACK, outline=OUTLINE, width=outline)

    # Folder front with gradient
    front = [0.04 * r, 0.30 * r, 0.96 * r, 0.90 * r]
    mask = Image.new("L", (r, r), 0)
    ImageDraw.Draw(mask).rounded_rectangle(front, radius, fill=255)
    image.paste(gradient(r, FRONT_TOP, FRONT_BOTTOM), (0, 0), mask)
    draw.rounded_rectangle(front, radius, outline=OUTLINE, width=outline)

    # Circular arrow: arc clockwise from 330 deg round to 270 deg, arrowhead at the top pointing right
    cx, cy = 0.50 * r, 0.60 * r
    small = size <= 24  # tray sizes: bigger arrow, so it doesn't turn into a blob
    ring = (0.215 if small else 0.19) * r
    stroke = max((0.09 if small else 0.075) * r, 1.6 * px)
    start, end = 330, 265
    draw.arc([cx - ring, cy - ring, cx + ring, cy + ring], start, end, fill=ARROW, width=round(stroke))

    angle = math.radians(end)
    ex, ey = cx + ring * math.cos(angle), cy + ring * math.sin(angle)
    tx, ty = -math.sin(angle), math.cos(angle)       # clockwise tangent (y points down)
    nx, ny = math.cos(angle), math.sin(angle)        # outward normal
    half_width, length = stroke * 1.45, stroke * 1.7
    mx, my = ex - nx * stroke / 2, ey - ny * stroke / 2  # centre line of the stroke
    draw.polygon(
        [
            (mx + nx * half_width, my + ny * half_width),
            (mx - nx * half_width, my - ny * half_width),
            (mx + tx * length, my + ty * length),
        ],
        fill=ARROW,
    )

    if busy:
        dot = (0.42 if size <= 24 else 0.36) * r
        x0, y0 = r - dot - 0.01 * r, r - dot - 0.01 * r
        draw.ellipse([x0, y0, x0 + dot, y0 + dot], fill=BADGE, outline=(255, 255, 255), width=max(round(0.05 * r), px))

    return image.resize((size, size), Image.LANCZOS)


def save_icon(file_name, busy):
    images = [render(size, busy) for size in SIZES]
    largest = images[-1]
    largest.save(OUTPUT / file_name, format="ICO", sizes=[(s, s) for s in SIZES],
                 append_images=images[:-1], bitmap_format="bmp")
    return images


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    normal = save_icon("FolderBackup.ico", busy=False)
    busy = save_icon("FolderBackupBusy.ico", busy=True)

    # Preview sheet: every size, normal and busy, on light and dark backgrounds
    gap = 16
    width = sum(SIZES) + gap * (len(SIZES) + 1)
    sheet = Image.new("RGBA", (width, (256 + gap * 2) * 2), (0, 0, 0, 0))
    for row, (background, icons) in enumerate([((243, 244, 246), normal), ((32, 32, 36), busy)]):
        band = Image.new("RGBA", (width, 256 + gap * 2), background + (255,))
        x = gap
        for size, icon in zip(SIZES, icons):
            band.alpha_composite(icon, (x, gap + 256 - size))
            x += size + gap
        sheet.paste(band, (0, row * (256 + gap * 2)))
    sheet.save(Path(__file__).resolve().parent / "icon-preview.png")
    print("Icons written to", OUTPUT)
