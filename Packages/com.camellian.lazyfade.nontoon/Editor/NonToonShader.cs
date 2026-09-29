using System;
using System.IO;
using System.Linq;
using jp.lilxyzw.shadercore;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using UnityEngine.Rendering;

namespace Camellian.NonToonDistanceFade.Editor
{
    internal sealed class NonToonProperties
    {
        internal string Distance, Strength;
    }

    internal static class NonToonShader
    {
        internal const string ModuleId = "jp.lilxyzw.nontoon.distancefade";
        internal const string ModuleGuid = "015199d0d3b79304ea86865ccef59f62";
        internal const string ShaderGuid = "78361b0b760724141a2d2c09100cf00f";
        internal const string FurGuid = "ecae2da2bee224d439be859858777d00";

        // Null without a reason means an unrelated shader. Never query ShaderCore's
        // ProjectSettings.GetShaderModules: it can save project settings on a read.
        internal static NonToonProperties Resolve(Shader shader, out string reason)
        {
            reason = null;
            if (shader == null) return null;
            var path = AssetDatabase.GetAssetPath(shader);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (guid != ShaderGuid && guid != FurGuid)
            {
                if (shader.name == "NonToon" || shader.name == "NonToonFur")
                    reason = "生成元を確認できないNonToonです。公式.scshaderから生成したShaderを使用してください。";
                return null;
            }
            var package = PackageInfo.FindForAssetPath(path);
            var core = PackageInfo.FindForAssembly(typeof(SCModule).Assembly);
            if (!path.EndsWith(".scshader", StringComparison.OrdinalIgnoreCase) ||
                package == null || package.name != "jp.lilxyzw.nontoon" || package.version != "0.1.3" ||
                core == null || core.name != "jp.lilxyzw.shadercore" || core.version != "0.1.11")
            {
                reason = "対応版はPackages導入のNonToon 0.1.3 / ShaderCore 0.1.11です。";
                return null;
            }
            var modulePath = AssetDatabase.GUIDToAssetPath(ModuleGuid);
            if (!modulePath.StartsWith(package.assetPath + "/", StringComparison.Ordinal))
            {
                reason = "公式Distance Fadeモジュールを確認できません。";
                return null;
            }
            var module = SCModule.FromFile(Path.Combine(package.resolvedPath, modulePath.Substring(package.assetPath.Length + 1)));
            if (module.uniqueID != ModuleId) { reason = "Distance FadeモジュールIDが一致しません。"; return null; }
            return ResolveProperties(shader, module, out reason);
        }

        internal static NonToonProperties ResolveProperties(Shader shader, SCModule module, out string reason)
        {
            reason = "Distance Fadeモジュール未組み込み、Property不一致、または未対応の定数化です。";
            var distance = module.properties?.SingleOrDefault(p => p.originalName == "_DistanceFade");
            var strength = module.properties?.SingleOrDefault(p => p.originalName == "_DistanceFadeStrength");
            if (distance == null || strength == null || distance.type != "float4" || strength.type != "float") return null;
            if (!Check(shader, distance, ShaderPropertyType.Vector) || !Check(shader, strength, ShaderPropertyType.Float)) return null;
            // ShaderCore places the module marker on the first generated property.
            if (!shader.GetPropertyAttributes(shader.FindPropertyIndex(distance.name)).Contains("SCModule(" + ModuleId + ")")) return null;
            reason = null;
            return new NonToonProperties { Distance = distance.name, Strength = strength.name };
        }

        private static bool Check(Shader shader, SCProperty property, ShaderPropertyType type)
        {
            var index = shader.FindPropertyIndex(property.name);
            return index >= 0 && shader.GetPropertyType(index) == type &&
                !(property.attributes?.Any(a => a.Contains("SCConstValue")) ?? false) &&
                !shader.GetPropertyAttributes(index).Any(a => a.StartsWith("SCConstValue", StringComparison.Ordinal));
        }
    }
}
