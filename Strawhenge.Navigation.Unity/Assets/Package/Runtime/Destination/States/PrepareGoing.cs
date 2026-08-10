using System;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class PrepareGoing : State
    {
        readonly IDestinationContext _context;
        readonly Agent _agent;
        readonly NavMeshPath _path = new();

        public PrepareGoing(IDestinationContext context, Agent agent)
        {
            _context = context;
            _agent = agent;
        }

        protected internal override Vector3 Velocity => Vector3.zero;

        public DestinationArgs CurrentArgs { private get; set; }

        protected internal override void Cancel()

        {
            ChangeState(States.Idle);

            CurrentArgs.Callback(DestinationResult.Cancelled);
        }

        protected internal override void GoTo(DestinationArgs args)
        {
            CurrentArgs.Callback(
                DestinationResult.CancelledByNewDestination);
            CurrentArgs = args;
        }

        protected internal override void Update(float deltaTime)
        {
            if (!_context.CanNavigate)
            {
                States.CannotNavigate.CurrentArgs = CurrentArgs;
                ChangeState(States.CannotNavigate);
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

                ChangeState(States.Idle);

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
                ChangeState(States.Idle);

                CurrentArgs.Callback(
                    DestinationResult.Inaccessible);

                return;
            }

            _agent.NavMeshAgent.speed = CurrentArgs.Speed;
            _agent.NavMeshAgent.SetPath(_path);
            _agent.NavMeshAgent.isStopped = false;

            States.Going.CurrentArgs = CurrentArgs;
            ChangeState(States.Going);
        }

        bool ShouldWaitForNextUpdate() => _agent.NavMeshAgent.isOnOffMeshLink;

        bool IsAccessible(NavMeshPath path) =>
            path.status == NavMeshPathStatus.PathComplete ||
            (!CurrentArgs.LocationMustBeExact && path.status == NavMeshPathStatus.PathPartial);
    }
}