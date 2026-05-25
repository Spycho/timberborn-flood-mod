using HarmonyLib;
using Timberborn.HazardousWeatherSystem;

namespace Spycho.FloodSeason.Patches;

// HazardousWeatherSoundPlayer.OnHazardousWeatherStarted is another
// hardcoded if/else that throws on unknown weather types:
//
//     if (event.HazardousWeather is BadtideWeather) ...
//     else if (event.HazardousWeather is DroughtWeather) ...
//     else throw new ArgumentException("No start sound for weather type: " + ...);
//
// When our flood becomes active, the HazardousWeatherStartedEvent fires
// with our FloodWeather as the payload — neither branch matches and
// the throw triggers, which would happen ~mid-cycle. Silence (skipping
// the original) is the cheapest fix; a proper FloodWeatherStartSound
// would need new audio assets and a configurator binding, out of scope
// for the crash fix.
//
// The sound player class is *internal*, so we patch by string name
// (same trick as DroughtFloorPatch).
[HarmonyPatch("Timberborn.HazardousWeatherSystemUI.HazardousWeatherSoundPlayer",
              "OnHazardousWeatherStarted")]
internal static class SoundPlayerPatch {

    [HarmonyPrefix]
    public static bool Prefix(HazardousWeatherStartedEvent hazardousWeatherStartedEvent) {
        if (hazardousWeatherStartedEvent.HazardousWeather is FloodWeather
            || hazardousWeatherStartedEvent.HazardousWeather is MixedTideWeather) {
            // Skip the original — no dedicated start sound for our
            // custom weathers, but no crash either (vanilla's hardcoded
            // if/else throws on unknown types).
            return false;
        }
        return true;
    }

}
