using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using Timberborn.WeatherSystem;
using UnityEngine;

namespace Kallikor.FloodSeason.Patches;

// Postfix on the game's DroughtWaterStrengthModifier.GetStrengthModifier
// that adds the user's DroughtAdditive setting on top, clamped to [0, 1].
//
// We do this here rather than in our own modifier because the per-source
// strength is computed by *multiplying* every modifier's return value
// into the running product. The game's drought modifier hits 0 at peak
// drought; multiplying our setting on top of that still gives 0. The
// only way to inject an additive contribution is to alter the game's
// own return value before WaterSource.UpdateCurrentStrength sees it.
//
// DroughtWaterStrengthModifier is *internal*, so we can't write
// typeof(DroughtWaterStrengthModifier) from this assembly — the C#
// compiler refuses. Harmony's string-overload of [HarmonyPatch] resolves
// the type by its full name at runtime, side-stepping the access check.
//
// We use ___fieldName parameter injection to read the patched object's
// private _hazardousWeatherService field — that lets us check we're
// actually in a drought rather than badtide/flood (the game's modifier
// returns 1.0 in those cases and we don't want our additive bonus
// raising flow above normal then).
[HarmonyPatch("Timberborn.WaterSourceSystem.DroughtWaterStrengthModifier",
              "GetStrengthModifier")]
internal static class DroughtFloorPatch {

    [HarmonyPostfix]
    public static void Postfix(
        ref float __result,
        HazardousWeatherService ___hazardousWeatherService,
        WeatherService ___weatherService) {
        var settings = FloodSeasonSettings.Instance;
        if (settings == null) {
            return;
        }
        // Only inject during an actual drought cycle. Outside drought
        // the game's modifier returns 1.0; raising that above 1 would
        // double-boost flow alongside our own multiplier.
        if (!___weatherService.IsHazardousWeather) {
            return;
        }
        if (___hazardousWeatherService.CurrentCycleHazardousWeather is not DroughtWeather) {
            return;
        }
        __result = Mathf.Clamp01(__result + settings.DroughtAdditive);
    }

}
