using Strawhenge.Navigation.Unity.Destination;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class DestinationMovementSource : IMovementSource
    {
        readonly DestinationController _destinationController;
        readonly List<(Action<bool> Original, Action Activated, Action Deactivated)> _isActiveChangedHandlers = new();

        public DestinationMovementSource(DestinationController destinationController)
        {
            _destinationController = destinationController;
        }

        public event Action JumpTriggerRequested;
        public event Action JumpBegan
        {
            add => _destinationController.JumpBegan += value;
            remove => _destinationController.JumpBegan -= value;
        }

        public event Action JumpEnded
        {
            add => _destinationController.JumpEnded += value;
            remove => _destinationController.JumpEnded -= value;
        }
        public event Action<int> PivotRequested;
        public event Action FallBegan
        {
            add => _destinationController.FallBegan += value;
            remove => _destinationController.FallBegan -= value;
        }

        public event Action FallEnded
        {
            add => _destinationController.FallEnded += value;
            remove => _destinationController.FallEnded -= value;
        }
        public event Action<int> LandingRequested;

        public event Action<bool> IsActiveChanged
        {
            add
            {
                if (value == null)
                    return;

                Action onActivated = () => value(true);
                Action onDeactivated = () => value(false);

                _isActiveChangedHandlers.Add((value, onActivated, onDeactivated));
                _destinationController.Activated += onActivated;
                _destinationController.Deactivated += onDeactivated;
            }
            remove
            {
                if (value == null)
                    return;

                var index = _isActiveChangedHandlers.FindLastIndex(h => h.Original == value);
                if (index < 0)
                    return;

                var handlers = _isActiveChangedHandlers[index];
                _destinationController.Activated -= handlers.Activated;
                _destinationController.Deactivated -= handlers.Deactivated;
                _isActiveChangedHandlers.RemoveAt(index);
            }
        }

        public Vector3 CurrentVelocity => _destinationController.CurrentVelocity;

        public bool IsActive => _destinationController.IsActive;

        public bool Strafe { get; }

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