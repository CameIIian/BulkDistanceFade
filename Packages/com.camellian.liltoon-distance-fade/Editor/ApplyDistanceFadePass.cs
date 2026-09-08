using System;
using System.Collections.Generic;
using UnityEngine;

namespace Camellian.DistanceFade.Editor
{
    // Called only with the NDMF build avatar. Inspectors call Collect, never Apply.
    internal static class ApplyDistanceFadePass
    {
        internal sealed class Plan
        {
            internal readonly BuildSummary Summary = new BuildSummary();
            internal readonly Dictionary<Material, Overrides> Materials = new Dictionary<Material, Overrides>();
            internal readonly List<(Renderer renderer, Material[] original)> Renderers = new List<(Renderer, Material[])>();
        }

        internal static Plan Collect(GameObject root, SettingsSnapshot settings)
        {
            var plan = new Plan();
            var inspected = new Dictionary<Material, string>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                plan.Summary.ScannedRenderers++;
                var materials = renderer.sharedMaterials;
                var target = false;
                foreach (var material in materials)
                {
                    string reason;
                    if (settings.Mask == Overrides.None) reason = "全項目OFF";
                    else if (material == null) reason = "null Material";
                    else if (material.shader == null) reason = "null Shader";
                    else if (!inspected.TryGetValue(material, out reason))
                    {
                        reason = "非lilToon";
                        if (MaterialUtility.IsCandidate(material.shader.name, settings.Strict))
                        {
                            var supported = MaterialUtility.Supported(material, settings.Mask, out var missing);
                            if (missing.Length > 0) plan.Summary.Missing.Add((material, missing));
                            reason = supported == Overrides.None ? "対応Propertyなし" : null;
                            if (supported != Overrides.None) plan.Materials.Add(material, supported);
                        }
                        inspected.Add(material, reason);
                    }
                    if (reason != null) plan.Summary.Skip(reason);
                    else target = true;
                }
                if (!target) continue;
                plan.Summary.TargetRenderers++;
                plan.Renderers.Add((renderer, materials));
            }
            plan.Summary.TargetMaterials = plan.Materials.Count;
            return plan;
        }

        internal static BuildSummary Apply(GameObject root, Action<Material> save,
            Action<BuildSummary> reportWarnings = null, Action<DistanceFadeBulkSetter> clamped = null)
        {
            var component = SettingsValidator.ValidatePlacement(root);
            if (component == null) return new BuildSummary();
            if (!component.enabled)
            {
                UnityEngine.Object.DestroyImmediate(component);
                return new BuildSummary();
            }
            var settings = SettingsValidator.Capture(component);
            var plan = Collect(root, settings);
            reportWarnings?.Invoke(plan.Summary);
            if (settings.FresnelClamped) clamped?.Invoke(component);
            using (var cache = new MaterialCloneCache())
            {
                foreach (var item in plan.Materials) cache.GetOrCreate(item.Key, settings, item.Value);
                cache.Save(save);
                var replaced = new List<(Renderer renderer, Material[] original)>();
                try
                {
                    foreach (var entry in plan.Renderers)
                    {
                        var output = (Material[])entry.original.Clone();
                        for (var i = 0; i < output.Length; i++)
                        {
                            if (output[i] == null || !plan.Materials.TryGetValue(output[i], out var supported)) continue;
                            output[i] = cache.GetOrCreate(output[i], settings, supported);
                            plan.Summary.ReplacedSlots++;
                        }
                        replaced.Add(entry);
                        entry.renderer.sharedMaterials = output;
                    }
                }
                catch
                {
                    foreach (var entry in replaced) if (entry.renderer != null) entry.renderer.sharedMaterials = entry.original;
                    throw;
                }
                cache.Commit();
                plan.Summary.Clones = cache.Count;
            }
            UnityEngine.Object.DestroyImmediate(component);
            return plan.Summary;
        }
    }
}
