using Strawhenge.Common.Unity;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    static class AnimatorParameters
    {
        internal static readonly AnimatorParameter MoveSpeed = new("Move Speed");
        internal static readonly AnimatorParameter MoveAnimationSpeed = new("Move Animation Speed");
        internal static readonly AnimatorParameter LocomotionCycleOffset = new("Locomotion Cycle Offset");
        internal static readonly AnimatorParameter Strafing = new("Strafing");
        internal static readonly AnimatorParameter StrafeX = new("Strafe X");
        internal static readonly AnimatorParameter StrafeY = new("Strafe Y");
        internal static readonly AnimatorParameter Falling = new("Falling");
        internal static readonly AnimatorParameter Jump = new("Jump");
        internal static readonly AnimatorParameter JumpId = new("Jump ID");
        internal static readonly AnimatorParameter Jumping = new("Jumping");
        internal static readonly AnimatorParameter PivotId = new("Pivot ID");
        internal static readonly AnimatorParameter PivotRightFootPlanted = new("Pivot Right Foot Planted");
        internal static readonly AnimatorParameter Pivoting = new("Pivoting");
        internal static readonly AnimatorParameter LandingId = new("Landing ID");
        internal static readonly AnimatorParameter Land = new("Land");
    }
}