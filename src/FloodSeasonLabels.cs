using System.Collections.Generic;

namespace Spycho.FloodSeason;

// Loc-keys and their English values for the flood weather UI strings.
// Kept in one place so LocalizationPatch can resolve them and
// UIHelperLabelsPatch can produce them.
//
// The keys are namespaced "FloodSeason.*" so LocalizationPatch can
// recognise them with a simple prefix check (without bloating the
// hot path of every ILoc.T call). Vanilla loc-keys are never
// prefixed with "FloodSeason.", so the prefix check is a clean
// disambiguator.
//
// To support more languages later, swap the Strings dict for a
// per-language lookup and let LocalizationPatch pick based on the
// game's current language (Loc only exposes the current language's
// translations, so we'd need to subscribe to ILocalizationService
// to know which language is active).
internal static class FloodSeasonLabels {

    public const string KeyPrefix = "FloodSeason.";

    public const string NameKey        = "FloodSeason.Name";
    public const string ApproachingKey = "FloodSeason.Approaching";
    public const string InProgressKey  = "FloodSeason.InProgress";
    public const string StartedKey     = "FloodSeason.Started";
    public const string EndedKey       = "FloodSeason.Ended";

    // Mixed Tide labels live under the same FloodSeason.* prefix so
    // LocalizationPatch's prefix check still routes them through
    // FloodSeasonLabels.TryGet without a second prefix-comparison
    // branch. The label values themselves read "Mixed tide" — they're
    // visually distinct from the Flood ones in-game (date panel,
    // forecast banner, notification text).
    public const string MixedTideNameKey        = "FloodSeason.MixedTide.Name";
    public const string MixedTideApproachingKey = "FloodSeason.MixedTide.Approaching";
    public const string MixedTideInProgressKey  = "FloodSeason.MixedTide.InProgress";
    public const string MixedTideStartedKey     = "FloodSeason.MixedTide.Started";
    public const string MixedTideEndedKey       = "FloodSeason.MixedTide.Ended";

    private static readonly Dictionary<string, string> Strings = new Dictionary<string, string> {
        [NameKey]                 = "Flood",
        [ApproachingKey]          = "Flood approaching",
        [InProgressKey]           = "Flood in progress",
        [StartedKey]              = "A flood has begun!",
        [EndedKey]                = "The flood has ended",
        [MixedTideNameKey]        = "Mixed tide",
        [MixedTideApproachingKey] = "Mixed tide approaching",
        [MixedTideInProgressKey]  = "Mixed tide in progress",
        [MixedTideStartedKey]     = "A mixed tide has begun!",
        [MixedTideEndedKey]       = "The mixed tide has ended",
    };

    public static bool TryGet(string key, out string value) {
        return Strings.TryGetValue(key, out value);
    }

}
