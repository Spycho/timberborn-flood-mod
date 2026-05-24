using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using Timberborn.HazardousWeatherSystemUI;

namespace Spycho.FloodSeason.Patches;

// HazardousWeatherUIHelper exposes five string-returning loc-key
// properties (NameLocKey, ApproachingLocKey, InProgressLocKey,
// StartedNotificationLocKey, EndedNotificationLocKey) that consumers
// pass to ILoc.T(...). WeatherUIHelperPatch substitutes the drought UI
// spec so vanilla code doesn't throw — but then "Drought" labels leak
// through during our flood, which the player finds confusing.
//
// This file overrides those five getters when the current weather is
// our FloodWeather, returning flood-flavoured loc-keys. LocalizationPatch
// then resolves those keys into the literal strings without going near
// the game's localization dictionary (avoiding the "no localization for
// FloodSeason.Name" error spam).
//
// The three CSS-class properties (InProgressClass, IconClass,
// NotificationBackgroundClass) are not patched here — those keep the
// drought icon/colours, which is acceptable visual side-effect of the
// drought-spec substitution and not worth a separate patch surface.
//
// We deliberately use five separate patch classes rather than one
// Configurable [HarmonyPatch] with a TargetMethod, because Harmony's
// MethodType.Getter resolution is dirt-simple per property and the
// readability is better.

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.NameLocKey),
              MethodType.Getter)]
internal static class NameLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        if (____hazardousWeatherService.CurrentCycleHazardousWeather is FloodWeather) {
            __result = FloodSeasonLabels.NameKey;
        }
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.ApproachingLocKey),
              MethodType.Getter)]
internal static class ApproachingLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        if (____hazardousWeatherService.CurrentCycleHazardousWeather is FloodWeather) {
            __result = FloodSeasonLabels.ApproachingKey;
        }
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.InProgressLocKey),
              MethodType.Getter)]
internal static class InProgressLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        if (____hazardousWeatherService.CurrentCycleHazardousWeather is FloodWeather) {
            __result = FloodSeasonLabels.InProgressKey;
        }
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.StartedNotificationLocKey),
              MethodType.Getter)]
internal static class StartedNotificationLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        if (____hazardousWeatherService.CurrentCycleHazardousWeather is FloodWeather) {
            __result = FloodSeasonLabels.StartedKey;
        }
    }
}

[HarmonyPatch(typeof(HazardousWeatherUIHelper),
              nameof(HazardousWeatherUIHelper.EndedNotificationLocKey),
              MethodType.Getter)]
internal static class EndedNotificationLocKeyPatch {
    [HarmonyPostfix]
    public static void Postfix(HazardousWeatherService ____hazardousWeatherService, ref string __result) {
        if (____hazardousWeatherService.CurrentCycleHazardousWeather is FloodWeather) {
            __result = FloodSeasonLabels.EndedKey;
        }
    }
}
