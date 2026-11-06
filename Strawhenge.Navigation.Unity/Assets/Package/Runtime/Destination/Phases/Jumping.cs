using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Moves the character along a jump-across off-mesh link by hand. Can't be interrupted: a new request
    // or a cancel is called back straight away, but the jump always lands first.
    class Jumping : Phase
    {
        readonly Agent _agent;
        readonly IDestinationSettings _settings;

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

        public Jumping(Agent agent, IDestinationSettings settings)
        {
            _agent = agent;
            _settings = settings;
        }

        public override bool CanInterrupt => false;

        public override Vector3 Velocity => Vector3.zero;

        // Keeps the facing of the request it started with, even if the request changes mid-jump.
        public override bool IsStrafing(DestinationArgs current) => _strafe;

        public override void Enter(DestinationArgs args)
        {
            _strafe = args.Strafe;
            _isJumpInProgress = false;
            _agentUnavailable = false;
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
            _jumpT = Mathf.Min(1f, _jumpT + (deltaTime / _jumpDuration));

            var position = Vector3.Lerp(_startPosition, _endPosition, _jumpT);
            var arc = 4f * _jumpT * (1f - _jumpT);
            position.y += _jumpHeight * arc;

            _agent.SetCurrentPosition(position);
            _agent.NavMeshAgent.nextPosition = position;

            if (_jumpT < 1f)
                return PhaseStep.Continue;

            if (_agent.NavMeshAgent.isOnOffMeshLink)
                _agent.NavMeshAgent.CompleteOffMeshLink();

            return Land();
        }

        // The agent's path is gone if it was unavailable during the jump, so set it up again.
        PhaseStep Land() =>
            PhaseStep.To(_agentUnavailable ? PhaseId.Preparing : PhaseId.Walking);

        void EndJump()
        {
            _isJumpInProgress = false;
            _jumpT = 0f;

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