"""
One-shot PowerPoint generator for the "Modding Timberborn" tech-guild
talk that this entire mod was built for.

Produces ./timberborn-talk.pptx — a ~30-slide deck targeting ~55
minutes of content + 10 minutes of Q&A. Each slide carries speaker
notes that show up in PowerPoint's presenter view.

Re-run with `python talk/generate.py` whenever the content changes.
Output is checked in so the projector laptop doesn't need Python or
python-pptx.

Library: python-pptx (pip install python-pptx).

Top-of-file constants below are the personalisation knobs — the user
fills these in before delivering the talk.
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.util import Inches, Pt


# --- Personalisation ---------------------------------------------------------
# Fill these in before running the script for delivery.

SON_NAME = "TODO_SON_NAME"   # 6yo who designed the mod's mechanics
SON_AGE = 6
TALK_DATE = "2026-09-10"
TALK_VENUE = "Kallikor tech guild"
GITHUB_URL = "https://github.com/Spycho/timberborn-flood-mod"

# The kid's original feature request — best-guess. Replace with the
# actual line he said (or close to it).
KID_QUOTE_OPENING = "Daddy, can you make it rain bigger?"

# The kid's Mixed Tide framing — same caveat.
KID_QUOTE_MIXED = "Not all the way poison — just kind of."

# Closing slide observation. The user writes this one or two lines —
# script ships with a placeholder so the deck reads end-to-end.
CLOSING_REFLECTION = (
    "TODO: one or two sentences on what pair-programming with a "
    "kindergartener actually teaches you. Probably something about "
    "'the customer should not write SQL' or 'best PM I've ever had'. "
    "User fills this in."
)


# --- Theme -------------------------------------------------------------------
# Colours sampled to match the mod's own palette — keeps the deck
# visually consistent with the screenshots that appear in it.

NAVY = RGBColor(0x0A, 0x20, 0x3A)
DEEP_BLUE = RGBColor(0x12, 0x40, 0x6E)
MID_BLUE = RGBColor(0x2C, 0x74, 0xAF)
ACCENT = RGBColor(0xAA, 0xDC, 0xF0)
OFFWHITE = RGBColor(0xF4, 0xF7, 0xFB)
DARK_TEXT = RGBColor(0x10, 0x18, 0x24)
MUTED = RGBColor(0x55, 0x60, 0x70)
WARN_RED = RGBColor(0xB8, 0x33, 0x33)
CODE_BG = RGBColor(0xF2, 0xF4, 0xF7)
CODE_TEXT = RGBColor(0x10, 0x18, 0x24)


# --- Geometry ---------------------------------------------------------------
# All in inches. Slide is 13.333 x 7.5 — 16:9 widescreen.

SLIDE_W = Inches(13.333)
SLIDE_H = Inches(7.5)

TITLE_TOP = Inches(0.35)
TITLE_HEIGHT = Inches(0.9)
BODY_TOP = Inches(1.4)
BODY_HEIGHT = Inches(5.6)
MARGIN_X = Inches(0.6)
BODY_WIDTH = SLIDE_W - MARGIN_X * 2
FOOTER_TOP = Inches(7.1)
FOOTER_HEIGHT = Inches(0.35)


# --- Slide-building helpers --------------------------------------------------


def _blank_slide(prs: Presentation):
    """Return a fresh slide using the blank layout (layout 6 in default theme)."""
    return prs.slides.add_slide(prs.slide_layouts[6])


def _set_background(slide, color: RGBColor):
    fill = slide.background.fill
    fill.solid()
    fill.fore_color.rgb = color


def _add_textbox(slide, left, top, width, height,
                 text="", *, font="Calibri", size=20, bold=False,
                 color=DARK_TEXT, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.TOP):
    box = slide.shapes.add_textbox(left, top, width, height)
    tf = box.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = anchor
    p = tf.paragraphs[0]
    p.alignment = align
    if text:
        run = p.add_run()
        run.text = text
        run.font.name = font
        run.font.size = Pt(size)
        run.font.bold = bold
        run.font.color.rgb = color
    return tf


def _add_title(slide, title: str):
    _add_textbox(
        slide,
        MARGIN_X, TITLE_TOP, SLIDE_W - MARGIN_X * 2, TITLE_HEIGHT,
        text=title, font="Calibri", size=32, bold=True,
        color=NAVY, anchor=MSO_ANCHOR.MIDDLE,
    )
    # Subtle underline rule under the title.
    line = slide.shapes.add_connector(
        1,  # straight connector
        MARGIN_X, Inches(1.25),
        SLIDE_W - MARGIN_X, Inches(1.25),
    )
    line.line.color.rgb = MID_BLUE
    line.line.width = Pt(1.5)


def _add_footer(slide, slide_num: int, total: int, section: str = ""):
    text = f"{section}   ·   {slide_num} / {total}" if section else f"{slide_num} / {total}"
    _add_textbox(
        slide,
        MARGIN_X, FOOTER_TOP, SLIDE_W - MARGIN_X * 2, FOOTER_HEIGHT,
        text=text, font="Calibri", size=10, color=MUTED, align=PP_ALIGN.RIGHT,
    )


def _add_notes(slide, notes: str):
    if not notes:
        return
    tf = slide.notes_slide.notes_text_frame
    tf.text = notes


# Specific slide types ---------------------------------------------


def add_title_slide(prs, title: str, subtitle: str, *, sub2: str = "", notes: str = "") -> None:
    slide = _blank_slide(prs)
    _set_background(slide, NAVY)
    # Big title centred vertically in the upper half.
    _add_textbox(
        slide,
        MARGIN_X, Inches(2.0), SLIDE_W - MARGIN_X * 2, Inches(1.6),
        text=title, font="Calibri Light", size=56, bold=True,
        color=OFFWHITE, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.MIDDLE,
    )
    _add_textbox(
        slide,
        MARGIN_X, Inches(3.7), SLIDE_W - MARGIN_X * 2, Inches(0.8),
        text=subtitle, font="Calibri", size=28,
        color=ACCENT, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.MIDDLE,
    )
    if sub2:
        _add_textbox(
            slide,
            MARGIN_X, Inches(4.6), SLIDE_W - MARGIN_X * 2, Inches(0.6),
            text=sub2, font="Calibri", size=18, color=OFFWHITE,
            align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.MIDDLE,
        )
    # Bottom-line credit.
    _add_textbox(
        slide,
        MARGIN_X, Inches(6.6), SLIDE_W - MARGIN_X * 2, Inches(0.5),
        text=f"{TALK_VENUE}   ·   {TALK_DATE}   ·   {GITHUB_URL}",
        font="Calibri", size=12, color=MUTED, align=PP_ALIGN.LEFT,
    )
    _add_notes(slide, notes)


def add_bullets_slide(prs, title: str, bullets: list, *, notes: str = "",
                      section: str = "", slide_num: int = 0, total: int = 0) -> None:
    """`bullets` items can be plain strings or 2-tuples (text, indent_level)."""
    slide = _blank_slide(prs)
    _set_background(slide, OFFWHITE)
    _add_title(slide, title)
    box = slide.shapes.add_textbox(MARGIN_X, BODY_TOP, BODY_WIDTH, BODY_HEIGHT)
    tf = box.text_frame
    tf.word_wrap = True
    for i, item in enumerate(bullets):
        if isinstance(item, tuple):
            text, level = item
        else:
            text, level = item, 0
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = PP_ALIGN.LEFT
        p.level = level
        p.space_after = Pt(8)
        marker = "•  " if level == 0 else "–  "
        indent = "    " * level
        run = p.add_run()
        run.text = f"{indent}{marker}{text}"
        run.font.name = "Calibri"
        run.font.size = Pt(22 if level == 0 else 18)
        run.font.color.rgb = DARK_TEXT if level == 0 else MUTED
    _add_footer(slide, slide_num, total, section)
    _add_notes(slide, notes)


def add_code_slide(prs, title: str, code: str, *, caption: str = "", notes: str = "",
                   section: str = "", slide_num: int = 0, total: int = 0) -> None:
    slide = _blank_slide(prs)
    _set_background(slide, OFFWHITE)
    _add_title(slide, title)
    # Code panel with light grey background.
    panel = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE,
        MARGIN_X, BODY_TOP, BODY_WIDTH, BODY_HEIGHT - (Inches(0.4) if caption else Inches(0)),
    )
    panel.line.fill.background()
    panel.fill.solid()
    panel.fill.fore_color.rgb = CODE_BG
    panel.shadow.inherit = False
    # Code text inside the panel.
    tf = panel.text_frame
    tf.word_wrap = False
    tf.margin_left = Inches(0.2)
    tf.margin_right = Inches(0.2)
    tf.margin_top = Inches(0.15)
    tf.margin_bottom = Inches(0.15)
    lines = code.rstrip("\n").split("\n")
    # Size code to fit by line count — shrink if more than 22 lines.
    if len(lines) <= 18:
        code_size = 16
    elif len(lines) <= 24:
        code_size = 14
    else:
        code_size = 12
    for i, line in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = PP_ALIGN.LEFT
        run = p.add_run()
        run.text = line if line else " "  # blank lines need a space
        run.font.name = "Consolas"
        run.font.size = Pt(code_size)
        run.font.color.rgb = CODE_TEXT
    if caption:
        _add_textbox(
            slide,
            MARGIN_X, Inches(6.65), BODY_WIDTH, Inches(0.4),
            text=caption, font="Calibri", size=14, color=MUTED,
            align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.MIDDLE,
        )
    _add_footer(slide, slide_num, total, section)
    _add_notes(slide, notes)


def add_demo_slide(prs, title: str, steps: list, *, notes: str = "",
                   section: str = "", slide_num: int = 0, total: int = 0) -> None:
    """Big 'switch to the game' card with the in-game steps for the presenter."""
    slide = _blank_slide(prs)
    _set_background(slide, DEEP_BLUE)
    _add_textbox(
        slide,
        MARGIN_X, Inches(0.5), BODY_WIDTH, Inches(1.0),
        text="LIVE DEMO", font="Calibri Light", size=18, bold=True,
        color=ACCENT, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.MIDDLE,
    )
    _add_textbox(
        slide,
        MARGIN_X, Inches(1.4), BODY_WIDTH, Inches(1.2),
        text=title, font="Calibri Light", size=44, bold=True,
        color=OFFWHITE, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.MIDDLE,
    )
    # Steps list — visible to the presenter in case they forget the path.
    box = slide.shapes.add_textbox(MARGIN_X, Inches(3.2), BODY_WIDTH, Inches(3.5))
    tf = box.text_frame
    tf.word_wrap = True
    for i, step in enumerate(steps):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = PP_ALIGN.LEFT
        p.space_after = Pt(10)
        run = p.add_run()
        run.text = f"{i + 1}.  {step}"
        run.font.name = "Calibri"
        run.font.size = Pt(20)
        run.font.color.rgb = OFFWHITE
    _add_footer(slide, slide_num, total, section)
    # Make presenter notes carry the same steps so they show in presenter view.
    notes_body = (notes + "\n\nIn-game steps:\n" + "\n".join(
        f"  {i + 1}. {s}" for i, s in enumerate(steps))) if notes else (
        "In-game steps:\n" + "\n".join(f"  {i + 1}. {s}" for i, s in enumerate(steps)))
    _add_notes(slide, notes_body)


def add_quote_slide(prs, quote: str, attribution: str = "", *, notes: str = "",
                    section: str = "", slide_num: int = 0, total: int = 0) -> None:
    slide = _blank_slide(prs)
    _set_background(slide, NAVY)
    _add_textbox(
        slide,
        MARGIN_X, Inches(2.0), BODY_WIDTH, Inches(2.5),
        text=f"“{quote}”",
        font="Calibri Light", size=40, bold=False,
        color=OFFWHITE, align=PP_ALIGN.CENTER, anchor=MSO_ANCHOR.MIDDLE,
    )
    if attribution:
        _add_textbox(
            slide,
            MARGIN_X, Inches(4.7), BODY_WIDTH, Inches(0.8),
            text=f"— {attribution}",
            font="Calibri", size=22, color=ACCENT,
            align=PP_ALIGN.CENTER, anchor=MSO_ANCHOR.MIDDLE,
        )
    _add_footer(slide, slide_num, total, section)
    _add_notes(slide, notes)


def add_image_slide(prs, title: str, image_path: Path, *, caption: str = "",
                    notes: str = "", section: str = "", slide_num: int = 0, total: int = 0) -> None:
    slide = _blank_slide(prs)
    _set_background(slide, OFFWHITE)
    _add_title(slide, title)
    if image_path.exists():
        # Centre image in the body area, scale to fit.
        pic = slide.shapes.add_picture(
            str(image_path),
            MARGIN_X, BODY_TOP,
            width=BODY_WIDTH, height=BODY_HEIGHT - Inches(0.5),
        )
    else:
        # Red TODO placeholder so the user knows what to capture.
        panel = slide.shapes.add_shape(
            MSO_SHAPE.RECTANGLE,
            MARGIN_X + Inches(2.5), BODY_TOP + Inches(0.5),
            BODY_WIDTH - Inches(5), BODY_HEIGHT - Inches(1.5),
        )
        panel.line.color.rgb = WARN_RED
        panel.line.width = Pt(2)
        panel.fill.solid()
        panel.fill.fore_color.rgb = OFFWHITE
        tf = panel.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.alignment = PP_ALIGN.CENTER
        run = p.add_run()
        run.text = f"TODO: capture screenshot\n{image_path.name}"
        run.font.name = "Calibri"
        run.font.size = Pt(22)
        run.font.bold = True
        run.font.color.rgb = WARN_RED
    if caption:
        _add_textbox(
            slide,
            MARGIN_X, Inches(6.65), BODY_WIDTH, Inches(0.4),
            text=caption, font="Calibri", size=14, color=MUTED,
            align=PP_ALIGN.CENTER, anchor=MSO_ANCHOR.MIDDLE,
        )
    _add_footer(slide, slide_num, total, section)
    _add_notes(slide, notes)


# --- Deck content ------------------------------------------------------------
# Each helper below builds one slide. They're called in order in build() —
# the order IS the talk's running order.

HERE = Path(__file__).parent
SCREENSHOTS = HERE / "screenshots"

SECTIONS = {
    "open": "Opening",
    "what": "What we're modding",
    "seam": "The clean seam",
    "noseam": "When the seam isn't there",
    "cascade": "The cascade",
    "save": "Save / load",
    "again": "Doing it twice",
    "workflow": "Workflow lessons",
    "close": "Closing",
}


@dataclass
class SlideContext:
    prs: Presentation
    section: str = ""
    slide_num: int = 0
    total: int = 0


def build() -> Presentation:
    prs = Presentation()
    prs.slide_width = SLIDE_W
    prs.slide_height = SLIDE_H

    # Two-pass build: first count slides for the footer, then emit. Cheap
    # to enumerate by calling the closures here in order.
    slide_builders = _slide_builders()
    total = len(slide_builders)

    ctx = SlideContext(prs=prs, total=total)
    for i, (section, fn) in enumerate(slide_builders, start=1):
        ctx.section = SECTIONS.get(section, "")
        ctx.slide_num = i
        fn(ctx)
    return prs


def _slide_builders():
    """Returns [(section_key, build_fn), …] in talk order."""
    return [
        ("open",     _slide_title),
        ("open",     _slide_hook_quote),
        ("open",     _slide_final_demo_flash),

        ("what",     _slide_timberborn_60s),
        ("what",     _slide_mod_loader),

        ("seam",     _slide_where_plug_in),
        ("seam",     _slide_decomp_tool),
        ("seam",     _slide_iwaterstrength),
        ("seam",     _slide_modifier_class),
        ("seam",     _slide_bindito_configurator),
        ("seam",     _slide_templatemodule),
        ("seam",     _slide_modsettings),
        ("seam",     _slide_demo_flow),

        ("noseam",   _slide_next_request),
        ("noseam",   _slide_randomizer_decomp),
        ("noseam",   _slide_harmony_101),
        ("noseam",   _slide_4line_postfix),
        ("noseam",   _slide_demo_always_flood),

        ("cascade",  _slide_cascade_intro),
        ("cascade",  _slide_ui_helper_throws),
        ("cascade",  _slide_id_spoof),
        ("cascade",  _slide_four_underscore),
        ("cascade",  _slide_inline_style_overrides),
        ("cascade",  _slide_deferred_resolver),

        ("save",     _slide_save_problem),
        ("save",     _slide_persistence_class),
        ("save",     _slide_fake_event_knockon),

        ("again",    _slide_mixed_tide_intro),
        ("again",    _slide_contamination_seam),
        ("again",    _slide_new_gotchas),

        ("workflow", _slide_workflow_gotchas),
        ("workflow", _slide_gotcha_catalogue),

        ("close",    _slide_what_he_taught),
        ("close",    _slide_thanks_qa),
    ]


# --- Individual slide builders ----------------------------------------------


def _slide_title(ctx: SlideContext) -> None:
    add_title_slide(
        ctx.prs,
        title="Modding Timberborn",
        subtitle=f"The project my {SON_AGE}-year-old came up with",
        sub2=f"Designed by {SON_NAME} (age {SON_AGE}) · Built by Chris Brett",
        notes=(
            "Opening line: 'This is a talk about a Timberborn mod. Specifically, "
            "it's a talk about doing a parent-child collaboration where the child is "
            "the customer and the parent is the engineer, and what that exercise "
            "teaches you about modding a Unity game without ever opening Unity.'\n\n"
            "Don't apologise for the kid being absent — frame him as the customer who "
            "approved the work async."
        ),
    )


def _slide_hook_quote(ctx: SlideContext) -> None:
    add_quote_slide(
        ctx.prs,
        quote=KID_QUOTE_OPENING,
        attribution=f"{SON_NAME}, age {SON_AGE} — feature request #1",
        notes=(
            "Land the quote, pause, then: 'This is how the project started. He plays "
            "Timberborn, the wet seasons aren't wet enough for him, so he asks me to "
            "fix it. I tell him I'll see what I can do.'\n\n"
            "If the actual quote is different, swap KID_QUOTE_OPENING at the top of the "
            "script."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_final_demo_flash(ctx: SlideContext) -> None:
    add_demo_slide(
        ctx.prs,
        title="Where we're going — final mod, live",
        steps=[
            "Switch to Timberborn (already running, autosave loaded mid-flood).",
            "Show date-panel icon (flood drop) + 'A flood has begun!' banner.",
            "Trigger a Mixed Tide (or load a Mixed-Tide autosave).",
            "Point at the partially-contaminated water emitting from sources.",
            "Switch back to slides — 'OK, now how did we get here.'",
        ],
        notes=(
            "30-second tour, no narrative — just show the destination. Don't explain "
            "anything yet, that's the whole rest of the talk. The point is to set "
            "expectations of what 'done' looks like."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_timberborn_60s(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="Timberborn in 60 seconds",
        bullets=[
            "Beaver colony sim — build dams, store water, survive seasonal hazards",
            "Three weather phases: Temperate, then either Drought or Badtide (contaminated water)",
            "Made by Mechanistry. Unity engine. Ships with an official mod loader",
            "(That's all the lore you need for the rest of the talk)",
        ],
        notes=(
            "Spend ~45 seconds here, no more. Audience just needs to know there are "
            "WATER SOURCES, BEAVERS, and HAZARDS. We're modding the hazards and the "
            "water flow."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_mod_loader(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="How Timberborn loads mods",
        code=(
            "Documents/Timberborn/Mods/Flood Season/\n"
            "    manifest.json     # name, version, required-mods\n"
            "    Code.dll          # your compiled C# assembly\n"
            "    assets/           # PNGs, blueprints, sound, whatever\n"
            "\n"
            "// On launch, the game walks every Mods/* folder:\n"
            "var dlls = modDir.GetFiles(\"*.dll\", SearchOption.AllDirectories);\n"
            "foreach (var dll in dlls) {\n"
            "    var asm = Assembly.LoadFile(dll.FullName);\n"
            "    foreach (var t in asm.GetTypes()) {\n"
            "        if (typeof(IModStarter).IsAssignableFrom(t)) {\n"
            "            ((IModStarter)Activator.CreateInstance(t)).StartMod(env);\n"
            "        }\n"
            "    }\n"
            "}"
        ),
        caption="Reflection-based discovery. RECURSIVE — bin/obj/Code.dll bites. (Gotcha #1.)",
        notes=(
            "Two things to land here:\n"
            "1. The mod IS the folder — no installer, no scripts, just drop a Code.dll.\n"
            "2. 'RECURSIVE' is doing a lot of work in that sentence. Any Code.dll under "
            "the folder gets loaded. We'll come back to this when worktrees explode.\n\n"
            "If anyone asks: yes the IModStarter discovery is just reflection. Yes that "
            "would be cleaner with a manifest entry. No it doesn't matter."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_where_plug_in(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="The first question: where do I plug in?",
        bullets=[
            "The kid wants more water flow. OK — how does flow work?",
            "Game ships compiled DLLs. No source.",
            ("ilspycmd → decompile the relevant assembly → read the C#", 1),
            ("Look for: extensibility seams. Interfaces. DI bindings. Public methods.", 1),
            "Strategy: clean seams first. Harmony patching only when no seam exists.",
        ],
        notes=(
            "Frame Harmony as a tool of last resort, not a first reach. Half the talk is "
            "about what happens when you DO have to reach for it — but the audience "
            "should leave knowing the order: read the code, find the seam, patch only "
            "when there's no seam."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_decomp_tool(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="Decompiling the game with ilspycmd",
        code=(
            "$ dotnet tool install -g ilspycmd\n"
            "$ ilspycmd -p -o ~/decomp/WaterSourceSystem \\\n"
            "    \"…/Timberborn_Data/Managed/Timberborn.WaterSourceSystem.dll\"\n"
            "\n"
            "Result: a tree of .cs files. Readable. Greppable. Not source-of-truth\n"
            "(Timberborn ships obfuscation-free DLLs — IL → C# is near-perfect).\n"
            "\n"
            "$ grep -rn \"IWaterStrengthModifier\" decomp/WaterSourceSystem/\n"
            "decomp/.../IWaterStrengthModifier.cs:5:    public interface IWaterStrengthModifier\n"
            "decomp/.../WaterSource.cs:42:    private readonly List<IWaterStrengthModifier>\n"
            "decomp/.../WaterSource.cs:149:    foreach (IWaterStrengthModifier waterStrengthModifier\n"
        ),
        notes=(
            "The reveal here is HOW EASY this is. You install one .NET tool. You point "
            "it at a DLL. You get readable C#. The game's authors didn't obfuscate, so "
            "everything you decompile reads more or less the way they wrote it.\n\n"
            "Important: this is for READING. Not for redistributing or modifying. You're "
            "reading public-ish API surface to figure out where to hook in."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_iwaterstrength(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="The seam: IWaterStrengthModifier",
        code=(
            "// Timberborn.WaterSourceSystem (vanilla, decompiled)\n"
            "public interface IWaterStrengthModifier {\n"
            "    float GetStrengthModifier();\n"
            "}\n"
            "\n"
            "// WaterSource ticks every game tick and composes ALL modifiers:\n"
            "private void UpdateCurrentStrength() {\n"
            "    float num = SpecifiedStrength;\n"
            "    foreach (IWaterStrengthModifier m in _waterStrengthModifiers) {\n"
            "        num *= m.GetStrengthModifier();   // multiplicative chain\n"
            "    }\n"
            "    SetCurrentStrength(num);\n"
            "}"
        ),
        caption="Drought ships its own IWaterStrengthModifier. We just register one more.",
        notes=(
            "Land the WIN here. We have:\n"
            "  - a one-method interface\n"
            "  - a multiplicative composition chain (so we don't fight other modifiers)\n"
            "  - a per-entity registration point (AddWaterStrengthModifier)\n\n"
            "This is the cleanest possible modding seam. We just implement the interface "
            "and ride the existing tick loop."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_modifier_class(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="Our modifier — switches on the current cycle's weather",
        code=(
            "internal class FloodSeasonWaterStrengthModifier\n"
            "    : BaseComponent, IAwakableComponent, IInitializableEntity,\n"
            "      IWaterStrengthModifier {\n"
            "\n"
            "    public float GetStrengthModifier() {\n"
            "        if (!_weatherService.IsHazardousWeather) {\n"
            "            return _settings.TemperateMultiplier;   // wetter wet seasons\n"
            "        }\n"
            "        return _hazardousWeatherService.CurrentCycleHazardousWeather switch {\n"
            "            FloodWeather _     => _settings.FloodMultiplier,\n"
            "            MixedTideWeather _ => _settings.MixedTideMultiplier,\n"
            "            BadtideWeather _   => _settings.BadtideMultiplier,\n"
            "            _ => 1.0f,    // drought is owned by another patch\n"
            "        };\n"
            "    }\n"
            "}"
        ),
        notes=(
            "Read the code top to bottom. The audience should notice:\n"
            "  - Per-cycle weather dispatch is a C# 8 switch expression\n"
            "  - FloodWeather and MixedTideWeather appear here as types we'll define later\n"
            "  - Drought is conspicuously absent — there's a Harmony patch on the game's "
            "own drought modifier that we'll touch later (sneak preview of 'patch as last "
            "resort')"
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_bindito_configurator(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="Bindito DI — Timberborn's container",
        code=(
            "[Context(\"Game\")]                       // auto-discovered by Bindito\n"
            "internal class FloodSeasonConfigurator : Configurator {\n"
            "    protected override void Configure() {\n"
            "        Bind<FloodSeasonSettings>().AsSingleton();\n"
            "        Bind<FloodWeather>().AsSingleton();\n"
            "        Bind<MixedTideWeather>().AsSingleton();\n"
            "        Bind<HazardousWeatherStatePersistence>().AsSingleton();\n"
            "        Bind<FloodSeasonWaterStrengthModifier>().AsTransient();\n"
            "        Bind<MixedTideContaminationController>().AsTransient();\n"
            "        MultiBind<TemplateModule>()\n"
            "            .ToProvider(ProvideTemplateModule).AsSingleton();\n"
            "    }\n"
            "    // …\n"
            "}"
        ),
        caption="One class. Game discovers it via reflection on [Context], wires the rest.",
        notes=(
            "Bindito is a custom-built DI container in Timberborn (similar shape to "
            "Zenject but lighter). The decorator hint [Context(\"Game\")] is what makes "
            "it run during a game session — there's a separate MainMenu context.\n\n"
            "Important: 'AsSingleton' is LAZY. The instance only constructs when "
            "something resolves it. We'll come back to that — it's gotcha #3."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_templatemodule(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="TemplateModule — attach component to every entity of type X",
        code=(
            "private static TemplateModule ProvideTemplateModule() {\n"
            "    var builder = new TemplateModule.Builder();\n"
            "\n"
            "    // 'For every WaterSource entity spawned, also attach\n"
            "    //  one of these components to it.'\n"
            "    builder.AddDecorator<WaterSource, FloodSeasonWaterStrengthModifier>();\n"
            "    builder.AddDecorator<WaterSource, MixedTideContaminationController>();\n"
            "\n"
            "    return builder.Build();\n"
            "}"
        ),
        caption="Decorator wiring is declarative. Mirrors how vanilla badtide attaches its controller.",
        notes=(
            "Decorators are the pattern: 'when an entity has component T1, attach T2 to "
            "the same entity'. The vanilla game uses this pattern to attach the badtide "
            "contamination controller to every water source — we copied the pattern, "
            "literally.\n\n"
            "Gotcha that bit us in Phase B of Mixed Tide: AddDecorator declares the "
            "ATTACH but NOT the BUILD. You also need Bind<T>().AsTransient() or "
            "Bindito crashes when materialising the first preview. Documented in "
            "CLAUDE.md."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_modsettings(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="Settings UI — eMka.ModSettings",
        code=(
            "// Third-party mod that provides a settings UI in main-menu + in-game\n"
            "internal class FloodSeasonSettings : ModSettingsOwner {\n"
            "\n"
            "    public ModSetting<int> TemperateMultiplierPercent { get; }\n"
            "        = new ModSetting<int>(defaultValue: 200,\n"
            "            ModSettingDescriptor\n"
            "                .Create(\"Temperate flow multiplier (%)\")\n"
            "                .SetTooltip(\"Water source flow during the Temperate phase. \"\n"
            "                          + \"200 = 2× normal.\"));\n"
            "\n"
            "    // 16 more settings in the real class.\n"
            "}"
        ),
        caption="Reflection over public ModSetting<T> properties → automatic UI controls.",
        notes=(
            "Worth pointing out: this is a mod depending on another mod. We declare "
            "eMka.ModSettings as a required dependency in manifest.json. The mod loader "
            "enforces dependency order.\n\n"
            "The UI is reflected — we don't write any UI code. Just declare ModSetting<T> "
            "properties on a class and the library renders matching controls.\n\n"
            "This is what makes the mod feel like a real product: the user can twiddle "
            "knobs without editing JSON."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_demo_flow(ctx: SlideContext) -> None:
    add_demo_slide(
        ctx.prs,
        title="Demo 1 — change the temperate multiplier live",
        steps=[
            "Open Mod Settings panel in-game.",
            "Set 'Temperate flow multiplier (%)' to 500.",
            "Close settings — point at water sources, flow jumps.",
            "Set it back to 200 (default) to keep things sane.",
            "Optional: speed-time to make the effect visible faster.",
        ],
        notes=(
            "First demo. Make sure the game's already at a save with a few water "
            "sources visible — empty map will be undramatic.\n\n"
            "Talking point during the demo: 'this is the WHOLE mod, basically. "
            "Implement an interface, register one Configurator, you're done. Took "
            "about an hour the first time.'"
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_next_request(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="Feature request #2: replace the bad seasons",
        bullets=[
            f"{SON_NAME}: 'Make a flood happen instead of a drought sometimes.'",
            "Translation: introduce a NEW IHazardousWeather type alongside Drought + Badtide",
            "Look for a seam… and there isn't one",
            ("HazardousWeatherRandomizer is the chokepoint. Hardcoded.", 1),
            ("No MultiBind<IHazardousWeather>. No registration call.", 1),
            ("Welcome to Harmony.", 1),
        ],
        notes=(
            "The pivot point of the talk. Up to here we've been doing 'clean modding'. "
            "From here on we're rewriting compiled methods at runtime. The audience "
            "should feel the gear shift."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_randomizer_decomp(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="HazardousWeatherRandomizer — no seam to hook",
        code=(
            "// Timberborn.HazardousWeatherSystem (vanilla, decompiled)\n"
            "public IHazardousWeather GetRandomWeatherForCycle(int cycle) {\n"
            "    if (ShouldBeBadtideWeather(cycle)) {\n"
            "        return _badtideWeather;\n"
            "    }\n"
            "    return _droughtWeather;\n"
            "}\n"
            "\n"
            "// That's it. No interface enumeration. No event hook.\n"
            "// Just an if/else with hardcoded private fields."
        ),
        caption="The thing we want to extend is the most closed shape in the whole game.",
        notes=(
            "Land this hard. The audience should appreciate that the game's authors "
            "didn't WANT this to be extensible. They wrote drought-or-badtide and moved "
            "on.\n\n"
            "We're about to overwrite the return value of this method at runtime. With "
            "no permission. Welcome to Harmony."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_harmony_101(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="Harmony — the runtime patcher",
        bullets=[
            "Library that rewrites method IL at load time. Originally for RimWorld mods.",
            "You don't ship modified game DLLs. You ship patch INSTRUCTIONS.",
            "Three patch positions:",
            ("PREFIX — runs before the original. Can read args. Can return false to SKIP original.", 1),
            ("POSTFIX — runs after. Can read AND modify the original's return value.", 1),
            ("TRANSPILER — rewrites the IL of the original itself. (We didn't need this.)", 1),
            "Patches discovered by [HarmonyPatch] attribute + a single PatchAll() call at startup.",
        ],
        notes=(
            "If anyone in the audience knows about Harmony from RimWorld or Stardew or "
            "BepInEx, they'll nod. For everyone else, the key model is: instead of "
            "shipping a modified game executable, you ship a recipe for patching the "
            "running game in memory.\n\n"
            "Mention: depending on Harmony in Timberborn means depending on the "
            "'Harmony for Timberborn' workshop mod (the game doesn't bundle 0Harmony.dll "
            "itself). That's why our manifest.json declares it as required."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_4line_postfix(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="The hijack — four lines of Harmony postfix",
        code=(
            "[HarmonyPatch(typeof(HazardousWeatherRandomizer),\n"
            "              nameof(HazardousWeatherRandomizer.GetRandomWeatherForCycle))]\n"
            "internal static class RandomizerPatch {\n"
            "\n"
            "    [HarmonyPostfix]\n"
            "    public static void Postfix(int cycle, ref IHazardousWeather __result) {\n"
            "        // … gates omitted (probability, grace cycles, weighted roll) …\n"
            "        if (RollSaysFlood()) {\n"
            "            __result = FloodWeather.Instance;\n"
            "        } else if (RollSaysMixedTide()) {\n"
            "            __result = MixedTideWeather.Instance;\n"
            "        }\n"
            "    }\n"
            "}"
        ),
        caption="Vanilla picks drought / badtide. We OVERWRITE the result. Done.",
        notes=(
            "Land the surprise: this is all it takes to extend a method that has no "
            "extension point. The original runs (so the game's internal streak tracking "
            "still ticks normally), then we get a chance to overwrite the return value.\n\n"
            "Important detail for the audience: __result has TWO underscores because "
            "Harmony has a magic-parameter convention. We'll see four underscores in a "
            "few slides — that's a different convention for instance fields. Don't worry "
            "about it yet.\n\n"
            "The static FloodWeather.Instance dodge: Harmony patches are static methods, "
            "so they can't take DI. We publish a static accessor from the Bindito-managed "
            "FloodWeather constructor. Discuss if Q&A asks."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_demo_always_flood(ctx: SlideContext) -> None:
    add_demo_slide(
        ctx.prs,
        title="Demo 2 — every hazard is a flood",
        steps=[
            "Open Mod Settings, enable Flood Season, set probability to 100%.",
            "Set grace cycles to 0 (so the first hazard fires immediately).",
            "Start a NEW game (cycle weather is decided AT CYCLE START — gotcha #6).",
            "Fast-forward to the first hazardous cycle.",
            "Point at the panel: 'A flood has begun!' (instead of the usual drought/badtide).",
        ],
        notes=(
            "Don't try to demo on an existing save where the cycle's weather was already "
            "decided as drought — settings changes never retro-apply mid-cycle. New game.\n\n"
            "Talking point during demo: 'Notice the icon, the banner, the labels — they all "
            "still say drought. We're not done. We're just at the point where the GAME LOGIC "
            "thinks it's a flood. The UI catches up over the next twenty minutes.'"
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_cascade_intro(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="And then everything else breaks",
        bullets=[
            "We introduced a NEW IHazardousWeather type. Vanilla code didn't expect this.",
            "Every place in the game that does 'is DroughtWeather else is BadtideWeather else throw' fires.",
            "Every place keyed off the weather's string Id misses the lookup.",
            "Every UI element CSS-bound to drought/badtide classes shows the wrong art.",
            "Each of these is a separate patch. We discovered them by crashing.",
        ],
        notes=(
            "This is THE slide that motivates the rest of the technical section. The "
            "audience should walk away with: 'introducing one new type into a hardcoded "
            "domain causes a cascade. The cost of bypassing the seam isn't four lines, "
            "it's the long tail of vanilla code that fights back.'\n\n"
            "Set expectations: we'll walk through the categories of breakage, with one "
            "representative fix each. Then circle back to the maintainability of all "
            "this when we do it AGAIN for Mixed Tide."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_ui_helper_throws(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="The first throw: HazardousWeatherUIHelper",
        code=(
            "// Vanilla — Timberborn.HazardousWeatherSystemUI (decompiled)\n"
            "private void UpdateCurrentUISpecification() {\n"
            "    var current = _hazardousWeatherService.CurrentCycleHazardousWeather;\n"
            "    if (!(current is DroughtWeather)) {\n"
            "        if (!(current is BadtideWeather)) {\n"
            "            throw new InvalidOperationException(\n"
            "                \"No UI for weather: \" + current);\n"
            "        }\n"
            "        _currentUISpecification = _badtideWeatherUISpecification;\n"
            "    }\n"
            "    else {\n"
            "        _currentUISpecification = _droughtWeatherUISpecification;\n"
            "    }\n"
            "}"
        ),
        caption="Our flood is neither. So this throws on the first hazardous cycle.",
        notes=(
            "The fix is a Harmony PREFIX that returns false (skips the original) when "
            "our flood is current — substituting the drought UI spec in its place. "
            "We'll show that in two slides.\n\n"
            "But first: there are at least THREE places like this. UI helper. Sound "
            "player. Sun fog spec. Each throws differently. Some by C# type, some by "
            "string Id. That's why the next slide is about Id spoofing."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_id_spoof(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="The Id-spoof trick — return the wrong string on purpose",
        code=(
            "internal class FloodWeather : IHazardousWeather, ILoadableSingleton {\n"
            "\n"
            "    public string Id => \"DroughtWeather\";  // ← deliberate\n"
            "\n"
            "    // Vanilla code does:\n"
            "    //   var fog = Sun.GetFogSettings(currentWeather.Id);\n"
            "    //   var count = HazardousWeatherHistory.GetCyclesCount(id);\n"
            "    // Both are string-keyed dictionary lookups. Vanilla ships data only\n"
            "    // for \"DroughtWeather\" and \"BadtideWeather\". Returning our own\n"
            "    // id-string \"FloodWeather\" → KeyNotFoundException.\n"
            "    //\n"
            "    // Returning \"DroughtWeather\" → the lookup hits the drought entry.\n"
            "    // Fog system thinks we're a drought. History counts us as a drought.\n"
            "    //\n"
            "    // Type-based dispatch (`is FloodWeather`) STILL works — that uses\n"
            "    // C# type identity, not the string. So our own modifier still\n"
            "    // distinguishes us from real drought.\n"
            "    // …\n"
            "}"
        ),
        caption="Spoof the id where vanilla uses strings. Keep the type where vanilla uses types.",
        notes=(
            "This is one of the cleverer bits and worth slowing down for. The trick:\n"
            "  - For string-keyed lookups, lie about the id. Inherit drought's data.\n"
            "  - For type-based dispatch, the C# type stays distinct. Our own code "
            "can still distinguish flood from drought via 'is FloodWeather'.\n\n"
            "The side effect — that vanilla's HazardousWeatherHistory counts our floods "
            "as droughts and slightly affects its handicap math — is acceptable for our "
            "scope. Mention it as a known wart, don't dwell.\n\n"
            "Mixed Tide does the same trick but spoofs as 'BadtideWeather'. Two custom "
            "hazards, two id-spoofs, no fog crashes."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_four_underscore(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="Harmony quirk: FOUR underscores to read a private field",
        code=(
            "// Vanilla field on HazardousWeatherUIHelper:\n"
            "private readonly HazardousWeatherService _hazardousWeatherService;\n"
            "\n"
            "// Our patch wants to READ that field. Harmony convention:\n"
            "[HarmonyPrefix]\n"
            "public static bool Prefix(\n"
            "        HazardousWeatherService ____hazardousWeatherService,  // ★\n"
            "        DroughtWeatherUISpecification ____droughtWeatherUISpecification,\n"
            "        ref object ____currentUISpecification) {\n"
            "    // … patch body …\n"
            "}\n"
            "\n"
            "// Why ★ has FOUR underscores:\n"
            "//   Harmony strips THREE leading underscores from the parameter name\n"
            "//   and uses the remainder as the field name.\n"
            "//   The game's fields start with ONE underscore.\n"
            "//   So we need 3 (for Harmony) + 1 (vanilla naming) = 4."
        ),
        caption="The kind of gotcha you only find by crashing once. Documented in CLAUDE.md.",
        notes=(
            "The audience reaction here should be: 'oh god'. Yes. This is the kind of "
            "library convention that bites once and you remember forever.\n\n"
            "Show this slide for ~30 seconds. Don't dwell — the point isn't to teach "
            "Harmony's calling convention, it's to convey 'this is what the actual code "
            "looks like, this is the friction, this is why CLAUDE.md gets a gotcha "
            "section.'"
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_inline_style_overrides(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="UI: the drought textures bleed through",
        bullets=[
            "Spoofing the UI spec to drought makes vanilla code stop throwing…",
            "…but the player now sees a DROUGHT sun glyph during a FLOOD",
            "Date panel icon: CSS class on root → swap inline style.backgroundImage",
            "Notification banner: same. Find the Image element, override inline.",
            "Weather panel progress strip: oh no, that one's a custom mesh.",
        ],
        notes=(
            "Quick walkthrough of three UI surfaces. The first two are 'inline override "
            "of a CSS-driven background-image' — straightforward Unity UI Toolkit.\n\n"
            "The third one (the progress strip) is where it gets interesting — that's "
            "the next slide. The bar uses a custom mesh that reads its sprite from a "
            "deferred-resolved custom-CSS-property. We fought the resolver."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_deferred_resolver(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="The deferred-resolver race",
        code=(
            "// SimpleProgressBar (in CoreUI). Custom mesh, sprite-from-USS:\n"
            "private Sprite _image;\n"
            "private static readonly CustomStyleProperty<string> BackgroundImageProperty\n"
            "    = new CustomStyleProperty<string>(\"--background-image\");\n"
            "private void OnCustomStyleResolved(CustomStyleResolvedEvent e) {\n"
            "    _image = e.customStyle.TryGetValue(BackgroundImageProperty, out var v)\n"
            "        ? Resources.Load<Sprite>(v) : null;\n"
            "}\n"
            "\n"
            "// First attempt: override _image from WeatherPanel.UpdatePanel.\n"
            "// FAILED. Unity's custom-style resolution is DEFERRED — runs AFTER our\n"
            "// UpdatePanel postfix returns and overwrites _image back to drought.\n"
            "\n"
            "// Working approach: postfix OnCustomStyleResolved itself.\n"
            "// Whatever vanilla writes, we write last."
        ),
        caption="UI Toolkit resolution timing. Discovered by adding diagnostic logs.",
        notes=(
            "This is one of the meatier bits of the talk. The lesson: UI Toolkit's "
            "custom style resolution is queued, not synchronous. Race conditions are "
            "real even in single-threaded UI code.\n\n"
            "The way we found this: added Debug.Log to both candidate patches. One log "
            "fired, the other didn't. Inferred the resolver was running between our "
            "postfix and the next repaint.\n\n"
            "Mention CLAUDE.md gotcha entry — captured for next time."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_save_problem(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="Save / load: vanilla schema can't fit",
        bullets=[
            "Vanilla persists the active hazard as ONE BOOL: IsDrought (true) / IsBadtide (false)",
            "Save during a Flood → IsDrought=false (vanilla checks REFERENCE equality)",
            "Load → game restores active hazard as Badtide. Flood vanishes.",
            "Fix: our own SingletonKey. Two bools, IsFloodActive + IsMixedTideActive.",
            "PostLoad force-overrides CurrentCycleHazardousWeather via AccessTools.PropertySetter.",
        ],
        notes=(
            "The vanilla schema is fundamentally incapable of representing our state. "
            "We can't extend it without breaking save compatibility, so we side-car: "
            "our own singleton-keyed save data alongside vanilla's.\n\n"
            "AccessTools.PropertySetter is a Harmony utility — bypasses the private-"
            "setter access check and gives you the setter MethodInfo."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_persistence_class(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="HazardousWeatherStatePersistence (excerpt)",
        code=(
            "internal class HazardousWeatherStatePersistence\n"
            "    : ILoadableSingleton, ISaveableSingleton, IPostLoadableSingleton {\n"
            "\n"
            "    private static readonly SingletonKey StateKey =\n"
            "        new SingletonKey(\"Spycho.FloodSeason.HazardousWeatherState\");\n"
            "    private static readonly PropertyKey<bool> IsFloodActiveKey      = …;\n"
            "    private static readonly PropertyKey<bool> IsMixedTideActiveKey  = …;\n"
            "\n"
            "    public void Save(ISingletonSaver s) {\n"
            "        // Always write BOTH bools (one true) when our singleton is present.\n"
            "        // IObjectLoader.Get throws on missing keys → can't write only one.\n"
            "        var saver = s.GetSingleton(StateKey);\n"
            "        saver.Set(IsFloodActiveKey,     current is FloodWeather);\n"
            "        saver.Set(IsMixedTideActiveKey, current is MixedTideWeather);\n"
            "    }\n"
            "\n"
            "    public void PostLoad() {\n"
            "        // Read bools; setter.Invoke to force-restore the right weather;\n"
            "        // re-fire HazardousWeatherSelectedEvent so UI helper refreshes;\n"
            "        // post fake HazardousWeatherEndedEvent(badtide) to clean up the\n"
            "        // contamination controllers vanilla load wrongly enabled.\n"
            "    }\n"
            "}"
        ),
        caption="Two new gotchas hidden here: missing-key throw, and the fake-event knock-on.",
        notes=(
            "Two pieces of folklore on this slide:\n\n"
            "1. The 'always write both keys' lesson — gotcha #8 in CLAUDE.md. We learned "
            "this by reading only the flood bool first on a mixed-tide save and crashing "
            "in SerializedObject.Get('Property not found: IsFloodActive').\n\n"
            "2. The fake EndedEvent — vanilla badtide-contamination controllers turned on "
            "during the boot phase before our PostLoad could correct the weather. We post "
            "a synthetic 'badtide ended' event to walk every contamination controller "
            "through its disable handler. THIS is where the next slide picks up."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_fake_event_knockon(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="The fake event knock-on",
        bullets=[
            "Posting a synthetic HazardousWeatherEndedEvent(BadtideWeather) cleans up contamination ✓",
            "It ALSO reaches GameMusicPlayer.OnHazardousWeatherEnded ✗",
            ("Music player thinks badtide just ended → stops drought track, starts standard.", 1),
            ("But Load() already started drought music. Standard plays ON TOP.", 1),
            "Fix: Harmony prefix on GameMusicPlayer.OnHazardousWeatherEnded.",
            ("If ended-weather is badtide AND our custom hazard is current → skip handler.", 1),
            ("That combination is unreachable in vanilla. Only our synthetic post produces it.", 1),
        ],
        notes=(
            "Audience reaction here should be a wince. We solved one bug by posting an "
            "event with the wrong meaning, then had to add another patch to suppress the "
            "wrong half of that meaning.\n\n"
            "The framing for the talk: this is what 'patch as last resort' actually "
            "costs. Each patch is small. The COMPOSITION of patches gets complicated. "
            "Every fake-event needs a guard to prevent another subscriber from doing "
            "the wrong thing.\n\n"
            "Lesson worth landing: synthetic events leak across subscribers you didn't "
            "intend. There's no 'private channel' on Timberborn's EventBus."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_mixed_tide_intro(ctx: SlideContext) -> None:
    add_quote_slide(
        ctx.prs,
        quote=KID_QUOTE_MIXED,
        attribution=f"{SON_NAME}, feature request #2 — Mixed Tide",
        notes=(
            "The kid's second feature request. After the floods worked, he wanted "
            "another mechanic — water that's partly bad, partly clean.\n\n"
            "Set up the rest of the section: 'Now I'm going to walk you through what "
            "doing all of this AGAIN, for a second custom hazard, taught us about the "
            "patch design we'd just shipped.'"
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_contamination_seam(ctx: SlideContext) -> None:
    add_code_slide(
        ctx.prs,
        title="The Mixed Tide seam — contamination as a real mix ratio",
        code=(
            "// Found by decompiling Timberborn.WaterSystem and reading\n"
            "// SimulateContamination:\n"
            "//\n"
            "//   float num3 = (column.Contamination * (num2 + waterDisposed)\n"
            "//                 + contaminationChange) / num;\n"
            "//   _contaminationsBuffer[index3D] =\n"
            "//       (num3 > _maxWaterContamination) ? _maxWaterContamination : num3;\n"
            "//\n"
            "// Translation: WaterSource.Contamination IS the bad-water fraction\n"
            "// emitted at the source. It diffuses into the water-column simulation.\n"
            "//\n"
            "// → setting 0.3 yields 30% bad / 70% clean. The mechanic comes for free.\n"
            "\n"
            "internal class MixedTideContaminationController : TickableComponent, … {\n"
            "    public override void Tick() {\n"
            "        _waterSourceContamination.SetContamination(_settings.MixedTideContamination);\n"
            "    }\n"
            "}"
        ),
        caption="Read the simulation code, found the variable, set it to 0.3.",
        notes=(
            "The Mixed Tide mechanic is genuinely just 'find the right variable, write "
            "to it'. The hard part was confirming it was a real ratio and not a binary "
            "threshold somewhere downstream. We confirmed by reading the simulation.\n\n"
            "Hat-tip to the game's authors: the contamination value flows through the "
            "water-column simulation as a real fraction. No hardcoded 'if > 0 emit bad "
            "water'. Means our 30% setting produces actual 30%/70% mixed water that "
            "diffuses across the map naturally."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_new_gotchas(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="What doing-it-twice revealed",
        bullets=[
            "Every patch from the cascade needed one parallel branch for MixedTide",
            ("WeatherUIHelperPatch: drought spec for flood, badtide spec for mixed", 1),
            ("UIHelperLabelsPatch: 5 × postfix gets MixedTide branch", 1),
            ("SoundPlayerPatch, NotificationBackgroundPatch, … — same shape", 1),
            "Two NEW gotchas surfaced (added to CLAUDE.md):",
            ("TemplateModule.AddDecorator needs paired Bind<T>().AsTransient()", 1),
            ("IObjectLoader.Get throws on missing keys — use loader.Has() to guard", 1),
            "Plus: custom-weather entity controllers don't auto-wake on save-restore",
            ("Subscribe to HazardousWeatherSelectedEvent + check IsHazardousWeather", 1),
        ],
        notes=(
            "The headline for this slide: the patch design from Flood Season held up. "
            "Mixed Tide didn't require RESTRUCTURING anything — every existing patch "
            "just needed one more conditional branch.\n\n"
            "Three new gotchas to call out — each captured in CLAUDE.md so future-me "
            "doesn't re-step on them."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_workflow_gotchas(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="Build & workflow lessons",
        bullets=[
            "Timberborn's mod loader scans the folder RECURSIVELY — bin/obj/Code.dll → duplicate-load",
            ("Fix: Directory.Build.props redirects bin/obj outside the mod folder", 1),
            ("Subtle: the path MUST be absolute. Relative paths collapse from a worktree.", 1),
            "Claude Code creates worktrees at .claude/worktrees/<name>/ — INSIDE the mod folder",
            ("Symptom: 'mod loaded' 5× in Player.log, Harmony patch chain weird, UI flicker", 1),
            "Slide-quality commits + per-phase incrementalism as a deliberate practice",
            ("git log --oneline reads like a tutorial — by design", 1),
        ],
        notes=(
            "The worktree-pollution story is great talk material because it's "
            "non-obvious. The mod folder is your repo, your worktrees live inside the "
            "repo (Claude Code convention), worktree builds drop their output relative "
            "to the worktree, and the game's recursive scan picks them all up.\n\n"
            "We hit this in real time during a debugging session — five copies of "
            "Code.dll under the mod folder, Harmony wrapper name UpdateCurrentUI"
            "Specification_Patch5, every singleton registered five times. Documented "
            "the fix in Directory.Build.props.\n\n"
            "End on a positive: the commit history itself is documentation. Per-phase "
            "commits with detailed messages → git log reads as a tutorial for the next "
            "person who wants to mod this game."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_gotcha_catalogue(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title="CLAUDE.md — the gotcha catalogue",
        bullets=[
            "bin/obj outside the mod folder (absolute path, not relative)",
            "Harmony field-injection needs FOUR underscores for vanilla underscored fields",
            "Bindito AsSingleton() is lazy without a tagged interface (ILoadableSingleton)",
            "Vanilla type-checks throw on unknown IHazardousWeather (UI helper, sound, fog)",
            "Id-keyed lookups are spoofed via FloodWeather.Id = \"DroughtWeather\"",
            "Cycle weather is decided at cycle START — settings changes never retro-apply",
            "TemplateModule.AddDecorator needs paired Bind<T>().AsTransient()",
            "IObjectLoader.Get(PropertyKey<T>) throws on missing keys → use Has() to guard",
            "Custom weathers don't auto-fire entity controllers on save-restore",
        ],
        notes=(
            "If anyone takes a photo of one slide during the talk, it'll be this one. "
            "Land it slowly. These are the nine landmines documented in CLAUDE.md.\n\n"
            "'Slide-quality commit messages + a gotcha catalogue in the repo' is the "
            "practice. Next time someone in this room mods Timberborn, they have a "
            "shorter path through."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_what_he_taught(ctx: SlideContext) -> None:
    add_bullets_slide(
        ctx.prs,
        title=f"What {SON_NAME} taught me about programming",
        bullets=[
            CLOSING_REFLECTION,
            "",
            "(Customer-driven scoping. No bikeshedding on architecture. \"Make floods big.\")",
        ],
        notes=(
            "The closing bullet is yours, not the script's. Write something true and "
            "specific. The placeholder is just to make sure the deck reads end-to-end "
            "while you're iterating.\n\n"
            "Possible angles if you're stuck:\n"
            "  - Specs from a 6yo are EXACTLY what good specs should be: behaviour-"
            "centric, no implementation prescription.\n"
            "  - He was the world's most honest user-tester. 'It's boring' is brutal "
            "feedback.\n"
            "  - The kid couldn't have cared less about the patch design. He cared "
            "about whether the floods were big. That's the right thing to care about."
        ),
        section=ctx.section, slide_num=ctx.slide_num, total=ctx.total,
    )


def _slide_thanks_qa(ctx: SlideContext) -> None:
    add_title_slide(
        ctx.prs,
        title="Thanks",
        subtitle="Questions?",
        sub2=f"github.com/Spycho/timberborn-flood-mod   ·   designed by {SON_NAME}",
        notes=(
            "Q&A starts here. ~10 minutes.\n\n"
            "Likely questions to prep for:\n"
            "  - 'Why not contribute a PR to Timberborn instead?' — Mechanistry doesn't "
            "take random PRs; mod ecosystem is the supported extension point.\n"
            "  - 'Is this on the Steam Workshop?' — Not yet. The build is hardened for "
            "publication (paths, gitignore) but I haven't packaged it for Workshop.\n"
            "  - 'How long did this take?' — Roughly N evenings; specifics in git history.\n"
            "  - 'Could the kid actually read the code?' — He couldn't read most of it. He "
            "understood the SHAPE — 'this part picks the weather', 'this part draws the "
            "picture' — and he made decisions at that level."
        ),
    )


# --- Driver ------------------------------------------------------------------


def main() -> None:
    out_path = HERE / "timberborn-talk.pptx"
    SCREENSHOTS.mkdir(exist_ok=True)
    prs = build()
    prs.save(out_path)
    print(f"wrote {out_path}  ({len(prs.slides)} slides)")


if __name__ == "__main__":
    main()
