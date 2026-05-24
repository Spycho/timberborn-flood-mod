using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spycho.FloodSeason;

// Lazy loader for the mod's PNG art assets.
//
// The game's vanilla CSS uses Unity's resource('UI/Images/...') for the
// drought icon and notification banner — those resources live inside the
// shipped Resources bundle and we can't add to it from a mod folder.
// Instead, our patches override the in-memory VisualElement style on the
// fly. To do that we need Background values (Unity's UI Toolkit wrapper
// around Texture2D), which we build here from raw PNG bytes.
//
// PNGs are shipped alongside Code.dll in <ModPath>/assets/. ModPath comes
// from IModEnvironment in ModStarter.StartMod and is stashed in a static
// for the patches (which are static methods and can't take DI) to read.
internal static class FloodArt {

    // Set by ModStarter.StartMod. PNGs live in <ModPath>/assets/.
    // Kept nullable so a misconfigured load surfaces as a clear log line
    // rather than a NullReferenceException deep inside a patch.
    public static string? ModPath { get; set; }

    private const string IconRelative         = "assets/flood-icon.png";
    private const string NotificationRelative = "assets/flood-notification.png";

    // Cached lazily on first use. Each Background instance wraps a
    // Texture2D that we keep around via HideFlags.HideAndDontSave so
    // Unity doesn't garbage-collect it between scenes.
    private static Background? _iconBackground;
    private static Background? _notificationBackground;
    // Sprite view of the same texture as the notification banner —
    // needed because the weather panel's wide progress indicator is
    // a custom-mesh SimpleProgressBar that takes a Sprite (not a
    // Background) and renders it directly via OnGenerateVisualContent.
    // WeatherPanelBackgroundPatch writes this into the bar's private
    // _image field.
    private static Sprite? _notificationSprite;

    // null if the PNG is missing or failed to decode — patches should
    // skip the override in that case to keep the vanilla art visible
    // rather than rendering an invisible element.
    public static Background? IconBackground {
        get {
            if (_iconBackground.HasValue) return _iconBackground;
            _iconBackground = TryLoad(IconRelative);
            return _iconBackground;
        }
    }

    public static Background? NotificationBackground {
        get {
            if (_notificationBackground.HasValue) return _notificationBackground;
            _notificationBackground = TryLoad(NotificationRelative);
            return _notificationBackground;
        }
    }

    public static Sprite? NotificationSprite {
        get {
            if (_notificationSprite != null) return _notificationSprite;
            // Piggyback on NotificationBackground so we don't decode
            // the PNG twice. Background struct exposes its Texture2D
            // via the .texture field; a null check guards against the
            // (vanishingly unlikely) case where the Background was
            // constructed from something other than a Texture2D.
            var bg = NotificationBackground;
            if (!bg.HasValue) return null;
            var tex = bg.Value.texture;
            if (tex == null) return null;
            _notificationSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
            _notificationSprite.hideFlags = HideFlags.HideAndDontSave;
            return _notificationSprite;
        }
    }

    private static Background? TryLoad(string relativePath) {
        if (string.IsNullOrEmpty(ModPath)) {
            Debug.LogWarning($"[Flood Season] FloodArt: ModPath not set, cannot load {relativePath}");
            return null;
        }
        var fullPath = Path.Combine(ModPath, relativePath);
        if (!File.Exists(fullPath)) {
            Debug.LogWarning($"[Flood Season] FloodArt: asset missing at {fullPath}");
            return null;
        }
        byte[] bytes;
        try {
            bytes = File.ReadAllBytes(fullPath);
        } catch (System.Exception ex) {
            Debug.LogWarning($"[Flood Season] FloodArt: read failed for {fullPath}: {ex.Message}");
            return null;
        }
        // Texture2D.LoadImage resizes the texture to match the PNG, so the
        // initial 2x2 size below is just a placeholder. mipmapChain=false
        // because UI elements draw 1:1, not at sub-resolutions.
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        if (!tex.LoadImage(bytes)) {
            Debug.LogWarning($"[Flood Season] FloodArt: PNG decode failed for {fullPath}");
            Object.Destroy(tex);
            return null;
        }
        // Point filtering would be wrong for our 2x-supersampled icon
        // (artwork was anti-aliased at generation time). Leave default
        // bilinear filter; do clamp so edge bleed doesn't appear if a
        // future element draws the texture with margin.
        tex.wrapMode = TextureWrapMode.Clamp;
        Debug.Log($"[Flood Season] FloodArt: loaded {relativePath} ({tex.width}x{tex.height})");
        return Background.FromTexture2D(tex);
    }

}
