using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Camellian.DistanceFade.Editor
{
    [Flags]
    internal enum Overrides
    {
        None = 0, Color = 1, Start = 2, End = 4, Strength = 8,
        Backface = 16, Mode = 32, RimColor = 64, Fresnel = 128,
        Vector = Start | End | Strength | Backface
    }

    internal sealed class SettingsSnapshot
    {
        internal Overrides Mask;
        internal bool Strict;
        internal Color Color, RimColor;
        internal Vector4 Vector;
        internal int Mode;
        internal float Fresnel;
        internal bool FresnelClamped;
        internal readonly HashSet<Material> ExcludedMaterials = new HashSet<Material>();
    }

    internal sealed class SettingsException : Exception
    {
        internal readonly UnityEngine.Object Context;
        internal SettingsException(string message, UnityEngine.Object context) : base(message) { Context = context; }
    }

    internal static class SettingsValidator
    {
        internal static DistanceFadeBulkSetter ValidatePlacement(GameObject root)
        {
            var components = root.GetComponentsInChildren<DistanceFadeBulkSetter>(true);
            if (components.Length == 0) return null;
            if (components.Length > 1)
                throw new SettingsException("E002: 設定はアバター全体で1個までです: " +
                    string.Join(", ", components.Select(c => Path(c.transform))), root);
            var setting = components[0];
            if (setting.gameObject != root || root.GetComponent<VRCAvatarDescriptor>() == null)
                throw new SettingsException("E001: VRC Avatar Descriptorと同じアバタールートに設定を取り付けてください: " +
                    Path(setting.transform), setting);
            return setting;
        }

        internal static GameObject FindAvatarRoot(Component component)
        {
            for (var t = component.transform; t != null; t = t.parent)
                if (t.GetComponent<VRCAvatarDescriptor>() != null) return t.gameObject;
            return null;
        }

        internal static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Color c) => Finite(c.r) && Finite(c.g) && Finite(c.b) && Finite(c.a);

        internal static SettingsSnapshot Capture(DistanceFadeBulkSetter s)
        {
            var mask = Overrides.None;
            if (s.overrideFadeColor) mask |= Overrides.Color;
            if (s.overrideStartDistance) mask |= Overrides.Start;
            if (s.overrideEndDistance) mask |= Overrides.End;
            if (s.overrideStrength) mask |= Overrides.Strength;
            if (s.overrideBackfaceShadow) mask |= Overrides.Backface;
            if (s.overrideMode) mask |= Overrides.Mode;
            if (s.overrideRimColor) mask |= Overrides.RimColor;
            if (s.overrideRimFresnelPower) mask |= Overrides.Fresnel;
            void Check(Overrides flag, bool valid, string label)
            {
                if ((mask & flag) != 0 && !valid)
                    throw new SettingsException("E003: " + label + "には有限値を指定してください。", s);
            }
            Check(Overrides.Color, Finite(s.fadeColor), "フェード色");
            Check(Overrides.Start, Finite(s.startDistance), "開始距離");
            Check(Overrides.End, Finite(s.endDistance), "終了距離");
            Check(Overrides.Strength, Finite(s.strength), "強度");
            Check(Overrides.RimColor, Finite(s.rimColor), "リム色");
            Check(Overrides.Fresnel, Finite(s.rimFresnelPower), "フレネル指数");
            var fresnel = (mask & Overrides.Fresnel) != 0 ? Mathf.Clamp(s.rimFresnelPower, 0.01f, 50) : s.rimFresnelPower;
            var snapshot = new SettingsSnapshot
            {
                Mask = mask, Strict = s.strictLilToonCheck, Color = s.fadeColor, RimColor = s.rimColor,
                Vector = new Vector4(s.startDistance, s.endDistance, s.strength, s.backfaceShadow ? 1 : 0),
                Mode = s.mode, Fresnel = fresnel,
                FresnelClamped = (mask & Overrides.Fresnel) != 0 && fresnel != s.rimFresnelPower
            };
            if (s.excludedMaterials != null)
                foreach (var material in s.excludedMaterials)
                    if (material != null) snapshot.ExcludedMaterials.Add(material);
            return snapshot;
        }
    }
}
