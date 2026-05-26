# Tech-guild talk: "Modding Timberborn"

A ~55-minute walkthrough of the three Timberborn mods built with
Jasper (age 6) plus a future-work tease for the fourth. Generated
to PowerPoint from a Python script so the deck is rebuildable.

## Files

- `generate.py` — slide builder. Run it to (re)produce the deck.
- `timberborn-talk.pptx` — the deck. Open in PowerPoint. Edit by hand
  or via the script.
- `screenshots/` — drop captured-from-game PNGs here. Slides that
  reference a missing file render a red TODO placeholder instead.

## Talk shape

| Section | Slides | Time |
|---|---|---|
| Opening — title + Kallikor parallel + Jasper + the three requests | 1–5 | 6 min |
| About Timberborn — game primer + mod loader + three layers | 6–8 | 5 min |
| Mod #1: Quadruple Platform (templates layer) | 9–13 | 6 min |
| Mod #2: Flood Season — flow rate (interfaces layer) | 14–19 | 7 min |
| Mod #2: Flood Season — flood hazard (Harmony) | 20–24 | 7 min |
| The cascade — UI helper throws, id-spoof, inline overrides | 25–28 | 6 min |
| Save / load + fake-event knock-on | 29–30 | 4 min |
| Mod #3: Mixed Tide (doing it twice) | 31–33 | 5 min |
| Gotchas + workflow lessons | 34–35 | 3 min |
| Mod #4: Rainy Season (future) | 36–37 | 3 min |
| Thanks / Q&A | 38 | 1 min + 10 min Q&A |

Total content: ~53 minutes. Buffer of a couple of minutes before Q&A.

## Rebuild

```bash
pip install python-pptx   # one-time
python talk/generate.py
```

Output goes to `talk/timberborn-talk.pptx`. Re-running overwrites.

## Before delivering — fill in

Open `generate.py` and confirm/replace:

```python
SON_NAME = "Jasper"
JASPER_PLATFORM_BRIEF = "I want a platform that goes higher than three."
JASPER_FLOOD_QUOTE   = "Dad, what if there was too much water?"
JASPER_MIXED_QUOTE   = "Dad, let's make a season with bad and good water."
JASPER_RAIN_BRIEF    = "What if water fell from the sky and made the map wet?"
CLOSING_REFLECTION   = "TODO: one or two sentences..."
```

The closing reflection appears in the Q&A slide's speaker notes — fill
in something specific you actually learnt.

## Dry-run checklist

1. PowerPoint open, presenter view confirms speaker notes on every slide.
2. Demo slides have actionable in-game steps in the notes.
3. Three live demos rehearsed in actual Timberborn:
   - Demo 1 (mod #1): place a Quadruple Platform from the Paths toolbar group.
   - Demo 2 (mod #2A): temperate multiplier 200 → 500, watch flow jump.
   - Demo 3 (mod #2B): flood probability 100, new game, fast-forward to first hazard.
4. Capture fallback screenshots for any demo that's risky:
   - `screenshots/quadruple-platform.png` — placed building in-game.
   - `screenshots/flood-banner.png` — "A flood has begun!" central banner.
   - `screenshots/mixed-tide-water.png` — partly-bad water emitting from a source.

## If over-running

Cut order (lowest information loss first):
1. The deferred-resolver detail in the cascade section (mention briefly instead of dwelling).
2. The Harmony-101 slide (assume audience understands prefix/postfix from context).
3. The workflow-lessons slide (compress into one bullet on the gotcha catalogue).

If under-running, demo 3 is the best place to spend extra minutes — let the
hazard cycle play out and walk through the visual changes live.
