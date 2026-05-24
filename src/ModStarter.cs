using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace Kallikor.FloodSeason;

// IModStarter.StartMod runs once when the game loads our mod, before any
// scene is active. Two jobs:
//
//   1. Discover and apply every [HarmonyPatch] class in this assembly via
//      Harmony.PatchAll(). This is what hooks our patches into the game's
//      methods — without it, the [HarmonyPatch] attributes are inert.
//   2. Log a confirmation line so we can verify the mod loaded.
//
// The Harmony id is just a string label used to group patches; if we ever
// needed to remove our patches at runtime we'd identify them by this id.
internal class ModStarter : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
        // Stash the mod folder path for FloodArt to find the PNGs.
        // ModPath is the absolute path to this mod's directory; PNGs
        // sit in <ModPath>/assets/. Patches access them via FloodArt's
        // static accessors (patches are static methods and can't take DI).
        FloodArt.ModPath = modEnvironment.ModPath;

        new Harmony("Kallikor.FloodSeason").PatchAll();
        Debug.Log("[Flood Season] mod loaded");
    }

}
