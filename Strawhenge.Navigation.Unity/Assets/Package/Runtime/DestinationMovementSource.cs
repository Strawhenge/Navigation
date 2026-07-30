using Strawhenge.Navigation.Unity.Destination;
using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class DestinationMovementSource : IMovementSource
    {
        readonly DestinationController _destinationController;

        public DestinationMovementSource(DestinationController destinationController)
        {
            _destinationController = destinationController;
        }

        public event Action JumpTriggerRequested;
        public event Action JumpBegan;
        public event Action JumpEnded;
        public event Action<int> PivotRequested;
        public event Action FallBegan;
        public event Action FallEnded;
        public event Action<int> LandingRequested;

        public Vector3 CurrentVelocity => _destinationController.CurrentVelocity;

        public bool Strafe { get; set; }

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