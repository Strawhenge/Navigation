using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Takes the Animator's root motion from a RootMotionRelayScript (e.g. a motor that moves the character by it).
    public interface IRootMotionHandler
    {
        // A frame's root motion. False when not taking it right now (e.g. a motor that's off): the Animator then
        // applies it itself.
        bool TryApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation);
    }
}
