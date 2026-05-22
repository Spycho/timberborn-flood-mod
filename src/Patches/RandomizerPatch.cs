using HarmonyLib;
using Timberborn.HazardousWeatherSystem;

namespace Kallikor.FloodSeason.Patches;

// Harmony postfix on HazardousWeatherRandomizer.GetRandomWeatherForCycle.
//
// The randomizer's source is a hardcoded binary choice between Drought
// and Badtide — no DI extension point, no IEnumerable<IHazardousWeather>.
// So we let the original method run as normal, then *overwrite its result*
// with our FloodWeather. The randomizer's internal streak tracking still
// runs against the vanilla choice, which is fine for our purposes.
//
// PHASE 8: unconditionally replaces every hazardous cycle with a flood.
// PHASE 9 will add the enable / probability / grace-cycles gating.
[HarmonyPatch(typeof(HazardousWeatherRandomizer),
              nameof(HazardousWeatherRandomizer.GetRandomWeatherForCycle))]
internal static class RandomizerPatch {

    [HarmonyPostfix]
    public static void Postfix(ref IHazardousWeather __result) {
        var flood = FloodWeather.Instance;
        if (flood != null) {
            __result = flood;
        }
    }

}
