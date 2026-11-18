using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A movement source the test drives by hand: it makes requests, and records the completions it gets back.
    class FakeMovementSource : IMovementSource
    {
        readonly List<Quaternion> _completedPivots = new();
        bool _isActive;

        public event Action<int> PivotRequested;
        public event Action<int> LandingRequested;
        public event Action PivotCancelled;
        public event Action<bool> IsActiveChanged;

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

        public IReadOnlyList<Quaternion> CompletedPivots => _completedPivots;

        public int CompletedLandings { get; private set; }

        public Vector3 CurrentVelocity => Vector3.zero;

        public bool IsStrafing => false;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;

                _isActive = value;
                IsActiveChanged?.Invoke(value);
            }
        }

        public void RequestPivot(int pivotId) => PivotRequested?.Invoke(pivotId);

        public void RequestLanding(int landingId) => LandingRequested?.Invoke(landingId);

        public void CancelPivot() => PivotCancelled?.Invoke();

        public void TriggerJump()
        {
        }

        public void CompletePivot(Quaternion rotationDelta) => _completedPivots.Add(rotationDelta);

        public void CompleteLanding() => CompletedLandings++;
    }
}
