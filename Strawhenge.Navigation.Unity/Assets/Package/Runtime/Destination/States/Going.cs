using System;
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

        public Idle IdleState { private get; set; }

        public CannotNavigate CannotNavigateState { private get; set; }

        public DestinationArgs CurrentArgs { private get; set; }

        protected internal override void Cancel()
        {
            if (!IsAgentUnavailable())
                _agent.NavMeshAgent.isStopped = true;

            ChangeState(IdleState);

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

        protected internal override void Update()
        {
            if (!_context.CanNavigate || IsAgentUnavailable())
            {
                CannotNavigateState.CurrentArgs = CurrentArgs;
                ChangeState(CannotNavigateState);
                return;
            }

            if (!IsPathAccessible())
            {
                ChangeState(IdleState);

                CurrentArgs.Callback(
                    DestinationResult.Inaccessible);

                return;
            }

            if (IsAtDestination())
            {
                if (CurrentArgs.LocationMustBeExact)
                    _agent.NavMeshAgent.Warp(CurrentArgs.Location);

                ChangeState(IdleState);

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