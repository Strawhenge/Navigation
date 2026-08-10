using Strawhenge.Navigation.Unity.Destination;
using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class MovementSourceManagerScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] DestinationScript _destination;

        IMovementSource _activeSource;
        DestinationMovementSource _destinationSource;

        public event Action JumpTriggerRequested;
        public event Action JumpBegan;
        public event Action JumpEnded;
        public event Action<int> PivotRequested;
        public event Action FallBegan;
        public event Action FallEnded;
        public event Action<int> LandingRequested;

        public Vector3 CurrentVelocity => _activeSource?.CurrentVelocity ?? Vector3.zero;

        public bool Strafe => _activeSource?.Strafe ?? false;

        void Awake()
        {
            SetLocomotionSource();
        }

        void OnDestroy()
        {
            UnsubscribeFromSource(_activeSource);
        }

        public void TriggerJump() => _activeSource?.TriggerJump();

        public void CompletePivot(Quaternion rotationDelta) => _activeSource?.CompletePivot(rotationDelta);

        public void CompleteLanding() => _activeSource?.CompleteLanding();

        [ContextMenu(nameof(SetLocomotionSource))]
        public void SetLocomotionSource()
        {
            SetMovementSource(_locomotion);
        }

        [ContextMenu(nameof(SetDestinationSource))]
        public void SetDestinationSource()
        {
            if (_destination == null)
                return;

            _destinationSource ??= new DestinationMovementSource(_destination.DestinationController);
            SetMovementSource(_destinationSource);
        }

        public void SetMovementSource(IMovementSource source)
        {
            var nextSource = source ?? _locomotion;
            if (ReferenceEquals(_activeSource, nextSource))
                return;

            UnsubscribeFromSource(_activeSource);
            _activeSource = nextSource;
            SubscribeToSource(_activeSource);
        }

        void SubscribeToSource(IMovementSource source)
        {
            if (source == null)
                return;

            source.JumpTriggerRequested += OnJumpTriggerRequested;
            source.JumpBegan += OnJumpBegan;
            source.JumpEnded += OnJumpEnded;
            source.PivotRequested += OnPivotRequested;
            source.FallBegan += OnFallBegan;
            source.FallEnded += OnFallEnded;
            source.LandingRequested += OnLandingRequested;
        }

        void UnsubscribeFromSource(IMovementSource source)
        {
            if (source == null)
                return;

            source.JumpTriggerRequested -= OnJumpTriggerRequested;
            source.JumpBegan -= OnJumpBegan;
            source.JumpEnded -= OnJumpEnded;
            source.PivotRequested -= OnPivotRequested;
            source.FallBegan -= OnFallBegan;
            source.FallEnded -= OnFallEnded;
            source.LandingRequested -= OnLandingRequested;
        }

        void OnJumpTriggerRequested() => JumpTriggerRequested?.Invoke();

        void OnJumpBegan() => JumpBegan?.Invoke();

        void OnJumpEnded() => JumpEnded?.Invoke();

        void OnPivotRequested(int pivotId) => PivotRequested?.Invoke(pivotId);

        void OnFallBegan() => FallBegan?.Invoke();

        void OnFallEnded() => FallEnded?.Invoke();

        void OnLandingRequested(int landingId) => LandingRequested?.Invoke(landingId);
    }
}



