using System;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.UIElements;

[assembly: ExportsPlugin(typeof(Camellian.NonToonDistanceFade.Editor.NonToonPlugin))]

namespace Camellian.NonToonDistanceFade.Editor
{
    internal sealed class NonToonDiagnostic : IError
    {
        private readonly string message;
        public ErrorSeverity Severity { get; }
        internal NonToonDiagnostic(string message, ErrorSeverity severity) { this.message = message; Severity = severity; }
        public string ToMessage() => message;
        public void AddReference(ObjectReference reference) { }
        public VisualElement CreateVisualElement(ErrorReport report)
        {
            var label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }
    }
    public sealed class NonToonPlugin : Plugin<NonToonPlugin>
    {
        public override string QualifiedName => "com.camellian.lazyfade.nontoon";
        public override string DisplayName => "lazyFade — NonToon";
        private sealed class State { public State() { } public bool Invalid; }
        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).Run("Validate NonToon settings", context =>
            {
                try { NonToonPass.ValidatePlacement(context.AvatarRootObject); }
                catch (Exception ex)
                {
                    context.GetState<State>().Invalid = true;
                    ErrorReport.ReportError(new NonToonDiagnostic(ex.Message, ErrorSeverity.Error));
                }
            });
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("nadena.dev.modular-avatar.late-transform-stages")
                .AfterPlugin("net.rs64.tex-trans-tool")
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run("Apply NonToon distance fade", context =>
                {
                    if (context.GetState<State>().Invalid || !context.Successful ||
                        context.AvatarRootObject.GetComponentsInChildren<LazyFadeNonToon>(true).Length == 0) return;
                    try
                    {
                        var result = NonToonPass.Apply(context.AvatarRootObject, material => context.AssetSaver.SaveAsset(material));
                        foreach (var entry in result.Unsupported)
                            ErrorReport.ReportError(new NonToonDiagnostic("NT004: " + entry.material.name + ": " + entry.reason, ErrorSeverity.NonFatal));
                        Debug.Log(result.ToString(), context.AvatarRootObject);
                    }
                    catch (Exception ex)
                    {
                        ErrorReport.ReportError(new NonToonDiagnostic("NonToon距離フェードの適用に失敗しました: " + ex.Message, ErrorSeverity.Error));
                        ErrorReport.ReportException(ex);
                    }
                });
        }
    }
}
