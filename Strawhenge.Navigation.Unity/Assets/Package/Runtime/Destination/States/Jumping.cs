using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Jumping : State
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;
        readonly IDestinationSettings _settings;

        internal event Action JumpBegan;
        internal event Action JumpEnded;

        bool _isJumpInProgress;
        bool _cancelRequested;
        bool _agentUnavailable;
        DestinationArgs _pendingArgs;
        Vector3 _startPosition;
        Vector3 _endPosition;
        float _jumpT;
        float _jumpDuration;
        float _jumpHeight;

        public Jumping(IDestinationContext context, Agent agent, IDestinationSettings settings)
        {
            _context = context;
            _agent = agent;
            _settings = settings;
        }

        public DestinationArgs CurrentArgs { private get; set; }

        protected internal override bool IsStrafing => CurrentArgs.Strafe;

        protected internal override Vector3 Velocity => Vector3.zero;

        protected internal override void Cancel()
        {
            var pending = _pendingArgs;
            _pendingArgs = null;
            _cancelRequested = true;

            pending?.Callback(DestinationResult.Cancelled);
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            var pending = _pendingArgs;
            _pendingArgs = args;
            _cancelRequested = false;

            pending?.Callback(DestinationResult.CancelledByNewDestination);
        }

        // After the traversal, go through PrepareGoing rather than Going, since the agent's path is gone.
        protected internal override void OnAgentReactivated() => _agentUnavailable = true;

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

            if (!_isJumpInProgress && !_agent.NavMeshAgent.isOnOffMeshLink)
            {
                ResolveNextStateAfterJump();
                return;
            }

            if (!_isJumpInProgress)
                BeginJump();

            TraverseJump(deltaTime);
        }

        void BeginJump()
        {
            var linkData = _agent.NavMeshAgent.currentOffMeshLinkData;
            _startPosition = _agent.GetCurrentPosition();
            _endPosition = linkData.endPos + (Vector3.up * _agent.NavMeshAgent.baseOffset);

            // A strafing character keeps its facing; it couldn't turn back afterwards, as rotation is off.
            if (!CurrentArgs.Strafe)
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

        void TraverseJump(float deltaTime)
        {
            _jumpT = Mathf.Min(1f, _jumpT + (deltaTime / _jumpDuration));

            var position = Vector3.Lerp(_startPosition, _endPosition, _jumpT);
            var arc = 4f * _jumpT * (1f - _jumpT);
            position.y += _jumpHeight * arc;

            _agent.SetCurrentPosition(position);
            _agent.NavMeshAgent.nextPosition = position;

            if (_jumpT >= 1f)
            {
                if (_agent.NavMeshAgent.isOnOffMeshLink)
                    _agent.NavMeshAgent.CompleteOffMeshLink();

                ResolveNextStateAfterJump();
            }
        }

        void ResolveNextStateAfterJump()
        {
            var agentUnavailable = _agentUnavailable;
            EndManualTraversal();

            if (TryResolveQueuedRequest())
                return;

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
            _isJumpInProgress = false;
            _jumpT = 0f;
            _agentUnavailable = false;

            JumpEnded?.Invoke();

            if (_agent.NavMeshAgent.isActiveAndEnabled)
            {
                _agent.NavMeshAgent.updatePosition = true;
                _agent.NavMeshAgent.updateRotation = !CurrentArgs.Strafe;
                _agent.NavMeshAgent.isStopped = false;
            }
        }

        void InterruptForCannotNavigate()
        {
            if (_isJumpInProgress)
                EndManualTraversal();

            if (TryResolveQueuedRequest())
                return;

            States.CannotNavigate.CurrentArgs = CurrentArgs;
            ChangeState(States.CannotNavigate);
        }

        // Applies a Cancel or GoTo queued during the traversal, changing state before any callback
        // so a callback that calls GoTo or Cancel reaches the new state.
        // Returns false if nothing was queued, so the caller picks the next state.
        bool TryResolveQueuedRequest()
        {
            var current = CurrentArgs;

            if (_cancelRequested)
            {
                _cancelRequested = false;
                ChangeState(States.Idle);

                current.Callback(DestinationResult.Cancelled);
                return true;
            }

            if (_pendingArgs != null)
            {
                States.PrepareGoing.CurrentArgs = _pendingArgs;
                _pendingArgs = null;
                ChangeState(States.PrepareGoing);

                current.Callback(DestinationResult.CancelledByNewDestination);
                return true;
            }

            return false;
        }
    }
}