using HarmonyLib;
using UnityEngine.UIElements;

namespace Spycho.FloodSeason.Patches;

// Overrides the big central "X has begun!" / "X is approaching!" banner
// background to our flood texture during a flood.
//
// HazardousWeatherNotificationPanel.ShowNotification (private, internal
// class, Timberborn.HazardousWeatherSystemUI) is the shared chokepoint
// for every banner the panel fades in. Three call sites reach it:
//
//   • OnHazardousWeatherStarted    → "Flood has begun!"
//   • UpdateSingleton (approaching)→ "Flood is approaching!"
//   • OnCycleEndedEvent (temperate)→ "Cycle N begins"
//
// The first two pass isHazardous=true and add NotificationBackgroundClass
// ("hazardous-weather-notification__background--dry" after our drought-
// spec substitution) to the Image element named "Background". The
// vanilla CSS then resolves that class to the drought artwork. An
// earlier version of this patch only intercepted OnHazardousWeather-
// Started, so the *approaching* banner stayed drought-textured even
// during a flood cycle.
//
// Postfixing the shared ShowNotification lets one patch handle both
// hazardous paths uniformly. Check FloodWeather.IsCurrent rather than
// the (no-longer-available) event payload: while a flood cycle is
// active, CurrentCycleHazardousWeather IS FloodWeather, so the same
// check works for "started" and "approaching" alike.
//
// For non-flood hazardous notifications (real drought, real badtide)
// and the temperate cycle-begin notification, we clear any stale
// inline override so the vanilla CSS class picks the correct image.
// Vanilla never sets an inline backgroundImage on _background itself,
// so clearing is safe — the class-based image always resurfaces.
[HarmonyPatch("Timberborn.HazardousWeatherSystemUI.HazardousWeatherNotificationPanel",
              "ShowNotification")]
internal static class NotificationBackgroundPatch {

    [HarmonyPostfix]
    public static void Postfix(bool isHazardous, Image ____background) {
        if (____background == null) {
            return;
        }
        if (isHazardous && FloodWeather.Instance is { IsCurrent: true }) {
            var bg = FloodArt.NotificationBackground;
            if (bg.HasValue) {
                ____background.style.backgroundImage = new StyleBackground(bg.Value);
                return;
            }
            // PNG missing — fall through to clearing so the vanilla CSS
            // class shows through rather than rendering nothing.
        }
        // Not a flood-themed notification (or asset missing). Clear any
        // stale inline override so the class-driven CSS background-image
        // resolves correctly (drought, badtide, or wet-weather).
        ____background.style.backgroundImage = StyleKeyword.Null;
    }

}
