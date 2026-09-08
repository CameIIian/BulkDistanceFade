using System.Collections.Generic;
using UnityEngine;

namespace Camellian.DistanceFade.Editor
{
    internal sealed class BuildSummary
    {
        internal int ScannedRenderers, TargetRenderers, TargetMaterials, Clones, ReplacedSlots;
        // Skip counts are slot counts; warnings are deduplicated per material.
        internal readonly Dictionary<string, int> SkippedSlots = new Dictionary<string, int>();
        internal readonly List<(Material material, string properties)> Missing = new List<(Material, string)>();
        internal void Skip(string reason)
        {
            SkippedSlots.TryGetValue(reason, out var count);
            SkippedSlots[reason] = count + 1;
        }
        public override string ToString() => $"Distance Fade: 走査Renderer {ScannedRenderers} / 対象Renderer {TargetRenderers} / " +
            $"対象Material {TargetMaterials} / 複製 {Clones} / 置換Slot {ReplacedSlots}";
    }
}
