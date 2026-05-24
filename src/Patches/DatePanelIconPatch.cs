using HarmonyLib;
using UnityEngine.UIElements;

namespace Spycho.FloodSeason.Patches;

// Overrides the top-right date-panel icon to the flood texture during a flood.
//
// DatePanel.UpdateIcon (Timberborn.WeatherSystemUI, internal) adds the
// IconClass it reads from HazardousWeatherUIHelper to the panel's _root.
// Because WeatherUIHelperPatch swaps the drought UI spec in when our
// flood is current, that class ends up as "date-panel--drought", which
// the CSS resolves to the drought sun texture. Visible result: a sun
// glyph during what the rest of the UI now calls "Flood".
//
// Cheapest fix: postfix UpdateIcon, find the child VisualElement that
// carries the "date-panel__icon" class, and assign style.backgroundImage
// inline. Inline styles override class-resolved styles, so the icon
// becomes ours without needing to inject a custom USS file.
//
// We do this every UpdateIcon call rather than just once because the
// game calls UpdateIcon on DaytimeStart — if a flood starts mid-day the
// icon class is removed and re-added. We want our override to follow
// that lifecycle. When flood is NOT current (back to drought / temperate
// / badtide), we clear our inline override so the vanilla CSS shows
// through again.
//
// DatePanel is internal, so we patch by string type name. UpdateIcon is
// private — Harmony's reflection-based patching reaches it regardless.
// _root is field-injected via the four-underscore convention (three for
// Harmony's prefix-strip + the field's own leading underscore).
[HarmonyPatch("Timberborn.WeatherSystemUI.DatePanel", "UpdateIcon")]
internal static class DatePanelIconPatch {

    private const string IconClass = "date-panel__icon";

    [HarmonyPostfix]
    public static void Postfix(VisualElement ____root) {
        // _root is set in DatePanel.Load. If UpdateIcon is somehow called
        // before Load completes, bail rather than throwing.
        if (____root == null) {
            return;
        }
        var icon = ____root.Q(className: IconClass);
        if (icon == null) {
            return;
        }
        var flood = FloodWeather.Instance;
        if (flood != null && flood.IsCurrent) {
            var bg = FloodArt.IconBackground;
            if (bg.HasValue) {
                icon.style.backgroundImage = new StyleBackground(bg.Value);
                return;
            }
            // PNG failed to load — fall through to clearing so we don't
            // freeze whatever previous override was sitting on the element.
        }
        // Not flood (or asset missing). Reset to StyleKeyword.Null so
        // class-based CSS resolution takes over again.
        icon.style.backgroundImage = StyleKeyword.Null;
    }

}
