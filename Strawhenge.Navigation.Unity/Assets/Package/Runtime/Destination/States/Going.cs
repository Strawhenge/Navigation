using System;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Going : IState
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;

        public Going(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        public bool IsActive => true;

        public Vector3 Velocity => _agent.NavMeshAgent.isActiveAndEnabled && _agent.NavMeshAgent.isOnNavMesh
            ? _agent.NavMeshAgent.velocity
            : Vector3.zero;

        public Idle IdleState { private get; set; }

        public CannotNavigate CannotNavigateState { private get; set; }

        public DestinationArgs CurrentArgs { private get; set; }

        public void Cancel(Action<IState> changeState)
        {
            if (!IsAgentUnavailable())
                _agent.NavMeshAgent.isStopped = true;

            changeState(IdleState);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        public void GoTo(DestinationArgs args, Action<IState> changeState)
        {
            CurrentArgs.Callback(
                DestinationResult.CancelledByNewDestination);

            CurrentArgs = args;
            _agent.NavMeshAgent.destination = args.Location;
            _agent.NavMeshAgent.speed = args.Speed;
        }

        public void Update(Action<IState> changeState)
        {
            if (!_context.CanNavigate || IsAgentUnavailable())
            {
                CannotNavigateState.CurrentArgs = CurrentArgs;
                changeState(CannotNavigateState);
                return;
            }

            if (!IsPathAccessible())
            {
                changeState(IdleState);

                CurrentArgs.Callback(
                    DestinationResult.Inaccessible);

                return;
            }

            if (IsAtDestination())
            {
                if (CurrentArgs.LocationMustBeExact)
                    _agent.NavMeshAgent.Warp(CurrentArgs.Location);

                changeState(IdleState);

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