using UnityEngine;
using VRC.SDKBase;

namespace Numeira
{
    [RequireComponent(typeof(Renderer))]
    [AddComponentMenu("NDMF/SFO AlphaMask Baker")]
    public sealed class AlphaMaskBaker : MonoBehaviour, IEditorOnly
    {
        public string AlphaMaskName = "_AlphaMask";
        public string[] TargetTextureNames = { "_MainTex" };

        public bool ReplaceSameReference = true;

        public MaterialListMode MaterialListMode = MaterialListMode.Blacklist;
        public Material[] Materials;
    }
}