using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using UnityEngine.UIElements;

namespace Spycho.FloodSeason.Patches;

// Overrides the "Flood has begun!" banner background to our flood texture.
//
// HazardousWeatherNotificationPanel.OnHazardousWeatherStarted (internal,
// Timberborn.HazardousWeatherSystemUI) is the [OnEvent] handler that
// fades in the 757x174 banner whenever a hazardous weather starts. Its
// private ShowNotification helper adds the
// NotificationBackgroundClass ("hazardous-weather-notification__background--dry"
// after our drought-spec substitution) to the Image element named
// "Background". The vanilla CSS resolves that class to the drought
// notification artwork.
//
// We postfix OnHazardousWeatherStarted: the event payload tells us
// directly whether the weather that just started is our FloodWeather, so
// we don't need to round-trip through FloodWeather.IsCurrent. If yes,
// reach into the private _background field and slap our texture on it
// via inline style — overrides the class-derived background-image.
//
// We don't bother resetting the inline style on subsequent non-flood
// notifications because the class machinery + transition animation
// re-applies the right background-image on each new banner anyway.
[HarmonyPatch("Timberborn.HazardousWeatherSystemUI.HazardousWeatherNotificationPanel",
              "OnHazardousWeatherStarted")]
internal static class NotificationBackgroundPatch {

    [HarmonyPostfix]
    public static void Postfix(
            HazardousWeatherStartedEvent hazardousWeatherStartedEvent,
            Image ____background) {
        if (hazardousWeatherStartedEvent.HazardousWeather is not FloodWeather) {
            // For drought/badtide, the vanilla CSS class is correct.
            // Clear any stale inline override left over from a previous
            // flood so the new banner displays the right texture.
            if (____background != null) {
                ____background.style.backgroundImage = StyleKeyword.Null;
            }
            return;
        }
        if (____background == null) {
            return;
        }
        var bg = FloodArt.NotificationBackground;
        if (!bg.HasValue) {
            return;
        }
        ____background.style.backgroundImage = new StyleBackground(bg.Value);
    }

}
