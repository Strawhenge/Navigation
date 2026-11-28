using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A movement source the test drives by hand: it makes requests, cancels them, begins jumps and falls, and
    // records the completions and jump triggers it gets back.
    class FakeMovementSource : IMovementSource
    {
        bool _isActive;

        public event Action<int> PivotRequested;
        public event Action<int> LandingRequested;
        public event Action<int> JumpTriggerRequested;
        public event Action JumpBegan;
        public event Action FallBegan;
        public event Action PivotCancelled;
        public event Action LandingCancelled;
        public event Action JumpTriggerCancelled;
        public event Action<bool> IsActiveChanged;

        public event Action JumpEnded
        {
            add { }
            remove { }
        }

        public event Action FallEnded
        {
            add { }
            remove { }
        }

        public int CompletedPivots { get; private set; }

        public int CompletedLandings { get; private set; }

        public int TriggeredJumps { get; private set; }

        public Vector3 CurrentVelocity { get; set; }

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

        public void RequestJumpTrigger(int jumpId) => JumpTriggerRequested?.Invoke(jumpId);

        public void BeginJump() => JumpBegan?.Invoke();

        public void BeginFall() => FallBegan?.Invoke();

        public void CancelPivot() => PivotCancelled?.Invoke();

        public void CancelLanding() => LandingCancelled?.Invoke();

        public void CancelJumpTrigger() => JumpTriggerCancelled?.Invoke();

        public void TriggerJump() => TriggeredJumps++;

        public void CompletePivot() => CompletedPivots++;

        public void CompleteLanding() => CompletedLandings++;
    }
}
