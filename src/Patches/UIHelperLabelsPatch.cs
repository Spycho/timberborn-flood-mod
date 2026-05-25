using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using Timberborn.HazardousWeatherSystemUI;

namespace Spycho.FloodSeason.Patches;

// HazardousWeatherUIHelper exposes five string-returning loc-key
// properties (NameLocKey, ApproachingLocKey, InProgressLocKey,
// StartedNotificationLocKey, EndedNotificationLocKey) that consumers
// pass to ILoc.T(...). WeatherUIHelperPatch substitutes the
// drought / badtide UI spec so vanilla code doesn't throw — but then
// "Drought" or "Badtide" labels leak through during our custom
// hazards, which the player finds confusing.
//
// This file overrides those five getters per custom weather:
//   • FloodWeather     → FloodSeasonLabels.* (flood-flavoured)
//   • MixedTideWeather → FloodSeasonLabels.MixedTide* (mixed-tide-
//                        flavoured)
// LocalizationPatch then resolves those keys into the literal strings
// without going near the game's localization dictionary (avoiding the
// "no localization for FloodSeason.Name" error spam).
//
// The three CSS-class properties (InProgressClass, IconClass,
// NotificationBackgroundClass) are not patched here — those keep the
// vanilla icon/colours from the spoofed spec, which is the visual
// fallback Phase E replaces with custom art. Patching them here would
// fight Phase E's art overrides.
//
// We deliberately use five separate patch classes rather than one
// Configurable [HarmonyPatch] with a TargetMethod, because Harmony's
// MethodType.Getter resolution is dirt-simple per property and the
// readability is better.
//
// Each getter postfix follows the same shape: type-check on
// CurrentCycleHazardousWeather → pick the matching loc-key family.
// The shared SubstituteLabel helper keeps the bodies one line each.

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.NameLocKey),
              MethodType.Getter)]
internal static class NameLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        UIHelperLabelsHelper.SubstituteLabel(
            ____hazardousWeatherService,
            FloodSeasonLabels.NameKey,
            FloodSeasonLabels.MixedTideNameKey,
            ref __result);
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.ApproachingLocKey),
              MethodType.Getter)]
internal static class ApproachingLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        UIHelperLabelsHelper.SubstituteLabel(
            ____hazardousWeatherService,
            FloodSeasonLabels.ApproachingKey,
            FloodSeasonLabels.MixedTideApproachingKey,
            ref __result);
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.InProgressLocKey),
              MethodType.Getter)]
internal static class InProgressLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        UIHelperLabelsHelper.SubstituteLabel(
            ____hazardousWeatherService,
            FloodSeasonLabels.InProgressKey,
            FloodSeasonLabels.MixedTideInProgressKey,
            ref __result);
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.StartedNotificationLocKey),
              MethodType.Getter)]
internal static class StartedNotificationLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        UIHelperLabelsHelper.SubstituteLabel(
            ____hazardousWeatherService,
            FloodSeasonLabels.StartedKey,
            FloodSeasonLabels.MixedTideStartedKey,
            ref __result);
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.EndedNotificationLocKey),
              MethodType.Getter)]
internal static class EndedNotificationLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        UIHelperLabelsHelper.SubstituteLabel(
            ____hazardousWeatherService,
            FloodSeasonLabels.EndedKey,
            FloodSeasonLabels.MixedTideEndedKey,
            ref __result);
    }
}

internal static class UIHelperLabelsHelper {
    // Branch on the current weather's C# type (NOT its spoofed Id —
    // Id collides intentionally with vanilla drought / badtide). For
    // non-flood-non-mixed weathers we leave __result alone so vanilla
    // drought / badtide loc-keys flow through unchanged.
    public static void SubstituteLabel(
            HazardousWeatherService hazardousWeatherService,
            string floodLocKey,
            string mixedTideLocKey,
            ref string result) {
        var current = hazardousWeatherService.CurrentCycleHazardousWeather;
        if (current is FloodWeather) {
            result = floodLocKey;
        } else if (current is MixedTideWeather) {
            result = mixedTideLocKey;
        }
    }
}
