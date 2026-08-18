using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Falling : State
    {
        const float HorizontalSpeed = 5f; // TODO: Make this configurable
        const float FallAcceleration = 9.81f; // TODO: Make this configurable
        const float GroundProbeDistance = 0.2f;
        const float EndAlignmentDistance = 0.05f;
        const float DownwardMovementEpsilon = 0.0001f;

        readonly IDestinationContext _context;
        readonly Agent _agent;

        internal event Action FallBegan;
        internal event Action FallEnded;

        bool _isFallInProgress;
        bool _cancelRequested;
        bool _agentUnavailable;
        DestinationArgs _pendingArgs;
        Vector3 _startPosition;
        Vector3 _endPosition;
        float _horizontalT;
        float _horizontalDuration;
        float _verticalSpeed;
        bool _isDescending;
        Vector3 _currentVelocity;
        bool _fallBeganRaised;

        public Falling(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        public DestinationArgs CurrentArgs { private get; set; }

        protected internal override Vector3 Velocity => _currentVelocity;

        protected internal override void Cancel()
        {
            _pendingArgs?.Callback(DestinationResult.Cancelled);
            _pendingArgs = null;
            _cancelRequested = true;
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            _pendingArgs?.Callback(DestinationResult.CancelledByNewDestination);
            _pendingArgs = args;
            _cancelRequested = false;
        }

        protected internal override void Update(float deltaTime)
        {
            if (!_context.CanNavigate)
            {
                InterruptForCannotNavigate();
                return;
            }

            _agentUnavailable |=
                !_agent.NavMeshAgent.isActiveAndEnabled ||
                !_agent.NavMeshAgent.isOnNavMesh;

            if (!_isFallInProgress && !_agent.NavMeshAgent.isOnOffMeshLink)
            {
                ResolveNextStateAfterFall();
                return;
            }

            if (!_isFallInProgress)
                BeginFall();

            TraverseFall(deltaTime);
        }

        void BeginFall()
        {
            var linkData = _agent.NavMeshAgent.currentOffMeshLinkData;
            _startPosition = _agent.GetCurrentPosition();
            _endPosition = linkData.endPos + (Vector3.up * _agent.NavMeshAgent.baseOffset);

            var horizontalDistance = Vector2.Distance(
                new Vector2(_startPosition.x, _startPosition.z),
                new Vector2(_endPosition.x, _endPosition.z));
            _horizontalDuration = Mathf.Max(0.01f, horizontalDistance / HorizontalSpeed);
            _horizontalT = 0f;
            _verticalSpeed = 0f;
            _currentVelocity = Vector3.zero;
            _isFallInProgress = true;
            _isDescending = false;
            _fallBeganRaised = false;

            _agent.NavMeshAgent.isStopped = true;
            _agent.NavMeshAgent.updatePosition = false;
            _agent.NavMeshAgent.updateRotation = false;
        }

        void TraverseFall(float deltaTime)
        {
            var previousPosition = _agent.GetCurrentPosition();

            _horizontalT = Mathf.Min(1f, _horizontalT + (deltaTime / _horizontalDuration));

            var x = Mathf.Lerp(_startPosition.x, _endPosition.x, _horizontalT);
            var z = Mathf.Lerp(_startPosition.z, _endPosition.z, _horizontalT);
            var y = _agent.GetCurrentPosition().y;

            var position = new Vector3(x, y, z);

            if (!_isDescending && _endPosition.y < _startPosition.y)
            {
                var flatPosition = new Vector3(x, _startPosition.y, z);
                _isDescending = CanBeginDescent(flatPosition);
            }

            if (!_isDescending && _endPosition.y < _startPosition.y)
                position.y = _startPosition.y;
            else if (_endPosition.y < _startPosition.y)
            {
                _verticalSpeed += FallAcceleration * deltaTime;
                position.y = Mathf.Max(_endPosition.y, y - (_verticalSpeed * deltaTime));
            }
            else
            {
                position.y = Mathf.Lerp(_startPosition.y, _endPosition.y, _horizontalT);
            }

            if (!_fallBeganRaised && position.y < y - DownwardMovementEpsilon)
            {
                _fallBeganRaised = true;
                FallBegan?.Invoke();
            }

            _agent.SetCurrentPosition(position);
            _agent.NavMeshAgent.nextPosition = position;

            _currentVelocity = deltaTime > Mathf.Epsilon
                ? (position - previousPosition) / deltaTime
                : Vector3.zero;

            var reachedEndHorizontally = _horizontalT >= 1f;
            var reachedEndVertically = _endPosition.y >= _startPosition.y || position.y <= _endPosition.y + 0.001f;
            if (reachedEndHorizontally && reachedEndVertically)
            {
                if (_agent.NavMeshAgent.isOnOffMeshLink)
                    _agent.NavMeshAgent.CompleteOffMeshLink();

                ResolveNextStateAfterFall();
            }
        }

        void ResolveNextStateAfterFall()
        {
            var agentUnavailable = _agentUnavailable;
            EndManualTraversal();

            if (_cancelRequested)
            {
                _cancelRequested = false;
                CurrentArgs.Callback(DestinationResult.Cancelled);
                ChangeState(States.Idle);
                return;
            }

            if (_pendingArgs != null)
            {
                var pendingArgs = _pendingArgs;
                _pendingArgs = null;

                CurrentArgs.Callback(DestinationResult.CancelledByNewDestination);
                States.PrepareGoing.CurrentArgs = pendingArgs;
                ChangeState(States.PrepareGoing);
                return;
            }

            if (agentUnavailable)
            {
                States.PrepareGoing.CurrentArgs = CurrentArgs;
                ChangeState(States.PrepareGoing);
                return;
            }

            States.Going.CurrentArgs = CurrentArgs;
            ChangeState(States.Going);
        }

        void EndManualTraversal()
        {
            _isFallInProgress = false;
            _horizontalT = 0f;
            _agentUnavailable = false;
            _verticalSpeed = 0f;
            _isDescending = false;
            _currentVelocity = Vector3.zero;
            _fallBeganRaised = false;

            FallEnded?.Invoke();

            if (_agent.NavMeshAgent.isActiveAndEnabled)
            {
                _agent.NavMeshAgent.updatePosition = true;
                _agent.NavMeshAgent.updateRotation = !CurrentArgs.Strafe;
                _agent.NavMeshAgent.isStopped = false;
            }
        }

        void InterruptForCannotNavigate()
        {
            if (_isFallInProgress)
                EndManualTraversal();

            States.CannotNavigate.CurrentArgs = CurrentArgs;
            ChangeState(States.CannotNavigate);
        }

        bool CanBeginDescent(Vector3 position) =>
            !HasGroundDirectlyBelow(position) || IsDirectlyAboveEnd(position);

        bool HasGroundDirectlyBelow(Vector3 position)
        {
            var origin = position + (Vector3.up * 0.05f);
            return Physics
                .Raycast(
                    origin, 
                    Vector3.down, 
                    GroundProbeDistance, 
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
        }

        bool IsDirectlyAboveEnd(Vector3 position)
        {
            var horizontalDistance = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(_endPosition.x, _endPosition.z));

            return horizontalDistance <= EndAlignmentDistance;
        }

    }
}

