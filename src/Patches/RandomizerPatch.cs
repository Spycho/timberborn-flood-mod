using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using UnityEngine;

namespace Spycho.FloodSeason.Patches;

// Harmony postfix on HazardousWeatherRandomizer.GetRandomWeatherForCycle.
//
// The randomizer's source is a hardcoded binary choice between Drought
// and Badtide — no DI extension point, no IEnumerable<IHazardousWeather>.
// So we let the original method run, then *overwrite its result* with
// our FloodWeather or MixedTideWeather when settings + dice say so.
//
// PROBABILITY SCHEME — weighted single roll:
//
//   1. Compute each custom hazard's effective probability (0 if its
//      feature is off OR we're still in its grace period).
//   2. Sum them. If 0, leave vanilla untouched.
//   3. If the sum is ≤100, the two probabilities ARE the slices and the
//      remainder is the vanilla slice (e.g. flood 30 + mixed 30 → 30%
//      flood, 30% mixed, 40% vanilla drought/badtide).
//   4. If the sum is >100, normalise so they share 100% (vanilla = 0):
//      floodSlice = floodP * 100 / total, mixedSlice = 100 - floodSlice.
//      Integer-truncation can round-off one percentage point at most;
//      acceptable for v1.
//   5. Single Random.value roll in [0, 100). Falls in the flood slice →
//      flood; falls in the mixed slice → mixed tide; otherwise keep
//      vanilla.
//
// The `cycle` parameter is the absolute game cycle (1, 2, 3, …), same
// as BadtideWeather.CanOccurAtCycle's grace gate. It is NOT the
// per-weather occurrence count the duration math uses (that one comes
// from HazardousWeatherHistory inside the service).
//
// Note: we use UnityEngine.Random, not the game's seeded
// IRandomNumberGenerator, so the roll is non-deterministic across
// reloads. Acceptable — we're not aiming for replay parity, and the
// roll happens once per cycle at SetForCycle time.
[HarmonyPatch(typeof(HazardousWeatherRandomizer),
              nameof(HazardousWeatherRandomizer.GetRandomWeatherForCycle))]
internal static class RandomizerPatch {

    [HarmonyPostfix]
    public static void Postfix(int cycle, ref IHazardousWeather __result) {
        var settings = FloodSeasonSettings.Instance;
        if (settings == null) {
            return;
        }
        int floodP = EffectiveProbability(
            settings.FloodSeasonEnabled.Value,
            cycle,
            settings.FloodGraceCycles.Value,
            settings.FloodProbabilityPercent.Value);
        int mixedP = EffectiveProbability(
            settings.MixedTideEnabled.Value,
            cycle,
            settings.MixedTideGraceCycles.Value,
            settings.MixedTideProbabilityPercent.Value);
        int total = floodP + mixedP;
        if (total == 0) {
            Debug.Log($"[Flood Season] cycle {cycle}: no custom hazard active, keeping vanilla {__result?.Id}");
            return;
        }
        int floodSlice = floodP;
        int mixedSlice = mixedP;
        if (total > 100) {
            floodSlice = floodP * 100 / total;
            mixedSlice = 100 - floodSlice;
        }
        float roll = Random.value * 100f;
        if (roll < floodSlice) {
            var flood = FloodWeather.Instance;
            if (flood != null) {
                Debug.Log($"[Flood Season] cycle {cycle}: roll {roll:F1} < {floodSlice} (flood slice) → replacing vanilla {__result?.Id} with flood");
                __result = flood;
            }
            return;
        }
        if (roll < floodSlice + mixedSlice) {
            var mixed = MixedTideWeather.Instance;
            if (mixed != null) {
                Debug.Log($"[Flood Season] cycle {cycle}: roll {roll:F1} in [{floodSlice},{floodSlice + mixedSlice}) (mixed slice) → replacing vanilla {__result?.Id} with mixed tide");
                __result = mixed;
            }
            return;
        }
        Debug.Log($"[Flood Season] cycle {cycle}: roll {roll:F1} ≥ {floodSlice + mixedSlice}, keeping vanilla {__result?.Id}");
    }

    private static int EffectiveProbability(bool enabled, int cycle, int graceCycles, int probabilityPercent) {
        if (!enabled) return 0;
        if (cycle <= graceCycles) return 0;
        return System.Math.Max(0, probabilityPercent);
    }

}
