using System;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class PrepareGoing : IState
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;
        readonly NavMeshPath _path = new();

        public PrepareGoing(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        public bool IsActive => true;

        public Vector3 Velocity => Vector3.zero;

        public Idle IdleState { private get; set; }

        public CannotNavigate CannotNavigateState { private get; set; }

        public Going GoingState { private get; set; }

        public DestinationArgs CurrentArgs { private get; set; }

        public void Cancel(Action<IState> changeState)
        {
            changeState(IdleState);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        public void GoTo(DestinationArgs args, Action<IState> changeState)
        {
            CurrentArgs.Callback(
                DestinationResult.CancelledByNewDestination);
            CurrentArgs = args;
        }

        public void Update(Action<IState> changeState)
        {
            if (!_context.CanNavigate)
            {
                CannotNavigateState.CurrentArgs = CurrentArgs;
                changeState(CannotNavigateState);
                return;
            }

            if (ShouldWaitForNextUpdate())
                return;

            _agent.Rigidbody.isKinematic = true;

            _agent.Enable(CurrentArgs.Leisurely);

            _agent.NavMeshAgent.Warp(
                CurrentArgs.StartingLocation ?? _agent.Rigidbody.position);

            if (!_agent.NavMeshAgent.isOnNavMesh)
            {
                _agent.Disable();

                changeState(IdleState);

                CurrentArgs.Callback(
                    DestinationResult.Inaccessible);
                return;
            }

            _agent.NavMeshAgent.isStopped = true;
            _agent.NavMeshAgent.updatePosition = true;
            _agent.NavMeshAgent.updateRotation = true;

            if (_agent.NavMeshAgent.stoppingDistance <= 0)
            {
                _agent.NavMeshAgent.stoppingDistance = 0.1f;
            }

            _agent.NavMeshAgent.CalculatePath(CurrentArgs.Location, _path);

            if (!IsAccessible(_path))
            {
                changeState(IdleState);

                CurrentArgs.Callback(
                    DestinationResult.Inaccessible);

                return;
            }

            _agent.NavMeshAgent.speed = CurrentArgs.Speed;
            _agent.NavMeshAgent.SetPath(_path);
            _agent.NavMeshAgent.isStopped = false;

            GoingState.CurrentArgs = CurrentArgs;
            changeState(GoingState);
        }

        bool ShouldWaitForNextUpdate() => _agent.NavMeshAgent.isOnOffMeshLink;

        bool IsAccessible(NavMeshPath path) =>
            path.status == NavMeshPathStatus.PathComplete ||
            (!CurrentArgs.LocationMustBeExact && path.status == NavMeshPathStatus.PathPartial);
    }
}