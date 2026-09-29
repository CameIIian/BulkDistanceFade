using System;
using System.Collections.Generic;
using UnityEngine;

namespace Camellian.DistanceFade.Editor
{
    internal sealed class MaterialCloneCache : IDisposable
    {
        private readonly Dictionary<Material, Material> clones = new Dictionary<Material, Material>();
        private bool committed;
        internal int Count => clones.Count;
        internal Material GetOrCreate(Material source, SettingsSnapshot settings, Overrides supported)
        {
            if (clones.TryGetValue(source, out var clone)) return clone;
            clone = new Material(source) { name = source.name + " (Distance Fade)" };
            clones.Add(source, clone);
            MaterialUtility.Apply(clone, settings, supported);
            return clone;
        }
        internal void Save(Action<Material> save)
        {
            foreach (var clone in clones.Values) save(clone);
        }
        internal void Commit() => committed = true;
        public void Dispose()
        {
            if (committed) return;
            foreach (var clone in clones.Values)
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone, true);
        }
    }
}
