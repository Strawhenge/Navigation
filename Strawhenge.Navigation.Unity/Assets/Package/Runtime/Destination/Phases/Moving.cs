using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    // The agent follows its path at the request's speed, whether that's a walk, jog or run, handing
    // over to Jumping or Falling at off-mesh links, and to Pivoting when a moving pivot matches a turn.
    class Moving : Phase
    {
        const float MinArrivalDistance = 0.1f;

        readonly Agent _agent;
        readonly TargetResolver _targets;
        readonly Pivoting _pivoting;

        public Moving(Agent agent, TargetResolver targets, Pivoting pivoting)
        {
            _agent = agent;
            _targets = targets;
            _pivoting = pivoting;
        }

        public override Vector3 Velocity =>
            IsAgentUnavailable()
                ? Vector3.zero
                : _agent.NavMeshAgent.velocity;

        // Starting from another point needs a warp and a fresh path, so that goes back through
        // Preparing. Other redirects are applied in place, so the agent keeps moving.
        public override bool CanRedirectInPlace(DestinationArgs args) => !args.StartingLocation.HasValue;

        public override void Enter(DestinationArgs args, bool strafe)
        {
            if (!IsAgentUnavailable())
                _agent.NavMeshAgent.isStopped = false;
        }

        public override void Exit()
        {
            if (!IsAgentUnavailable())
                _agent.NavMeshAgent.isStopped = true;
        }

        public override PhaseStep Redirect(DestinationArgs args)
        {
            // Paused (DestinationScript disabled): the agent is disabled and can't be steered.
            // OnAgentReactivated starts this request through Preparing when it's enabled again.
            if (IsAgentUnavailable())
                return PhaseStep.Continue;

            if (!_targets.TryResolve(args, out var target))
                return PhaseStep.Finish(DestinationResult.Inaccessible);

            // Re-reserves avoidance priority and applies the mode's area mask. Before the destination,
            // so the new path is calculated with the new mask. Compared with the mode actually applied,
            // since in a chain of redirects the previous request may never have been applied.
            if (args.Leisurely != _agent.IsLeisurely)
                _agent.Enable(args.Leisurely);

            _agent.NavMeshAgent.updateRotation = !args.Strafe;
            _agent.NavMeshAgent.speed = args.Speed;

            // False if no path can be requested, e.g. for an exact destination off the NavMesh. Unity then
            // keeps the old path, which would take the agent to the previous destination.
            return _agent.NavMeshAgent.SetDestination(target)
                ? PhaseStep.Continue
                : PhaseStep.Finish(DestinationResult.Inaccessible);
        }

        // Same request: Preparing re-enables the agent, warps it to where it is, reserves its slot and
        // calculates the path again.
        public override PhaseStep OnAgentReactivated() => PhaseStep.To(PhaseId.Preparing);

        public override PhaseStep Update(DestinationArgs args, float deltaTime)
        {
            if (IsAgentUnavailable())
                return PhaseStep.To(PhaseId.Paused);

            if (!IsPathAccessible(args))
                return PhaseStep.Finish(DestinationResult.Inaccessible);

            if (_agent.NavMeshAgent.isOnOffMeshLink)
            {
                switch (_agent.NavMeshAgent.currentOffMeshLinkData.linkType)
                {
                    case OffMeshLinkType.LinkTypeJumpAcross:
                        return PhaseStep.To(PhaseId.Jumping);
                    case OffMeshLinkType.LinkTypeDropDown:
                        return PhaseStep.To(PhaseId.Falling);
                    default:
                        _agent.NavMeshAgent.CompleteOffMeshLink();
                        break;
                }
            }

            if (!IsAtDestination(args))
                return !args.Strafe && TryPivot()
                    ? PhaseStep.To(PhaseId.Pivoting)
                    : PhaseStep.Continue;

            if (args.LocationMustBeExact)
                _agent.Warp(args.Location);

            return PhaseStep.Finish(DestinationResult.Arrived);
        }

        // With a stopping distance of 0, remainingDistance can stay just above 0, so arrival allows at
        // least MinArrivalDistance. Applied here rather than to the agent, so its settings are left alone.
        bool IsAtDestination(DestinationArgs args) =>
            !_agent.NavMeshAgent.pathPending &&
            _agent.NavMeshAgent.remainingDistance <=
            (args.TargetDistance ?? Mathf.Max(_agent.NavMeshAgent.stoppingDistance, MinArrivalDistance));

        // The turn is from the character's facing to the way the agent wants to go (desiredVelocity), which
        // swings round at a corner while the agent's own rotation catches up.
        bool TryPivot()
        {
            var desiredVelocity = _agent.NavMeshAgent.desiredVelocity;
            desiredVelocity.y = 0f;

            if (desiredVelocity.sqrMagnitude < 0.0001f)
                return false;

            var velocity = _agent.NavMeshAgent.velocity;
            velocity.y = 0f;

            var turnAngle = Vector3.SignedAngle(
                _agent.NavMeshAgent.transform.forward,
                desiredVelocity,
                Vector3.up);

            return _pivoting.TrySetUpMoving(velocity.magnitude, turnAngle);
        }

        bool IsAgentUnavailable() =>
            _agent.NavMeshAgent.isActiveAndEnabled == false ||
            _agent.NavMeshAgent.isOnNavMesh == false;

        // pathStatus, not path.status: the path getter allocates a new NavMeshPath on every read.
        bool IsPathAccessible(DestinationArgs args) =>
            _agent.NavMeshAgent.pathStatus == NavMeshPathStatus.PathComplete ||
            (!args.LocationMustBeExact && _agent.NavMeshAgent.pathStatus == NavMeshPathStatus.PathPartial);
    }
}