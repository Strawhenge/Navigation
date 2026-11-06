using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Moves the character along a jump-across off-mesh link by hand, then hands over to Landing if a jump
    // landing matches. Can't be interrupted: a new request or a cancel is called back straight away, but
    // the jump always lands first.
    class Jumping : Phase
    {
        readonly Agent _agent;
        readonly IDestinationSettings _settings;
        readonly Landing _landing;

        internal event Action JumpBegan;
        internal event Action JumpEnded;

        bool _strafe;
        bool _isJumpInProgress;
        bool _agentUnavailable;
        Vector3 _startPosition;
        Vector3 _endPosition;
        float _jumpT;
        float _jumpDuration;
        float _jumpHeight;
        Vector3 _currentVelocity;

        public Jumping(Agent agent, IDestinationSettings settings, Landing landing)
        {
            _agent = agent;
            _settings = settings;
            _landing = landing;
        }

        public override bool CanInterrupt => false;

        // Measured from how far the character moved last update, as it's moved by hand.
        public override Vector3 Velocity => _currentVelocity;

        // Keeps the facing of the request it started with, even if the request changes mid-jump.
        public override bool IsStrafing(DestinationArgs current) => _strafe;

        public override void Enter(DestinationArgs args, bool strafe)
        {
            _strafe = strafe;
            _isJumpInProgress = false;
            _agentUnavailable = false;
            _currentVelocity = Vector3.zero;
        }

        // args isn't used: it's null if the request was cancelled mid-jump.
        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            _agentUnavailable |=
                !_agent.NavMeshAgent.isActiveAndEnabled ||
                !_agent.NavMeshAgent.isOnNavMesh;

            if (!_isJumpInProgress && !_agent.NavMeshAgent.isOnOffMeshLink)
                return Land();

            if (!_isJumpInProgress)
                BeginJump();

            return TraverseJump(deltaTime);
        }

        public override void Exit()
        {
            if (_isJumpInProgress)
                EndJump();
        }

        void BeginJump()
        {
            var linkData = _agent.NavMeshAgent.currentOffMeshLinkData;
            _startPosition = _agent.GetCurrentPosition();
            _endPosition = linkData.endPos + (Vector3.up * _agent.NavMeshAgent.baseOffset);

            // A strafing character keeps its facing; it couldn't turn back afterwards, as rotation is off.
            if (!_strafe)
                FaceJumpDirection();

            var horizontalDistance = Vector2.Distance(
                new Vector2(_startPosition.x, _startPosition.z),
                new Vector2(_endPosition.x, _endPosition.z));

            _jumpDuration = Mathf.Max(0.15f, horizontalDistance / _settings.JumpSpeed);
            _jumpHeight = Mathf.Clamp(horizontalDistance * 0.25f, 0.5f, 2.5f);
            _jumpT = 0f;
            _isJumpInProgress = true;

            JumpBegan?.Invoke();

            _agent.NavMeshAgent.isStopped = true;
            _agent.NavMeshAgent.updatePosition = false;
            _agent.NavMeshAgent.updateRotation = false;
        }

        void FaceJumpDirection()
        {
            var jumpDirection = _endPosition - _startPosition;
            jumpDirection.y = 0f;

            if (jumpDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            var rotation = Quaternion.LookRotation(jumpDirection.normalized, Vector3.up);
            _agent.NavMeshAgent.transform.rotation = rotation;
        }

        PhaseStep TraverseJump(float deltaTime)
        {
            var previousPosition = _agent.GetCurrentPosition();

            _jumpT = Mathf.Min(1f, _jumpT + (deltaTime / _jumpDuration));

            var position = Vector3.Lerp(_startPosition, _endPosition, _jumpT);
            var arc = 4f * _jumpT * (1f - _jumpT);
            position.y += _jumpHeight * arc;

            _agent.SetCurrentPosition(position);
            _agent.NavMeshAgent.nextPosition = position;

            _currentVelocity = deltaTime > Mathf.Epsilon
                ? (position - previousPosition) / deltaTime
                : Vector3.zero;

            if (_jumpT < 1f)
                return PhaseStep.Continue;

            if (_agent.NavMeshAgent.isOnOffMeshLink)
                _agent.NavMeshAgent.CompleteOffMeshLink();

            return LandFromJump();
        }

        // The agent's path is gone if it was unavailable during the jump, so set it up again.
        PhaseStep Land() =>
            PhaseStep.To(_agentUnavailable ? PhaseId.Preparing : PhaseId.Moving);

        // Matched on the horizontal speed coming in, and the drop from take-off to landing (0 when jumping
        // up), as LocomotionScript measures from where the character was last grounded.
        PhaseStep LandFromJump()
        {
            var next = _agentUnavailable ? PhaseId.Preparing : PhaseId.Moving;

            var horizontalVelocity = _currentVelocity;
            horizontalVelocity.y = 0f;
            var fallDistance = Mathf.Max(0f, _startPosition.y - _endPosition.y);

            return _landing.TrySetUp(_settings.JumpLandings, horizontalVelocity.magnitude, fallDistance, next)
                ? PhaseStep.To(PhaseId.Landing)
                : PhaseStep.To(next);
        }

        void EndJump()
        {
            _isJumpInProgress = false;
            _jumpT = 0f;
            _currentVelocity = Vector3.zero;

            JumpEnded?.Invoke();

            if (_agent.NavMeshAgent.isActiveAndEnabled)
            {
                _agent.NavMeshAgent.updatePosition = true;
                _agent.NavMeshAgent.updateRotation = !_strafe;
                _agent.NavMeshAgent.isStopped = false;
            }
        }
    }
}