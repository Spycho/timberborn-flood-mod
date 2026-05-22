using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using Timberborn.HazardousWeatherSystemUI;

namespace Kallikor.FloodSeason.Patches;

// Stops a crash when our FloodWeather becomes the active hazardous weather.
//
// HazardousWeatherUIHelper.UpdateCurrentUISpecification has a hardcoded
// if/else between DroughtWeather and BadtideWeather that throws
// InvalidOperationException for anything else — including our flood:
//
//     throw new InvalidOperationException("No UI for weather: " + ...);
//
// We can't easily provide our own IHazardousWeatherUISpecification
// because the interface is *internal* to the game's assembly. Quick
// patch: when our flood is current, redirect the helper to the existing
// drought UI spec (icon, colours, text). The labels will be wrong —
// "Drought" shows in the UI during a flood — but the game no longer
// crashes and the player can clearly see *some* hazardous state in
// progress. A proper FloodWeatherUISpecification belongs in a follow-up
// (would need InternalsVisibleTo, reflection trickery, or a custom UI
// element overlay).
//
// We touch three private fields via Harmony's ___fieldName injection:
//   _hazardousWeatherService    (to check the current weather)
//   _droughtWeatherUISpecification (to read the substitute)
//   _currentUISpecification     (to write — ref to assign the substitute)
//
// The interface type IHazardousWeatherUISpecification is internal, so
// we type-erase to `object` for the ref-write — Harmony's field-injection
// machinery handles the conversion.
[HarmonyPatch(typeof(HazardousWeatherUIHelper), "UpdateCurrentUISpecification")]
internal static class WeatherUIHelperPatch {

    [HarmonyPrefix]
    public static bool Prefix(
        HazardousWeatherService ___hazardousWeatherService,
        DroughtWeatherUISpecification ___droughtWeatherUISpecification,
        ref object ___currentUISpecification) {
        if (___hazardousWeatherService.CurrentCycleHazardousWeather is not FloodWeather) {
            // Not our weather — let the original method handle drought/badtide.
            return true;
        }
        ___currentUISpecification = ___droughtWeatherUISpecification;
        // Skip the original. The hardcoded if/else would otherwise throw
        // because our flood is neither DroughtWeather nor BadtideWeather.
        return false;
    }

}
