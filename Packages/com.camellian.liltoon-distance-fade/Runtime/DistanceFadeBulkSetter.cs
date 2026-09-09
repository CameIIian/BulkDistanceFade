using UnityEngine;
using VRC.SDKBase;

namespace Camellian.DistanceFade
{
    [DisallowMultipleComponent]
    [AddComponentMenu("BulkDistanceFade/Distance Fade Bulk Setter")]
    public sealed class DistanceFadeBulkSetter : MonoBehaviour, IEditorOnly
    {
        // Color32 uses 0-255 for every channel, including alpha; Color uses 0-1.
        public static Color DefaultFadeColor => new Color32(10, 7, 7, 255);
        public static Color DefaultRimColor => new Color32(255, 188, 177, 0);

        public bool strictLilToonCheck = true;
        public Material[] excludedMaterials = new Material[0];

        public bool overrideFadeColor = true;
        [ColorUsage(true, true)] public Color fadeColor = DefaultFadeColor;
        public bool overrideStartDistance = true;
        public float startDistance = 0.18f;
        public bool overrideEndDistance = true;
        public float endDistance = 0.01f;
        public bool overrideStrength = true;
        public float strength = 0.95f;
        public bool overrideBackfaceShadow = false;
        public bool backfaceShadow = false;
        public bool overrideMode = false;
        public int mode;
        public bool overrideRimColor = true;
        [ColorUsage(true, true)] public Color rimColor = DefaultRimColor;
        public bool overrideRimFresnelPower = true;
        public float rimFresnelPower = 4.5f;
    }
}
