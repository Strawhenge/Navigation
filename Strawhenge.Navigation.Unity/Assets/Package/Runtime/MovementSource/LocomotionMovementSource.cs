using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LocomotionMovementSource : IMovementSource
    {
        readonly LocomotionController _locomotion;

        public LocomotionMovementSource(LocomotionController locomotion)
        {
            _locomotion = locomotion;
        }
        
        public event Action JumpTriggerRequested
        {
            add => _locomotion.JumpTriggerRequested += value;
            remove => _locomotion.JumpTriggerRequested -= value;
        }

        public event Action JumpBegan
        {
            add => _locomotion.JumpBegan += value;
            remove => _locomotion.JumpBegan -= value;
        }

        public event Action JumpEnded
        {
            add => _locomotion.JumpEnded += value;
            remove => _locomotion.JumpEnded -= value;
        }

        public event Action<int> PivotRequested
        {
            add => _locomotion.PivotRequested += value;
            remove => _locomotion.PivotRequested -= value;
        }

        public event Action FallBegan
        {
            add => _locomotion.FallBegan += value;
            remove => _locomotion.FallBegan -= value;
        }

        public event Action FallEnded
        {
            add => _locomotion.FallEnded += value;
            remove => _locomotion.FallEnded -= value;
        }

        public event Action<int> LandingRequested
        {
            add => _locomotion.LandingRequested += value;
            remove => _locomotion.LandingRequested -= value;
        }

        // Never raised: LocomotionController clears its own waits when it's deactivated, which also changes the
        // active movement source.
        public event Action PivotCancelled
        {
            add { }
            remove { }
        }

        public event Action LandingCancelled
        {
            add { }
            remove { }
        }

        public event Action JumpTriggerCancelled
        {
            add { }
            remove { }
        }

        public event Action<bool> IsActiveChanged
        {
            add => _locomotion.IsActiveChanged += value;
            remove => _locomotion.IsActiveChanged -= value;
        }

        public Vector3 CurrentVelocity => _locomotion.CurrentVelocity;
       
        public bool IsActive => _locomotion.IsActive;
      
        public bool IsStrafing => _locomotion.Strafe;
        
        public void TriggerJump()
        {
            _locomotion.TriggerJump();
        }

        public void CompletePivot()
        {
            _locomotion.CompletePivot();
        }

        public void CompleteLanding()
        {
            _locomotion.CompleteLanding();
        }
    }
}