using System;
using UnityEditor;
using UnityEngine;

namespace Camellian.DistanceFade.Editor
{
    internal static class LazyFadeLilToonPresets
    {
        // 編集箇所: 各プリセットの名前と6項目をここで指定してください。
        // Color32のRGBとAlphaは0～255です。Alpha 1なら255を指定します。
        // HDR色を指定する場合は new Color(r, g, b, a) を使用してください。
        internal static readonly DistanceFadePreset[] BuiltIn =
        {
            new DistanceFadePreset(
                name: "warm",
                startDistance: 0.18f,
                endDistance: 0.01f,
                fadeColor: new Color32(10, 7, 7, 255),
                strength: 0.95f,
                rimColor: new Color32(255, 188, 177, 0),
                rimFresnelPower: 4.5f),

            new DistanceFadePreset(
                name: "cold",
                startDistance: 0.18f,
                endDistance: 0.01f,
                fadeColor: new Color32(7, 8, 11, 255),
                strength: 0.95f,
                rimColor: new Color32(177, 207, 255, 0),
                rimFresnelPower: 4.5f),

            new DistanceFadePreset(
                name: "none",
                startDistance: 0.1f,
                endDistance: 0.01f,
                fadeColor: new Color32(0, 0, 0, 255),
                strength: 0,
                rimColor: new Color32(0, 0, 0, 0),
                rimFresnelPower: 5),
        };

        // 以下は反映処理です。プリセットの内容だけを変える場合は編集不要です。
        internal static void Apply(SerializedObject target, DistanceFadePreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.Name))
                throw new ArgumentException("プリセット名を指定してください。");
            if (!SettingsValidator.Finite(preset.StartDistance) || !SettingsValidator.Finite(preset.EndDistance) ||
                !SettingsValidator.Finite(preset.Strength) || !SettingsValidator.Finite(preset.RimFresnelPower) ||
                !SettingsValidator.Finite(preset.FadeColor) || !SettingsValidator.Finite(preset.RimColor))
                throw new ArgumentException("プリセットの6項目には有限値を指定してください。");
            if (preset.RimFresnelPower < 0.01f || preset.RimFresnelPower > 50)
                throw new ArgumentException("プリセットのリムライトの細さは0.01～50で指定してください。");

            // Validate before writing. The caller commits all edits together for Undo/Prefab support.
            target.FindProperty("startDistance").floatValue = preset.StartDistance;
            target.FindProperty("endDistance").floatValue = preset.EndDistance;
            target.FindProperty("fadeColor").colorValue = preset.FadeColor;
            target.FindProperty("strength").floatValue = preset.Strength;
            target.FindProperty("rimColor").colorValue = preset.RimColor;
            target.FindProperty("rimFresnelPower").floatValue = preset.RimFresnelPower;
        }
    }

    internal sealed class DistanceFadePreset
    {
        internal readonly string Name;
        internal readonly float StartDistance, EndDistance, Strength, RimFresnelPower;
        internal readonly Color FadeColor, RimColor;

        internal DistanceFadePreset(string name, float startDistance, float endDistance, Color fadeColor,
            float strength, Color rimColor, float rimFresnelPower)
        {
            Name = name;
            StartDistance = startDistance;
            EndDistance = endDistance;
            FadeColor = fadeColor;
            Strength = strength;
            RimColor = rimColor;
            RimFresnelPower = rimFresnelPower;
        }
    }
}
