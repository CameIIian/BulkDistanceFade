using UnityEditor;
using UnityEngine;

namespace Camellian.DistanceFade.Editor
{
    [CustomEditor(typeof(DistanceFadeBulkSetter))]
    internal sealed class DistanceFadeBulkSetterEditor : UnityEditor.Editor
    {
        private BuildSummary summary;
        private string error;
        private bool stale = true;
        private bool refreshed;
        private bool enabledLast;

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += Invalidate;
            EditorApplication.projectChanged += Invalidate;
            Undo.undoRedoPerformed += Invalidate;
            enabledLast = ((DistanceFadeBulkSetter)target).enabled;
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
            var setting = (DistanceFadeBulkSetter)target;
            if (enabledLast != setting.enabled) { enabledLast = setting.enabled; Invalidate(); }
            serializedObject.Update();
            EditorGUILayout.LabelField("BulkDistanceFade", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Enabled"), new GUIContent("処理を有効化"));
            EditorGUILayout.LabelField("配置先", setting.gameObject.name);
            EditorGUILayout.LabelField("対象範囲", "アバター全体（非アクティブを含む）");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("strictLilToonCheck"), new GUIContent("Strict lilToon Check"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("excludedMaterials"), new GUIContent("除外マテリアル"), true);
            EditorGUILayout.HelpBox("指定したMaterialを使うすべてのスロットを除外します。未設定・重複要素は無視します。" +
                "他ツールによる置換後の除外は、NDMFに置換元が登録されている場合に引き継がれます。", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("距離フェード", EditorStyles.boldLabel);
            Field("overrideFadeColor", "fadeColor", "色");
            Field("overrideStartDistance", "startDistance", "開始距離");
            Field("overrideEndDistance", "endDistance", "終了距離");
            Field("overrideStrength", "strength", "強度");
            Field("overrideBackfaceShadow", "backfaceShadow", "裏面を影にする");
            Field("overrideMode", "mode", "モード");
            EditorGUILayout.LabelField("モード: 0 = 頂点 / 1 = オブジェクト位置", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("その他のモード値も数値のまま保持します。", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("リム", EditorStyles.boldLabel);
            Field("overrideRimColor", "rimColor", "色");
            Field("overrideRimFresnelPower", "rimFresnelPower", "リムライトの細さ");
            if (serializedObject.ApplyModifiedProperties()) Invalidate();

            if (!refreshed) RefreshSummary();
            ValidateOnly();
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (!setting.enabled) EditorGUILayout.HelpBox("処理は無効です。設定値は保持されます。", MessageType.Info);
            if (setting.overrideStrength && setting.strength == 0)
                EditorGUILayout.HelpBox("強度0はフェード無効です。既存Materialの強度も0に上書きします。", MessageType.Info);
            if (summary != null)
            {
                EditorGUILayout.LabelField($"編集時集計: 対象Renderer {summary.TargetRenderers} / 対象Material {summary.TargetMaterials}");
                EditorGUILayout.LabelField($"手動除外: Material {summary.ExcludedMaterials} / Slot {summary.ExcludedSlots}");
                if (summary.Missing.Count > 0)
                    EditorGUILayout.HelpBox($"{summary.Missing.Count} Materialで適用項目の一部が非対応です。", MessageType.Warning);
            }
            if (stale) EditorGUILayout.HelpBox("設定または構成が変更されました。集計を更新してください。", MessageType.Info);
            if (GUILayout.Button("集計を更新")) RefreshSummary();
            EditorGUILayout.HelpBox("集計は読み取り専用です。MA・TTT処理後の対象数とは異なる場合があります。", MessageType.Info);
        }

        private void Field(string toggle, string value, string label)
        {
            var apply = serializedObject.FindProperty(toggle);
            var property = serializedObject.FindProperty(value);
            using (new EditorGUILayout.HorizontalScope())
            {
                var toggleRect = EditorGUILayout.GetControlRect(GUILayout.Width(62));
                EditorGUI.BeginProperty(toggleRect, GUIContent.none, apply);
                apply.boolValue = EditorGUI.ToggleLeft(toggleRect, "適用", apply.boolValue);
                EditorGUI.EndProperty();
                using (new EditorGUI.DisabledScope(!apply.boolValue))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property, new GUIContent(label));
                    if (EditorGUI.EndChangeCheck() && value == "rimFresnelPower" && SettingsValidator.Finite(property.floatValue))
                        property.floatValue = Mathf.Clamp(property.floatValue, 0.01f, 50);
                }
            }
        }

        private void ValidateOnly()
        {
            error = null;
            var setting = (DistanceFadeBulkSetter)target;
            var root = SettingsValidator.FindAvatarRoot(setting);
            if (root == null) { error = "E001: VRC Avatar Descriptorのあるアバタールートに取り付けてください。"; return; }
            try
            {
                SettingsValidator.ValidatePlacement(root);
                if (setting.enabled) SettingsValidator.Capture(setting);
            }
            catch (SettingsException ex) { error = ex.Message; }
        }

        private void RefreshSummary()
        {
            refreshed = true;
            stale = false;
            summary = null;
            ValidateOnly();
            if (error != null) return;
            var setting = (DistanceFadeBulkSetter)target;
            if (!setting.enabled) { summary = new BuildSummary(); return; }
            summary = ApplyDistanceFadePass.Collect(setting.gameObject, SettingsValidator.Capture(setting)).Summary;
            Repaint();
        }
    }
}
