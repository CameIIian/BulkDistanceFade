using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Camellian.DistanceFade.Editor
{
    internal static class MaterialUtility
    {
        internal const string Color = "_DistanceFadeColor", Vector = "_DistanceFade", Mode = "_DistanceFadeMode",
            RimColor = "_DistanceFadeRimColor", Fresnel = "_DistanceFadeRimFresnelPower";
        // A complete path segment beginning with lilToon, not a substring such as NotlilToon.
        private static readonly Regex CustomName = new Regex(@"(^|/)(\[Optional\] )?lilToon([A-Z0-9_ /].*)?$", RegexOptions.CultureInvariant);

        internal static bool IsCandidate(string name, bool strict) => name != null &&
            (OfficialShaders.Names.Contains(name) || (!strict && CustomName.IsMatch(name)));

        internal static Overrides Supported(Material material, Overrides requested, out string missing)
        {
            var available = Overrides.None;
            var absent = new List<string>();
            void Check(string property, Overrides flags)
            {
                var enabled = requested & flags;
                if (enabled == Overrides.None) return;
                if (material.HasProperty(property)) available |= enabled;
                else absent.Add(property);
            }
            Check(Color, Overrides.Color);
            Check(Vector, Overrides.Vector);
            Check(Mode, Overrides.Mode);
            Check(RimColor, Overrides.RimColor);
            Check(Fresnel, Overrides.Fresnel);
            missing = string.Join(", ", absent);
            return available;
        }

        internal static void Apply(Material dst, SettingsSnapshot s, Overrides supported)
        {
            bool Has(Overrides flag) => (supported & flag) != 0;
            if (Has(Overrides.Color)) dst.SetColor(Color, s.Color);
            if (Has(Overrides.Vector))
            {
                var v = dst.GetVector(Vector);
                if (Has(Overrides.Start)) v.x = s.Vector.x;
                if (Has(Overrides.End)) v.y = s.Vector.y;
                if (Has(Overrides.Strength)) v.z = s.Vector.z;
                if (Has(Overrides.Backface)) v.w = s.Vector.w;
                dst.SetVector(Vector, v);
            }
            if (Has(Overrides.Mode)) dst.SetInt(Mode, s.Mode);
            if (Has(Overrides.RimColor)) dst.SetColor(RimColor, s.RimColor);
            if (Has(Overrides.Fresnel)) dst.SetFloat(Fresnel, s.Fresnel);
        }
    }
}
