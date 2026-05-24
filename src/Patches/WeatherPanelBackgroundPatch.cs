using HarmonyLib;
using Timberborn.CoreUI;
using Timberborn.WeatherSystem;
using UnityEngine;

namespace Spycho.FloodSeason.Patches;

// Overrides the wide drought/badtide graphic on the in-progress weather
// panel (top-right, under the cycle counter) to a flood texture during
// a flood.
//
// First-pass attempt was to override _root.style.backgroundImage. That
// did nothing visually because the wide texture isn't a normal CSS
// background-image at all — it's a Sprite drawn directly by the
// SimpleProgressBar's custom mesh:
//
//     // SimpleProgressBar.cs
//     private Sprite _image;
//     private static readonly CustomStyleProperty<string>
//         BackgroundImageProperty = new CustomStyleProperty<string>(
//             "--background-image");
//     private void OnCustomStyleResolved(...) {
//         _image = customStyle.TryGetValue(...) ? Resources.Load<Sprite>(...) : null;
//     }
//     private void OnGenerateVisualContent(MeshGenerationContext mgc) {
//         meshWriter.StartWriting(mgc, _image.texture);
//         WriteMesh(...);  // draws _image revealed left-to-right by _progress
//     }
//
// CSS sets a custom property (--background-image) keyed on the
// "weather-panel--dry" class our spec substitution forces. The bar
// resolves that to a Sprite via Resources.Load and draws the texture as
// a custom mesh that scrolls right as the hazard progresses. The
// "wide graphic that shows how long is left" the player sees is this
// mesh.
//
// Override approach: postfix WeatherPanel.UpdatePanel, reach into the
// _simpleProgressBar's private _image via reflection, swap it with a
// Sprite view of our flood texture. Cache the vanilla Sprite the first
// time we override so we can restore it cleanly when the flood ends
// (Sprite assignment is destructive; we don't get the original back
// from Resources.Load for free).
//
// IMPORTANT: gate on _weatherService.IsHazardousWeather as well as
// FloodWeather.IsCurrent, same as DatePanelIconPatch — Current-
// CycleHazardousWeather is set for the WHOLE cycle (temperate phase
// included), and we don't want to paint the flood bar during the
// non-hazardous half of the cycle.
//
// WeatherPanel is internal so we patch by string type name. _image is
// private; AccessTools resolves it once at class init.
[HarmonyPatch("Timberborn.WeatherSystemUI.WeatherPanel", "UpdatePanel")]
internal static class WeatherPanelBackgroundPatch {

    private static readonly System.Reflection.FieldInfo? BarImageField =
        AccessTools.Field(typeof(SimpleProgressBar), "_image");

    // Cache the vanilla Sprite (loaded by SimpleProgressBar from the
    // CSS-resolved Resources path) so we can put it back on flood exit.
    // `_isOverriding` distinguishes "we're currently substituting" from
    // "vanilla naturally had a null sprite" — both leave _cachedVanilla-
    // Sprite as null but mean very different things.
    private static Sprite? _cachedVanillaSprite;
    private static bool _isOverriding;

    [HarmonyPostfix]
    public static void Postfix(
            SimpleProgressBar ____simpleProgressBar,
            WeatherService ____weatherService) {
        if (____simpleProgressBar == null || BarImageField == null) {
            return;
        }
        var flood = FloodWeather.Instance;
        bool shouldOverride =
            flood != null
            && flood.IsCurrent
            && ____weatherService.IsHazardousWeather;

        if (shouldOverride) {
            var floodSprite = FloodArt.NotificationSprite;
            if (floodSprite == null) {
                // PNG missing — leave vanilla sprite alone so SOMETHING
                // renders instead of an invisible bar.
                return;
            }
            var currentImage = (Sprite?)BarImageField.GetValue(____simpleProgressBar);
            if (ReferenceEquals(currentImage, floodSprite)) {
                // Already overridden this frame, nothing to do.
                return;
            }
            if (!_isOverriding) {
                _cachedVanillaSprite = currentImage;
                _isOverriding = true;
            }
            BarImageField.SetValue(____simpleProgressBar, floodSprite);
            ____simpleProgressBar.MarkDirtyRepaint();
            return;
        }
        if (_isOverriding) {
            // Flood just ended (or we left the hazardous phase). Put the
            // vanilla Sprite back so drought/badtide/temperate render
            // correctly. If vanilla's value was null (no --background-
            // image custom property set), restoring null is correct —
            // SimpleProgressBar.OnGenerateVisualContent no-ops on null.
            BarImageField.SetValue(____simpleProgressBar, _cachedVanillaSprite);
            ____simpleProgressBar.MarkDirtyRepaint();
            _isOverriding = false;
            _cachedVanillaSprite = null;
        }
    }

}
