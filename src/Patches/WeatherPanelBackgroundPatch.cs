using HarmonyLib;
using Timberborn.CoreUI;
using Timberborn.WeatherSystem;
using UnityEngine.UIElements;

namespace Spycho.FloodSeason.Patches;

// Overrides the wide hazard-themed background BEHIND the progress bar
// during the in-progress phase of a custom hazard: flood texture for a
// Flood, mixed-tide texture for a Mixed Tide.
//
// Vanilla weather panel composition (per the user's observation against
// real drought / badtide cycles):
//
//   • In-progress phase
//       background of the SimpleProgressBar = HAZARD texture (drought,
//           badtide; substituted to drought-bg by our spec spoof)
//       mesh fill of the SimpleProgressBar  = TEMPERATE sprite
//       As progress 0→1, the temperate sprite covers the hazard
//       backdrop, ending at a fully-temperate bar — narratively "the
//       hazard is over".
//
//   • Approaching phase (~3 days before hazard starts)
//       background = TEMPERATE
//       mesh fill  = HAZARD sprite
//       As approach 0→1, the hazard sprite covers temperate, ending
//       fully-hazard before the in-progress phase begins.
//
// So during in-progress the bar's BACKGROUND is the drought-themed
// texture (and the mesh fill is temperate). Earlier patches tried to
// override _image (the mesh fill) to flood; that produced "flood
// covering drought" instead of "temperate covering flood" — wrong
// reading of the slot the texture lives in.
//
// This patch handles only the in-progress override: set the bar's
// inline backgroundImage to our flood texture so the wide hazard
// backdrop is flood-themed instead of drought-themed. Vanilla's mesh
// fill stays temperate, untouched.
//
// The approaching-phase override lives in SimpleProgressBarPatch — it
// has to fight Unity's deferred custom-style resolver, so the postfix
// has to run from the resolver itself, not from UpdatePanel.
//
// WeatherPanel is internal — patch by string type name. _simpleProgress-
// Bar and _weatherService are field-injected (Harmony strips three
// underscores from the parameter name; game field names carry a
// leading underscore; four underscores total).
[HarmonyPatch("Timberborn.WeatherSystemUI.WeatherPanel", "UpdatePanel")]
internal static class WeatherPanelBackgroundPatch {

    [HarmonyPostfix]
    public static void Postfix(
            SimpleProgressBar ____simpleProgressBar,
            WeatherService ____weatherService) {
        if (____simpleProgressBar == null) {
            return;
        }
        if (____weatherService.IsHazardousWeather) {
            Background? bg = null;
            if (FloodWeather.Instance is { IsCurrent: true }) {
                bg = FloodArt.NotificationBackground;
            } else if (MixedTideWeather.Instance is { IsCurrent: true }) {
                bg = FloodArt.MixedTideNotificationBackground;
            }
            if (bg.HasValue) {
                ____simpleProgressBar.style.backgroundImage = new StyleBackground(bg.Value);
                return;
            }
            // Either vanilla hazard (no override) or matching PNG
            // failed to load — fall through to clearing.
        }
        // Not in-progress of a custom hazard (or asset missing). Reset
        // to StyleKeyword.Null so vanilla CSS resolves the correct
        // class-driven background (drought, badtide, temperate-of-
        // approaching, or nothing).
        ____simpleProgressBar.style.backgroundImage = StyleKeyword.Null;
    }

}
