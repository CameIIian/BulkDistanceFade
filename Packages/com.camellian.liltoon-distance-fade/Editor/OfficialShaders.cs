using System;
using System.Collections.Generic;

namespace Camellian.DistanceFade.Editor
{
    // Exact public shader names from lilToon 2.3.2 (Shader directory).
    internal static class OfficialShaders
    {
        internal static readonly HashSet<string> Names = new HashSet<string>(StringComparer.Ordinal)
        {
            "_lil/[Optional] lilToonFakeShadow",
            "_lil/[Optional] lilToonFurOnlyCutout",
            "_lil/[Optional] lilToonFurOnlyTransparent",
            "_lil/[Optional] lilToonFurOnlyTwoPass",
            "_lil/[Optional] lilToonLiteOverlay",
            "_lil/[Optional] lilToonLiteOverlayOnePass",
            "_lil/[Optional] lilToonOutlineOnly",
            "_lil/[Optional] lilToonOutlineOnlyCutout",
            "_lil/[Optional] lilToonOutlineOnlyTransparent",
            "_lil/[Optional] lilToonOverlay",
            "_lil/[Optional] lilToonOverlayOnePass",
            "_lil/lilToonMulti",
            "Hidden/lilToonCutout",
            "Hidden/lilToonCutoutOutline",
            "Hidden/lilToonFur",
            "Hidden/lilToonFurCutout",
            "Hidden/lilToonFurTwoPass",
            "Hidden/lilToonGem",
            "Hidden/lilToonLite",
            "Hidden/lilToonLiteCutout",
            "Hidden/lilToonLiteCutoutOutline",
            "Hidden/lilToonLiteOnePassTransparent",
            "Hidden/lilToonLiteOnePassTransparentOutline",
            "Hidden/lilToonLiteOutline",
            "Hidden/lilToonLiteTransparent",
            "Hidden/lilToonLiteTransparentOutline",
            "Hidden/lilToonLiteTwoPassTransparent",
            "Hidden/lilToonLiteTwoPassTransparentOutline",
            "Hidden/lilToonMultiFur",
            "Hidden/lilToonMultiGem",
            "Hidden/lilToonMultiOutline",
            "Hidden/lilToonMultiRefraction",
            "Hidden/lilToonOnePassTransparent",
            "Hidden/lilToonOnePassTransparentOutline",
            "Hidden/lilToonOutline",
            "Hidden/lilToonRefraction",
            "Hidden/lilToonRefractionBlur",
            "Hidden/lilToonTessellation",
            "Hidden/lilToonTessellationCutout",
            "Hidden/lilToonTessellationCutoutOutline",
            "Hidden/lilToonTessellationOnePassTransparent",
            "Hidden/lilToonTessellationOnePassTransparentOutline",
            "Hidden/lilToonTessellationOutline",
            "Hidden/lilToonTessellationTransparent",
            "Hidden/lilToonTessellationTransparentOutline",
            "Hidden/lilToonTessellationTwoPassTransparent",
            "Hidden/lilToonTessellationTwoPassTransparentOutline",
            "Hidden/lilToonTransparent",
            "Hidden/lilToonTransparentOutline",
            "Hidden/lilToonTwoPassTransparent",
            "Hidden/lilToonTwoPassTransparentOutline",
            "lilToon",
        };
    }
}
