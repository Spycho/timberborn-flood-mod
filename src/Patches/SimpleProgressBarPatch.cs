using System.Reflection;
using HarmonyLib;
using Timberborn.CoreUI;
using UnityEngine.UIElements;

namespace Spycho.FloodSeason.Patches;

// Wins the race against Unity's custom-style resolver for the weather
// panel's progress bar.
//
// SimpleProgressBar reads its draw sprite from a private _image field,
// which is populated by a CustomStyleProperty<string> "--background-
// image" resolved via Resources.Load<Sprite>(value):
//
//     private void OnCustomStyleResolved(CustomStyleResolvedEvent e) {
//         _image = customStyle.TryGetValue(...) ? Resources.Load<Sprite>(...) : null;
//     }
//
// Because WeatherUIHelperPatch substitutes the drought UI spec into
// HazardousWeatherUIHelper, the weather panel's root gets the
// "weather-panel--dry" class, and Unity's CSS pipeline resolves the
// custom property to the drought sprite path. The bar then draws the
// drought texture via OnGenerateVisualContent.
//
// WeatherPanelBackgroundPatch (postfix on WeatherPanel.UpdatePanel) was
// supposed to override _image with our flood sprite. It didn't work
// because custom-style resolution is DEFERRED — UpdatePanel's
// Remove+AddToClassList marks the element dirty, UpdatePanel returns,
// our postfix sets _image to flood, then Unity processes the queued
// resolution and OnCustomStyleResolved fires, overwriting _image back
// to drought before the next repaint. Drought wins every frame.
//
// This patch postfixes OnCustomStyleResolved itself, so our override
// runs AFTER vanilla writes _image. Whichever order Unity flushes
// resolutions in, our value is the last write into _image before the
// next repaint reads it.
//
// SCOPE: we only override progress bars whose ancestor chain has a
// class containing "weather-panel" — without that gate, every progress
// bar in the game (manufactory throughput, science, etc.) would render
// the flood texture during a flood. The walk is bounded by depth so a
// runaway ancestry tree doesn't eat the frame.
//
// SimpleProgressBar is public, so we can patch by typeof rather than a
// string name. OnCustomStyleResolved is private; AccessTools resolves
// it via reflection regardless.
[HarmonyPatch(typeof(SimpleProgressBar), "OnCustomStyleResolved")]
internal static class SimpleProgressBarPatch {

    private const string WeatherPanelMarker = "weather-panel";
    private const int MaxAncestorWalk = 10;

    private static readonly FieldInfo? BarImageField =
        AccessTools.Field(typeof(SimpleProgressBar), "_image");

    [HarmonyPostfix]
    public static void Postfix(SimpleProgressBar __instance) {
        if (BarImageField == null || __instance == null) {
            return;
        }
        if (!IsWeatherPanelBar(__instance)) {
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

    private static bool IsWeatherPanelBar(VisualElement bar) {
        VisualElement? cursor = bar.parent;
        int depth = 0;
        while (cursor != null && depth < MaxAncestorWalk) {
            foreach (var cls in cursor.GetClasses()) {
                if (cls != null && cls.Contains(WeatherPanelMarker)) {
                    return true;
                }
            }
            cursor = cursor.parent;
            depth++;
        }
        return false;
    }

}
