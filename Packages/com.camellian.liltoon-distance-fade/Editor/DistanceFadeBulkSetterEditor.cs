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
        // View state belongs to this Inspector, never to the avatar's serialized settings.
        private bool showExclusions;
        private bool showAdvanced;

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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("1. 処理の有効化", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Enabled"), new GUIContent("処理を有効化"));
            EditorGUILayout.LabelField("配置先", setting.gameObject.name);
            EditorGUILayout.LabelField("対象範囲", "アバター全体（非アクティブを含む）");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. 基本設定", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("変更する項目だけ左端のチェックをONにしてください。", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("距離フェード", EditorStyles.boldLabel);
            Field("overrideStartDistance", "startDistance", "開始距離");
            Field("overrideEndDistance", "endDistance", "終了距離");
            Field("overrideFadeColor", "fadeColor", "色");
            Field("overrideStrength", "strength", "強度");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("リム", EditorStyles.boldLabel);
            Field("overrideRimColor", "rimColor", "色");
            Field("overrideRimFresnelPower", "rimFresnelPower", "リムライトの細さ");
            DrawOptions();
            if (serializedObject.ApplyModifiedProperties()) Invalidate();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3. 集計・確認", EditorStyles.boldLabel);
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
            EditorGUILayout.HelpBox("集計は対象件数の確認用です。更新しなくても、NDMFビルド時に最新の設定で処理されます。" +
                "MA・TTT処理後の対象数とは異なる場合があります。", MessageType.Info);
        }

        private void DrawOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("オプション", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("見出しをクリックして開閉します。閉じても設定は適用されます。", EditorStyles.wordWrappedMiniLabel);

            var exclusions = serializedObject.FindProperty("excludedMaterials");
            // Array PropertyField draws its own foldout header. Keep outer foldouts ungrouped.
            showExclusions = EditorGUILayout.Foldout(showExclusions, $"除外マテリアル（登録枠 {exclusions.arraySize}）", true, EditorStyles.foldoutHeader);
            if (showExclusions)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(exclusions, new GUIContent("除外リスト"), true);
                    EditorGUILayout.HelpBox("指定したMaterialを使うすべてのスロットを除外します。未設定・重複要素は無視します。" +
                        "他ツールによる置換後の除外は、NDMFに置換元が登録されている場合に引き継がれます。", MessageType.Info);
                }
            }
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "高度な設定", true, EditorStyles.foldoutHeader);
            if (showAdvanced)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    Field("overrideBackfaceShadow", "backfaceShadow", "裏面を陰にする");
                    Field("overrideMode", "mode", "モード");
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("strictLilToonCheck"), new GUIContent("Strict lilToon Check"));
                    EditorGUILayout.LabelField("StrictをOFFにすると、名前の規則に一致するカスタムShaderも候補に含めます。", EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private void Field(string toggle, string value, string label)
        {
            var apply = serializedObject.FindProperty(toggle);
            var property = serializedObject.FindProperty(value);
            using (new EditorGUILayout.HorizontalScope())
            {
                var toggleRect = EditorGUILayout.GetControlRect(GUILayout.Width(18));
                var toggleLabel = new GUIContent(string.Empty, label + "を適用します。OFFの場合はマテリアルの既存値を保持します。");
                EditorGUI.BeginProperty(toggleRect, toggleLabel, apply);
                apply.boolValue = EditorGUI.ToggleLeft(toggleRect, toggleLabel, apply.boolValue);
                EditorGUI.EndProperty();
                using (new EditorGUI.DisabledScope(!apply.boolValue))
                {
                    EditorGUI.BeginChangeCheck();
                    if (value == "mode") ModeField(property, label);
                    else EditorGUILayout.PropertyField(property, new GUIContent(label));
                    if (EditorGUI.EndChangeCheck() && value == "rimFresnelPower" && SettingsValidator.Finite(property.floatValue))
                        property.floatValue = Mathf.Clamp(property.floatValue, 0.01f, 50);
                    if ((value == "fadeColor" || value == "rimColor") &&
                        GUILayout.Button(new GUIContent("初期色", "この色だけを初期値に戻します。他の設定は変更しません。"), GUILayout.Width(54)))
                        ResetColorToDefault(property);
                }
            }
        }

        internal static void ResetColorToDefault(SerializedProperty property)
        {
            switch (property.name)
            {
                case "fadeColor": property.colorValue = DistanceFadeBulkSetter.DefaultFadeColor; break;
                case "rimColor": property.colorValue = DistanceFadeBulkSetter.DefaultRimColor; break;
                default: throw new System.ArgumentException("Expected a distance fade color property.", nameof(property));
            }
        }

        private static void ModeField(SerializedProperty property, string label)
        {
            var current = property.intValue;
            var known = current == 0 || current == 1;
            var labels = known ? new[] { "頂点", "座標" } : new[] { "頂点", "座標", $"未対応値 ({current})" };
            var values = known ? new[] { 0, 1 } : new[] { 0, 1, current };
            var rect = EditorGUILayout.GetControlRect();
            var content = EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            EditorGUI.BeginChangeCheck();
            var selected = EditorGUI.IntPopup(rect, content, current,
                System.Array.ConvertAll(labels, text => new GUIContent(text)), values);
            if (EditorGUI.EndChangeCheck()) property.intValue = selected;
            EditorGUI.EndProperty();
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
