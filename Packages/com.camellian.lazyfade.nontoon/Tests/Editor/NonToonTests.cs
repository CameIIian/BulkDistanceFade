using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Camellian.NonToonDistanceFade.Editor;
using jp.lilxyzw.shadercore;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Camellian.NonToonDistanceFade.Tests
{
    public sealed class NonToonVersionTests
    {
        [TestCase("0.1.3", "0.1.3", true)]
        [TestCase("0.1.4", "0.1.3", true)]
        [TestCase("0.1.10", "0.1.3", true)]
        [TestCase("0.2.0", "0.1.3", true)]
        [TestCase("1.0.0", "0.1.3", true)]
        [TestCase("0.1.2", "0.1.3", false)]
        [TestCase("0.1.11", "0.1.11", true)]
        [TestCase("0.1.12", "0.1.11", true)]
        [TestCase("0.1.9", "0.1.11", false)]
        [TestCase("0.1.3+build.1", "0.1.3", true)]
        [TestCase("0.1.3-beta.1", "0.1.3", false)]
        [TestCase("0.2.0-beta.1", "0.1.3", false)]
        [TestCase("invalid", "0.1.3", false)]
        [TestCase("0.1", "0.1.3", false)]
        [TestCase("0.1.3.0", "0.1.3", false)]
        [TestCase("", "0.1.3", false)]
        [TestCase(null, "0.1.3", false)]
        public void AcceptsStableVersionsAtOrAboveMinimum(string value, string minimum, bool expected)
        {
            Assert.That(NonToonShader.IsSupportedVersion(value, new Version(minimum)), Is.EqualTo(expected));
        }
    }

    internal sealed class InspectorWindow : EditorWindow
    {
        internal UnityEditor.Editor Inspector;
        internal int Repaints;
        private void OnGUI() { Inspector.OnInspectorGUI(); if (Event.current.type == EventType.Repaint) Repaints++; }
    }

    public sealed class NonToonTests
    {
        private readonly List<Object> owned = new List<Object>();
        private GameObject root;
        private LazyFadeNonToon setting;
        private Material material;
        private NonToonProperties properties;
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        [SetUp]
        public void SetUp()
        {
            root = Own(new GameObject("Avatar"));
            root.AddComponent<VRCAvatarDescriptor>();
            setting = root.AddComponent<LazyFadeNonToon>();
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(NonToonShader.ShaderGuid));
            Assert.That(shader, Is.Not.Null, "Install NonToon 0.1.3 and ShaderCore 0.1.11.");
            properties = NonToonShader.Resolve(shader, out var reason);
            Assert.That(properties, Is.Not.Null, reason);
            material = Own(new Material(shader));
            material.SetVector(properties.Distance, new Vector4(0.01f, 0.1f, 7, 9));
            material.SetFloat(properties.Strength, 0.3f);
        }
        [TearDown]
        public void TearDown()
        {
            foreach (var obj in owned.AsEnumerable().Reverse()) if (obj != null) Object.DestroyImmediate(obj);
            owned.Clear();
        }
        private MeshRenderer Renderer(params Material[] materials)
        {
            var child = Own(new GameObject("Mesh")); child.transform.SetParent(root.transform);
            var renderer = child.AddComponent<MeshRenderer>(); renderer.sharedMaterials = materials; return renderer;
        }
        private NonToonPlan Apply() => NonToonPass.Apply(root, m => Own(m));
        private void AllOn()
        {
            setting.overrideNearDistance = setting.overrideFarDistance = setting.overrideStrength = true;
            setting.nearDistance = 0.02f; setting.farDistance = 0.2f; setting.strength = 0.8f;
        }

        [TestCase(NonToonShader.ShaderGuid)]
        [TestCase(NonToonShader.FurGuid)]
        public void OfficialGeneratedShadersResolveModuleNames(string guid)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            var result = NonToonShader.Resolve(shader, out var reason);
            Assert.That(result, Is.Not.Null, reason);
            Assert.That(result.Distance, Is.EqualTo("_jp_lilxyzw_nontoon_distancefade_DistanceFade"));
            Assert.That(result.Strength, Is.EqualTo("_jp_lilxyzw_nontoon_distancefade_DistanceFadeStrength"));
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void PartialOverridesPreserveOtherValuesAndShareOneClone(int mask)
        {
            AllOn();
            setting.overrideNearDistance = (mask & 1) != 0;
            setting.overrideFarDistance = (mask & 2) != 0;
            setting.overrideStrength = (mask & 4) != 0;
            var before = EditorJsonUtility.ToJson(material);
            var first = Renderer(material, null, material);
            var second = Renderer(material); second.enabled = false; second.gameObject.SetActive(false);
            var plan = Apply();
            var output = first.sharedMaterial;
            Assert.That(first.sharedMaterials[1], Is.Null);
            Assert.That(first.sharedMaterials[2], Is.SameAs(output));
            Assert.That(second.sharedMaterial, Is.SameAs(output));
            Assert.That(plan.Materials.Count, Is.EqualTo(mask == 0 ? 0 : 1));
            Assert.That(output == material, Is.EqualTo(mask == 0));
            Assert.That(output.GetVector(properties.Distance), Is.EqualTo(new Vector4((mask & 1) != 0 ? 0.02f : 0.01f, (mask & 2) != 0 ? 0.2f : 0.1f, 7, 9)));
            Assert.That(output.GetFloat(properties.Strength), Is.EqualTo((mask & 4) != 0 ? 0.8f : 0.3f));
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(before));
            Assert.That(root.GetComponent<LazyFadeNonToon>(), Is.Null);
        }

        [Test]
        public void DefaultsDoNotOverwriteAndDisabledSettingsAreRemovedOnlyFromOutput()
        {
            Assert.That(NonToonPass.Capture(setting).Any, Is.False);
            var renderer = Renderer(material); AllOn(); setting.enabled = false;
            setting.strength = float.NaN;
            Assert.That(Apply().Materials, Is.Empty);
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            Assert.That(root.GetComponent<LazyFadeNonToon>(), Is.Null);
        }

        [TestCase(false)] [TestCase(true)]
        public void PlacementAndDuplicateChecksAreSpecificToNonToon(bool duplicate)
        {
            var child = Own(new GameObject("Child")); child.transform.SetParent(root.transform);
            if (!duplicate) Object.DestroyImmediate(setting);
            child.AddComponent<LazyFadeNonToon>().enabled = false;
            Assert.Throws<InvalidOperationException>(() => NonToonPass.ValidatePlacement(root));
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-0.1f)] [TestCase(1.1f)]
        public void InvalidEnabledValuesFailBeforeAnyCloning(float invalid)
        {
            var renderer = Renderer(material); setting.overrideStrength = true; setting.strength = invalid;
            var saves = 0;
            Assert.Throws<InvalidOperationException>(() => NonToonPass.Apply(root, m => saves++));
            Assert.That(saves, Is.Zero); Assert.That(renderer.sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void PartialDistanceOverrideValidatesRetainedBoundary()
        {
            Renderer(material); setting.overrideNearDistance = true; setting.nearDistance = 0.1f;
            Assert.Throws<InvalidOperationException>(() => Apply());
        }

        [Test]
        public void CollectionIsReadOnlyAndBuildUsesLatestExclusionsWithoutRefresh()
        {
            AllOn(); var renderer = Renderer(material, material);
            var before = EditorJsonUtility.ToJson(material);
            Assert.That(NonToonPass.Collect(root, NonToonPass.Capture(setting)).Materials.Count, Is.EqualTo(1));
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(before));
            setting.excludedMaterials = new[] { material, null, material };
            var result = Apply();
            Assert.That(result.ExcludedMaterials, Is.EqualTo(1)); Assert.That(result.ExcludedSlots, Is.EqualTo(2));
            Assert.That(result.Materials, Is.Empty); Assert.That(renderer.sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void NdmfOriginMappingCarriesExclusionAcrossReplacement()
        {
            AllOn(); setting.excludedMaterials = new[] { material };
            var replacement = Own(new Material(material)); Renderer(replacement);
            using (new ObjectRegistryScope(new ObjectRegistry(root.transform)))
            {
                ObjectRegistry.RegisterReplacedObject(material, replacement);
                var result = NonToonPass.Collect(root, NonToonPass.Capture(setting));
                Assert.That(result.ExcludedSlots, Is.EqualTo(1)); Assert.That(result.Materials, Is.Empty);
            }
        }

        [Test]
        public void SaveFailurePreservesReferencesAndDisposesClones()
        {
            AllOn(); var renderer = Renderer(material); Material created = null;
            Assert.Throws<InvalidOperationException>(() => NonToonPass.Apply(root, m => { created = m; throw new InvalidOperationException("Save failed"); }));
            Assert.That(created == null, Is.True); Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            Assert.That(root.GetComponent<LazyFadeNonToon>(), Is.SameAs(setting));
        }

        [TestCase(false)] [TestCase(true)]
        public void UnknownGeneratedOriginRespectsFailurePolicy(bool fail)
        {
            AllOn(); setting.failOnUnsupported = fail;
            var untracked = Own(Object.Instantiate(material.shader));
            untracked.name = "NonToon";
            Renderer(Own(new Material(untracked)));
            if (fail) Assert.Throws<InvalidOperationException>(() => Apply());
            else { var result = Apply(); Assert.That(result.Unsupported.Count, Is.EqualTo(1)); Assert.That(result.Materials, Is.Empty); }
        }

        [Test]
        public void WrongModulePropertiesAreRejected()
        {
            var module = new SCModule { properties = new List<SCProperty> {
                new SCProperty { originalName = "_DistanceFade", name = "_Unrelated", type = "float4" },
                new SCProperty { originalName = "_DistanceFadeStrength", name = properties.Strength, type = "float" }
            }};
            Assert.That(NonToonShader.ResolveProperties(material.shader, module, out var reason), Is.Null);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void PresetCopiesOnlyThreeValuesAndRejectsInvalidBeforeWriting()
        {
            setting.excludedMaterials = new[] { material }; setting.failOnUnsupported = true;
            var serialized = new SerializedObject(setting);
            NonToonPresets.Apply(serialized, new NonToonPreset("Custom", 0.03f, 0.4f, 0.7f));
            serialized.ApplyModifiedProperties();
            Assert.That(setting.nearDistance, Is.EqualTo(0.03f)); Assert.That(setting.farDistance, Is.EqualTo(0.4f));
            Assert.That(setting.strength, Is.EqualTo(0.7f)); Assert.That(NonToonPass.Capture(setting).Any, Is.False);
            Assert.That(setting.excludedMaterials, Is.EqualTo(new[] { material })); Assert.That(setting.failOnUnsupported, Is.True);
            var before = EditorJsonUtility.ToJson(setting);
            Assert.Throws<InvalidOperationException>(() => NonToonPresets.Apply(serialized, new NonToonPreset("Invalid", 0.8f, 0.2f, 0.5f)));
            serialized.ApplyModifiedProperties(); Assert.That(EditorJsonUtility.ToJson(setting), Is.EqualTo(before));
        }

        [Test]
        public void RealNdmfBuildSupportsCoexistenceWithLilToonComponent()
        {
            AllOn(); var renderer = Renderer(material);
            var lilType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Camellian.DistanceFade.LazyFadeLilToon")).FirstOrDefault(t => t != null);
            if (lilType == null) Assert.Ignore("Coexistence test requires the separate lilToon package.");
            var lilSetting = root.AddComponent(lilType);
            var lilMaterial = Own(new Material(Shader.Find("lilToon")));
            Renderer(lilMaterial);
            var before = EditorJsonUtility.ToJson(setting);
            var clone = Own(Object.Instantiate(root));
            using (new OverrideTemporaryDirectoryScope(null)) AvatarProcessor.ProcessAvatar(clone);
            var outputs = clone.GetComponentsInChildren<MeshRenderer>();
            foreach (var output in outputs) Own(output.sharedMaterial);
            Assert.That(outputs[0].sharedMaterial, Is.Not.SameAs(material));
            Assert.That(outputs[0].sharedMaterial.GetFloat(properties.Strength), Is.EqualTo(0.8f));
            Assert.That(outputs[1].sharedMaterial, Is.Not.SameAs(lilMaterial));
            Assert.That(outputs[1].sharedMaterial.GetVector("_DistanceFade").z, Is.EqualTo(0.95f));
            Assert.That(clone.GetComponent<LazyFadeNonToon>(), Is.Null);
            Assert.That(clone.GetComponent(lilType), Is.Null);
            Assert.That(root.GetComponent(lilType), Is.SameAs(lilSetting));
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            Assert.That(EditorJsonUtility.ToJson(setting), Is.EqualTo(before));
        }

        [Test]
        public void RenamedComponentLoadsLegacyPrefabIdentityAndValues()
        {
            var path = "Assets/lazyFadeNonToonMigration_" + Guid.NewGuid().ToString("N") + ".prefab";
            try
            {
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(setting))),
                    Is.EqualTo("bfee454e379545bba38b19dcb64f6270"));
                setting.nearDistance = 0.03f;
                setting.overrideNearDistance = true;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var yaml = System.IO.File.ReadAllText(path);
                yaml = System.Text.RegularExpressions.Regex.Replace(yaml, @"(?m)^  m_EditorClassIdentifier:.*$",
                    "  m_EditorClassIdentifier: Camellian.NonToonDistanceFade.Runtime::Camellian.NonToonDistanceFade.NonToonDistanceFadeBulkSetter");
                System.IO.File.WriteAllText(path, yaml);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LazyFadeNonToon>();
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.nearDistance, Is.EqualTo(0.03f));
                Assert.That(loaded.overrideNearDistance, Is.True);
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [UnityTest]
        public IEnumerator ExpandedInspectorDoesNotMutateSettingsOrNestHeaders()
        {
            setting.excludedMaterials = new[] { material, null };
            var inspector = Own(UnityEditor.Editor.CreateEditor(setting));
            inspector.serializedObject.FindProperty("excludedMaterials").isExpanded = true;
            typeof(LazyFadeNonToonEditor).GetField("advanced", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inspector, true);
            var before = EditorJsonUtility.ToJson(setting);
            var window = ScriptableObject.CreateInstance<InspectorWindow>();
            try
            {
                window.Inspector = inspector; window.position = new Rect(0, 0, 520, 950); window.ShowUtility();
                for (var i = 0; i < 30 && window.Repaints < 2; i++) { window.Repaint(); yield return null; }
                Assert.That(window.Repaints, Is.GreaterThan(0));
                Assert.That(EditorJsonUtility.ToJson(setting), Is.EqualTo(before));
                LogAssert.NoUnexpectedReceived();
            }
            finally { window.Close(); }
        }
    }
}
