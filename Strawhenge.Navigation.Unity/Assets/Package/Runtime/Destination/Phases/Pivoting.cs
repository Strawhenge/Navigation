using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Plays a pivot animation in place of the agent turning: before a trip sets off (a stationary pivot,
    // matched by Preparing) or on a sharp turn along the path (a moving pivot, matched by Moving). The
    // agent stands still until the Animator completes the pivot, or AnimatorWaitTimeout passes, then
    // carries on along its path. Can't be interrupted: a new request or a cancel is called back straight
    // away, but the pivot always finishes first.
    class Pivoting : Phase
    {
        readonly Agent _agent;
        readonly IDestinationSettings _settings;
        readonly HashSet<int> _timedOutIds = new();

        internal event Action<int> PivotRequested;

        bool _strafe;
        int _pivotId;
        bool _agentUnavailable;
        float _waitTime;
        Vector3 _velocity;

        // A pivot that didn't turn the character (no animation played it, or a clip without root rotation)
        // would match again straight away and stop the character ever turning. The agent turns normally
        // until out of every pivot's angle range, as LocomotionScript does.
        bool _suppressUntilTurned;

        public Pivoting(Agent agent, IDestinationSettings settings)
        {
            _agent = agent;
            _settings = settings;
        }

        public override bool CanInterrupt => false;

        // The velocity coming in, as LocomotionScript reports while a pivot plays: its CharacterController
        // keeps the velocity of its last move.
        public override Vector3 Velocity => _velocity;

        public override bool IsStrafing(DestinationArgs current) => _strafe;

        // Called by Preparing and Moving: true if a pivot matches, and they should hand over to this phase.
        // turnAngle is the signed angle from the character's facing to the way the path goes.
        internal bool TrySetUpStationary(float speed, float turnAngle) =>
            TrySetUp(_settings.StationaryPivots, speed, turnAngle);

        internal bool TrySetUpMoving(float speed, float turnAngle) =>
            TrySetUp(_settings.MovingPivots, speed, turnAngle);

        bool TrySetUp(IPivotProfile[] pivots, float speed, float turnAngle)
        {
            if (_suppressUntilTurned)
            {
                if (IsInAnyPivotAngleRange(turnAngle))
                    return false;

                _suppressUntilTurned = false;
            }

            foreach (var pivot in pivots)
            {
                if (pivot.Matches(speed, turnAngle) && pivot.Id is int pivotId)
                {
                    _pivotId = pivotId;
                    return true;
                }
            }

            return false;
        }

        bool IsInAnyPivotAngleRange(float turnAngle) =>
            IsInAngleRange(_settings.StationaryPivots, turnAngle) ||
            IsInAngleRange(_settings.MovingPivots, turnAngle);

        static bool IsInAngleRange(IPivotProfile[] pivots, float turnAngle)
        {
            foreach (var pivot in pivots)
            {
                if (pivot.IsInAngleRange(turnAngle))
                    return true;
            }

            return false;
        }

        // args isn't used: it's null if the request was cancelled meanwhile.
        public override void Enter(DestinationArgs args, bool strafe)
        {
            _strafe = strafe;
            _waitTime = 0f;
            _agentUnavailable = IsAgentUnavailable();
            _velocity = Vector3.zero;

            if (!_agentUnavailable)
            {
                _velocity = _agent.NavMeshAgent.velocity;

                _agent.NavMeshAgent.isStopped = true;
                _agent.NavMeshAgent.velocity = Vector3.zero;

                // MovementAnimationSyncScript turns on root motion for the pivot, so the agent mustn't
                // move or turn the character meanwhile. Exit syncs the agent with where it ends up.
                _agent.NavMeshAgent.updatePosition = false;
                _agent.NavMeshAgent.updateRotation = false;
            }

            // Last: without a pivot animation, the Animator completes it straight away, which leaves this
            // phase before Enter returns.
            PivotRequested?.Invoke(_pivotId);
        }

        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            _agentUnavailable |= IsAgentUnavailable();

            _waitTime += deltaTime;

            if (_waitTime <= _settings.AnimatorWaitTimeout)
                return PhaseStep.Continue;

            if (_timedOutIds.Add(_pivotId))
            {
                Debug.LogWarning(
                    $"Pivot ID {_pivotId} wasn't completed within {_settings.AnimatorWaitTimeout} s - " +
                    $"carrying on without it. Check a {nameof(MovementAnimationSyncScript)} is set up and the " +
                    "Animator has a transition for it.",
                    _agent.NavMeshAgent);
            }

            return OnPivotCompleted(Quaternion.identity);
        }

        public override PhaseStep OnPivotCompleted(Quaternion rotationDelta)
        {
            var transform = _agent.NavMeshAgent.transform;
            transform.rotation = rotationDelta * transform.rotation;

            if (rotationDelta == Quaternion.identity)
                _suppressUntilTurned = true;

            // The agent's path is gone if it was unavailable during the pivot, so set it up again.
            return PhaseStep.To(_agentUnavailable ? PhaseId.Preparing : PhaseId.Moving);
        }

        public override void Exit()
        {
            _velocity = Vector3.zero;

            if (IsAgentUnavailable())
                return;

            // Root motion may have moved the character: carry on from there, kept on the NavMesh.
            _agent.NavMeshAgent.nextPosition = _agent.GetCurrentPosition();
            _agent.NavMeshAgent.updatePosition = true;
            _agent.NavMeshAgent.updateRotation = !_strafe;
            _agent.NavMeshAgent.isStopped = false;
        }

        bool IsAgentUnavailable() =>
            !_agent.NavMeshAgent.isActiveAndEnabled ||
            !_agent.NavMeshAgent.isOnNavMesh;
    }
}
