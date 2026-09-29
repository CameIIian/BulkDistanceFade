using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Camellian.NonToonDistanceFade.Editor
{
    [CustomEditor(typeof(LazyFadeNonToon))]
    internal sealed class LazyFadeNonToonEditor : UnityEditor.Editor
    {
        private NonToonPlan summary;
        private bool stale = true, refreshed, advanced;
        private int preset;
        private string presetError;
        private bool enabledLast;
        private void OnEnable()
        {
            enabledLast = ((LazyFadeNonToon)target).enabled;
            EditorApplication.hierarchyChanged += Invalidate;
            EditorApplication.projectChanged += Invalidate;
            Undo.undoRedoPerformed += Invalidate;
        }
        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Invalidate;
            EditorApplication.projectChanged -= Invalidate;
            Undo.undoRedoPerformed -= Invalidate;
        }
        private void Invalidate() { stale = true; Repaint(); }
        public override void OnInspectorGUI()
        {
            var setting = (LazyFadeNonToon)target;
            if (enabledLast != setting.enabled) { enabledLast = setting.enabled; Invalidate(); }
            serializedObject.Update();
            EditorGUILayout.LabelField("lazyFade — NonToon", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. 処理の有効化", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Enabled"), new GUIContent("処理を有効化"));
            EditorGUILayout.LabelField("配置先", setting.gameObject.name);
            EditorGUILayout.LabelField("対象範囲", "アバター全体（非アクティブを含む）");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. 基本設定", EditorStyles.boldLabel);
            var choices = new[] { "現在の設定（個別指定）" }.Concat(NonToonPresets.BuiltIn.Select(p => p?.Name ?? "未定義")).ToArray();
            EditorGUI.BeginChangeCheck();
            var next = EditorGUILayout.Popup("プリセット", Mathf.Clamp(preset, 0, choices.Length - 1), choices);
            if (EditorGUI.EndChangeCheck())
            {
                presetError = null;
                try
                {
                    if (next > 0) NonToonPresets.Apply(serializedObject, NonToonPresets.BuiltIn[next - 1]);
                    preset = next;
                }
                catch (Exception ex) { presetError = ex.Message; }
            }
            if (presetError != null) EditorGUILayout.HelpBox(presetError, MessageType.Error);
            EditorGUI.BeginChangeCheck();
            Field("overrideNearDistance", "nearDistance", "近距離境界");
            Field("overrideFarDistance", "farDistance", "遠距離境界");
            Field("overrideStrength", "strength", "強度");
            if (EditorGUI.EndChangeCheck()) preset = 0;
            EditorGUILayout.HelpBox("左端のチェックをONにした項目だけ適用します。初期状態は全項目OFFです。強度0の適用は減光を無効にします。", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("excludedMaterials"), new GUIContent("除外マテリアル"), true);
            advanced = EditorGUILayout.Foldout(advanced, "高度な設定", true, EditorStyles.foldoutHeader);
            if (advanced)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("failOnUnsupported"), new GUIContent("未対応NonToonでビルドを停止", "OFFでは警告してスキップします。"));
            if (serializedObject.ApplyModifiedProperties()) Invalidate();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3. 集計・確認", EditorStyles.boldLabel);
            var refresh = GUILayout.Button("再集計");
            string error = null;
            try
            {
                NonToonPass.ValidatePlacement(NonToonPass.FindRoot(setting));
                if (setting.enabled) NonToonPass.Capture(setting);
                if (!refreshed || refresh)
                {
                    summary = setting.enabled ? NonToonPass.Collect(setting.gameObject, NonToonPass.Capture(setting)) : new NonToonPlan();
                    refreshed = true; stale = false;
                }
            }
            catch (Exception ex) { error = ex.Message; summary = null; }
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (summary != null)
            {
                EditorGUILayout.LabelField($"対象Renderer {summary.Renderers.Count} / Material {summary.Materials.Count}");
                EditorGUILayout.LabelField($"手動除外Material {summary.ExcludedMaterials} / Slot {summary.ExcludedSlots}");
                foreach (var unsupported in summary.Unsupported)
                    EditorGUILayout.HelpBox(unsupported.material.name + ": " + unsupported.reason, MessageType.Warning);
            }
            if (!setting.enabled) EditorGUILayout.HelpBox("処理は無効です。設定値は保持されます。", MessageType.Info);
            if (stale) EditorGUILayout.HelpBox("設定や構成が変更されました。再集計で件数を確認できます。", MessageType.Info);
            EditorGUILayout.HelpBox("集計は任意です。NDMFビルド時は最新の設定を使用します。NonToon 0.1.3 / ShaderCore 0.1.11の公式Shaderが対象です。Distance FadeモジュールはShader側で組み込んでください。", MessageType.Info);
        }
        private void Field(string toggle, string value, string label)
        {
            var apply = serializedObject.FindProperty(toggle);
            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(18));
                var content = new GUIContent("", label + "を上書きします。OFFは既存値を保持します。");
                EditorGUI.BeginProperty(rect, content, apply);
                apply.boolValue = EditorGUI.ToggleLeft(rect, content, apply.boolValue);
                EditorGUI.EndProperty();
                using (new EditorGUI.DisabledScope(!apply.boolValue))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(value), new GUIContent(label));
            }
        }
    }
}
