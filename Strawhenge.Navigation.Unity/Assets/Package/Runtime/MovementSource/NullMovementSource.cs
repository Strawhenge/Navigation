using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    sealed class NullMovementSource : IMovementSource
    {
        internal static NullMovementSource Instance { get; } = new();

        NullMovementSource()
        {
        }

        public event Action JumpTriggerRequested
        {
            add { }
            remove { }
        }

        public event Action JumpBegan
        {
            add { }
            remove { }
        }

        public event Action JumpEnded
        {
            add { }
            remove { }
        }

        public event Action<int> PivotRequested
        {
            add { }
            remove { }
        }

        public event Action FallBegan
        {
            add { }
            remove { }
        }

        public event Action FallEnded
        {
            add { }
            remove { }
        }

        public event Action<int> LandingRequested
        {
            add { }
            remove { }
        }

        public event Action<bool> IsActiveChanged
        {
            add { }
            remove { }
        }

        public Vector3 CurrentVelocity => Vector3.zero;

        public bool IsActive => false;

        public bool IsStrafing => false;

        public void TriggerJump()
        {
        }

        public void CompletePivot(Quaternion rotationDelta)
        {
        }

        public void CompleteLanding()
        {
        }
    }
}

