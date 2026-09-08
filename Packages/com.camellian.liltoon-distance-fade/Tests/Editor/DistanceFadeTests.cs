using System;
using System.Collections.Generic;
using System.Linq;
using Camellian.DistanceFade.Editor;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Camellian.DistanceFade.Tests
{
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
            setting.backfaceShadow = true;
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
        public void PersistentSourceAssetBytesRemainUnchanged()
        {
            var assetPath = "Assets/DistanceFadeTest_" + Guid.NewGuid().ToString("N") + ".mat";
            var persistent = new Material(material);
            try
            {
                AssetDatabase.CreateAsset(persistent, assetPath);
                var bytesBefore = System.IO.File.ReadAllBytes(assetPath);
                var dirtyBefore = EditorUtility.IsDirty(persistent);
                var r = Renderer(persistent);
                Apply();
                Assert.That(r.sharedMaterial, Is.Not.SameAs(persistent));
                Assert.That(System.IO.File.ReadAllBytes(assetPath), Is.EqualTo(bytesBefore));
                Assert.That(EditorUtility.IsDirty(persistent), Is.EqualTo(dirtyBefore));
            }
            finally { AssetDatabase.DeleteAsset(assetPath); }
        }
    }
}
