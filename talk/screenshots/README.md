# Screenshots

Optional in-game captures the deck refers to. `generate.py` tolerates
missing files (renders a red TODO placeholder), so the deck builds
without these — but for the actual delivery, capture and drop here.

## Currently referenced by the deck

None yet — the deck uses live demo slides instead of static
screenshots. Capture these only if you want fallback images visible
on the projector while you context-switch to the game:

- `flood-banner.png` — central banner "A flood has begun!" during a
  flood cycle (~757×174 effective; PrintScreen the whole window
  and crop later if needed).
- `mixed-tide-water.png` — close-up of a water source emitting
  partly-contaminated water during a Mixed Tide.
- `weather-panel-bar.png` — top-right cycle counter + progress strip
  showing the mixed-tide texture.

If you add these, update `generate.py` to slot them into specific
slides via `add_image_slide(...)`.
