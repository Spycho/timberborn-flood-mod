using System.Reflection;
using HarmonyLib;

namespace Spycho.FloodSeason.Patches;

// Prefix on Timberborn.Localization.Loc.T(string) that short-circuits
// our FloodSeason.* loc-keys to the literal English strings in
// FloodSeasonLabels. The vanilla Loc.T returns the key verbatim and
// LogErrors "No localization for ..." when a key is missing, which
// would spam the log every UI tick during a flood. Resolving here
// avoids both the spam and a localization-file ship in this v1.
//
// Loc is *internal* and has several generic T<…> overloads that
// share the same method name. We use the TargetMethod pattern with
// AccessTools.Method to resolve the (string)→string overload at
// runtime — HarmonyPatch attributes don't expose a string-type +
// argumentTypes constructor for this combination.
[HarmonyPatch]
internal static class LocalizationPatch {

    static MethodBase TargetMethod() {
        return AccessTools.Method("Timberborn.Localization.Loc:T", new[] { typeof(string) });
    }

    [HarmonyPrefix]
    public static bool Prefix(string key, ref string __result) {
        if (key != null
            && key.StartsWith(FloodSeasonLabels.KeyPrefix)
            && FloodSeasonLabels.TryGet(key, out var value)) {
            __result = value;
            return false; // skip original, no error logged, no dict lookup
        }
        return true; // not ours — let the original run
    }

}
