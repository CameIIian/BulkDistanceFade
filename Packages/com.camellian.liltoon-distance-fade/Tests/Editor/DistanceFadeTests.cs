using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Camellian.DistanceFade.Editor;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Camellian.DistanceFade.Tests
{
    internal sealed class DistanceFadeInspectorTestWindow : EditorWindow
    {
        internal UnityEditor.Editor Inspector;
        internal int Repaints;
        private void OnGUI()
        {
            if (Inspector == null) return;
            Inspector.OnInspectorGUI();
            if (Event.current.type == EventType.Repaint) Repaints++;
        }
    }

    public sealed class DistanceFadeTests
    {
        private readonly List<Object> owned = new List<Object>();
        private GameObject root;
        private DistanceFadeBulkSetter setting;
        private Material material;

        private T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }
        [SetUp]
        public void SetUp()
        {
            root = Own(new GameObject("Avatar"));
            root.AddComponent<VRCAvatarDescriptor>();
            setting = root.AddComponent<DistanceFadeBulkSetter>();
            var shader = Shader.Find("lilToon");
            Assert.That(shader, Is.Not.Null, "Install lilToon before running these tests.");
            material = Own(new Material(shader));
        }
        [TearDown]
        public void TearDown()
        {
            LateMaterialReplacementTestPlugin.Target = null;
            LateMaterialReplacementTestPlugin.Replacement = null;
            LateMaterialReplacementTestPlugin.RegisterOrigin = false;
            foreach (var obj in owned.AsEnumerable().Reverse()) if (obj != null) Object.DestroyImmediate(obj);
            owned.Clear();
        }
        private MeshRenderer Renderer(params Material[] materials)
        {
            var child = Own(new GameObject("Mesh"));
            child.transform.SetParent(root.transform);
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }
        private BuildSummary Apply() => ApplyDistanceFadePass.Apply(root, m => Own(m));
        private static void AssertColor(Color actual, Color expected)
        {
            for (var i = 0; i < 4; i++) Assert.That(actual[i], Is.EqualTo(expected[i]).Within(0.00001f));
        }
        private void AllOff()
        {
            setting.overrideFadeColor = setting.overrideStartDistance = setting.overrideEndDistance =
                setting.overrideStrength = setting.overrideBackfaceShadow = setting.overrideMode =
                setting.overrideRimColor = setting.overrideRimFresnelPower = false;
        }

        [Test]
        public void AppliesAllSettingsToCloneAndPreservesSource()
        {
            var r = Renderer(material);
            var before = EditorJsonUtility.ToJson(material);
            setting.fadeColor = new Color(3, 2, 1, 0.4f);
            setting.startDistance = 0.8f;
            setting.endDistance = 0.2f;
            setting.strength = 0.9f;
            setting.overrideBackfaceShadow = true;
            setting.backfaceShadow = true;
            setting.overrideMode = true;
            setting.mode = 17;
            setting.rimColor = new Color(2, 4, 6, 0.7f);
            setting.rimFresnelPower = 7;
            var result = Apply();
            var output = r.sharedMaterial;
            Assert.That(output, Is.Not.SameAs(material));
            Assert.That(output.GetVector(MaterialUtility.Vector), Is.EqualTo(new Vector4(0.8f, 0.2f, 0.9f, 1)));
            AssertColor(output.GetColor(MaterialUtility.Color), new Color(3, 2, 1, 0.4f));
            AssertColor(output.GetColor(MaterialUtility.RimColor), new Color(2, 4, 6, 0.7f));
            Assert.That(output.GetInt(MaterialUtility.Mode), Is.EqualTo(17));
            Assert.That(output.GetFloat(MaterialUtility.Fresnel), Is.EqualTo(7));
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(before));
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.Null);
            Assert.That(result.Clones, Is.EqualTo(1));
        }

        [Test]
        public void ApplyUsesLatestSettingsWithoutInspectorCollection()
        {
            var renderer = Renderer(material);
            setting.strength = 0.25f;
            var oldSummary = ApplyDistanceFadePass.Collect(root, SettingsValidator.Capture(setting)).Summary;
            Assert.That(oldSummary.TargetMaterials, Is.EqualTo(1));
            // Change the live settings without refreshing the Inspector summary.
            setting.excludedMaterials = new[] { material };
            var result = Apply();
            Assert.That(result.TargetMaterials, Is.Zero);
            Assert.That(result.ExcludedSlots, Is.EqualTo(1));
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
        }

        [UnityTest]
        public IEnumerator ExpandedExclusionInspectorDrawsWithoutErrorsOrChangingSettings()
        {
            setting.excludedMaterials = new[] { material, null };
            var before = EditorJsonUtility.ToJson(setting);
            var inspector = Own(UnityEditor.Editor.CreateEditor(setting));
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(DistanceFadeBulkSetterEditor).GetField("showExclusions", flags).SetValue(inspector, true);
            typeof(DistanceFadeBulkSetterEditor).GetField("showAdvanced", flags).SetValue(inspector, true);
            inspector.serializedObject.FindProperty("excludedMaterials").isExpanded = true;
            var window = ScriptableObject.CreateInstance<DistanceFadeInspectorTestWindow>();
            try
            {
                window.Inspector = inspector;
                window.position = new Rect(0, 0, 520, 950);
                window.ShowUtility();
                for (var frame = 0; frame < 30 && window.Repaints < 2; frame++)
                {
                    window.Repaint();
                    yield return null;
                }
                Assert.That(window.Repaints, Is.GreaterThan(0), "The actual Inspector GUI must render.");
                LogAssert.NoUnexpectedReceived();
                Assert.That(EditorJsonUtility.ToJson(setting), Is.EqualTo(before));
            }
            finally { window.Close(); }
        }

        [Test]
        public void DefaultColorsUseByteRgbWithoutHdrAmplificationAndApplyToMaterial()
        {
            var fade = new Color(10f / 255f, 7f / 255f, 7f / 255f, 1);
            var rim = new Color(1, 188f / 255f, 177f / 255f, 0);
            AssertColor(setting.fadeColor, fade);
            AssertColor(setting.rimColor, rim);
            Assert.That(setting.fadeColor.maxColorComponent, Is.LessThanOrEqualTo(1));
            Assert.That(setting.rimColor.maxColorComponent, Is.LessThanOrEqualTo(1));
            var before = EditorJsonUtility.ToJson(material);
            var r = Renderer(material);
            Apply();
            AssertColor(r.sharedMaterial.GetColor(MaterialUtility.Color), fade);
            AssertColor(r.sharedMaterial.GetColor(MaterialUtility.RimColor), rim);
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(before));
        }

        [TestCase("fadeColor")]
        [TestCase("rimColor")]
        public void ResetColorOnlyUpdatesSelectedColorAndSupportsUndoRedo(string propertyName)
        {
            var oldFade = new Color(10, 7, 7, 1);
            var oldRim = new Color(255, 188, 177, 0);
            setting.fadeColor = oldFade;
            setting.rimColor = oldRim;
            setting.startDistance = 0.42f;
            setting.strength = 0.63f;
            setting.overrideFadeColor = false;
            var snapshot = SettingsValidator.Capture(setting);
            AssertColor(snapshot.Color, oldFade);
            AssertColor(snapshot.RimColor, oldRim);

            Undo.IncrementCurrentGroup();
            var serialized = new SerializedObject(setting);
            DistanceFadeBulkSetterEditor.ResetColorToDefault(serialized.FindProperty(propertyName));
            AssertColor(setting.fadeColor, oldFade);
            AssertColor(setting.rimColor, oldRim);
            serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            var expectedFade = propertyName == "fadeColor" ? new Color(10f / 255f, 7f / 255f, 7f / 255f, 1) : oldFade;
            var expectedRim = propertyName == "rimColor" ? new Color(1, 188f / 255f, 177f / 255f, 0) : oldRim;
            AssertColor(setting.fadeColor, expectedFade);
            AssertColor(setting.rimColor, expectedRim);
            Assert.That(setting.startDistance, Is.EqualTo(0.42f));
            Assert.That(setting.strength, Is.EqualTo(0.63f));
            Assert.That(setting.overrideFadeColor, Is.False);
            Undo.PerformUndo();
            AssertColor(setting.fadeColor, oldFade);
            AssertColor(setting.rimColor, oldRim);
            Undo.PerformRedo();
            AssertColor(setting.fadeColor, expectedFade);
            AssertColor(setting.rimColor, expectedRim);
            Undo.ClearUndo(setting);
        }

        [Test]
        public void DefaultSettingsPreserveMaterialBackfaceAndMode()
        {
            Assert.That(setting.overrideBackfaceShadow, Is.False);
            Assert.That(setting.overrideMode, Is.False);
            Assert.That(setting.backfaceShadow, Is.False);
            material.SetVector(MaterialUtility.Vector, new Vector4(0.4f, 0.2f, 0.7f, 1));
            material.SetInt(MaterialUtility.Mode, 1);
            var r = Renderer(material);
            Apply();
            Assert.That(r.sharedMaterial, Is.Not.SameAs(material));
            Assert.That(r.sharedMaterial.GetVector(MaterialUtility.Vector).w, Is.EqualTo(1));
            Assert.That(r.sharedMaterial.GetInt(MaterialUtility.Mode), Is.EqualTo(1));
        }

        [Test]
        public void SharedSlotsUseOneCloneAndKeepNullsAndOtherShaders()
        {
            var standard = Own(new Material(Shader.Find("Standard")));
            var a = Renderer(material, null, standard, material);
            var b = Renderer(material);
            var c = Renderer(material);
            var result = Apply();
            Assert.That(result.Clones, Is.EqualTo(1));
            Assert.That(result.ReplacedSlots, Is.EqualTo(4));
            Assert.That(a.sharedMaterials.Length, Is.EqualTo(4));
            Assert.That(a.sharedMaterials[1], Is.Null);
            Assert.That(a.sharedMaterials[2], Is.SameAs(standard));
            Assert.That(a.sharedMaterials[0], Is.SameAs(a.sharedMaterials[3]));
            Assert.That(b.sharedMaterial, Is.SameAs(c.sharedMaterial).And.SameAs(a.sharedMaterials[0]));
        }

        [Test]
        public void PartialVectorOverridePreservesOtherElements()
        {
            material.SetVector(MaterialUtility.Vector, new Vector4(0.4f, 0.2f, 0.7f, 3));
            AllOff();
            setting.overrideStartDistance = true;
            setting.startDistance = 0.1f;
            var r = Renderer(material);
            Apply();
            Assert.That(r.sharedMaterial.GetVector(MaterialUtility.Vector), Is.EqualTo(new Vector4(0.1f, 0.2f, 0.7f, 3)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DisabledOrAllOffDoesNotClone(bool disabled)
        {
            if (disabled) { setting.enabled = false; setting.strength = float.NaN; }
            else AllOff();
            var r = Renderer(material);
            var summary = Apply();
            Assert.That(summary.Clones, Is.Zero);
            Assert.That(r.sharedMaterial, Is.SameAs(material));
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.Null);
        }

        [Test]
        public void IncludesInactiveRootAndChildrenAndDisabledRenderers()
        {
            var r = Renderer(material);
            r.enabled = false;
            r.gameObject.SetActive(false);
            root.SetActive(false);
            Assert.That(Apply().TargetRenderers, Is.EqualTo(1));
            Assert.That(r.sharedMaterial, Is.Not.SameAs(material));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ChildPlacementFailsEvenWhenDisabled(bool enabled)
        {
            Object.DestroyImmediate(setting);
            var r = Renderer(material);
            r.gameObject.AddComponent<DistanceFadeBulkSetter>().enabled = enabled;
            Assert.Throws<SettingsException>(() => Apply());
            Assert.That(r.sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void DuplicateFailsEvenWhenOneIsDisabled()
        {
            Renderer(material).gameObject.AddComponent<DistanceFadeBulkSetter>().enabled = false;
            Assert.That(Assert.Throws<SettingsException>(() => Apply()).Message, Does.StartWith("E002"));
        }

        [Test]
        public void MissingDescriptorFails()
        {
            Object.DestroyImmediate(root.GetComponent<VRCAvatarDescriptor>());
            Assert.That(Assert.Throws<SettingsException>(() => Apply()).Message, Does.StartWith("E001"));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidEnabledValueFailsBeforeSaving(float value)
        {
            var r = Renderer(material);
            setting.strength = value;
            var saved = 0;
            Assert.Throws<SettingsException>(() => ApplyDistanceFadePass.Apply(root, m => saved++));
            Assert.That(saved, Is.Zero);
            Assert.That(r.sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void InvalidHdrAlphaIsRejectedButOffValueIsIgnored()
        {
            setting.rimColor = new Color(2, 3, 4, float.NaN);
            Assert.Throws<SettingsException>(() => SettingsValidator.Capture(setting));
            setting.overrideRimColor = false;
            Assert.DoesNotThrow(() => SettingsValidator.Capture(setting));
        }

        [TestCase(-1f, 0.01f)]
        [TestCase(0f, 0.01f)]
        [TestCase(100f, 50f)]
        public void FresnelIsClampedWithoutMutatingSettings(float input, float expected)
        {
            setting.rimFresnelPower = input;
            var snapshot = SettingsValidator.Capture(setting);
            Assert.That(snapshot.Fresnel, Is.EqualTo(expected));
            Assert.That(snapshot.FresnelClamped, Is.True);
            Assert.That(setting.rimFresnelPower, Is.EqualTo(input));
        }

        [Test]
        public void ReadOnlySummaryDoesNotMutateOrClone()
        {
            var r = Renderer(material, material);
            var before = EditorJsonUtility.ToJson(material);
            var plan = ApplyDistanceFadePass.Collect(root, SettingsValidator.Capture(setting));
            Assert.That(plan.Summary.TargetMaterials, Is.EqualTo(1));
            Assert.That(plan.Summary.Clones, Is.Zero);
            Assert.That(r.sharedMaterial, Is.SameAs(material));
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(before));
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.SameAs(setting));
        }

        [Test]
        public void SaveFailureDoesNotReplaceRendererReferences()
        {
            var r = Renderer(material);
            Material failedClone = null;
            Assert.Throws<InvalidOperationException>(() => ApplyDistanceFadePass.Apply(root, m =>
            {
                failedClone = m;
                throw new InvalidOperationException("Injected save failure");
            }));
            Assert.That(r.sharedMaterial, Is.SameAs(material));
            Assert.That(failedClone == null, Is.True, "Uncommitted clones must be destroyed.");
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.SameAs(setting));
        }

        [Test]
        public void ConsecutiveAvatarsDoNotShareCloneCache()
        {
            var r = Renderer(material);
            Apply();
            var first = r.sharedMaterial;
            setting = root.AddComponent<DistanceFadeBulkSetter>();
            r.sharedMaterial = material;
            Apply();
            Assert.That(r.sharedMaterial, Is.Not.SameAs(first));
        }

        [TestCase("lilToon", true, true)]
        [TestCase("Hidden/lilToonTransparent", true, true)]
        [TestCase("Custom/lilToonVariant", true, false)]
        [TestCase("Custom/lilToonVariant", false, true)]
        [TestCase("NotlilToon", false, false)]
        [TestCase("Standard", false, false)]
        public void ShaderCandidateRules(string name, bool strict, bool expected)
        {
            Assert.That(MaterialUtility.IsCandidate(name, strict), Is.EqualTo(expected));
        }

        [Test]
        public void UnsupportedPropertiesAreDeduplicatedAndOnlySupportedValuesChange()
        {
            var partial = Own(new Material(Shader.Find("DistanceFadeTests/lilToonPartial")));
            setting.strictLilToonCheck = false;
            setting.startDistance = 0.6f;
            var r = Renderer(partial, partial);
            var summary = Apply();
            Assert.That(summary.Missing.Count, Is.EqualTo(1));
            Assert.That(summary.Missing[0].properties, Does.Contain(MaterialUtility.RimColor));
            Assert.That(r.sharedMaterial.GetVector(MaterialUtility.Vector).x, Is.EqualTo(0.6f));
            Assert.That(partial.GetVector(MaterialUtility.Vector).x, Is.EqualTo(0.4f));
        }

        [Test]
        public void AllUnsupportedDoesNotClone()
        {
            var partial = Own(new Material(Shader.Find("DistanceFadeTests/lilToonPartial")));
            setting.strictLilToonCheck = false;
            AllOff();
            setting.overrideRimColor = true;
            var r = Renderer(partial);
            var summary = Apply();
            Assert.That(summary.Clones, Is.Zero);
            Assert.That(summary.Missing.Count, Is.EqualTo(1));
            Assert.That(r.sharedMaterial, Is.SameAs(partial));
        }

        [Test]
        public void RegisteredPluginProcessesBuildCloneThroughNdmf()
        {
            var originalRenderer = Renderer(material);
            setting.strength = 0.75f;
            var avatarClone = Own(Object.Instantiate(root));
            using (new OverrideTemporaryDirectoryScope(null)) AvatarProcessor.ProcessAvatar(avatarClone);
            var output = avatarClone.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            if (output != material) Own(output);
            Assert.That(output, Is.Not.SameAs(material), "The exported NDMF plugin must actually execute.");
            Assert.That(output.GetVector(MaterialUtility.Vector).z, Is.EqualTo(0.75f));
            Assert.That(avatarClone.GetComponent<DistanceFadeBulkSetter>(), Is.Null);
            Assert.That(originalRenderer.sharedMaterial, Is.SameAs(material));
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.SameAs(setting));
        }

        [Test]
        public void AppliesAfterLateOptimizingMaterialReplacement()
        {
            Renderer(material);
            setting.strength = 0.75f;
            setting.overrideStartDistance = false;
            var replacement = Own(new Material(material));
            replacement.SetVector(MaterialUtility.Vector, new Vector4(0.8f, 0.3f, 0.2f, 0));
            var clone = Own(Object.Instantiate(root));
            LateMaterialReplacementTestPlugin.Target = clone;
            LateMaterialReplacementTestPlugin.Replacement = replacement;
            using (new OverrideTemporaryDirectoryScope(null)) AvatarProcessor.ProcessAvatar(clone);
            var output = clone.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            if (output != replacement && output != material) Own(output);
            Assert.That(output, Is.Not.SameAs(replacement));
            Assert.That(output.GetVector(MaterialUtility.Vector).x, Is.EqualTo(0.8f));
            Assert.That(output.GetVector(MaterialUtility.Vector).z, Is.EqualTo(0.75f));
            Assert.That(replacement.GetVector(MaterialUtility.Vector).z, Is.EqualTo(0.2f));
        }

        [Test]
        public void ExclusionPreservesSharedSlotsAndDistinguishesSameNamedMaterials()
        {
            var included = Own(new Material(material) { name = material.name });
            var unused = Own(new Material(material));
            var deleted = new Material(material);
            Object.DestroyImmediate(deleted);
            setting.excludedMaterials = new[] { material, null, material, unused, deleted };
            var before = EditorJsonUtility.ToJson(material);
            var a = Renderer(material, included, null, material);
            var b = Renderer(material, included);
            b.enabled = false;
            b.gameObject.SetActive(false);
            var child = Own(new GameObject("Skinned"));
            child.transform.SetParent(root.transform);
            var skinned = child.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMaterials = new[] { material, material };
            skinned.enabled = false;
            var result = Apply();
            Assert.That(result.ScannedRenderers, Is.EqualTo(3));
            Assert.That(result.TargetRenderers, Is.EqualTo(2));
            Assert.That(result.TargetMaterials, Is.EqualTo(1));
            Assert.That(result.Clones, Is.EqualTo(1));
            Assert.That(result.ReplacedSlots, Is.EqualTo(2));
            Assert.That(result.ExcludedMaterials, Is.EqualTo(1));
            Assert.That(result.ExcludedSlots, Is.EqualTo(5));
            Assert.That(result.SkippedSlots["手動除外"], Is.EqualTo(5));
            Assert.That(a.sharedMaterials, Is.EqualTo(new[] { material, b.sharedMaterials[1], null, material }));
            Assert.That(b.sharedMaterials[0], Is.SameAs(material));
            Assert.That(b.sharedMaterials[1], Is.Not.SameAs(included));
            Assert.That(skinned.sharedMaterials, Is.EqualTo(new[] { material, material }));
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyOrNullExclusionKeepsExistingBehavior(bool nullArray)
        {
            Assert.That(setting.excludedMaterials, Is.Empty);
            if (nullArray) setting.excludedMaterials = null;
            var r = Renderer(material);
            var summary = Apply();
            Assert.That(summary.Clones, Is.EqualTo(1));
            Assert.That(summary.ExcludedSlots, Is.Zero);
            Assert.That(r.sharedMaterial, Is.Not.SameAs(material));
        }

        [Test]
        public void AllExcludedSkipsSavingAndRemovesOutputSetting()
        {
            setting.excludedMaterials = new[] { material };
            var r = Renderer(material, material);
            var result = ApplyDistanceFadePass.Apply(root, m => Assert.Fail("Excluded materials must not be saved."));
            Assert.That(result.TargetRenderers, Is.Zero);
            Assert.That(result.TargetMaterials, Is.Zero);
            Assert.That(result.Clones, Is.Zero);
            Assert.That(result.ReplacedSlots, Is.Zero);
            Assert.That(result.ExcludedSlots, Is.EqualTo(2));
            Assert.That(r.sharedMaterials, Is.EqualTo(new[] { material, material }));
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisabledOrAllOffTakesPriorityOverExclusion(bool disabled)
        {
            setting.excludedMaterials = new[] { material };
            if (disabled) setting.enabled = false;
            else AllOff();
            var r = Renderer(material);
            var result = Apply();
            Assert.That(result.ExcludedSlots, Is.Zero);
            Assert.That(result.Clones, Is.Zero);
            if (!disabled) Assert.That(result.SkippedSlots["全項目OFF"], Is.EqualTo(1));
            Assert.That(r.sharedMaterial, Is.SameAs(material));
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.Null);
        }

        [Test]
        public void ExclusionDoesNotBypassSettingsValidation()
        {
            setting.excludedMaterials = new[] { material };
            setting.strength = float.NaN;
            Renderer(material);
            Assert.That(Assert.Throws<SettingsException>(() => Apply()).Message, Does.StartWith("E003"));
        }

        [Test]
        public void ExcludedUnsupportedMaterialsDoNotWarn()
        {
            var partial = Own(new Material(Shader.Find("DistanceFadeTests/lilToonPartial")));
            var standard = Own(new Material(Shader.Find("Standard")));
            setting.strictLilToonCheck = false;
            setting.excludedMaterials = new[] { partial, standard };
            Renderer(partial, partial, standard);
            var result = Apply();
            Assert.That(result.Missing, Is.Empty);
            Assert.That(result.ExcludedMaterials, Is.EqualTo(2));
            Assert.That(result.ExcludedSlots, Is.EqualTo(3));
            Assert.That(result.Clones, Is.Zero);
        }

        [Test]
        public void ExclusionSnapshotAndCollectionAreReadOnly()
        {
            setting.excludedMaterials = new[] { material, null, material };
            var snapshot = SettingsValidator.Capture(setting);
            Assert.That(setting.excludedMaterials.Length, Is.EqualTo(3));
            setting.excludedMaterials[0] = null;
            setting.excludedMaterials[2] = null;
            var before = EditorJsonUtility.ToJson(setting);
            var materialBefore = EditorJsonUtility.ToJson(material);
            var r = Renderer(material);
            IObjectRegistry registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                var plan = ApplyDistanceFadePass.Collect(root, snapshot);
                Assert.That(plan.Summary.ExcludedSlots, Is.EqualTo(1));
                Assert.That(plan.Summary.Clones, Is.Zero);
                Assert.That(registry.GetReference(material, false), Is.Null);
            }
            Assert.That(r.sharedMaterial, Is.SameAs(material));
            Assert.That(EditorJsonUtility.ToJson(setting), Is.EqualTo(before));
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(materialBefore));
            Assert.That(ApplyDistanceFadePass.Collect(root, SettingsValidator.Capture(setting)).Summary.TargetMaterials, Is.EqualTo(1));
        }

        [Test]
        public void ExclusionFollowsRegisteredChainsAndBranchesOnlyWithinBuild()
        {
            var intermediate = Own(new Material(material));
            var final = Own(new Material(material));
            var sibling = Own(new Material(material));
            setting.excludedMaterials = new[] { intermediate };
            Renderer(material, final, sibling);
            using (new ObjectRegistryScope(new ObjectRegistry(root.transform)))
            {
                ObjectRegistry.RegisterReplacedObject(material, intermediate);
                ObjectRegistry.RegisterReplacedObject(intermediate, final);
                ObjectRegistry.RegisterReplacedObject(material, sibling);
                var result = ApplyDistanceFadePass.Collect(root, SettingsValidator.Capture(setting)).Summary;
                Assert.That(result.ExcludedMaterials, Is.EqualTo(3));
                Assert.That(result.ExcludedSlots, Is.EqualTo(3));
                Assert.That(result.TargetMaterials, Is.Zero);
            }
            using (new ObjectRegistryScope(new ObjectRegistry(root.transform)))
            {
                var result = Apply();
                Assert.That(result.ExcludedSlots, Is.Zero);
                Assert.That(result.Clones, Is.EqualTo(3));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NdmfReplacementExclusionRequiresRegisteredOrigin(bool registered)
        {
            var sourceRenderer = Renderer(material);
            setting.excludedMaterials = new[] { material };
            setting.strength = 0.75f;
            var replacement = Own(new Material(material));
            replacement.SetVector(MaterialUtility.Vector, new Vector4(0.8f, 0.3f, 0.2f, 0));
            var before = EditorJsonUtility.ToJson(replacement);
            var clone = Own(Object.Instantiate(root));
            LateMaterialReplacementTestPlugin.Target = clone;
            LateMaterialReplacementTestPlugin.Replacement = replacement;
            LateMaterialReplacementTestPlugin.RegisterOrigin = registered;
            using (new OverrideTemporaryDirectoryScope(null)) AvatarProcessor.ProcessAvatar(clone);
            var output = clone.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            if (output != replacement && output != material) Own(output);
            if (registered) Assert.That(output, Is.SameAs(replacement));
            else
            {
                Assert.That(output, Is.Not.SameAs(replacement));
                Assert.That(output.GetVector(MaterialUtility.Vector).z, Is.EqualTo(0.75f));
            }
            Assert.That(EditorJsonUtility.ToJson(replacement), Is.EqualTo(before));
            Assert.That(sourceRenderer.sharedMaterial, Is.SameAs(material));
            Assert.That(setting.excludedMaterials, Is.EqualTo(new[] { material }));
            Assert.That(clone.GetComponent<DistanceFadeBulkSetter>(), Is.Null);
        }

        [Test]
        public void NdmfBuildCloneKeepsDirectExclusion()
        {
            Renderer(material);
            setting.excludedMaterials = new[] { material };
            var clone = Own(Object.Instantiate(root));
            using (new OverrideTemporaryDirectoryScope(null)) AvatarProcessor.ProcessAvatar(clone);
            Assert.That(clone.GetComponentInChildren<MeshRenderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(clone.GetComponent<DistanceFadeBulkSetter>(), Is.Null);
            Assert.That(root.GetComponent<DistanceFadeBulkSetter>(), Is.SameAs(setting));
        }

        [Test]
        public void SerializedExclusionSupportsUndoAndRedo()
        {
            Undo.IncrementCurrentGroup();
            var serialized = new SerializedObject(setting);
            var exclusions = serialized.FindProperty("excludedMaterials");
            exclusions.arraySize = 1;
            exclusions.GetArrayElementAtIndex(0).objectReferenceValue = material;
            serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            Assert.That(setting.excludedMaterials, Is.EqualTo(new[] { material }));
            Undo.PerformUndo();
            Assert.That(setting.excludedMaterials, Is.Empty);
            Undo.PerformRedo();
            Assert.That(setting.excludedMaterials, Is.EqualTo(new[] { material }));
            Undo.ClearUndo(setting);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PrefabExclusionsRoundTripAndLegacyDataLoads(bool legacy)
        {
            var prefix = "Assets/DistanceFadeExclusion_" + Guid.NewGuid().ToString("N");
            var materialPath = prefix + ".mat";
            var prefabPath = prefix + ".prefab";
            var persistent = new Material(material);
            GameObject instance = null;
            try
            {
                AssetDatabase.CreateAsset(persistent, materialPath);
                setting.excludedMaterials = new[] { persistent };
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (legacy)
                {
                    var yaml = System.IO.File.ReadAllText(prefabPath);
                    var oldYaml = System.Text.RegularExpressions.Regex.Replace(yaml,
                        @"(?m)^  excludedMaterials:.*\r?\n(?:  - .*\r?\n)*", "");
                    Assert.That(oldYaml, Is.Not.EqualTo(yaml));
                    System.IO.File.WriteAllText(prefabPath, oldYaml);
                    AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var loaded = instance.GetComponent<DistanceFadeBulkSetter>();
                var snapshot = SettingsValidator.Capture(loaded);
                Assert.That(snapshot.ExcludedMaterials.Contains(persistent), Is.EqualTo(!legacy));
                Assert.That(snapshot.ExcludedMaterials.Count, Is.EqualTo(legacy ? 0 : 1));
                var serialized = new SerializedObject(loaded);
                var exclusions = serialized.FindProperty("excludedMaterials");
                exclusions.arraySize = legacy ? 1 : 0;
                if (legacy) exclusions.GetArrayElementAtIndex(0).objectReferenceValue = persistent;
                serialized.ApplyModifiedProperties();
                Assert.That(PrefabUtility.GetPropertyModifications(instance).Any(p => p.propertyPath.StartsWith("excludedMaterials")), Is.True);
                Assert.That(SettingsValidator.Capture(prefab.GetComponent<DistanceFadeBulkSetter>()).ExcludedMaterials.Count,
                    Is.EqualTo(legacy ? 0 : 1), "Editing the instance must preserve the prefab.");
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.DeleteAsset(materialPath);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PersistentSourceAssetBytesRemainUnchanged(bool excluded)
        {
            var assetPath = "Assets/DistanceFadeTest_" + Guid.NewGuid().ToString("N") + ".mat";
            var persistent = new Material(material);
            try
            {
                AssetDatabase.CreateAsset(persistent, assetPath);
                var bytesBefore = System.IO.File.ReadAllBytes(assetPath);
                var dirtyBefore = EditorUtility.IsDirty(persistent);
                var r = Renderer(persistent);
                if (excluded) setting.excludedMaterials = new[] { persistent };
                Apply();
                // Persistent assets can have distinct managed wrappers for the same Unity object.
                if (excluded) Assert.That(r.sharedMaterial.GetInstanceID(), Is.EqualTo(persistent.GetInstanceID()));
                else Assert.That(r.sharedMaterial, Is.Not.SameAs(persistent));
                Assert.That(System.IO.File.ReadAllBytes(assetPath), Is.EqualTo(bytesBefore));
                Assert.That(EditorUtility.IsDirty(persistent), Is.EqualTo(dirtyBefore));
            }
            finally { AssetDatabase.DeleteAsset(assetPath); }
        }
    }
}
