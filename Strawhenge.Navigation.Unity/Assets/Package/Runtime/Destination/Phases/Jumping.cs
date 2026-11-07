using System;
using UnityEngine;
using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Moves the character along a jump-across off-mesh link by hand, then hands over to Landing if a jump
    // landing matches. With DeferJumpTrigger, it first waits at the start of the link for the jump
    // animation to launch it (TriggerJump), or for AnimatorWaitTimeout, after which it launches anyway:
    // unlike LocomotionScript, it can't cancel the jump, as the agent is already on the link. Can't be
    // interrupted: a new request or a cancel is called back straight away, but the jump always lands first.
    class Jumping : Phase
    {
        readonly Agent _agent;
        readonly IDestinationSettings _settings;
        readonly Landing _landing;
        readonly ILogger _logger;

        internal event Action JumpTriggerRequested;
        internal event Action JumpBegan;
        internal event Action JumpEnded;

        bool _strafe;

        // The request's speed going into the link, which landings are matched on.
        float _approachSpeed;
        bool _isJumpInProgress;
        bool _isAwaitingTrigger;
        float _triggerWaitTime;
        bool _triggerTimeoutWarned;
        bool _agentUnavailable;
        Vector3 _startPosition;
        Vector3 _endPosition;
        float _jumpT;
        float _jumpDuration;
        float _jumpHeight;
        Vector3 _currentVelocity;

        public Jumping(Agent agent, IDestinationSettings settings, Landing landing, ILogger logger)
        {
            _agent = agent;
            _settings = settings;
            _landing = landing;
            _logger = logger;
        }

        public override bool CanInterrupt => false;

        // Measured from how far the character moved last update, as it's moved by hand.
        public override Vector3 Velocity => _currentVelocity;

        // Keeps the facing of the request it started with, even if the request changes mid-jump.
        public override bool IsStrafing(DestinationArgs current) => _strafe;

        public override void Enter(DestinationArgs args, bool strafe)
        {
            _strafe = strafe;
            _approachSpeed = args?.Speed ?? 0f;
            _isJumpInProgress = false;
            _isAwaitingTrigger = false;
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

            if (_isAwaitingTrigger)
                return AwaitTrigger(deltaTime);

            return TraverseJump(deltaTime);
        }

        public override PhaseStep OnJumpTriggered()
        {
            if (_isAwaitingTrigger)
                Launch();

            return PhaseStep.Continue;
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

            _agent.NavMeshAgent.isStopped = true;
            _agent.NavMeshAgent.updatePosition = false;
            _agent.NavMeshAgent.updateRotation = false;

            if (!_settings.DeferJumpTrigger)
            {
                Launch();
                return;
            }

            _isAwaitingTrigger = true;
            _triggerWaitTime = 0f;

            // Last: without an Animator, the jump is triggered straight away (OnJumpTriggered).
            JumpTriggerRequested?.Invoke();
        }

        PhaseStep AwaitTrigger(float deltaTime)
        {
            _currentVelocity = Vector3.zero;
            _triggerWaitTime += deltaTime;

            if (_triggerWaitTime <= _settings.AnimatorWaitTimeout)
                return PhaseStep.Continue;

            if (!_triggerTimeoutWarned)
            {
                _triggerTimeoutWarned = true;
                _logger.LogWarning(
                    $"Deferred jump wasn't launched within {_settings.AnimatorWaitTimeout} s - launching it anyway. " +
                    $"Check the jump animation calls {nameof(MovementAnimationSyncScript)}.OnJumpLaunch.");
            }

            Launch();
            return PhaseStep.Continue;
        }

        void Launch()
        {
            _isAwaitingTrigger = false;
            JumpBegan?.Invoke();
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

        // Matched on the request's speed going into the link (the jump itself moves at JumpSpeed, so a walk
        // into a jump gets a walking landing, as in locomotion), and the drop from take-off to landing (0
        // when jumping up), as LocomotionScript measures from where the character was last grounded.
        PhaseStep LandFromJump()
        {
            var next = _agentUnavailable ? PhaseId.Preparing : PhaseId.Moving;
            var fallDistance = Mathf.Max(0f, _startPosition.y - _endPosition.y);

            return _landing.TrySetUp(_settings.JumpLandings, _approachSpeed, fallDistance, next)
                ? PhaseStep.To(PhaseId.Landing)
                : PhaseStep.To(next);
        }

        void EndJump()
        {
            _isJumpInProgress = false;
            _jumpT = 0f;
            _currentVelocity = Vector3.zero;

            // Only if it launched, so JumpBegan and JumpEnded always come in pairs.
            if (_isAwaitingTrigger)
                _isAwaitingTrigger = false;
            else
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