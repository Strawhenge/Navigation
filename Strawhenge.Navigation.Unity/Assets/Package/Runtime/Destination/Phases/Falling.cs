using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Moves the character along a drop-down off-mesh link by hand, then hands over to Landing if it fell
    // and a fall landing matches. Can't be interrupted: a new request or a cancel is called back straight
    // away, but the drop always lands first.
    class Falling : Phase
    {
        const float GroundProbeDistance = 0.2f;
        const float EndAlignmentDistance = 0.05f;
        const float DownwardMovementEpsilon = 0.0001f;

        readonly Agent _agent;
        readonly IDestinationSettings _settings;
        readonly Landing _landing;

        internal event Action FallBegan;
        internal event Action FallEnded;

        bool _strafe;
        bool _isFallInProgress;
        bool _agentUnavailable;
        Vector3 _startPosition;
        Vector3 _endPosition;
        float _horizontalT;
        float _horizontalDuration;
        float _verticalSpeed;
        bool _isDescending;
        Vector3 _currentVelocity;
        bool _fallBeganRaised;

        public Falling(Agent agent, IDestinationSettings settings, Landing landing)
        {
            _agent = agent;
            _settings = settings;
            _landing = landing;
        }

        public override bool CanInterrupt => false;

        public override Vector3 Velocity => _currentVelocity;

        // Keeps the facing of the request it started with, even if the request changes mid-drop.
        public override bool IsStrafing(DestinationArgs current) => _strafe;

        public override void Enter(DestinationArgs args, bool strafe)
        {
            _strafe = strafe;
            _isFallInProgress = false;
            _agentUnavailable = false;
            _currentVelocity = Vector3.zero;
        }

        // args isn't used: it's null if the request was cancelled mid-drop.
        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            _agentUnavailable |=
                !_agent.NavMeshAgent.isActiveAndEnabled ||
                !_agent.NavMeshAgent.isOnNavMesh;

            if (!_isFallInProgress && !_agent.NavMeshAgent.isOnOffMeshLink)
                return Land();

            if (!_isFallInProgress)
                BeginFall();

            return TraverseFall(deltaTime);
        }

        public override void Exit()
        {
            if (_isFallInProgress)
                EndFall();
        }

        void BeginFall()
        {
            var linkData = _agent.NavMeshAgent.currentOffMeshLinkData;
            _startPosition = _agent.GetCurrentPosition();
            _endPosition = linkData.endPos + (Vector3.up * _agent.NavMeshAgent.baseOffset);

            var horizontalDistance = Vector2.Distance(
                new Vector2(_startPosition.x, _startPosition.z),
                new Vector2(_endPosition.x, _endPosition.z));
            _horizontalDuration = Mathf.Max(0.01f, horizontalDistance / _settings.FallHorizontalSpeed);
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

        PhaseStep TraverseFall(float deltaTime)
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
                _verticalSpeed += _settings.FallAcceleration * deltaTime;
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
            if (!reachedEndHorizontally || !reachedEndVertically)
                return PhaseStep.Continue;

            if (_agent.NavMeshAgent.isOnOffMeshLink)
                _agent.NavMeshAgent.CompleteOffMeshLink();

            return LandFromFall();
        }

        // The agent's path is gone if it was unavailable during the drop, so set it up again.
        PhaseStep Land() =>
            PhaseStep.To(_agentUnavailable ? PhaseId.Preparing : PhaseId.Moving);

        // Only after actually falling (FallBegan raised), matched on the horizontal speed coming in and the
        // height dropped.
        PhaseStep LandFromFall()
        {
            var next = _agentUnavailable ? PhaseId.Preparing : PhaseId.Moving;

            if (!_fallBeganRaised)
                return PhaseStep.To(next);

            var horizontalVelocity = _currentVelocity;
            horizontalVelocity.y = 0f;
            var fallDistance = Mathf.Max(0f, _startPosition.y - _endPosition.y);

            return _landing.TrySetUp(_settings.FallLandings, horizontalVelocity.magnitude, fallDistance, next)
                ? PhaseStep.To(PhaseId.Landing)
                : PhaseStep.To(next);
        }

        void EndFall()
        {
            _isFallInProgress = false;
            _horizontalT = 0f;
            _verticalSpeed = 0f;
            _isDescending = false;
            _currentVelocity = Vector3.zero;
            _fallBeganRaised = false;

            FallEnded?.Invoke();

            if (_agent.NavMeshAgent.isActiveAndEnabled)
            {
                _agent.NavMeshAgent.updatePosition = true;
                _agent.NavMeshAgent.updateRotation = !_strafe;
                _agent.NavMeshAgent.isStopped = false;
            }
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
                    _settings.GroundLayers,
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