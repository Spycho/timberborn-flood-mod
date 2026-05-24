using System.Reflection;
using HarmonyLib;
using Timberborn.CoreUI;
using UnityEngine.UIElements;

namespace Spycho.FloodSeason.Patches;

// Overrides the SimpleProgressBar's mesh-fill sprite to flood during
// the APPROACHING phase of a flood cycle.
//
// SimpleProgressBar's draw is a custom mesh:
//
//     private Sprite _image;
//     private void OnCustomStyleResolved(...) {
//         _image = customStyle.TryGetValue(BackgroundImageProperty, ...)
//             ? Resources.Load<Sprite>(value) : null;
//     }
//     private void OnGenerateVisualContent(MeshGenerationContext mgc) {
//         meshWriter.StartWriting(mgc, _image.texture);
//         WriteMesh(...);  // reveals more of _image left-to-right as _progress grows
//     }
//
// The _image is loaded from a CustomStyleProperty<string>
// "--background-image" that the (drought-spoofed) USS class resolves
// to a Resources sprite path.
//
// During the in-progress phase, vanilla's CSS sets _image to the
// TEMPERATE sprite — narratively "this is what the bar fills towards
// as the hazard ends" — and we leave that alone
// (WeatherPanelBackgroundPatch handles the hazard-backdrop swap).
//
// During the approaching phase, vanilla sets _image to the HAZARD
// sprite (drought, after our spec spoof) — narratively "this is what
// the bar fills towards as the hazard arrives". For flood-themed
// continuity we want a flood sprite here instead.
//
// Why we postfix OnCustomStyleResolved instead of overriding from
// WeatherPanel.UpdatePanel: Unity's custom-style resolution is
// DEFERRED. UpdatePanel's Remove+AddToClassList queues a style
// invalidation, UpdatePanel returns, and only then does Unity flush
// the resolution and fire OnCustomStyleResolved, which overwrites
// whatever _image we just set. Postfixing the resolver itself
// guarantees our override is the last write into _image before the
// next repaint.
//
// SCOPE: only override bars whose ancestor chain has "weather-
// approaching". The walk stops early if "weather-in-progress" is seen
// first — that means we're an in-progress bar (vanilla _image is
// temperate, correct, leave it). If neither class is found within the
// walk depth, the bar isn't a weather panel bar at all (could be
// manufactory throughput, science, etc.) and we leave it alone.
[HarmonyPatch(typeof(SimpleProgressBar), "OnCustomStyleResolved")]
internal static class SimpleProgressBarPatch {

    private const int MaxAncestorWalk = 10;

    private static readonly FieldInfo? BarImageField =
        AccessTools.Field(typeof(SimpleProgressBar), "_image");

    [HarmonyPostfix]
    public static void Postfix(SimpleProgressBar __instance) {
        if (BarImageField == null || __instance == null) {
            return;
        }
        if (!IsWeatherPanelApproachingBar(__instance)) {
            return;
        }
        var flood = FloodWeather.Instance;
        if (flood == null || !flood.IsCurrent) {
            return;
        }
        var floodSprite = FloodArt.NotificationSprite;
        if (floodSprite == null) {
            return;
        }
        BarImageField.SetValue(__instance, floodSprite);
        __instance.MarkDirtyRepaint();
    }

    // True iff the bar's ancestor chain reaches a "weather-approaching"
    // class before either "weather-in-progress" or running out of
    // ancestors. The approaching/in-progress classes are mutually
    // exclusive (vanilla UpdateHazardousWeatherClasses adds exactly
    // one), so seeing in-progress first means the bar is in the
    // in-progress phase and we shouldn't touch _image.
    private static bool IsWeatherPanelApproachingBar(VisualElement bar) {
        VisualElement? cursor = bar.parent;
        int depth = 0;
        while (cursor != null && depth < MaxAncestorWalk) {
            if (cursor.ClassListContains("weather-approaching")) {
                return true;
            }
            if (cursor.ClassListContains("weather-in-progress")) {
                return false;
            }
            cursor = cursor.parent;
            depth++;
        }
        return false;
    }

}
