# Tech-guild talk: "Modding Timberborn"

A ~55-minute walkthrough of the Flood Season + Mixed Tide build, with
~10 minutes of Q&A budgeted on top. Generated to PowerPoint from a
Python script so the deck is rebuildable.

## Files

- `generate.py` — slide builder. Run it to (re)produce the deck.
- `timberborn-talk.pptx` — the deck. Open in PowerPoint. Edit by hand
  or via the script.
- `screenshots/` — drop captured-from-game PNGs in here. Slides that
  reference a missing file render a red TODO placeholder instead.

## Rebuild

```bash
pip install python-pptx   # one-time
python talk/generate.py
```

Output goes to `talk/timberborn-talk.pptx`. Re-running overwrites.

## Before delivering

Open `generate.py` and fill in:

```python
SON_NAME = "TODO_SON_NAME"
KID_QUOTE_OPENING = "Daddy, can you make it rain bigger?"  # or actual line
KID_QUOTE_MIXED = "Not all the way poison — just kind of."  # or actual line
CLOSING_REFLECTION = "…"  # one or two sentences for the closing slide
```

Then rebuild and walk the deck end-to-end with a stopwatch.

## Dry-run checklist

1. PowerPoint open, presenter view confirms speaker notes on every slide.
2. Demo slides have actionable in-game steps in the notes block.
3. Three live demos rehearsed in actual Timberborn with the mod loaded:
   - Demo 1: temperate multiplier 200 → 500 (flow jump).
   - Demo 2: flood probability 100, new game, fast-forward to first hazard.
   - Demo 3: both Flood + Mixed Tide at 50% each, cycle through.
4. Capture fallback screenshots for any demo that's risky:
   - `screenshots/flood-banner.png` — "A flood has begun!" banner mid-flood.
   - `screenshots/mixed-tide-water.png` — partly-bad water emitting from a source.
   - `screenshots/weather-panel-bar.png` — progress bar reading the right hazard.

## Total time budget

55 min content + 10 min Q&A = 65 min, leaving 5 min for handoff.
If you over-run, the cascade section (~12 min) is where to cut — drop
the four-underscore slide first, then the deferred-resolver slide.
