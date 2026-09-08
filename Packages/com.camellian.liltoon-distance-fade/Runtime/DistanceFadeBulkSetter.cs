using UnityEngine;
using VRC.SDKBase;

namespace Camellian.DistanceFade
{
    [DisallowMultipleComponent]
    [AddComponentMenu("lilToon/Distance Fade Bulk Setter")]
    public sealed class DistanceFadeBulkSetter : MonoBehaviour, IEditorOnly
    {
        public bool strictLilToonCheck = true;

        public bool overrideFadeColor = true;
        [ColorUsage(true, true)] public Color fadeColor = new Color(0, 0, 0, 1);
        public bool overrideStartDistance = true;
        public float startDistance = 0.1f;
        public bool overrideEndDistance = true;
        public float endDistance = 0.01f;
        public bool overrideStrength = true;
        public float strength;
        public bool overrideBackfaceShadow = true;
        public bool backfaceShadow;
        public bool overrideMode = true;
        public int mode;
        public bool overrideRimColor = true;
        [ColorUsage(true, true)] public Color rimColor = new Color(0, 0, 0, 0);
        public bool overrideRimFresnelPower = true;
        public float rimFresnelPower = 5;
    }
}
