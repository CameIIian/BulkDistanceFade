using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Camellian.DistanceFade.Editor
{
    internal sealed class BuildDiagnostic : IError
    {
        private readonly string message;
        private readonly List<ObjectReference> references = new List<ObjectReference>();
        public ErrorSeverity Severity { get; }
        internal BuildDiagnostic(string message, ErrorSeverity severity) { this.message = message; Severity = severity; }
        public string ToMessage() => message;
        public void AddReference(ObjectReference reference) { if (!references.Contains(reference)) references.Add(reference); }
        public VisualElement CreateVisualElement(ErrorReport report)
        {
            var container = new VisualElement();
            var label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            container.Add(label);
            foreach (var reference in references)
            {
                var captured = reference;
                container.Add(new Button(() =>
                {
                    if (captured.TryResolve(report, out var obj)) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
                }) { text = captured.ToString() });
            }
            return container;
        }
        internal static void Report(string message, ErrorSeverity severity, Object context)
        {
            using (ErrorReport.WithContextObject(context)) ErrorReport.ReportError(new BuildDiagnostic(message, severity));
        }
    }
}
