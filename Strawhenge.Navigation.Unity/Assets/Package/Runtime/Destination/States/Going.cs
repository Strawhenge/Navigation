using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Going : State
    {
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

        protected internal override void Cancel()
        {
            if (!IsAgentUnavailable())
                _agent.NavMeshAgent.isStopped = true;

            ChangeState(States.Idle);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            CurrentArgs.Callback(
                DestinationResult.CancelledByNewDestination);

            CurrentArgs = args;
            _agent.NavMeshAgent.destination = args.Location;
            _agent.NavMeshAgent.speed = args.Speed;
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

        bool IsAtDestination() =>
            !_agent.NavMeshAgent.pathPending &&
            _agent.NavMeshAgent.remainingDistance <=
            (CurrentArgs.TargetDistance ?? _agent.NavMeshAgent.stoppingDistance);

        bool IsAgentUnavailable() =>
            _agent.NavMeshAgent.isActiveAndEnabled == false ||
            _agent.NavMeshAgent.isOnNavMesh == false;

        bool IsPathAccessible() =>
            _agent.NavMeshAgent.path.status == NavMeshPathStatus.PathComplete ||
            (!CurrentArgs.LocationMustBeExact && _agent.NavMeshAgent.path.status == NavMeshPathStatus.PathPartial);
    }
}