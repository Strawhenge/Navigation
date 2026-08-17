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
        event Action<bool> IsActiveChanged;

        Vector3 CurrentVelocity { get; }

        bool IsActive { get; }
      
        bool Strafe { get; }

        void TriggerJump();
     
        void CompletePivot(Quaternion rotationDelta);
     
        void CompleteLanding();
    }
}

