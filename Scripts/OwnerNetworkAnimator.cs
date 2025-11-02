using Unity.Netcode.Components;
using UnityEngine;

namespace LethalMin
{
    public class OwnerNetworkAnimator : NetworkAnimator
    {
        public override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}