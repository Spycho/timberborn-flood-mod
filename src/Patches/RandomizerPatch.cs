using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using UnityEngine;

namespace Spycho.FloodSeason.Patches;

// Harmony postfix on HazardousWeatherRandomizer.GetRandomWeatherForCycle.
//
// The randomizer's source is a hardcoded binary choice between Drought
// and Badtide — no DI extension point, no IEnumerable<IHazardousWeather>.
// So we let the original method run as normal, then *overwrite its
// result* with our FloodWeather when the player has opted in and a dice
// roll succeeds. The randomizer's internal streak tracking still runs
// against the vanilla choice, which is fine for our purposes.
//
// The `cycle` parameter is the absolute game cycle (1, 2, 3, …) — the
// same value BadtideWeather.CanOccurAtCycle uses for its grace-period
// gate. It is NOT the per-weather occurrence count the duration math
// uses (that one comes from HazardousWeatherHistory inside the service).
[HarmonyPatch(typeof(HazardousWeatherRandomizer),
              nameof(HazardousWeatherRandomizer.GetRandomWeatherForCycle))]
internal static class RandomizerPatch {

    [HarmonyPostfix]
    public static void Postfix(int cycle, ref IHazardousWeather __result) {
        var settings = FloodSeasonSettings.Instance;
        var flood = FloodWeather.Instance;
        if (settings == null || flood == null) {
            return;
        }
        if (!settings.FloodSeasonEnabled.Value) {
            Debug.Log($"[Flood Season] cycle {cycle}: feature off, keeping vanilla {__result?.Id}");
            return;
        }
        if (cycle <= settings.FloodGraceCycles.Value) {
            Debug.Log($"[Flood Season] cycle {cycle}: within grace ({settings.FloodGraceCycles.Value}), keeping vanilla {__result?.Id}");
            return;
        }
        // Random.value is a uniform [0, 1] float; scaling to percent keeps
        // the slider's UI semantics ("30% means 30 out of 100 cycles") obvious.
        // Note: this uses UnityEngine.Random (unseeded), not the game's
        // IRandomNumberGenerator. Saves are therefore not deterministic for
        // flood occurrences — acceptable, we're not aiming for replay parity.
        if (Random.value * 100f > settings.FloodProbabilityPercent.Value) {
            Debug.Log($"[Flood Season] cycle {cycle}: probability roll missed");
            return;
        }
        Debug.Log($"[Flood Season] cycle {cycle}: replacing vanilla {__result?.Id} with flood");
        __result = flood;
    }

}
