using Strawhenge.Navigation.Unity.Destination;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class MovementSourceManagerScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] DestinationScript _destination;

        readonly List<IMovementSource> _movementSources = new();

        IMovementSource _activeSource = NullMovementSource.Instance;
        DestinationMovementSource _destinationSource;

        public event Action JumpTriggerRequested;
        public event Action JumpBegan;
        public event Action JumpEnded;
        public event Action<int> PivotRequested;
        public event Action FallBegan;
        public event Action FallEnded;
        public event Action<int> LandingRequested;

        public Vector3 CurrentVelocity => _activeSource.CurrentVelocity;

        public bool IsActive => _activeSource.IsActive;

        public bool Strafe => _activeSource.IsStrafing;

        void Awake()
        {
            InitializeMovementSources();
            RefreshActiveSource();
        }

        void OnDestroy()
        {
            UnsubscribeFromSource(_activeSource);

            foreach (var source in _movementSources)
                UnsubscribeFromSourceActivity(source);
        }

        public void TriggerJump() => _activeSource.TriggerJump();

        public void CompletePivot(Quaternion rotationDelta) => _activeSource.CompletePivot(rotationDelta);

        public void CompleteLanding() => _activeSource.CompleteLanding();

        public void SetMovementSource(IMovementSource source)
        {
            if (source == null)
                return;

            AddMovementSource(source);

            _movementSources.Remove(source);
            _movementSources.Insert(0, source);
            RefreshActiveSource();
        }

        public void AddMovementSource(IMovementSource source)
        {
            if (source == null || ReferenceEquals(source, NullMovementSource.Instance) ||
                _movementSources.Contains(source))
                return;

            _movementSources.Add(source);
            SubscribeToSourceActivity(source);
            RefreshActiveSource();
        }

        public void RemoveMovementSource(IMovementSource source)
        {
            if (source == null)
                return;

            if (!_movementSources.Remove(source))
                return;

            UnsubscribeFromSourceActivity(source);
            if (ReferenceEquals(_activeSource, source))
                RefreshActiveSource();
        }

        void InitializeMovementSources()
        {
            _movementSources.Clear();

            if (_destination != null)
            {
                _destinationSource ??= new DestinationMovementSource(_destination.DestinationController);
                AddMovementSource(_destinationSource);
            }

            if (_locomotion != null)
                AddMovementSource(new LocomotionMovementSource(_locomotion));
        }

        void RefreshActiveSource()
        {
            var nextSource = GetFirstActiveSource();
            if (ReferenceEquals(_activeSource, nextSource))
                return;

            UnsubscribeFromSource(_activeSource);
            _activeSource = nextSource;
            SubscribeToSource(_activeSource);
        }

        IMovementSource GetFirstActiveSource()
        {
            foreach (var source in _movementSources)
            {
                if (source.IsActive)
                    return source;
            }

            return NullMovementSource.Instance;
        }

        void SubscribeToSourceActivity(IMovementSource source)
        {
            if (source == null)
                return;

            source.IsActiveChanged += OnSourceIsActiveChanged;
        }

        void UnsubscribeFromSourceActivity(IMovementSource source)
        {
            if (source == null)
                return;

            source.IsActiveChanged -= OnSourceIsActiveChanged;
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

        void OnSourceIsActiveChanged(bool isActive) => RefreshActiveSource();
    }
}