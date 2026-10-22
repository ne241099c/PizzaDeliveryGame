#!/usr/bin/env python3
"""Original flat art for the shop scene (設計書_画面デザイン 5). Every shape is drawn here from simple
primitives in the UITheme v2 colours and rendered to PNG with headless Chrome (transparent background).

usage: python3 Tools/Art/shop_art.py [name ...]
Writes Assets/Resources/ShopArt/<name>.png at 2x the display size. Re-running reproduces the same files.
"""
import os, subprocess, sys, tempfile, pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = pathlib.Path(os.environ["ART_OUT"]) if "ART_OUT" in os.environ else ROOT / "Assets/Resources/ShopArt"
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"

INK = "#3A2A20"; PAPER = "#FFF6E5"; DEEP = "#F3E3C3"; TOMATO = "#E04B2D"; BASIL = "#4C9A52"
CHEESE = "#F7BE3D"; BERRY = "#B3261E"; BLUE = "#3C79C2"; WOOD = "#C98B5B"; STEEL = "#C8D0D6"; STEEL_D = "#8F9BA5"
DOUGH = "#F2C98A"; CRUST = "#E3A75B"; SAUCE = "#D8432A"; MOZZ = "#FFF3D6"
W = "#FFFFFF"

def icon(body, stroke=W):
    """24-unit icon, white strokes 2.2 with round ends (tinted in Unity)."""
    return (24, 24, f'<g fill="none" stroke="{stroke}" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">{body}</g>')

ICONS = {
    "icon_sun": icon('<circle cx="12" cy="12" r="4.2"/><path d="M12 2.5v2.4M12 19.1v2.4M2.5 12h2.4M19.1 12h2.4M5.3 5.3l1.7 1.7M17 17l1.7 1.7M5.3 18.7 7 17M17 7l1.7-1.7"/>'),
    "icon_clock": icon('<circle cx="12" cy="12" r="9"/><path d="M12 7v5.2l3.4 2.2"/>'),
    "icon_coin": icon('<circle cx="12" cy="12" r="9"/><path d="M8.6 7.6 12 12l3.4-4.4M12 12v5M9 12.2h6M9 14.8h6"/>'),
    "icon_star": icon('<path d="M12 3.2l2.6 5.5 6 .7-4.4 4.1 1.2 5.9L12 16.5l-5.4 2.9 1.2-5.9-4.4-4.1 6-.7z"/>'),
    "icon_star_fill": (24, 24, f'<path d="M12 3.2l2.6 5.5 6 .7-4.4 4.1 1.2 5.9L12 16.5l-5.4 2.9 1.2-5.9-4.4-4.1 6-.7z" fill="{W}" stroke="{W}" stroke-width="1.6" stroke-linejoin="round"/>'),
    "icon_menu": icon('<path d="M6 8h12M6 12h12M6 16h12"/>'),
    "icon_bell": icon('<path d="M6.5 16.5V11a5.5 5.5 0 0 1 11 0v5.5l1.5 1.5H5z"/><path d="M10 20.5a2.2 2.2 0 0 0 4 0"/>'),
    "icon_bike": icon('<circle cx="6" cy="16.5" r="3.3"/><circle cx="18" cy="16.5" r="3.3"/><path d="M6 16.5l4-7h5l3 7M10 9.5h-2.5M15 9.5l-1.6-3.2h2.4"/>'),
    "icon_box": icon('<path d="M12 3 20 7.4v9.2L12 21l-8-4.4V7.4z"/><path d="M4 7.4 12 12l8-4.6M12 12v9"/>'),
    "icon_flame": icon('<path d="M12 21c-3.6 0-6-2.4-6-5.6 0-3.3 2.6-5.1 3.4-8.4 1.9 1.4 2.4 3.3 2.2 4.9 1.1-.9 1.8-2.2 1.9-3.6 2.4 1.9 4.5 4.3 4.5 7.1 0 3.2-2.4 5.6-6 5.6z"/>'),
    "icon_plus": icon('<path d="M12 5v14M5 12h14"/>'),
    "icon_minus": icon('<path d="M5 12h14"/>'),
    "icon_target": icon('<circle cx="12" cy="12" r="7.5"/><circle cx="12" cy="12" r="3.2"/>'),
    "icon_traffic": icon('<path d="M8 19V5M5 8l3-3 3 3M16 5v14M13 16l3 3 3-3"/>'),
    "icon_check": icon('<path d="M5 12.5 10 17 19 7"/>'),
    "icon_play": (24, 24, f'<path d="M7 4.8v14.4L19 12z" fill="{W}" stroke="{W}" stroke-width="1.6" stroke-linejoin="round"/>'),
    "icon_close": icon('<path d="M6 6l12 12M18 6 6 18"/>'),
    "icon_home": icon('<path d="M4 11 12 4l8 7M6.5 9.5V20h11V9.5"/><path d="M10 20v-5h4v5"/>'),
    "icon_hand": icon('<path d="M8 12V5.5a1.5 1.5 0 0 1 3 0V11M11 10V4.5a1.5 1.5 0 0 1 3 0V11M14 10.5V6a1.5 1.5 0 0 1 3 0v8c0 4-2.6 7-6.4 7-2.5 0-4-1-5.3-3L3.6 14.5a1.6 1.6 0 0 1 2.6-1.8L8 15"/>'),
    "icon_map": icon('<path d="M3.5 6.5 9 4l6 2.5 5.5-2.5v13.5L15 20l-6-2.5-5.5 2.5z"/><path d="M9 4v13.5M15 6.5V20"/>'),
    "icon_pizza": icon('<path d="M12 21 4 6.5Q12 2 20 6.5z"/><circle cx="10.5" cy="9" r="1.2"/><circle cx="13.5" cy="12.5" r="1.2"/>'),
    "icon_up": icon('<path d="M12 19V5M6 11l6-6 6 6"/>'),
    "icon_down": icon('<path d="M12 5v14M6 13l6 6 6-6"/>'),
}

def ring(cx, cy, r, fill, stroke=INK, sw=4):
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>'

ART = {
    # ingredient shelf trays (display 120x96)
    "tray_dough": (120, 96, f'<ellipse cx="60" cy="58" rx="46" ry="28" fill="{DOUGH}" stroke="{INK}" stroke-width="4"/><ellipse cx="44" cy="48" rx="14" ry="6" fill="#FFF0D6" opacity=".9"/>'),
    "tray_sauce": (120, 96, f'<rect x="22" y="22" width="76" height="62" rx="12" fill="{TOMATO}" stroke="{INK}" stroke-width="4"/><rect x="16" y="16" width="88" height="18" rx="8" fill="{BERRY}" stroke="{INK}" stroke-width="4"/><path d="M98 32h12" stroke="{INK}" stroke-width="5" stroke-linecap="round"/>'),
    "tray_cheese": (120, 96, f'<rect x="12" y="20" width="96" height="64" rx="12" fill="#FFF6E5" stroke="{INK}" stroke-width="4"/>' + "".join(ring(x, y, 11, CHEESE, INK, 3) for x, y in [(36, 44), (60, 58), (84, 44), (48, 70), (74, 72)])),
    "tray_pepperoni": (120, 96, f'<rect x="12" y="20" width="96" height="64" rx="12" fill="#FFF6E5" stroke="{INK}" stroke-width="4"/>' + "".join(ring(x, y, 12, TOMATO, INK, 3) for x, y in [(38, 46), (64, 40), (86, 58), (52, 66)])),
    "tray_basil": (120, 96, f'<rect x="12" y="20" width="96" height="64" rx="12" fill="#FFF6E5" stroke="{INK}" stroke-width="4"/>' + "".join(f'<ellipse cx="{x}" cy="{y}" rx="15" ry="8" transform="rotate({a} {x} {y})" fill="{BASIL}" stroke="{INK}" stroke-width="3"/>' for x, y, a in [(38, 46, -20), (68, 42, 25), (84, 64, -30), (50, 66, 15)])),
    "tray_shrimp": (120, 96, f'<rect x="12" y="20" width="96" height="64" rx="12" fill="#FFF6E5" stroke="{INK}" stroke-width="4"/>' + "".join(f'<path d="M{x-12} {y} q12 -16 24 0 q-6 -4 -12 4" fill="#F4A58A" stroke="{INK}" stroke-width="3" stroke-linejoin="round"/>' for x, y in [(42, 52), (76, 46), (58, 72)])),
    # pizza layers (display 160x160)
    "pizza_base": (160, 160, f'<circle cx="80" cy="80" r="74" fill="{CRUST}" stroke="{INK}" stroke-width="5"/><circle cx="80" cy="80" r="60" fill="{DOUGH}"/>'),
    "pizza_sauce": (160, 160, f'<circle cx="80" cy="80" r="60" fill="{SAUCE}"/>'),
    "sauce_blob": (40, 40, f'<circle cx="20" cy="20" r="19" fill="{SAUCE}"/>'),
    "pizza_cheese": (160, 160, "".join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="{MOZZ}" opacity=".95"/>' for x, y, rx, ry in [(56, 58, 16, 12), (100, 52, 14, 11), (82, 84, 17, 13), (52, 104, 14, 12), (108, 100, 16, 12), (76, 120, 12, 9), (118, 76, 10, 8), (40, 80, 9, 8)])),
    "pizza_basil": (160, 160, "".join(f'<ellipse cx="{x}" cy="{y}" rx="12" ry="6.5" transform="rotate({a} {x} {y})" fill="{BASIL}" stroke="{INK}" stroke-width="2.5"/>' for x, y, a in [(64, 62, -25), (100, 70, 30), (78, 100, -10), (110, 106, 40), (50, 98, 20)])),
    "pizza_pepperoni": (160, 160, "".join(ring(x, y, 11, BERRY, INK, 2.5) for x, y in [(60, 58), (100, 56), (82, 84), (54, 102), (106, 100), (80, 120)])),
    "tray_squid": (120, 96, f'<rect x="12" y="20" width="96" height="64" rx="12" fill="#FFF6E5" stroke="{INK}" stroke-width="4"/>' + "".join(f'<ellipse cx="{x}" cy="{y}" rx="12" ry="8" fill="none" stroke="{INK}" stroke-width="8"/><ellipse cx="{x}" cy="{y}" rx="12" ry="8" fill="none" stroke="{W}" stroke-width="4"/>' for x, y in [(38, 45), (76, 47), (57, 69)])),
    "pizza_squid": (160, 160, "".join(f'<ellipse cx="{x}" cy="{y}" rx="11" ry="7" fill="none" stroke="{INK}" stroke-width="7"/><ellipse cx="{x}" cy="{y}" rx="11" ry="7" fill="none" stroke="{W}" stroke-width="4"/>' for x, y in [(77, 45), (43, 84), (110, 83), (90, 119)])),
    "pizza_shrimp": (160, 160, "".join(f'<path d="M{x-10} {y} q10 -14 20 0 q-5 -3 -10 3" fill="#F4A58A" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>' for x, y in [(62, 64), (100, 62), (80, 96), (56, 106), (108, 104)])),
    "pizza_burnt": (160, 160, "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#3A2A20" opacity=".55"/>' for x, y, r in [(56, 60, 12), (104, 66, 10), (80, 98, 14), (112, 108, 9), (50, 108, 8)])),
    "ladle": (120, 120, f'<path d="M44 76 L104 16" stroke="{STEEL_D}" stroke-width="12" stroke-linecap="round"/><path d="M44 76 L104 16" stroke="{STEEL}" stroke-width="6" stroke-linecap="round"/>{ring(38, 82, 26, STEEL)}{ring(38, 82, 15, SAUCE, SAUCE, 0)}'),
    # oven (display 300x220) and its fire
    "oven": (300, 220, f'<rect x="6" y="8" width="288" height="206" rx="22" fill="#5A4636" stroke="{INK}" stroke-width="5"/><rect x="26" y="24" width="248" height="150" rx="16" fill="#2C211A" stroke="{INK}" stroke-width="4"/><rect x="26" y="182" width="248" height="18" rx="6" fill="{STEEL}" stroke="{INK}" stroke-width="3"/>'),
    "oven_fire": (248, 150, '<defs><radialGradient id="g" cx=".5" cy=".75" r=".8"><stop offset="0" stop-color="#FFE08A"/><stop offset=".45" stop-color="#F38A3A"/><stop offset="1" stop-color="#B8452A" stop-opacity=".9"/></radialGradient></defs><rect x="0" y="0" width="248" height="150" rx="16" fill="url(#g)"/>'),
    # boxes (display 150x150)
    "box_open": (150, 150, f'<rect x="10" y="10" width="130" height="130" rx="10" fill="#E2B27A" stroke="{INK}" stroke-width="5"/><rect x="22" y="22" width="106" height="106" rx="6" fill="#F3D3A2" stroke="{INK}" stroke-width="3"/>'),
    "box_closed": (150, 150, f'<rect x="10" y="10" width="130" height="130" rx="10" fill="#E2B27A" stroke="{INK}" stroke-width="5"/><path d="M10 40h130" stroke="{INK}" stroke-width="3" opacity=".5"/><rect x="34" y="54" width="82" height="46" rx="6" fill="{PAPER}" stroke="{INK}" stroke-width="3"/>'),
    "box_stack": (150, 120, "".join(f'<rect x="{18+i*4}" y="{70-i*22}" width="110" height="30" rx="6" fill="#E2B27A" stroke="{INK}" stroke-width="4"/>' for i in range(4))),
    # map pins (display 56x72) and the shop badge (display 76x76)
    "pin_drop": (56, 72, f'<path d="M28 68 C14 50 6 40 6 28 a22 22 0 0 1 44 0 c0 12-8 22-22 40z" fill="{TOMATO}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/><circle cx="28" cy="27" r="13" fill="{W}" stroke="{INK}" stroke-width="3"/>'),
    "pin_white": (56, 72, f'<path d="M28 68 C14 50 6 40 6 28 a22 22 0 0 1 44 0 c0 12-8 22-22 40z" fill="{W}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/><circle cx="28" cy="27" r="8" fill="#B9AE98" stroke="{INK}" stroke-width="3"/>'),
    "shop_badge": (76, 76, f'{ring(38, 38, 34, W)}<path d="M18 38 38 22l20 16" fill="none" stroke="{TOMATO}" stroke-width="6" stroke-linejoin="round" stroke-linecap="round"/><path d="M24 34v20h28V34" fill="{TOMATO}" stroke="{INK}" stroke-width="3"/><rect x="34" y="42" width="8" height="12" fill="{W}"/>'),
    # facility pictures (display 96x96)
    "fac_oven": (96, 96, f'<rect x="10" y="18" width="76" height="64" rx="12" fill="#5A4636" stroke="{INK}" stroke-width="4"/><path d="M24 70V52a24 20 0 0 1 48 0v18z" fill="#F38A3A" stroke="{INK}" stroke-width="3"/>'),
    "fac_bike": (96, 96, f'{ring(24, 66, 14, INK)}{ring(74, 66, 14, INK)}<path d="M24 66h30l14-24h12" fill="none" stroke="{INK}" stroke-width="5" stroke-linecap="round" stroke-linejoin="round"/><path d="M30 52h28l10-12" fill="{TOMATO}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/><rect x="14" y="30" width="28" height="22" rx="4" fill="{CHEESE}" stroke="{INK}" stroke-width="3"/>'),
    "fac_ad": (96, 96, f'<path d="M14 40v18l12 2 40 18V20L26 38z" fill="{CHEESE}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/><path d="M26 60l4 20h10l-4-18" fill="{PAPER}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/><path d="M76 36l8-6M78 50h10M76 64l8 6" stroke="{INK}" stroke-width="4" stroke-linecap="round"/>'),
    "fac_shop": (96, 96, f'<rect x="14" y="36" width="68" height="48" rx="4" fill="{PAPER}" stroke="{INK}" stroke-width="4"/><path d="M10 38l8-20h60l8 20z" fill="{TOMATO}" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/><rect x="40" y="56" width="16" height="28" fill="{WOOD}" stroke="{INK}" stroke-width="3"/><path d="M62 52h12M68 46v12" stroke="{BASIL}" stroke-width="4" stroke-linecap="round"/>'),
    "fac_bench": (96, 96, f'<rect x="8" y="34" width="80" height="20" rx="6" fill="{STEEL}" stroke="{INK}" stroke-width="4"/><path d="M18 54v28M78 54v28M18 70h60" stroke="{INK}" stroke-width="4" stroke-linecap="round"/><ellipse cx="48" cy="30" rx="22" ry="8" fill="{DOUGH}" stroke="{INK}" stroke-width="3"/>'),
    "ticket": (150, 110, f'<rect x="6" y="6" width="138" height="98" rx="10" fill="{PAPER}" stroke="{INK}" stroke-width="4"/><rect x="6" y="6" width="138" height="24" rx="10" fill="{TOMATO}" stroke="{INK}" stroke-width="4"/><path d="M22 52h96M22 72h70M22 90h50" stroke="{INK}" stroke-width="4" stroke-linecap="round"/>'),
    # kitchen floor tile (display 64x64; tiled)
    "floor_tile": (64, 64, '<rect width="64" height="64" fill="#E9C99C"/><rect width="32" height="32" fill="#DDB783"/><rect x="32" y="32" width="32" height="32" fill="#DDB783"/>'),
}

ALL = {**{k: v for k, v in ICONS.items()}, **ART}

def render(name):
    w, h, body = ALL[name]
    scale = 4 if name.startswith("icon_") else 2
    pw, ph = w * scale, h * scale
    html = f'<!doctype html><html><body style="margin:0;background:transparent"><svg xmlns="http://www.w3.org/2000/svg" width="{pw}" height="{ph}" viewBox="0 0 {w} {h}">{body}</svg></body></html>'
    with tempfile.TemporaryDirectory() as tmp:
        page = pathlib.Path(tmp) / "art.html"; page.write_text(html)
        out = OUT / f"{name}.png"
        subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                        f"--window-size={pw},{ph}", "--default-background-color=00000000", f"--screenshot={out}", page.as_uri()],
                       check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return out

if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    names = sys.argv[1:] or list(ALL)
    for n in names:
        print(render(n).name)
