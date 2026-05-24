"""
One-shot art generator for the Flood Season mod.

Produces two PNGs the mod loads at runtime:

  - flood-icon.png        52x52 — replaces the drought sun icon at the
                                  top-right date panel during a flood.
  - flood-notification.png 757x174 — replaces the dry/badtide banner that
                                     fades in when a hazardous weather
                                     starts. Sized to match the vanilla
                                     hazardous-weather-notification__background
                                     dimensions so positioning stays put.

Re-run with `python assets/generate.py` whenever the art needs a refresh.
Output files are checked in so the mod doesn't need Python to build.

Pure PIL — no external assets, no fonts beyond PIL's default. The drawings
are deliberately simple flat-graphic style to match Timberborn's stylised
UI iconography rather than attempting photoreal water.
"""

from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


HERE = Path(__file__).parent


# --- Palette -----------------------------------------------------------------
# Picked to harmonise with Timberborn's existing UI: warm beige text on
# dark backgrounds, blue tones for "water" so the icon reads at a glance
# against the green nine-slice borders.

DEEP_NAVY = (10, 32, 58, 255)
DEEP_BLUE = (18, 64, 110, 255)
MID_BLUE = (44, 116, 175, 255)
WAVE_BLUE = (72, 160, 210, 255)
CREST_LIGHT = (170, 220, 240, 255)
WHITE_FOAM = (230, 245, 250, 255)
RAIN_PALE = (210, 235, 250, 220)
TRANSPARENT = (0, 0, 0, 0)


# --- Icon (52x52) ------------------------------------------------------------

def render_icon(size: int = 52) -> Image.Image:
    """A stylised wave with three raindrops.

    Drawn at 4x supersampled and downsampled with LANCZOS for clean edges
    at the 26x26 the game's CSS sizes it to.
    """
    s = size * 4
    img = Image.new("RGBA", (s, s), TRANSPARENT)
    d = ImageDraw.Draw(img, "RGBA")

    # Soft circular halo behind the wave so the icon reads even when the
    # date-panel green strip is busy.
    halo_pad = int(s * 0.04)
    d.ellipse(
        [halo_pad, halo_pad, s - halo_pad, s - halo_pad],
        fill=(DEEP_NAVY[0], DEEP_NAVY[1], DEEP_NAVY[2], 180),
    )
    # Inner highlight ring.
    ring_pad = int(s * 0.10)
    d.ellipse(
        [ring_pad, ring_pad, s - ring_pad, s - ring_pad],
        outline=(WAVE_BLUE[0], WAVE_BLUE[1], WAVE_BLUE[2], 90),
        width=max(2, s // 80),
    )

    # Wave body: a fat sine curve filled below.
    cx, cy = s // 2, int(s * 0.62)
    amplitude = int(s * 0.10)
    wave_w = int(s * 0.72)
    wave_top_pts = []
    steps = 80
    for i in range(steps + 1):
        x = cx - wave_w // 2 + (wave_w * i) // steps
        # Two stacked sines for a more organic crest.
        y = cy - int(
            amplitude * math.sin(math.pi * 2 * i / steps)
            + amplitude * 0.35 * math.sin(math.pi * 4 * i / steps + 0.7)
        )
        wave_top_pts.append((x, y))

    bottom_y = int(s * 0.92)
    wave_poly = (
        wave_top_pts
        + [(wave_top_pts[-1][0], bottom_y), (wave_top_pts[0][0], bottom_y)]
    )
    d.polygon(wave_poly, fill=DEEP_BLUE)

    # Lighter wave layer in front.
    wave2_pts = []
    for i in range(steps + 1):
        x = cx - wave_w // 2 + (wave_w * i) // steps
        y = cy + int(s * 0.06) - int(
            amplitude * 0.7 * math.sin(math.pi * 2 * i / steps + 1.1)
        )
        wave2_pts.append((x, y))
    wave2_poly = (
        wave2_pts
        + [(wave2_pts[-1][0], bottom_y), (wave2_pts[0][0], bottom_y)]
    )
    d.polygon(wave2_poly, fill=MID_BLUE)

    # Foam highlight along the second crest.
    foam_pts = wave2_pts
    d.line(foam_pts, fill=CREST_LIGHT, width=max(2, s // 70), joint="curve")

    # Three raindrops above the wave, descending in size from left to right.
    drops = [
        (int(s * 0.30), int(s * 0.30), int(s * 0.075)),
        (int(s * 0.52), int(s * 0.20), int(s * 0.095)),
        (int(s * 0.72), int(s * 0.32), int(s * 0.065)),
    ]
    for (dx, dy, dr) in drops:
        _draw_raindrop(d, dx, dy, dr, fill=WAVE_BLUE, outline=CREST_LIGHT,
                       outline_width=max(1, s // 90))

    # Slight blur then downsample so anti-aliasing reads cleanly at 26px.
    img = img.filter(ImageFilter.GaussianBlur(radius=0.6))
    return img.resize((size, size), Image.LANCZOS)


def _draw_raindrop(d: ImageDraw.ImageDraw, x: int, y: int, r: int,
                   fill, outline, outline_width: int) -> None:
    """Classic teardrop: circle below + triangle tip above."""
    # Body circle.
    d.ellipse([x - r, y - r + r, x + r, y + r + r], fill=fill)
    # Pointed tip — triangle whose base aligns with the top of the circle.
    tip_h = int(r * 1.6)
    tip = [
        (x - r + int(r * 0.05), y + r // 2),
        (x + r - int(r * 0.05), y + r // 2),
        (x, y + r // 2 - tip_h),
    ]
    d.polygon(tip, fill=fill)
    # Outline pass — slight glow.
    d.ellipse([x - r, y - r + r, x + r, y + r + r],
              outline=outline, width=outline_width)
    d.line([tip[0], tip[2]], fill=outline, width=outline_width)
    d.line([tip[1], tip[2]], fill=outline, width=outline_width)


# --- Notification banner (757x174) ------------------------------------------

def render_banner(width: int = 757, height: int = 174) -> Image.Image:
    """Wide banner: roiling waves under a sheet of rain.

    Composition: dark vertical gradient sky (top half), waves filling the
    bottom 55%, white-foam crest line, then a translucent rain mesh
    overlaid across everything. Edges fade to transparent so the
    notification reads as a floating ribbon rather than a hard rectangle.
    """
    s_scale = 2  # supersample
    W, H = width * s_scale, height * s_scale
    img = Image.new("RGBA", (W, H), TRANSPARENT)
    d = ImageDraw.Draw(img, "RGBA")

    # Vertical gradient sky.
    for y in range(H):
        t = y / H
        # Darker at top, lighter (more blue) toward the wave line.
        r = int(DEEP_NAVY[0] + (DEEP_BLUE[0] - DEEP_NAVY[0]) * t)
        g = int(DEEP_NAVY[1] + (DEEP_BLUE[1] - DEEP_NAVY[1]) * t)
        b = int(DEEP_NAVY[2] + (DEEP_BLUE[2] - DEEP_NAVY[2]) * t)
        d.line([(0, y), (W, y)], fill=(r, g, b, 255))

    # Three layered waves, deepest to shallowest. Each is a polygon that
    # spans the bottom of the image with a sinusoidal top edge.
    wave_layers = [
        # (amp, period, baseline_y, color)
        (int(H * 0.10), W // 2, int(H * 0.42), DEEP_BLUE),
        (int(H * 0.08), int(W / 2.5), int(H * 0.55), MID_BLUE),
        (int(H * 0.06), int(W / 1.7), int(H * 0.68), WAVE_BLUE),
    ]
    rng = random.Random(20260910)  # talk date as seed — deterministic art
    for amp, period, baseline, color in wave_layers:
        pts = []
        steps = 240
        phase = rng.uniform(0, math.pi * 2)
        for i in range(steps + 1):
            x = (W * i) // steps
            y = baseline - int(amp * math.sin(2 * math.pi * x / period + phase))
            # Add a low-freq second harmonic for variety.
            y -= int(amp * 0.35 * math.sin(4 * math.pi * x / period + phase * 1.7))
            pts.append((x, y))
        poly = pts + [(W, H), (0, H)]
        d.polygon(poly, fill=color)
        # Foam highlight on the crest.
        d.line(pts, fill=(CREST_LIGHT[0], CREST_LIGHT[1], CREST_LIGHT[2], 130),
               width=max(2, s_scale))

    # Diagonal rain streaks across the whole banner.
    rain_overlay = Image.new("RGBA", (W, H), TRANSPARENT)
    rd = ImageDraw.Draw(rain_overlay, "RGBA")
    for _ in range(900):
        x = rng.randint(-H, W)
        y = rng.randint(0, H)
        length = rng.randint(12, 38) * s_scale // 2
        dx = int(length * 0.35)  # diagonal slant
        rd.line(
            [(x, y), (x + dx, y + length)],
            fill=(RAIN_PALE[0], RAIN_PALE[1], RAIN_PALE[2], rng.randint(45, 110)),
            width=1 * s_scale,
        )
    rain_overlay = rain_overlay.filter(ImageFilter.GaussianBlur(radius=0.4))
    img.alpha_composite(rain_overlay)

    # Splash dots on the front wave for texture.
    for _ in range(60):
        sx = rng.randint(0, W)
        sy = rng.randint(int(H * 0.65), int(H * 0.9))
        sr = rng.randint(2, 6) * s_scale // 2
        d.ellipse([sx - sr, sy - sr, sx + sr, sy + sr],
                  fill=(WHITE_FOAM[0], WHITE_FOAM[1], WHITE_FOAM[2], 180))

    # Vignette + horizontal edge fade so the banner doesn't look like a
    # rigid box pasted into the screen.
    fade = Image.new("L", (W, H), 255)
    fd = ImageDraw.Draw(fade)
    edge = int(W * 0.08)
    for x in range(edge):
        a = int(255 * x / edge)
        fd.line([(x, 0), (x, H)], fill=a)
        fd.line([(W - 1 - x, 0), (W - 1 - x, H)], fill=a)
    # Top/bottom soft fade — gentler than the side fade.
    top_edge = int(H * 0.10)
    for y in range(top_edge):
        a = int(255 * y / top_edge)
        col = fd.im.getpixel((W // 2, y))
        # Multiply existing column alpha by top fade.
        for x in range(W):
            existing = fade.getpixel((x, y))
            fade.putpixel((x, y), min(existing, a))
    for y in range(top_edge):
        ay = H - 1 - y
        a = int(255 * y / top_edge)
        for x in range(W):
            existing = fade.getpixel((x, ay))
            fade.putpixel((x, ay), min(existing, a))
    img.putalpha(fade)

    return img.resize((width, height), Image.LANCZOS)


# --- Driver ------------------------------------------------------------------

def main() -> None:
    icon = render_icon()
    icon.save(HERE / "flood-icon.png", "PNG")

    banner = render_banner()
    banner.save(HERE / "flood-notification.png", "PNG")

    print(f"wrote {HERE / 'flood-icon.png'}  ({icon.size[0]}x{icon.size[1]})")
    print(f"wrote {HERE / 'flood-notification.png'}  ({banner.size[0]}x{banner.size[1]})")


if __name__ == "__main__":
    main()
