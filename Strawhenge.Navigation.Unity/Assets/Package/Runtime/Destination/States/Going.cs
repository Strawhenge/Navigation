using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Going : State
    {
        const float MinArrivalDistance = 0.1f;

        readonly IDestinationContext _context;
        readonly Agent _agent;

        public Going(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        protected internal override Vector3 Velocity =>
            _agent.NavMeshAgent.isActiveAndEnabled && _agent.NavMeshAgent.isOnNavMesh
                ? _agent.NavMeshAgent.velocity
                : Vector3.zero;

        public DestinationArgs CurrentArgs { private get; set; }

        protected internal override bool IsStrafing => CurrentArgs.Strafe;

        protected internal override void Cancel()
        {
            if (!IsAgentUnavailable())
                _agent.NavMeshAgent.isStopped = true;

            ChangeState(States.Idle);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            var previous = CurrentArgs;

            // Starting from another point needs a warp and a fresh path: start over in PrepareGoing.
            // Other redirects are applied in place, so the agent keeps moving.
            if (args.StartingLocation.HasValue)
            {
                States.PrepareGoing.CurrentArgs = args;
                ChangeState(States.PrepareGoing);

                previous.Callback(DestinationResult.CancelledByNewDestination);
                return;
            }

            CurrentArgs = args;

            previous.Callback(DestinationResult.CancelledByNewDestination);

            // The callback may already have started a newer request, which steered the agent itself,
            // or cancelled. Steer the agent only if this request is still current: setting the
            // destination twice in one call chain left the agent heading for the first of the two.
            if (!IsCurrent || !ReferenceEquals(CurrentArgs, args))
                return;

            // Paused (DestinationScript disabled): the agent is disabled and can't be steered.
            // OnAgentReactivated starts this request through PrepareGoing when it's enabled again.
            if (IsAgentUnavailable())
                return;

            // Re-reserves avoidance priority and applies the mode's area mask. Before the destination,
            // so the new path is calculated with the new mask. Compared with the mode actually applied,
            // since in a chain of redirects the previous request may never have been applied.
            if (args.Leisurely != _agent.IsLeisurely)
                _agent.Enable(args.Leisurely);

            _agent.NavMeshAgent.updateRotation = !args.Strafe;
            _agent.NavMeshAgent.speed = args.Speed;
            _agent.NavMeshAgent.destination = args.Location;
        }

        protected internal override void OnAgentReactivated()
        {
            // Same request, so no callback: PrepareGoing re-enables the agent, warps it to where it is,
            // reserves its slot and calculates the path again.
            States.PrepareGoing.CurrentArgs = CurrentArgs;
            ChangeState(States.PrepareGoing);
        }

        protected internal override void Update(float deltaTime)
        {
            if (!_context.CanNavigate || IsAgentUnavailable())
            {
                States.CannotNavigate.CurrentArgs = CurrentArgs;
                ChangeState(States.CannotNavigate);
                return;
            }

            if (!IsPathAccessible())
            {
                ChangeState(States.Idle);

                CurrentArgs.Callback(
                    DestinationResult.Inaccessible);

                return;
            }
            
            if (_agent.NavMeshAgent.isOnOffMeshLink)
            {
                switch (_agent.NavMeshAgent.currentOffMeshLinkData.linkType)
                {
                    case OffMeshLinkType.LinkTypeJumpAcross:
                        States.Jumping.CurrentArgs = CurrentArgs;
                        ChangeState(States.Jumping);
                        return;
                    case OffMeshLinkType.LinkTypeDropDown:
                        States.Falling.CurrentArgs = CurrentArgs;
                        ChangeState(States.Falling);
                        return;
                    default:
                        _agent.NavMeshAgent.CompleteOffMeshLink();
                        break;
                }
            }

            if (IsAtDestination())
            {
                if (CurrentArgs.LocationMustBeExact)
                    _agent.NavMeshAgent.Warp(CurrentArgs.Location);

                ChangeState(States.Idle);

                CurrentArgs.Callback(
                    DestinationResult.Arrived);
            }
        }

        // With a stopping distance of 0, remainingDistance can stay just above 0, so arrival allows at
        // least MinArrivalDistance. Applied here rather than to the agent, so its settings are left alone.
        bool IsAtDestination() =>
            !_agent.NavMeshAgent.pathPending &&
            _agent.NavMeshAgent.remainingDistance <=
            (CurrentArgs.TargetDistance ?? Mathf.Max(_agent.NavMeshAgent.stoppingDistance, MinArrivalDistance));

        bool IsAgentUnavailable() =>
            _agent.NavMeshAgent.isActiveAndEnabled == false ||
            _agent.NavMeshAgent.isOnNavMesh == false;

        // pathStatus, not path.status: the path getter allocates a new NavMeshPath on every read.
        bool IsPathAccessible() =>
            _agent.NavMeshAgent.pathStatus == NavMeshPathStatus.PathComplete ||
            (!CurrentArgs.LocationMustBeExact && _agent.NavMeshAgent.pathStatus == NavMeshPathStatus.PathPartial);
    }
}