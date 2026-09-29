using System;
using UnityEditor;

namespace Camellian.NonToonDistanceFade.Editor
{
    internal sealed class NonToonPreset
    {
        internal readonly string Name;
        internal readonly float Near, Far, Strength;
        internal NonToonPreset(string name, float near, float far, float strength)
        { Name = name; Near = near; Far = far; Strength = strength; }
    }

    internal static class NonToonPresets
    {
        // 名前と3項目の編集箇所。仮設定なので用途に合わせて変更してください。
        internal static readonly NonToonPreset[] BuiltIn =
        {
            new NonToonPreset("プリセット1（仮）", 0.01f, 0.1f, 0),
            new NonToonPreset("プリセット2（仮）", 0.01f, 0.1f, 0),
            new NonToonPreset("プリセット3（仮）", 0.01f, 0.1f, 0)
        };
        internal static void Apply(SerializedObject target, NonToonPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.Name)) throw new ArgumentException("プリセット名を指定してください。");
            NonToonPass.ValidateValues(preset.Near, preset.Far, preset.Strength);
            target.FindProperty("nearDistance").floatValue = preset.Near;
            target.FindProperty("farDistance").floatValue = preset.Far;
            target.FindProperty("strength").floatValue = preset.Strength;
        }
    }
}
