using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class MovementSourceManagerScript : MonoBehaviour
    {
        [SerializeField] MovementSourceScript[] _movementSourceScripts;

        readonly List<IMovementSource> _movementSources = new();

        IMovementSource _activeSource = NullMovementSource.Instance;

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

        void InitializeMovementSources()
        {
            _movementSources.Clear();

            if (_movementSourceScripts == null)
                return;

            foreach (var movementSourceScript in _movementSourceScripts)
            {
                if (movementSourceScript == null)
                    continue;

                var movementSource = movementSourceScript.MovementSource;
                if (movementSource == null ||
                    ReferenceEquals(movementSource, NullMovementSource.Instance) ||
                    _movementSources.Contains(movementSource))
                    continue;

                _movementSources.Add(movementSource);
                SubscribeToSourceActivity(movementSource);
            }
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