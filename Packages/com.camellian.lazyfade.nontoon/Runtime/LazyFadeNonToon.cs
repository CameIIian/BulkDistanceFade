using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using VRC.SDKBase;

namespace Camellian.NonToonDistanceFade
{
    [DisallowMultipleComponent]
    [MovedFrom(true, "Camellian.NonToonDistanceFade", "Camellian.NonToonDistanceFade.Runtime", "NonToonDistanceFadeBulkSetter")]
    [AddComponentMenu("lazyFade/lazyFade NonToon")]
    public sealed class LazyFadeNonToon : MonoBehaviour, IEditorOnly
    {
        public bool overrideNearDistance;
        public float nearDistance = 0.01f;
        public bool overrideFarDistance;
        public float farDistance = 0.1f;
        public bool overrideStrength;
        public float strength;
        public Material[] excludedMaterials = new Material[0];
        public bool failOnUnsupported;
    }
}
