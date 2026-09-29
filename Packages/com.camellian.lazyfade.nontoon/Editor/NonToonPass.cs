using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Camellian.NonToonDistanceFade.Editor
{
    internal sealed class NonToonSnapshot
    {
        internal bool Near, Far, Strength, Fail;
        internal float NearValue, FarValue, StrengthValue;
        internal readonly HashSet<Material> Excluded = new HashSet<Material>();
        internal bool Any => Near || Far || Strength;
    }

    internal sealed class NonToonPlan
    {
        internal sealed class Change
        {
            internal NonToonProperties Properties;
            internal Vector4 Distance;
            internal float Strength;
        }
        internal readonly Dictionary<Material, Change> Materials = new Dictionary<Material, Change>();
        internal readonly List<(Renderer renderer, Material[] original)> Renderers = new List<(Renderer, Material[])>();
        internal readonly List<(Material material, string reason)> Unsupported = new List<(Material, string)>();
        internal int ExcludedMaterials, ExcludedSlots, ReplacedSlots;
        public override string ToString() => $"NonToon Distance Fade: 対象Renderer {Renderers.Count} / Material {Materials.Count} / " +
            $"除外Material {ExcludedMaterials} / Slot {ExcludedSlots} / 非対応Material {Unsupported.Count} / 置換Slot {ReplacedSlots}";
    }

    internal static class NonToonPass
    {
        internal static bool Unit(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
        internal static void ValidateValues(float near, float far, float strength)
        {
            if (!Unit(near) || !Unit(far) || near >= far || !Unit(strength))
                throw new InvalidOperationException("NT003: 0 ≦ 近距離 < 遠距離 ≦ 1、強度0～1の有限値を指定してください。保持するMaterial値も検証します。");
        }
        internal static LazyFadeNonToon ValidatePlacement(GameObject root)
        {
            var settings = root.GetComponentsInChildren<LazyFadeNonToon>(true);
            if (settings.Length == 0) return null;
            if (settings.Length > 1) throw new InvalidOperationException("NT002: NonToon用コンポーネントはアバター全体で1個までです。");
            if (settings[0].gameObject != root || root.GetComponent<VRCAvatarDescriptor>() == null)
                throw new InvalidOperationException("NT001: VRC Avatar Descriptorと同じGameObjectへ取り付けてください。");
            return settings[0];
        }
        internal static GameObject FindRoot(Component component)
        {
            for (var t = component.transform; t != null; t = t.parent)
                if (t.GetComponent<VRCAvatarDescriptor>() != null) return t.gameObject;
            return component.gameObject;
        }
        internal static NonToonSnapshot Capture(LazyFadeNonToon setting)
        {
            if ((setting.overrideNearDistance && !Unit(setting.nearDistance)) ||
                (setting.overrideFarDistance && !Unit(setting.farDistance)) ||
                (setting.overrideStrength && !Unit(setting.strength)) ||
                (setting.overrideNearDistance && setting.overrideFarDistance && setting.nearDistance >= setting.farDistance))
                throw new InvalidOperationException("NT003: 適用する距離と強度には0～1の有限値を指定し、近距離 < 遠距離にしてください。");
            var result = new NonToonSnapshot
            {
                Near = setting.overrideNearDistance, Far = setting.overrideFarDistance, Strength = setting.overrideStrength,
                NearValue = setting.nearDistance, FarValue = setting.farDistance, StrengthValue = setting.strength, Fail = setting.failOnUnsupported
            };
            if (setting.excludedMaterials != null)
                foreach (var material in setting.excludedMaterials) if (material != null) result.Excluded.Add(material);
            return result;
        }
        internal static NonToonPlan Collect(GameObject root, NonToonSnapshot settings)
        {
            var plan = new NonToonPlan();
            if (!settings.Any) return plan;
            var registry = ObjectRegistry.ActiveRegistry;
            var origins = new HashSet<ObjectReference>(settings.Excluded.Select(m => registry?.GetReference(m, false)).Where(r => r != null));
            var excluded = new HashSet<Material>();
            var inspected = new HashSet<Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                var materials = renderer.sharedMaterials;
                var target = false;
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    var origin = origins.Count > 0 ? registry?.GetReference(material, false) : null;
                    if (settings.Excluded.Contains(material) || (origin != null && origins.Contains(origin)))
                    {
                        excluded.Add(material); plan.ExcludedSlots++; continue;
                    }
                    if (inspected.Add(material))
                    {
                        var properties = NonToonShader.Resolve(material.shader, out var reason);
                        if (properties != null)
                        {
                            var distance = material.GetVector(properties.Distance);
                            if (settings.Near) distance.x = settings.NearValue;
                            if (settings.Far) distance.y = settings.FarValue;
                            var strength = settings.Strength ? settings.StrengthValue : material.GetFloat(properties.Strength);
                            ValidateValues(distance.x, distance.y, strength);
                            plan.Materials.Add(material, new NonToonPlan.Change { Properties = properties, Distance = distance, Strength = strength });
                        }
                        else if (reason != null)
                        {
                            if (settings.Fail) throw new InvalidOperationException("NT004: " + material.name + ": " + reason);
                            plan.Unsupported.Add((material, reason));
                        }
                    }
                    if (plan.Materials.ContainsKey(material)) target = true;
                }
                if (target) plan.Renderers.Add((renderer, materials));
            }
            plan.ExcludedMaterials = excluded.Count;
            return plan;
        }
        internal static NonToonPlan Apply(GameObject root, Action<Material> save)
        {
            var setting = ValidatePlacement(root);
            if (setting == null) return new NonToonPlan();
            if (!setting.enabled) { UnityEngine.Object.DestroyImmediate(setting); return new NonToonPlan(); }
            var settings = Capture(setting);
            var plan = Collect(root, settings);
            var clones = new Dictionary<Material, Material>();
            var changed = new List<(Renderer renderer, Material[] original)>();
            try
            {
                foreach (var pair in plan.Materials)
                {
                    var clone = new Material(pair.Key) { name = pair.Key.name + " (NonToon Distance Fade)" };
                    clones.Add(pair.Key, clone);
                    if (settings.Near || settings.Far) clone.SetVector(pair.Value.Properties.Distance, pair.Value.Distance);
                    if (settings.Strength) clone.SetFloat(pair.Value.Properties.Strength, pair.Value.Strength);
                }
                foreach (var clone in clones.Values) save(clone);
                foreach (var entry in plan.Renderers)
                {
                    var output = (Material[])entry.original.Clone();
                    for (var i = 0; i < output.Length; i++)
                        if (output[i] != null && clones.TryGetValue(output[i], out var clone)) { output[i] = clone; plan.ReplacedSlots++; }
                    changed.Add(entry);
                    entry.renderer.sharedMaterials = output;
                }
            }
            catch
            {
                foreach (var entry in changed) if (entry.renderer != null) entry.renderer.sharedMaterials = entry.original;
                foreach (var clone in clones.Values) if (clone != null) UnityEngine.Object.DestroyImmediate(clone, true);
                throw;
            }
            UnityEngine.Object.DestroyImmediate(setting);
            return plan;
        }
    }
}
