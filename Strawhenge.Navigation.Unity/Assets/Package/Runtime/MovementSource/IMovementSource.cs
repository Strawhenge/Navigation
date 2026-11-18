using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public interface IMovementSource
    {
        event Action JumpTriggerRequested;
        event Action JumpBegan;
        event Action JumpEnded;
        event Action<int> PivotRequested;
        event Action FallBegan;
        event Action FallEnded;
        event Action<int> LandingRequested;

        // A requested pivot, landing or deferred jump trigger is no longer wanted, and won't be completed.
        event Action PivotCancelled;
        event Action LandingCancelled;
        event Action JumpTriggerCancelled;
        event Action<bool> IsActiveChanged;

        Vector3 CurrentVelocity { get; }

        bool IsActive { get; }
      
        bool IsStrafing { get; }

        void TriggerJump();
     
        void CompletePivot(Quaternion rotationDelta);
     
        void CompleteLanding();
    }
}

