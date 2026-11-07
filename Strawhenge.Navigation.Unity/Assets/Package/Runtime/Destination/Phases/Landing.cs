using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Plays a landing animation after a jump or drop: the agent stands still until the Animator completes
    // it, or AnimatorWaitTimeout passes. Can't be interrupted: a new request or a cancel is called back
    // straight away, but the landing always finishes first.
    class Landing : Phase
    {
        readonly Agent _agent;
        readonly IDestinationSettings _settings;
        readonly HashSet<int> _timedOutIds = new();

        internal event Action<int> LandingRequested;

        bool _strafe;
        int _landingId;
        PhaseId _next;
        bool _agentUnavailable;
        float _waitTime;

        public Landing(Agent agent, IDestinationSettings settings)
        {
            _agent = agent;
            _settings = settings;
        }

        public override bool CanInterrupt => false;

        // Keeps the facing of the jump or drop it follows.
        public override bool IsStrafing(DestinationArgs current) => _strafe;

        // Called by Jumping or Falling as they land. True if a landing matches, and they should hand over
        // to this phase; next is where to go once it's done.
        internal bool TrySetUp(ILandingProfile[] landings, float speed, float fallDistance, PhaseId next)
        {
            foreach (var landing in landings)
            {
                if (landing.Matches(speed, fallDistance) && landing.Id is int landingId)
                {
                    _landingId = landingId;
                    _next = next;
                    return true;
                }
            }

            return false;
        }

        // args isn't used: it's null if the request was cancelled mid-jump or drop.
        public override void Enter(DestinationArgs args, bool strafe)
        {
            _strafe = strafe;
            _waitTime = 0f;
            _agentUnavailable = IsAgentUnavailable();

            if (!_agentUnavailable)
            {
                _agent.NavMeshAgent.isStopped = true;
                _agent.NavMeshAgent.velocity = Vector3.zero;
            }

            // Last: without a landing animation, the Animator completes it straight away, which leaves
            // this phase before Enter returns.
            LandingRequested?.Invoke(_landingId);
        }

        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            _agentUnavailable |= IsAgentUnavailable();

            _waitTime += deltaTime;

            if (_waitTime <= _settings.AnimatorWaitTimeout)
                return PhaseStep.Continue;

            if (_timedOutIds.Add(_landingId))
            {
                Debug.LogWarning(
                    $"Landing ID {_landingId} wasn't completed within {_settings.AnimatorWaitTimeout} s - " +
                    $"carrying on without it. Check a {nameof(MovementAnimationSyncScript)} is set up and the " +
                    "Animator has a transition for it.",
                    _agent.NavMeshAgent);
            }

            return Done();
        }

        public override PhaseStep OnLandingCompleted() => Done();

        public override void Exit()
        {
            if (!IsAgentUnavailable())
                _agent.NavMeshAgent.isStopped = false;
        }

        // The agent's path is gone if it was unavailable during the landing, so set it up again.
        PhaseStep Done() =>
            PhaseStep.To(_agentUnavailable ? PhaseId.Preparing : _next);

        bool IsAgentUnavailable() =>
            !_agent.NavMeshAgent.isActiveAndEnabled ||
            !_agent.NavMeshAgent.isOnNavMesh;
    }
}
