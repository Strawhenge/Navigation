using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    static class AnimatorParameters
    {
        internal static readonly int MoveSpeed = Animator.StringToHash("Move Speed");
        internal static readonly int Strafing = Animator.StringToHash("Strafing");
        internal static readonly int StrafeX = Animator.StringToHash("Strafe X");
        internal static readonly int StrafeY = Animator.StringToHash("Strafe Y");
        internal static readonly int Falling = Animator.StringToHash("Falling");
        internal static readonly int Jump = Animator.StringToHash("Jump");
        internal static readonly int Jumping = Animator.StringToHash("Jumping");
        internal static readonly int Pivot = Animator.StringToHash("Pivot");
        internal static readonly int PivotId = Animator.StringToHash("Pivot ID");
        internal static readonly int PivotRightFoot = Animator.StringToHash("Pivot Right Foot");
        internal static readonly int LandingId = Animator.StringToHash("Landing ID");
        internal static readonly int Land = Animator.StringToHash("Land");
    }
}