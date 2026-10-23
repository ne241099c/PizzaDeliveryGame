#!/usr/bin/env python3
"""Original flat art for the title screen (設計書_画面デザイン D-5a, approved mock Docs/UI_Mock/Title.png).

Dusk shop front, no people. Every layer is drawn in 1920x1080 screen coordinates and cropped to its own rectangle,
so the sky, the town silhouette, the shop, the string lights and the road are separate images that can move or be
replaced independently. Text (logo, PIZZA, OPEN) is not drawn here: it stays as TextMeshPro so the name in GameInfo
can change without new art.

usage: python3 Tools/Art/title_art.py
Writes Assets/Art/Title/<layer>.png, Assets/Art/Title/layers.json (the rectangles the prefab builder uses), the
shared UI sprites Assets/Art/UI/ui_round.png / ui_circle.png and other screens' backgrounds in Assets/Art/Screens. Re-running reproduces the same files.
"""
import json, pathlib, sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from shop_art import render_svg, ROOT, INK, PAPER, DEEP, TOMATO, CHEESE, WOOD  # noqa: E402

OUT = ROOT / "Assets/Art/Title"
UI_OUT = ROOT / "Assets/Art/UI"
CREAM = "#F3E3C3"
GLASS_IN, GLASS_OUT = "#FFF1B8", "#F9C35C"

def stripes():
    """Awning: slanted red/white panels between y 458 and 550."""
    out, top0, top1, bot0, bot1, n = [], 875, 1868, 878, 1836, 8
    for i in range(n):
        tx0, tx1 = top0 + (top1 - top0) * i / n, top0 + (top1 - top0) * (i + 1) / n
        bx0, bx1 = bot0 + (bot1 - bot0) * i / n, bot0 + (bot1 - bot0) * (i + 1) / n
        fill = TOMATO if i % 2 == 0 else "#FFFFFF"
        out.append(f'<path d="M{tx0:.1f} 458 L{tx1:.1f} 458 L{bx1:.1f} 550 L{bx0:.1f} 550 z" fill="{fill}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>')
    return "".join(out)

def pizza(cx, cy, toppings):
    return (f'<circle cx="{cx}" cy="{cy}" r="48" fill="#E9B86B" stroke="{INK}" stroke-width="4"/>'
            f'<circle cx="{cx}" cy="{cy}" r="37" fill="{TOMATO}" stroke="{INK}" stroke-width="3"/>' + toppings)

def box(x, y):
    return f'<rect x="{x}" y="{y}" width="100" height="44" rx="8" fill="#EBD3A3" stroke="{INK}" stroke-width="4"/>'

WIRE = "M-10 116 C 160 168, 380 160, 660 104 S 900 120, 1000 168 L 1660 126 C 1760 136, 1840 156, 1930 170"
BULBS = [(120, 148), (300, 163), (480, 140), (660, 107), (1680, 130), (1800, 158), (1880, 170)]

# name: (x, y, w, h, svg body in screen coordinates). Order = drawing order (back to front).
LAYERS = {
    "title_sky": (0, 0, 1920, 1080,
        '<defs><linearGradient id="s" x1="0" y1="0" x2="0" y2="1">'
        '<stop offset="0" stop-color="#5D4D8D"/><stop offset=".3" stop-color="#B47488"/>'
        '<stop offset=".55" stop-color="#EE9372"/><stop offset=".85" stop-color="#FBBE8E"/><stop offset="1" stop-color="#FBC895"/>'
        '</linearGradient></defs><rect width="1920" height="1080" fill="url(#s)"/>'),
    "title_town": (0, 470, 1920, 230,
        '<g fill="#5A2E5C" opacity=".22">' + "".join(
            f'<rect x="{x}" y="{y}" width="{w}" height="{690 - y}"/>' for x, y, w in
            [(0, 560, 120), (120, 490, 90), (210, 580, 110), (320, 490, 100), (420, 620, 120), (540, 540, 110),
             (650, 600, 140), (790, 560, 90), (880, 520, 120), (1000, 600, 160), (1160, 540, 120), (1280, 590, 150),
             (1430, 510, 110), (1540, 580, 140), (1680, 530, 120), (1800, 600, 120)]) + '</g>'),
    "title_road": (0, 916, 1920, 164,
        f'<rect x="-4" y="918" width="1928" height="40" fill="#EAD8BD" stroke="{INK}" stroke-width="4"/>'
        f'<rect x="0" y="960" width="1920" height="120" fill="#8A7867"/>'
        f'<line x1="0" y1="960" x2="1920" y2="960" stroke="{INK}" stroke-width="4"/>'
        + "".join(f'<rect x="{x}" y="1004" width="70" height="9" fill="{PAPER}"/>' for x in range(0, 1920, 124))),
    "title_shop": (836, 134, 1068, 790,
        # sign board and the pizza peeking above the roof
        f'<ellipse cx="1560" cy="300" rx="105" ry="40" fill="#F8D3A8"/>'
        f'<rect x="968" y="138" width="804" height="154" rx="36" fill="{INK}"/>'
        # building, roof band, awning
        f'<rect x="870" y="380" width="1000" height="540" fill="{CREAM}" stroke="{INK}" stroke-width="5"/>'
        f'<rect x="840" y="318" width="1060" height="64" rx="14" fill="{TOMATO}" stroke="{INK}" stroke-width="5"/>'
        + stripes() +
        # door with a lit window and a knob
        f'<rect x="1530" y="580" width="250" height="340" rx="16" fill="{WOOD}" stroke="{INK}" stroke-width="5"/>'
        f'<defs><radialGradient id="d" cx=".5" cy=".5" r=".7"><stop offset="0" stop-color="{GLASS_IN}"/><stop offset="1" stop-color="{GLASS_OUT}"/></radialGradient></defs>'
        f'<rect x="1560" y="620" width="190" height="130" rx="12" fill="url(#d)" stroke="{INK}" stroke-width="4"/>'
        f'<circle cx="1742" cy="780" r="9" fill="{CHEESE}" stroke="{INK}" stroke-width="3"/>'
        # OPEN tag (the word is TextMeshPro)
        f'<rect x="1580" y="485" width="172" height="70" rx="35" fill="{INK}"/>'),
    "title_window": (926, 596, 528, 268,
        f'<defs><radialGradient id="w" cx=".5" cy=".5" r=".75"><stop offset="0" stop-color="{GLASS_IN}"/><stop offset="1" stop-color="{GLASS_OUT}"/></radialGradient></defs>'
        f'<rect x="930" y="600" width="520" height="260" rx="16" fill="url(#w)" stroke="{INK}" stroke-width="5"/>'
        + pizza(1060, 665, "".join(f'<circle cx="{1060 + dx}" cy="{665 + dy}" r="8" fill="#B3261E"/>' for dx, dy in [(-12, -8), (12, -4), (0, 14)]))
        + pizza(1320, 665, "".join(f'<circle cx="{1320 + dx}" cy="{665 + dy}" r="8" fill="#FFFFFF"/>' for dx, dy in [(-12, -8), (12, -4), (0, 14)]))
        + box(1010, 778) + box(1270, 778) + box(1290, 748) +
        f'<path d="M1190 600 V860 M930 730 H1450" stroke="{INK}" stroke-width="5"/>'),
    # The flickering light over the shop window: a soft warm wash, faded in and out by TitleView.
    "title_window_glow": (900, 570, 580, 320,
        '<defs><radialGradient id="g" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#FFF8D8" stop-opacity=".95"/>'
        '<stop offset=".6" stop-color="#FFE7A0" stop-opacity=".45"/><stop offset="1" stop-color="#FFE7A0" stop-opacity="0"/></radialGradient></defs>'
        '<ellipse cx="1190" cy="730" rx="290" ry="160" fill="url(#g)"/>'),
    "title_wire": (0, 60, 1920, 130,
        f'<path d="{WIRE}" fill="none" stroke="{INK}" stroke-width="4" stroke-linecap="round"/>'),
}

# Small sprites placed several times by the builder.
SPRITES = {
    "title_bulb": (40, 40, f'<circle cx="20" cy="20" r="16" fill="#FFE69A" stroke="{INK}" stroke-width="3"/><circle cx="15" cy="15" r="4" fill="#FFFFFF" opacity=".8"/>'),
    "title_bulb_glow": (120, 120, '<defs><radialGradient id="h" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#FFF1B0" stop-opacity=".9"/><stop offset="1" stop-color="#FFE08A" stop-opacity="0"/></radialGradient></defs><circle cx="60" cy="60" r="60" fill="url(#h)"/>'),
}

# Shared UI shapes as assets (same geometry as UiKit's generated ones), so prefabs can reference them.
UI = {
    "ui_round": (128, 128, '<rect width="128" height="128" rx="64" fill="#FFFFFF"/>'),
    "ui_circle": (128, 128, '<circle cx="64" cy="64" r="64" fill="#FFFFFF"/>'),
    # The ▼ marker above bars (potential / ceiling).
    "ui_triangle": (32, 32, '<path d="M0 0 H32 L16 32 z" fill="#FFFFFF"/>'),
}

# Other screens' backgrounds (Assets/Art/Screens): stretched to full screen, so a narrow strip is enough.
SCREENS = {
    # 精算 (4-3): evening sky, light at the top to tomato-orange at the bottom.
    "settlement_sky": (16, 256, '<defs><linearGradient id="e" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FCE3B3"/>'
                       '<stop offset="1" stop-color="#E8855C"/></linearGradient></defs><rect width="16" height="256" fill="url(#e)"/>'),
}

def main():
    manifest = {"layers": [], "bulbs": [{"x": x, "y": y} for x, y in BULBS]}
    for name, (x, y, w, h, body) in LAYERS.items():
        render_svg(body, OUT / f"{name}.png", w, h, 1, f"{x} {y} {w} {h}")
        manifest["layers"].append({"name": name, "x": x, "y": y, "w": w, "h": h}); print(name)
    for name, (w, h, body) in SPRITES.items():
        render_svg(body, OUT / f"{name}.png", w, h, 2); print(name)
    for name, (w, h, body) in UI.items():
        render_svg(body, UI_OUT / f"{name}.png", w, h, 1); print(name)
    for name, (w, h, body) in SCREENS.items():
        render_svg(body, ROOT / "Assets/Art/Screens" / f"{name}.png", w, h, 1); print(name)
    (OUT / "layers.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + "\n")

if __name__ == "__main__":
    main()
