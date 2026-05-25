using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using Timberborn.HazardousWeatherSystemUI;

namespace Spycho.FloodSeason.Patches;

// Stops a crash when one of our custom weathers becomes the active
// hazardous weather.
//
// HazardousWeatherUIHelper.UpdateCurrentUISpecification has a hardcoded
// if/else between DroughtWeather and BadtideWeather that throws
// InvalidOperationException for anything else:
//
//     throw new InvalidOperationException("No UI for weather: " + ...);
//
// Both of our custom weathers (FloodWeather, MixedTideWeather) match
// neither branch, so the throw would fire as soon as the randomizer
// picks them. We can't easily provide our own
// IHazardousWeatherUISpecification because the interface is *internal*
// to the game's assembly — workaround is to redirect the helper to an
// existing UI spec for the closest vanilla analogue:
//
//   FloodWeather     → drought UI spec (visually wet, but vanilla
//                      drought theming is the closest match the game
//                      knows about). The label substitutions in
//                      UIHelperLabelsPatch then rewrite "Drought" →
//                      "Flood" wherever the helper exposes a loc key.
//   MixedTideWeather → badtide UI spec. Mixed tide spoofs Id="Badtide-
//                      Weather" already and reads thematically as a
//                      badtide variant, so the badtide spec is the
//                      right starting point. UIHelperLabelsPatch
//                      rewrites the labels to "Mixed Tide".
//
// We touch four private fields via Harmony's ___fieldName injection:
//   _hazardousWeatherService        (to check the current weather)
//   _droughtWeatherUISpecification  (substitute for FloodWeather)
//   _badtideWeatherUISpecification  (substitute for MixedTideWeather)
//   _currentUISpecification         (ref to assign the substitute)
//
// Harmony strips exactly three leading underscores from the parameter
// name and uses the rest as the field name verbatim. The game's fields
// all carry a leading underscore, so our parameter names need FOUR
// underscores total (___ + _fieldName).
//
// The interface type IHazardousWeatherUISpecification is internal, so
// we type-erase to `object` for the ref-write — Harmony's field-
// injection machinery handles the conversion.
[HarmonyPatch(typeof(HazardousWeatherUIHelper), "UpdateCurrentUISpecification")]
internal static class WeatherUIHelperPatch {

    [HarmonyPrefix]
    public static bool Prefix(
            HazardousWeatherService ____hazardousWeatherService,
            DroughtWeatherUISpecification ____droughtWeatherUISpecification,
            BadtideWeatherUISpecification ____badtideWeatherUISpecification,
            ref object ____currentUISpecification) {
        var current = ____hazardousWeatherService.CurrentCycleHazardousWeather;
        if (current is FloodWeather) {
            ____currentUISpecification = ____droughtWeatherUISpecification;
            return false;
        }
        if (current is MixedTideWeather) {
            ____currentUISpecification = ____badtideWeatherUISpecification;
            return false;
        }
        // Not one of our weathers — let the original method handle
        // drought / badtide normally.
        return true;
    }

}
