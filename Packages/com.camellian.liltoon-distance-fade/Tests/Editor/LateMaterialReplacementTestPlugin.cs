using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(Camellian.DistanceFade.Tests.LateMaterialReplacementTestPlugin))]

namespace Camellian.DistanceFade.Tests
{
    // Exercises a material replacement in Optimizing without impersonating an optional plugin.
    public sealed class LateMaterialReplacementTestPlugin : Plugin<LateMaterialReplacementTestPlugin>
    {
        internal static GameObject Target;
        internal static Material Replacement;
        public override string QualifiedName => "com.camellian.distance-fade.tests.late-replacement";
        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing).BeforePlugin("com.camellian.liltoon-distance-fade")
                .Run("Test late material replacement", ctx =>
                {
                    if (Target == null || ctx.AvatarRootObject != Target) return;
                    foreach (var r in Target.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = Replacement;
                });
        }
    }
}
