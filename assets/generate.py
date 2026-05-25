"""
One-shot art generator for the Flood Season mod.

Produces four PNGs the mod loads at runtime:

  - flood-icon.png            52x52 — replaces the drought sun icon at
                                      the top-right date panel during a
                                      flood.
  - flood-notification.png   757x174 — replaces the dry/badtide banner
                                       that fades in when a hazardous
                                       weather starts. Sized to match
                                       the vanilla hazardous-weather-
                                       notification__background
                                       dimensions so positioning stays
                                       put.
  - mixedtide-icon.png        52x52 — date-panel icon during a Mixed
                                      Tide. Visually a vertical split:
                                      clean blue waves on one half,
                                      contaminated olive waves on the
                                      other — reads as "this water is
                                      partly bad" at a glance.
  - mixedtide-notification.png 757x174 — Mixed-Tide notification banner
                                         + weather-panel strip. Same
                                         wave/rain composition as the
                                         flood banner but with a
                                         horizontal palette blend —
                                         clean on the left, bad on the
                                         right, with a transition in
                                         the middle.

Re-run with `python assets/generate.py` whenever the art needs a
refresh. Output files are checked in so the mod doesn't need Python to
build.

Pure PIL — no external assets, no fonts beyond PIL's default. The
drawings are deliberately simple flat-graphic style to match
Timberborn's stylised UI iconography rather than attempting photoreal
water.
"""

from __future__ import annotations

import math
import random
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


HERE = Path(__file__).parent


# --- Palettes ---------------------------------------------------------------
# Picked to harmonise with Timberborn's existing UI: warm beige text on
# dark backgrounds. CLEAN_PALETTE is blue water tones — what the flood
# banner uses, and what the left half of the mixed-tide banner uses.
# BAD_PALETTE is olive / muddy tones — what the right half of the
# mixed-tide banner uses, suggesting contaminated water that a beaver
# would not want to drink.
#
# Each palette has five tone slots used consistently across the icon
# and banner renderers:
#
#   sky_top    — top of the vertical gradient sky (darkest)
#   deep       — deepest wave layer, halo behind icon
#   mid        — middle wave layer
#   wave       — front wave layer, raindrop fill
#   crest      — foam-line highlight, icon-ring inner highlight
#   foam       — splash dots on the front wave
#   rain       — semi-transparent rain streaks across the banner

@dataclass(frozen=True)
class Palette:
    sky_top: tuple[int, int, int, int]
    deep: tuple[int, int, int, int]
    mid: tuple[int, int, int, int]
    wave: tuple[int, int, int, int]
    crest: tuple[int, int, int, int]
    foam: tuple[int, int, int, int]
    rain: tuple[int, int, int, int]


CLEAN_PALETTE = Palette(
    sky_top=(10, 32, 58, 255),       # deep navy
    deep=(18, 64, 110, 255),         # deep blue
    mid=(44, 116, 175, 255),         # mid blue
    wave=(72, 160, 210, 255),        # wave blue
    crest=(170, 220, 240, 255),      # crest light
    foam=(230, 245, 250, 255),       # white foam
    rain=(210, 235, 250, 220),       # rain pale
)

# Olive / sickly-green tones for contaminated water. Slightly desaturated
# so it sits visually next to CLEAN_PALETTE without clashing — the goal
# is "same water, different state" not "totally different art language".
BAD_PALETTE = Palette(
    sky_top=(28, 24, 14, 255),       # muddy darkest
    deep=(58, 60, 28, 255),          # deep olive
    mid=(96, 105, 38, 255),          # mid olive
    wave=(140, 150, 60, 255),        # olive-yellow
    crest=(195, 200, 115, 255),      # scummy highlight
    foam=(220, 220, 160, 255),       # sickly foam
    rain=(180, 185, 130, 220),       # tinted rain
)

TRANSPARENT = (0, 0, 0, 0)


# --- Icon (52x52) ------------------------------------------------------------

def _render_icon(size: int, palette: Palette) -> Image.Image:
    """A stylised wave with three raindrops, rendered in `palette`.

    Drawn at 4x supersampled and downsampled with LANCZOS for clean
    edges at the 26x26 the game's CSS sizes it to.
    """
    s = size * 4
    img = Image.new("RGBA", (s, s), TRANSPARENT)
    d = ImageDraw.Draw(img, "RGBA")

    # Soft circular halo behind the wave so the icon reads even when
    # the date-panel green strip is busy.
    halo_pad = int(s * 0.04)
    d.ellipse(
        [halo_pad, halo_pad, s - halo_pad, s - halo_pad],
        fill=(palette.sky_top[0], palette.sky_top[1], palette.sky_top[2], 180),
    )
    # Inner highlight ring.
    ring_pad = int(s * 0.10)
    d.ellipse(
        [ring_pad, ring_pad, s - ring_pad, s - ring_pad],
        outline=(palette.wave[0], palette.wave[1], palette.wave[2], 90),
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
    d.polygon(wave_poly, fill=palette.deep)

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
    d.polygon(wave2_poly, fill=palette.mid)

    # Foam highlight along the second crest.
    foam_pts = wave2_pts
    d.line(foam_pts, fill=palette.crest, width=max(2, s // 70), joint="curve")

    # Three raindrops above the wave, descending in size from left to right.
    drops = [
        (int(s * 0.30), int(s * 0.30), int(s * 0.075)),
        (int(s * 0.52), int(s * 0.20), int(s * 0.095)),
        (int(s * 0.72), int(s * 0.32), int(s * 0.065)),
    ]
    for (dx, dy, dr) in drops:
        _draw_raindrop(d, dx, dy, dr, fill=palette.wave, outline=palette.crest,
                       outline_width=max(1, s // 90))

    # Slight blur then downsample so anti-aliasing reads cleanly at 26px.
    img = img.filter(ImageFilter.GaussianBlur(radius=0.6))
    return img.resize((size, size), Image.LANCZOS)


def render_icon(size: int = 52) -> Image.Image:
    """Flood icon — the clean palette across the whole drop."""
    return _render_icon(size, CLEAN_PALETTE)


def render_mixedtide_icon(size: int = 52) -> Image.Image:
    """Mixed-tide icon — vertical split: clean left, contaminated right.

    Renders the same drop twice (once per palette) and masks the two
    halves together with a sharp vertical seam. At 52x52 (downsampled
    from 26x26 display), a sharp seam reads better than a gradient —
    the player sees "this water is half-and-half" immediately.
    """
    clean = _render_icon(size, CLEAN_PALETTE)
    bad = _render_icon(size, BAD_PALETTE)
    # Mask: white on the left half (keeps clean), black on the right half
    # (keeps bad).
    mask = Image.new("L", (size, size), 0)
    md = ImageDraw.Draw(mask)
    md.rectangle([0, 0, size // 2, size], fill=255)
    # A 2-pixel feather along the seam softens what would otherwise be
    # a jarring antialiased-edge join between the two LANCZOS-resampled
    # halves.
    for x in range(size // 2, size // 2 + 2):
        md.line([(x, 0), (x, size)], fill=128)
    return Image.composite(clean, bad, mask)


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

def _render_banner(width: int, height: int, palette: Palette) -> Image.Image:
    """Wide banner: roiling waves under a sheet of rain, in `palette`.

    Composition: dark vertical gradient sky (top half), waves filling
    the bottom 55%, white-foam crest line, then a translucent rain mesh
    overlaid across everything. Edges fade to transparent so the
    notification reads as a floating ribbon rather than a hard
    rectangle.
    """
    s_scale = 2  # supersample
    W, H = width * s_scale, height * s_scale
    img = Image.new("RGBA", (W, H), TRANSPARENT)
    d = ImageDraw.Draw(img, "RGBA")

    # Vertical gradient sky.
    for y in range(H):
        t = y / H
        r = int(palette.sky_top[0] + (palette.deep[0] - palette.sky_top[0]) * t)
        g = int(palette.sky_top[1] + (palette.deep[1] - palette.sky_top[1]) * t)
        b = int(palette.sky_top[2] + (palette.deep[2] - palette.sky_top[2]) * t)
        d.line([(0, y), (W, y)], fill=(r, g, b, 255))

    # Three layered waves, deepest to shallowest. Each is a polygon
    # that spans the bottom of the image with a sinusoidal top edge.
    wave_layers = [
        # (amp, period, baseline_y, color)
        (int(H * 0.10), W // 2, int(H * 0.42), palette.deep),
        (int(H * 0.08), int(W / 2.5), int(H * 0.55), palette.mid),
        (int(H * 0.06), int(W / 1.7), int(H * 0.68), palette.wave),
    ]
    rng = random.Random(20260910)  # talk date as seed — deterministic art
    for amp, period, baseline, color in wave_layers:
        pts = []
        steps = 240
        phase = rng.uniform(0, math.pi * 2)
        for i in range(steps + 1):
            x = (W * i) // steps
            y = baseline - int(amp * math.sin(2 * math.pi * x / period + phase))
            y -= int(amp * 0.35 * math.sin(4 * math.pi * x / period + phase * 1.7))
            pts.append((x, y))
        poly = pts + [(W, H), (0, H)]
        d.polygon(poly, fill=color)
        d.line(pts, fill=(palette.crest[0], palette.crest[1], palette.crest[2], 130),
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
            fill=(palette.rain[0], palette.rain[1], palette.rain[2], rng.randint(45, 110)),
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
                  fill=(palette.foam[0], palette.foam[1], palette.foam[2], 180))

    # Vignette + horizontal edge fade so the banner doesn't look like
    # a rigid box pasted into the screen.
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


def render_banner(width: int = 757, height: int = 174) -> Image.Image:
    """Flood banner — clean palette across the whole strip."""
    return _render_banner(width, height, CLEAN_PALETTE)


def render_mixedtide_banner(width: int = 757, height: int = 174) -> Image.Image:
    """Mixed-Tide banner — horizontal blend from clean (left) to bad (right).

    Renders the same composition twice (once per palette) and composites
    with a horizontal alpha mask. The mask is mostly-linear from
    left=opaque to right=transparent, with a moderate plateau on each
    end so the player still sees pure clean and pure bad at the
    extremes. The transition zone in the middle reads as "the water
    started clean and is becoming bad" — visually maps to the Mixed
    Tide mechanic.
    """
    clean = _render_banner(width, height, CLEAN_PALETTE)
    bad = _render_banner(width, height, BAD_PALETTE)
    # Build a horizontal-gradient mask: white (= keep clean) on the
    # left, black (= keep bad) on the right, with a soft transition.
    mask = Image.new("L", (width, height), 0)
    md = ImageDraw.Draw(mask)
    plateau = int(width * 0.18)   # pure clean on the left
    plateau_right = int(width * 0.18)  # pure bad on the right
    transition_start = plateau
    transition_end = width - plateau_right
    for x in range(width):
        if x < transition_start:
            a = 255
        elif x >= transition_end:
            a = 0
        else:
            t = (x - transition_start) / (transition_end - transition_start)
            a = int(255 * (1 - t))
        md.line([(x, 0), (x, height)], fill=a)
    return Image.composite(clean, bad, mask)


# --- Driver ------------------------------------------------------------------

def main() -> None:
    icon = render_icon()
    icon.save(HERE / "flood-icon.png", "PNG")

    banner = render_banner()
    banner.save(HERE / "flood-notification.png", "PNG")

    mixed_icon = render_mixedtide_icon()
    mixed_icon.save(HERE / "mixedtide-icon.png", "PNG")

    mixed_banner = render_mixedtide_banner()
    mixed_banner.save(HERE / "mixedtide-notification.png", "PNG")

    for name, im in [
        ("flood-icon.png", icon),
        ("flood-notification.png", banner),
        ("mixedtide-icon.png", mixed_icon),
        ("mixedtide-notification.png", mixed_banner),
    ]:
        print(f"wrote {HERE / name}  ({im.size[0]}x{im.size[1]})")


if __name__ == "__main__":
    main()
