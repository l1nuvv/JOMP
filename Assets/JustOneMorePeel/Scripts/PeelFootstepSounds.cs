using UnityEngine;

namespace JustOneMorePeel
{
    public sealed class PeelFootstepSounds : ScriptableObject
    {
        [SerializeField] AudioClip[] clips;
        [SerializeField] AudioClip landing;
        internal AudioClip[] Clips => clips;
        internal AudioClip Landing => landing;
    }
}
