using System;
using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(Camellian.DistanceFade.Editor.LazyFadeLilToonPlugin))]

namespace Camellian.DistanceFade.Editor
{
    public sealed class LazyFadeLilToonPlugin : Plugin<LazyFadeLilToonPlugin>
    {
        public override string QualifiedName => "com.camellian.lazyfade.liltoon";
        public override string DisplayName => "lazyFade";
        private sealed class State { public State() { } public bool Invalid; }

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
                .BeforePlugin("nadena.dev.modular-avatar")
                .BeforePlugin("net.rs64.tex-trans-tool")
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run("Validate avatar-root placement", Validate);

            // TTT 1.0 also modifies materials in Optimizing; wait for its final output.
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("net.rs64.tex-trans-tool")
                .AfterPlugin("nadena.dev.modular-avatar.late-transform-stages")
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run("Apply distance fade", Apply);
        }

        private static void Validate(BuildContext context)
        {
            try { SettingsValidator.ValidatePlacement(context.AvatarRootObject); }
            catch (SettingsException ex)
            {
                context.GetState<State>().Invalid = true;
                BuildDiagnostic.Report(ex.Message, ErrorSeverity.Error, ex.Context);
            }
        }

        private static void Apply(BuildContext context)
        {
            if (context.GetState<State>().Invalid || !context.Successful) return;
            if (context.AvatarRootObject.GetComponentsInChildren<LazyFadeLilToon>(true).Length == 0) return;
            try
            {
                var summary = ApplyDistanceFadePass.Apply(context.AvatarRootObject,
                    material => context.AssetSaver.SaveAsset(material),
                    result =>
                    {
                        foreach (var missing in result.Missing)
                            BuildDiagnostic.Report("W001: " + missing.material.name + " の非対応項目をスキップ: " + missing.properties,
                                ErrorSeverity.NonFatal, missing.material);
                    },
                    setting => BuildDiagnostic.Report("W002: フレネル指数を0.01～50の範囲に補正しました。",
                        ErrorSeverity.NonFatal, setting));
                Debug.Log(summary.ToString(), context.AvatarRootObject);
                foreach (var skip in summary.SkippedSlots) Debug.Log($"Distance Fade: Skip ({skip.Key}) {skip.Value} Slot");
            }
            catch (SettingsException ex) { BuildDiagnostic.Report(ex.Message, ErrorSeverity.Error, ex.Context); }
            catch (Exception ex)
            {
                BuildDiagnostic.Report("E004: 距離フェードの適用に失敗しました: " + ex.Message,
                    ErrorSeverity.Error, context.AvatarRootObject);
                ErrorReport.ReportException(ex);
            }
        }
    }
}
